using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AIStartupTycoon.Data;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.Systems
{
    /// <summary>
    /// Polls each frame for AchievementData requirements being met against current
    /// game stats (mirrors GameManager.CheckModelTierUnlocks' polling pattern).
    /// Unlocks are permanent and never reset by IPO - a lifetime record, like
    /// LifetimeRevenue itself.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [Header("Data")]
        public List<AchievementData> allAchievements;

        public event Action<AchievementData> OnAchievementUnlocked;

        private readonly HashSet<AchievementData> _unlocked = new HashSet<AchievementData>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (CurrencyManager.Instance == null || GameManager.Instance == null) return;

            foreach (var achievement in allAchievements)
            {
                if (_unlocked.Contains(achievement)) continue;
                if (GetCurrentValue(achievement.requirementType) < achievement.requirementValue) continue;

                Unlock(achievement);
            }
        }

        private double GetCurrentValue(AchievementRequirementType type)
        {
            var cm = CurrencyManager.Instance;
            var gm = GameManager.Instance;

            switch (type)
            {
                case AchievementRequirementType.LifetimeRevenue: return cm.LifetimeRevenue.ToDouble();
                case AchievementRequirementType.TotalClicks: return cm.TotalClicks;
                case AchievementRequirementType.Headcount: return gm.GetTotalHeadcount();
                case AchievementRequirementType.ComputeUpgradesPurchased: return gm.GetPurchasedUpgradeCount();
                case AchievementRequirementType.ReputationUpgradesPurchased: return gm.GetPurchasedReputationUpgradeCount();
                case AchievementRequirementType.IPOCount: return gm.IPOCount;
                case AchievementRequirementType.Reputation: return cm.Reputation;
                case AchievementRequirementType.PeakCombo: return ClickComboManager.Instance != null ? ClickComboManager.Instance.PeakComboCount : 0;
                case AchievementRequirementType.RandomEventsSeen: return RandomEventManager.Instance != null ? RandomEventManager.Instance.TotalEventsTriggered : 0;
                default: return 0;
            }
        }

        private void Unlock(AchievementData achievement)
        {
            _unlocked.Add(achievement);
            if (achievement.reputationMultiplierReward != 1.0)
                CurrencyManager.Instance.ReputationMultiplier *= achievement.reputationMultiplierReward;
            OnAchievementUnlocked?.Invoke(achievement);
        }

        public bool IsUnlocked(AchievementData achievement) => _unlocked.Contains(achievement);

        public int GetUnlockedCount() => _unlocked.Count;

        /// <summary>0-1 progress toward an achievement not yet unlocked - for the shop row's progress bar.</summary>
        public float GetProgress(AchievementData achievement)
        {
            if (_unlocked.Contains(achievement)) return 1f;
            if (achievement.requirementValue <= 0) return 0f;
            return Mathf.Clamp01((float)(GetCurrentValue(achievement.requirementType) / achievement.requirementValue));
        }

        /// <summary>The tracked stat's current raw value (not clamped/normalized) - for the
        /// shop row's "current/target" pill text on a still-locked achievement.</summary>
        public double GetCurrentRawValue(AchievementData achievement) => GetCurrentValue(achievement.requirementType);

        // --- Save/Load support (called from GameManager, which owns the save file) ---

        public List<string> GetUnlockedNames() => _unlocked.Select(a => a.name).ToList();

        /// <summary>Restores unlocked state from save WITHOUT re-invoking OnAchievementUnlocked
        /// (that event is only for celebrating new unlocks, not replaying old ones) and WITHOUT
        /// re-applying rewards - GameManager.LoadGame restores ReputationMultiplier directly
        /// from the saved raw value, which already has past achievement rewards baked in.</summary>
        public void LoadUnlockedNames(List<string> names)
        {
            _unlocked.Clear();
            if (names == null) return;
            foreach (var name in names)
            {
                var achievement = allAchievements.FirstOrDefault(a => a.name == name);
                if (achievement != null) _unlocked.Add(achievement);
            }
        }
    }
}
