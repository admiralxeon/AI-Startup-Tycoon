using UnityEngine;

namespace AIStartupTycoon.Data
{
    public enum UpgradeEffectType
    {
        ClickPowerMultiplier,   // boosts $ per click
        PassiveOutputMultiplier, // boosts $/sec from all engineers
        GlobalMultiplier         // boosts both
    }

    [CreateAssetMenu(fileName = "ComputeUpgrade_", menuName = "AIStartupTycoon/ComputeUpgrade")]
    public class ComputeUpgradeData : ScriptableObject
    {
        [Header("Identity")]
        public string upgradeName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Cost")]
        public double cost;

        [Header("Effect")]
        public UpgradeEffectType effectType;
        [Tooltip("Multiplier applied when purchased. e.g. 1.25 = +25%.")]
        public double multiplierValue = 1.0;

        [Header("Unlock Condition")]
        [Tooltip("Total lifetime revenue required before this upgrade appears in the shop.")]
        public double unlockRevenueThreshold;

        [Header("Prerequisite (optional)")]
        [Tooltip("If set, this upgrade requires the prerequisite to be purchased first.")]
        public ComputeUpgradeData prerequisite;
    }
}