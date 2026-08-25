using UnityEngine;

namespace AIStartupTycoon.Data
{
    /// <summary>
    /// A permanent upgrade purchased with Reputation (the IPO prestige currency).
    /// Unlike ComputeUpgradeData, these are never reset by GameManager.ExecuteIPO -
    /// they're the actual payoff for prestiging. Applied multiplicatively onto
    /// CurrencyManager.ReputationMultiplier, which persists across every future run.
    /// </summary>
    [CreateAssetMenu(fileName = "RepUpgrade_", menuName = "AIStartupTycoon/ReputationUpgrade")]
    public class ReputationUpgradeData : ScriptableObject
    {
        [Header("Identity")]
        public string upgradeName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Cost")]
        [Tooltip("Reputation points required to purchase.")]
        public double reputationCost;

        [Header("Effect")]
        [Tooltip("Multiplier applied permanently to CurrencyManager.ReputationMultiplier. e.g. 1.10 = +10%, stacks multiplicatively with other purchased upgrades.")]
        public double permanentMultiplier = 1.0;

        [Header("Prerequisite (optional)")]
        [Tooltip("If set, this upgrade requires the prerequisite to be purchased first.")]
        public ReputationUpgradeData prerequisite;
    }
}
