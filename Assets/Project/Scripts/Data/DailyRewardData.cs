using UnityEngine;

namespace AIStartupTycoon.Data
{
    /// <summary>
    /// One entry in DailyLoginManager's reward cycle. Each reward field is optional -
    /// leave it at its default (0 for cash/reputation, 1.0 for the multiplier) to skip
    /// that part, same "optional, no-op default" convention as AchievementData and
    /// RandomEventData use.
    /// </summary>
    [CreateAssetMenu(fileName = "DailyReward_", menuName = "AIStartupTycoon/DailyReward")]
    public class DailyRewardData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("e.g. \"Day 1\", \"Day 7\". Leave blank to fall back to \"Day {N}\".")]
        public string dayLabel;
        public Sprite icon;

        [Header("Reward (each optional - leave at 0 / 1.0 to skip)")]
        [Tooltip("Free cash granted instantly on claim. Counts toward LifetimeRevenue like any other income.")]
        public double cashReward = 0;
        [Tooltip("Free Reputation (prestige currency) granted instantly on claim.")]
        public double reputationReward = 0;
        [Tooltip("Temporary multiplier applied to ALL earnings while active. 1.0 = no boost.")]
        public double temporaryEarningsMultiplier = 1.0;
        [Tooltip("How long the boost lasts, in seconds. Ignored if multiplier is 1.0.")]
        public float boostDurationSeconds = 300f;
    }
}
