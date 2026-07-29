using System;
using UnityEngine;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.Core
{
    
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

        [Header("Multipliers (fed by ModelTiers / ComputeUpgrades)")]
        public double ClickPowerBase = 1.0;
        public double GlobalEarningsMultiplier = 1.0; // from ModelTier unlocks
        public double PassiveOutputMultiplier = 1.0;  // from ComputeUpgrades
        public double ReputationMultiplier = 1.0;      // from IPO prestige tree

        public event Action<BigNumber> OnRevenueChanged;
        public event Action<BigNumber> OnLifetimeRevenueChanged;

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
            double amount = ClickPowerBase * GlobalEarningsMultiplier * ReputationMultiplier;
            BigNumber earned = new BigNumber(amount, 0);
            AddRevenue(earned);
            return earned;
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

        /// <summary>Attempts to spend; returns false without deducting if insufficient funds.</summary>
        public bool TrySpend(BigNumber cost)
        {
            if (CurrentRevenue < cost) return false;
            CurrentRevenue -= cost;
            OnRevenueChanged?.Invoke(CurrentRevenue);
            return true;
        }

        /// <summary>Called on IPO: resets current run currency, grants Reputation, keeps LifetimeRevenue as historical record.</summary>
        public double ExecuteIPO()
        {
            double reputationGained = CalculateReputationGain(LifetimeRevenue);
            reputation += reputationGained;
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
        public void LoadState(double revMantissa, int revExp, double lifetimeMantissa, int lifetimeExp, double savedReputation)
        {
            CurrentRevenue = new BigNumber(revMantissa, revExp);
            LifetimeRevenue = new BigNumber(lifetimeMantissa, lifetimeExp);
            reputation = savedReputation;
            OnRevenueChanged?.Invoke(CurrentRevenue);
            OnLifetimeRevenueChanged?.Invoke(LifetimeRevenue);
        }
    }
}