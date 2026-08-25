using System;
using UnityEngine;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.Core
{
    /// <summary>
    /// Single source of truth for currency state: current revenue, lifetime revenue
    /// (used for unlock thresholds), reputation, and the multipliers that feed into
    /// both click and passive income.
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        [Header("Runtime State (read-only in Inspector)")]
        [SerializeField] private double currentRevenueMantissa;
        [SerializeField] private int currentRevenueExponent;
        [SerializeField] private double lifetimeRevenueMantissa;
        [SerializeField] private int lifetimeRevenueExponent;
        [SerializeField] private double reputation;

        public BigNumber CurrentRevenue { get; private set; }
        public BigNumber LifetimeRevenue { get; private set; }
        public double Reputation { get; private set; }
        public long TotalClicks { get; private set; } // "Features" stat in the WebGL mockup

        [Header("Multipliers (fed by ModelTiers / ComputeUpgrades)")]
        public double ClickPowerBase = 1.0;
        public double GlobalEarningsMultiplier = 1.0; // from ModelTier unlocks
        public double PassiveOutputMultiplier = 1.0;  // from ComputeUpgrades
        public double ReputationMultiplier = 1.0;      // from IPO prestige tree

        public event Action<BigNumber> OnRevenueChanged;
        public event Action<BigNumber> OnLifetimeRevenueChanged;
        public event Action<BigNumber> OnClickEarned; // fires only for player taps, with the final (combo-boosted) amount

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            CurrentRevenue = BigNumber.Zero;
            LifetimeRevenue = BigNumber.Zero;
        }

        /// <summary>Call this on click. Returns the amount actually earned (for VFX/number popups).</summary>
        public BigNumber EarnFromClick()
        {
            TotalClicks++;
            double comboMultiplier = ClickComboManager.Instance != null
                ? ClickComboManager.Instance.RegisterClick()
                : 1.0;
            double amount = ClickPowerBase * GlobalEarningsMultiplier * ReputationMultiplier * comboMultiplier;
            BigNumber earned = new BigNumber(amount, 0);
            AddRevenue(earned);
            OnClickEarned?.Invoke(earned);
            return earned;
        }

        public void ResetLifetimeRevenue()
        {
            LifetimeRevenue = BigNumber.Zero;
            OnLifetimeRevenueChanged?.Invoke(LifetimeRevenue);
        }

        /// <summary>Call this once per tick from GameManager with the summed passive output.</summary>
        public void EarnFromPassive(BigNumber passivePerSecond, float deltaTime)
        {
            BigNumber earned = passivePerSecond * (PassiveOutputMultiplier * GlobalEarningsMultiplier * ReputationMultiplier * deltaTime);
            AddRevenue(earned);
        }

        private void AddRevenue(BigNumber amount)
        {
            CurrentRevenue += amount;
            LifetimeRevenue += amount;
            OnRevenueChanged?.Invoke(CurrentRevenue);
            OnLifetimeRevenueChanged?.Invoke(LifetimeRevenue);
        }

        /// <summary>Grants currency from a source that isn't a click or passive tick (e.g.
        /// daily login rewards). Counts toward LifetimeRevenue like any other income.</summary>
        public BigNumber GrantCash(BigNumber amount)
        {
            AddRevenue(amount);
            return amount;
        }

        /// <summary>Grants raw Reputation (prestige currency) directly, as opposed to a
        /// permanent multiplier reward like ReputationUpgradeData/AchievementData use -
        /// for one-off rewards such as daily login bonuses.</summary>
        public void GrantReputation(double amount)
        {
            Reputation += amount;
            reputation = Reputation; // keep the Inspector-visible mirror in sync
        }

        /// <summary>Attempts to spend; returns false without deducting if insufficient funds.</summary>
        public bool TrySpend(BigNumber cost)
        {
            if (CurrentRevenue < cost) return false;
            CurrentRevenue -= cost;
            OnRevenueChanged?.Invoke(CurrentRevenue);
            return true;
        }

        /// <summary>Attempts to spend Reputation (the prestige currency); returns false without deducting if insufficient.</summary>
        public bool TrySpendReputation(double cost)
        {
            if (Reputation < cost) return false;
            Reputation -= cost;
            reputation = Reputation; // keep the Inspector-visible mirror in sync
            return true;
        }

        /// <summary>Called on IPO: resets current run currency, grants Reputation, keeps LifetimeRevenue as historical record.</summary>
        public double ExecuteIPO()
        {
            double reputationGained = CalculateReputationGain(LifetimeRevenue);
            Reputation += reputationGained;
            reputation = Reputation; // keep the Inspector-visible mirror in sync
            CurrentRevenue = BigNumber.Zero;
            OnRevenueChanged?.Invoke(CurrentRevenue);
            return reputationGained;
        }

      
        private double CalculateReputationGain(BigNumber lifetimeRevenue)
        {
            double revenue = lifetimeRevenue.ToDouble();
            return Math.Sqrt(revenue / 10000.0);
        }

        // --- Save/Load support ---
        public void LoadState(double revMantissa, int revExp, double lifetimeMantissa, int lifetimeExp, double savedReputation, long savedTotalClicks = 0)
        {
            CurrentRevenue = new BigNumber(revMantissa, revExp);
            LifetimeRevenue = new BigNumber(lifetimeMantissa, lifetimeExp);
            Reputation = savedReputation;
            reputation = Reputation; // keep the Inspector-visible mirror in sync
            TotalClicks = savedTotalClicks;
            OnRevenueChanged?.Invoke(CurrentRevenue);
            OnLifetimeRevenueChanged?.Invoke(LifetimeRevenue);
        }
    }
}