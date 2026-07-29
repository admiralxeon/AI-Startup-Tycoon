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

        [Header("Offline Earnings Config")]
        [Range(0f, 1f)] public float offlineEarningsRate = 0.5f;
        public float maxOfflineHours = 8f;

        // Runtime ownership state
        private Dictionary<EngineerData, int> _ownedEngineers = new Dictionary<EngineerData, int>();
        private HashSet<ModelTierData> _unlockedTiers = new HashSet<ModelTierData>();
        private HashSet<ComputeUpgradeData> _purchasedUpgrades = new HashSet<ComputeUpgradeData>();

        public event Action<EngineerData, int> OnEngineerCountChanged;
        public event Action<ModelTierData> OnModelTierUnlocked;
        public event Action<ComputeUpgradeData> OnComputeUpgradePurchased;
        public event Action<BigNumber> OnOfflineEarningsApplied; // UI can subscribe to show a "welcome back" popup

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foreach (var e in allEngineers) _ownedEngineers[e] = 0;
        }

        private void Start()
        {
            LoadGame();
            ApplyOfflineEarnings();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            BigNumber totalPassivePerSecond = GetTotalPassiveOutput();
            CurrencyManager.Instance.EarnFromPassive(totalPassivePerSecond, dt);

            CheckModelTierUnlocks();
        }

        private BigNumber GetTotalPassiveOutput()
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

        // --- IPO / Prestige ---

        public bool CanIPO(double minimumLifetimeRevenue)
        {
            return CurrencyManager.Instance.LifetimeRevenue.ToDouble() >= minimumLifetimeRevenue;
        }

        public double ExecuteIPO()
        {
            double reputationGained = CurrencyManager.Instance.ExecuteIPO();

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

                OnOfflineEarningsApplied?.Invoke(earned); // UI subscribes to show "Welcome back! +$X" popup
            }
        }

        private void OnApplicationQuit() => SaveGame();

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveGame();
        }

        // --- Save/Load (real JSON implementation via SaveSystem) ---

        public void SaveGame()
        {
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

                lastQuitTimestampUtc = DateTime.UtcNow.ToString("o")
            };

            foreach (var kvp in _ownedEngineers)
            {
                data.ownedEngineerNames.Add(kvp.Key.name);
                data.ownedEngineerCounts.Add(kvp.Value);
            }
            foreach (var tier in _unlockedTiers) data.unlockedModelTierNames.Add(tier.name);
            foreach (var upg in _purchasedUpgrades) data.purchasedUpgradeNames.Add(upg.name);

            SaveSystem.Save(data);
        }

        public void LoadGame()
        {
            SaveData data = SaveSystem.Load();
            if (data == null) return; // no save yet - fresh game, defaults already set

            CurrencyManager.Instance.LoadState(
                data.revenueMantissa, data.revenueExponent,
                data.lifetimeRevenueMantissa, data.lifetimeRevenueExponent,
                data.reputation);

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
        }
    }
}