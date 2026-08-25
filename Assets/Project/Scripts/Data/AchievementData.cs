using UnityEngine;

namespace AIStartupTycoon.Data
{
    public enum AchievementRequirementType
    {
        LifetimeRevenue,
        TotalClicks,
        Headcount,
        ComputeUpgradesPurchased,
        ReputationUpgradesPurchased,
        IPOCount,
        Reputation
    }

    [CreateAssetMenu(fileName = "Achievement_", menuName = "AIStartupTycoon/Achievement")]
    public class AchievementData : ScriptableObject
    {
        [Header("Identity")]
        public string achievementName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Requirement")]
        public AchievementRequirementType requirementType;
        public double requirementValue;

        [Header("Reward (optional)")]
        [Tooltip("Permanent multiplier applied to CurrencyManager.ReputationMultiplier when unlocked - same permanent multiplier ReputationUpgradeData uses, since it survives IPO resets (unlike GlobalEarningsMultiplier, which ExecuteIPO zeroes out). 1.0 = no reward, just bragging rights.")]
        public double reputationMultiplierReward = 1.0;
    }
}
