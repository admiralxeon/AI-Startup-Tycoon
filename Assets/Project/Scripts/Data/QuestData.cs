using UnityEngine;

namespace AIStartupTycoon.Data
{
    /// <summary>
    /// What a quest counts progress against. Unlike AchievementRequirementType (a lifetime
    /// total), these are all measured as a DELTA from whatever the stat was when the quest
    /// was assigned - see QuestManager for the snapshot/diff logic.
    /// </summary>
    public enum QuestRequirementType
    {
        RevenueEarned,
        ClicksMade,
        EngineersHired,
        UpgradesPurchased
    }

    /// <summary>
    /// A quest template. QuestManager rolls these into timed slots at runtime - this asset
    /// only describes the target/reward/time-limit, not any particular attempt's progress.
    /// </summary>
    [CreateAssetMenu(fileName = "Quest_", menuName = "AIStartupTycoon/Quest")]
    public class QuestData : ScriptableObject
    {
        [Header("Identity")]
        public string questName;
        [Tooltip("Use {0} for the target amount, e.g. \"Earn ${0:N0} in revenue\".")]
        [TextArea] public string descriptionFormat;
        public Sprite icon;

        [Header("Requirement")]
        public QuestRequirementType requirementType;
        public double targetAmount;
        [Tooltip("How long the player has to complete this quest once it's assigned.")]
        public float timeLimitSeconds = 3600f;

        [Header("Reward (each optional - leave at 0 / 1.0 to skip)")]
        public double cashReward = 0;
        public double reputationReward = 0;
        public double temporaryEarningsMultiplier = 1.0;
        public float boostDurationSeconds = 300f;

        [Header("Weighting")]
        [Tooltip("Relative chance of this template being picked vs others. Higher = more common.")]
        public float weight = 1f;
        [Tooltip("Minimum lifetime revenue before this quest can be assigned, to avoid gating early game with a late-game target.")]
        public double minRevenueThreshold = 0;
    }
}
