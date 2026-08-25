using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AIStartupTycoon.Data;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.Core
{
    /// <summary>
    /// Central game loop controller: owns engineer counts, ticks passive income each frame,
    /// applies model tier / compute upgrade unlocks, and handles save/load + offline earnings.
    /// Depends on CurrencyManager for all currency state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Data References (assign in Inspector)")]
        public List<EngineerData> allEngineers;
        public List<ModelTierData> allModelTiers;
        public List<ComputeUpgradeData> allComputeUpgrades;
        public List<ReputationUpgradeData> allReputationUpgrades;

        [Header("Offline Earnings Config")]
        [Range(0f, 1f)] public float offlineEarningsRate = 0.5f;
        public float maxOfflineHours = 8f;

        // Runtime ownership state
        private Dictionary<EngineerData, int> _ownedEngineers = new Dictionary<EngineerData, int>();
        private HashSet<ModelTierData> _unlockedTiers = new HashSet<ModelTierData>();
        private HashSet<ComputeUpgradeData> _purchasedUpgrades = new HashSet<ComputeUpgradeData>();

        // Permanent - intentionally NOT cleared by ExecuteIPO. This is the actual
        // payoff for prestiging, so it must survive every future reset.
        private HashSet<ReputationUpgradeData> _purchasedReputationUpgrades = new HashSet<ReputationUpgradeData>();

        public event Action<EngineerData, int> OnEngineerCountChanged;
        public event Action<ModelTierData> OnModelTierUnlocked;
        public event Action<ComputeUpgradeData> OnComputeUpgradePurchased;
        public event Action<ReputationUpgradeData> OnReputationUpgradePurchased;
        public event Action<BigNumber, double> OnOfflineEarningsApplied; // (earned, secondsAway) - UI can subscribe to show a "welcome back" popup

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foreach (var e in allEngineers) _ownedEngineers[e] = 0;
        }

        private bool _hasLoaded;

        private void Start()
        {
            LoadGame();
            _hasLoaded = true; // guards OnApplicationPause/Quit below - see their comments
            // Deferred one frame: this fires OnOfflineEarningsApplied for the "welcome
            // back" popup, but Unity doesn't guarantee this Start() runs after other
            // scripts' Start() - firing immediately risked the popup's own Start()
            // subscribing too late and missing the event entirely.
            StartCoroutine(ApplyOfflineEarningsNextFrame());
        }

        private System.Collections.IEnumerator ApplyOfflineEarningsNextFrame()
        {
            yield return null;
            ApplyOfflineEarnings();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            BigNumber totalPassivePerSecond = GetTotalPassiveOutput();
            CurrencyManager.Instance.EarnFromPassive(totalPassivePerSecond, dt);

            CheckModelTierUnlocks();
        }

        /// <summary>Total $/sec from all owned engineers, before rate multipliers
        /// (CurrencyManager applies PassiveOutputMultiplier/GlobalEarningsMultiplier/
        /// ReputationMultiplier on top of this when actually earning). Exposed
        /// publicly so UI can display an accurate passive rate.</summary>
        public BigNumber GetTotalPassiveOutput()
        {
            BigNumber total = BigNumber.Zero;
            foreach (var kvp in _ownedEngineers)
            {
                if (kvp.Value <= 0) continue;
                double output = kvp.Key.GetOutputForCount(kvp.Value);
                total += new BigNumber(output, 0);
            }
            return total;
        }

        /// <summary>Sum of all owned engineers across every tier - the mockup's "Headcount" stat.</summary>
        public int GetTotalHeadcount()
        {
            int total = 0;
            foreach (var kvp in _ownedEngineers) total += kvp.Value;
            return total;
        }

        /// <summary>Returns the next ModelTier not yet unlocked, and progress toward it
        /// (0-1), for the mockup's "Next milestone" progress bar. Returns null if all
        /// tiers are unlocked.</summary>
        public (ModelTierData tier, float progress) GetNextMilestone()
        {
            double lifetime = CurrencyManager.Instance.LifetimeRevenue.ToDouble();

            ModelTierData next = null;
            foreach (var tier in allModelTiers)
            {
                if (_unlockedTiers.Contains(tier)) continue;
                if (next == null || tier.unlockRevenueThreshold < next.unlockRevenueThreshold)
                    next = tier;
            }

            if (next == null) return (null, 1f);

            float progress = (float)(lifetime / next.unlockRevenueThreshold);
            return (next, Mathf.Clamp01(progress));
        }

        // --- Engineer hiring ---

        public int GetOwnedCount(EngineerData engineer) =>
            _ownedEngineers.TryGetValue(engineer, out int count) ? count : 0;

        public bool IsEngineerUnlocked(EngineerData engineer) =>
            CurrencyManager.Instance.LifetimeRevenue.ToDouble() >= engineer.unlockRevenueThreshold;

        public bool TryHireEngineer(EngineerData engineer)
        {
            if (!IsEngineerUnlocked(engineer)) return false;

            int owned = GetOwnedCount(engineer);
            double cost = engineer.GetCostForUnit(owned);
            BigNumber costBig = new BigNumber(cost, 0);

            if (!CurrencyManager.Instance.TrySpend(costBig)) return false;

            _ownedEngineers[engineer] = owned + 1;
            OnEngineerCountChanged?.Invoke(engineer, owned + 1);
            return true;
        }

        // --- Model tier unlocks (automatic, based on lifetime revenue) ---

        private void CheckModelTierUnlocks()
        {
            double lifetime = CurrencyManager.Instance.LifetimeRevenue.ToDouble();
            foreach (var tier in allModelTiers)
            {
                if (_unlockedTiers.Contains(tier)) continue;
                if (lifetime < tier.unlockRevenueThreshold) continue;

                _unlockedTiers.Add(tier);
                CurrencyManager.Instance.GlobalEarningsMultiplier *= tier.globalEarningsMultiplier;
                OnModelTierUnlocked?.Invoke(tier);
            }
        }

        // --- Compute upgrades (manual purchase) ---

        /// <summary>Count of permanently purchased Compute Upgrades - drives the
        /// badge on the Upgrades tab in the shop UI.</summary>
        public int GetPurchasedUpgradeCount() => _purchasedUpgrades.Count;

        public bool IsUpgradePurchased(ComputeUpgradeData upgrade) => _purchasedUpgrades.Contains(upgrade);

        public bool IsUpgradeUnlocked(ComputeUpgradeData upgrade)
        {
            if (upgrade.prerequisite != null && !_purchasedUpgrades.Contains(upgrade.prerequisite))
                return false;
            return CurrencyManager.Instance.LifetimeRevenue.ToDouble() >= upgrade.unlockRevenueThreshold;
        }

        public bool TryPurchaseUpgrade(ComputeUpgradeData upgrade)
        {
            if (_purchasedUpgrades.Contains(upgrade)) return false;
            if (!IsUpgradeUnlocked(upgrade)) return false;

            BigNumber cost = new BigNumber(upgrade.cost, 0);
            if (!CurrencyManager.Instance.TrySpend(cost)) return false;

            _purchasedUpgrades.Add(upgrade);
            ApplyUpgradeEffect(upgrade);
            OnComputeUpgradePurchased?.Invoke(upgrade);
            return true;
        }

        private void ApplyUpgradeEffect(ComputeUpgradeData upgrade)
        {
            switch (upgrade.effectType)
            {
                case UpgradeEffectType.ClickPowerMultiplier:
                    CurrencyManager.Instance.ClickPowerBase *= upgrade.multiplierValue;
                    break;
                case UpgradeEffectType.PassiveOutputMultiplier:
                    CurrencyManager.Instance.PassiveOutputMultiplier *= upgrade.multiplierValue;
                    break;
                case UpgradeEffectType.GlobalMultiplier:
                    CurrencyManager.Instance.GlobalEarningsMultiplier *= upgrade.multiplierValue;
                    break;
            }
        }

        // --- Reputation Upgrades (permanent - survive IPO resets) ---

        public bool IsReputationUpgradePurchased(ReputationUpgradeData upgrade) =>
            _purchasedReputationUpgrades.Contains(upgrade);

        /// <summary>Count of permanently purchased Reputation (Prestige) upgrades - drives the AchievementManager's ReputationUpgradesPurchased requirement.</summary>
        public int GetPurchasedReputationUpgradeCount() => _purchasedReputationUpgrades.Count;

        public bool IsReputationUpgradeUnlocked(ReputationUpgradeData upgrade)
        {
            if (upgrade.prerequisite != null && !_purchasedReputationUpgrades.Contains(upgrade.prerequisite))
                return false;
            return true;
        }

        public bool TryPurchaseReputationUpgrade(ReputationUpgradeData upgrade)
        {
            if (_purchasedReputationUpgrades.Contains(upgrade)) return false;
            if (!IsReputationUpgradeUnlocked(upgrade)) return false;

            if (!CurrencyManager.Instance.TrySpendReputation(upgrade.reputationCost)) return false;

            _purchasedReputationUpgrades.Add(upgrade);
            CurrencyManager.Instance.ReputationMultiplier *= upgrade.permanentMultiplier;
            OnReputationUpgradePurchased?.Invoke(upgrade);
            SaveGame();
            return true;
        }

        // --- IPO / Prestige ---

        /// <summary>Lifetime count of completed IPOs - drives the AchievementManager's IPOCount requirement. Never reset.</summary>
        public int IPOCount { get; private set; }

        // --- Onboarding ---

        /// <summary>True once the player has finished (or skipped) the first-launch coach-mark sequence. Never reset.</summary>
        public bool OnboardingCompleted { get; private set; }

        public void CompleteOnboarding()
        {
            if (OnboardingCompleted) return;
            OnboardingCompleted = true;
            SaveGame();
        }

        // --- In-app purchases ---

        /// <summary>True once the player has bought a "remove ads" product. Never reset -
        /// set by IAPManager.GrantReward, persisted here alongside every other permanent flag.</summary>
        public bool AdsRemoved { get; private set; }

        public void SetAdsRemoved(bool value)
        {
            if (AdsRemoved == value) return;
            AdsRemoved = value;
            SaveGame();
        }

        public bool CanIPO(double minimumLifetimeRevenue)
        {
            return CurrencyManager.Instance.LifetimeRevenue.ToDouble() >= minimumLifetimeRevenue;
        }

        public double ExecuteIPO()
        {
            double reputationGained = CurrencyManager.Instance.ExecuteIPO();
            IPOCount++;

            // Reset run-scoped state; Reputation and its multiplier persist.
            foreach (var key in _ownedEngineers.Keys.ToList()) _ownedEngineers[key] = 0;
            _unlockedTiers.Clear();
            _purchasedUpgrades.Clear();
            CurrencyManager.Instance.GlobalEarningsMultiplier = 1.0;
            CurrencyManager.Instance.PassiveOutputMultiplier = 1.0;
            CurrencyManager.Instance.ClickPowerBase = 1.0;

            SaveGame();
            return reputationGained;
        }

        // --- Offline Earnings ---

        private void ApplyOfflineEarnings()
        {
            SaveData data = SaveSystem.Load();
            if (data == null || string.IsNullOrEmpty(data.lastQuitTimestampUtc)) return;

            if (DateTime.TryParse(data.lastQuitTimestampUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastQuit))
            {
                double secondsAway = (DateTime.UtcNow - lastQuit).TotalSeconds;
                secondsAway = Math.Min(secondsAway, maxOfflineHours * 3600.0);
                if (secondsAway <= 1.0) return; // ignore trivial gaps (e.g. quick scene reloads)

                BigNumber passivePerSecond = GetTotalPassiveOutput();
                BigNumber before = CurrencyManager.Instance.CurrentRevenue;
                CurrencyManager.Instance.EarnFromPassive(passivePerSecond, (float)(secondsAway * offlineEarningsRate));
                BigNumber earned = CurrencyManager.Instance.CurrentRevenue - before;

                OnOfflineEarningsApplied?.Invoke(earned, secondsAway); // UI subscribes to show "Welcome back! +$X" popup
            }
        }

        // _hasLoaded guards both of these: a pause/quit event that fires before Start()
        // has run LoadGame() would otherwise SaveGame() with Awake()'s still-default,
        // zeroed-out state, silently overwriting the player's real save file.
        private void OnApplicationQuit()
        {
            if (_hasLoaded) SaveGame();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _hasLoaded) SaveGame();
        }

        // --- Save/Load (real JSON implementation via SaveSystem) ---

        public void SaveGame()
        {
            // CurrencyManager can already be torn down by the time this fires - e.g. object
            // destruction order during a real app quit, or the editor's exit-play-mode pass -
            // in which case there's nothing valid left to save.
            if (CurrencyManager.Instance == null) return;

            var data = new SaveData
            {
                revenueMantissa = CurrencyManager.Instance.CurrentRevenue.Mantissa,
                revenueExponent = CurrencyManager.Instance.CurrentRevenue.Exponent,
                lifetimeRevenueMantissa = CurrencyManager.Instance.LifetimeRevenue.Mantissa,
                lifetimeRevenueExponent = CurrencyManager.Instance.LifetimeRevenue.Exponent,
                reputation = CurrencyManager.Instance.Reputation,

                clickPowerBase = CurrencyManager.Instance.ClickPowerBase,
                globalEarningsMultiplier = CurrencyManager.Instance.GlobalEarningsMultiplier,
                passiveOutputMultiplier = CurrencyManager.Instance.PassiveOutputMultiplier,
                reputationMultiplier = CurrencyManager.Instance.ReputationMultiplier,

                lastQuitTimestampUtc = DateTime.UtcNow.ToString("o"),

                totalClicks = CurrencyManager.Instance.TotalClicks,
                ipoCount = IPOCount,
                onboardingCompleted = OnboardingCompleted
            };

            foreach (var kvp in _ownedEngineers)
            {
                data.ownedEngineerNames.Add(kvp.Key.name);
                data.ownedEngineerCounts.Add(kvp.Value);
            }
            foreach (var tier in _unlockedTiers) data.unlockedModelTierNames.Add(tier.name);
            foreach (var upg in _purchasedUpgrades) data.purchasedUpgradeNames.Add(upg.name);
            foreach (var upg in _purchasedReputationUpgrades) data.purchasedReputationUpgradeNames.Add(upg.name);
            if (Systems.AchievementManager.Instance != null)
                data.unlockedAchievementNames = Systems.AchievementManager.Instance.GetUnlockedNames();

            if (DailyLoginManager.Instance != null)
            {
                var (lastClaimedDateUtc, nextRewardDay) = DailyLoginManager.Instance.GetSaveState();
                data.dailyLoginLastClaimedDateUtc = lastClaimedDateUtc;
                data.dailyLoginNextRewardDay = nextRewardDay;
            }

            if (Systems.QuestManager.Instance != null)
            {
                var (templateNames, snapshots, expiries) = Systems.QuestManager.Instance.GetSaveState();
                data.questTemplateNames = templateNames;
                data.questSnapshotValues = snapshots;
                data.questExpiresAtUtc = expiries;
            }

            data.adsRemoved = AdsRemoved;
            if (IAPManager.Instance != null) data.ownedIAPProductIds = IAPManager.Instance.GetOwnedProductIds();

            SaveSystem.Save(data);
        }

        public void LoadGame()
        {
            SaveData data = SaveSystem.Load();
            if (data == null) return; // no save yet - fresh game, defaults already set

            CurrencyManager.Instance.LoadState(
                data.revenueMantissa, data.revenueExponent,
                data.lifetimeRevenueMantissa, data.lifetimeRevenueExponent,
                data.reputation, data.totalClicks);

            IPOCount = data.ipoCount;
            OnboardingCompleted = data.onboardingCompleted;

            CurrencyManager.Instance.ClickPowerBase = data.clickPowerBase > 0 ? data.clickPowerBase : 1.0;
            CurrencyManager.Instance.GlobalEarningsMultiplier = data.globalEarningsMultiplier > 0 ? data.globalEarningsMultiplier : 1.0;
            CurrencyManager.Instance.PassiveOutputMultiplier = data.passiveOutputMultiplier > 0 ? data.passiveOutputMultiplier : 1.0;
            CurrencyManager.Instance.ReputationMultiplier = data.reputationMultiplier > 0 ? data.reputationMultiplier : 1.0;

            // Restore engineer counts by matching ScriptableObject asset names.
            for (int i = 0; i < data.ownedEngineerNames.Count; i++)
            {
                var engineer = allEngineers.FirstOrDefault(e => e.name == data.ownedEngineerNames[i]);
                if (engineer != null)
                {
                    _ownedEngineers[engineer] = data.ownedEngineerCounts[i];
                    OnEngineerCountChanged?.Invoke(engineer, data.ownedEngineerCounts[i]);
                }
            }

            foreach (var name in data.unlockedModelTierNames)
            {
                var tier = allModelTiers.FirstOrDefault(t => t.name == name);
                if (tier != null) _unlockedTiers.Add(tier);
            }
            foreach (var name in data.purchasedUpgradeNames)
            {
                var upg = allComputeUpgrades.FirstOrDefault(u => u.name == name);
                if (upg != null) _purchasedUpgrades.Add(upg);
            }
            foreach (var name in data.purchasedReputationUpgradeNames)
            {
                var upg = allReputationUpgrades.FirstOrDefault(u => u.name == name);
                if (upg != null) _purchasedReputationUpgrades.Add(upg);
            }

            if (Systems.AchievementManager.Instance != null)
                Systems.AchievementManager.Instance.LoadUnlockedNames(data.unlockedAchievementNames);

            if (DailyLoginManager.Instance != null)
                DailyLoginManager.Instance.LoadSaveState(data.dailyLoginLastClaimedDateUtc, data.dailyLoginNextRewardDay);

            if (Systems.QuestManager.Instance != null)
                Systems.QuestManager.Instance.LoadSaveState(data.questTemplateNames, data.questSnapshotValues, data.questExpiresAtUtc);

            AdsRemoved = data.adsRemoved;
            if (IAPManager.Instance != null) IAPManager.Instance.LoadOwnedProductIds(data.ownedIAPProductIds);
        }
    }
}