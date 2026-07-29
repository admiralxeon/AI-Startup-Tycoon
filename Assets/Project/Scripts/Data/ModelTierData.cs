using UnityEngine;

namespace AIStartupTycoon.Data
{
   
    [CreateAssetMenu(fileName = "ModelTier_", menuName = "AIStartupTycoon/ModelTier")]
    public class ModelTierData : ScriptableObject
    {
        [Header("Identity")]
        public string tierName;
        [TextArea] public string unlockFlavorText;
        public Sprite icon;
        public int tierOrder;

        [Header("Unlock Condition")]
        [Tooltip("Total lifetime revenue required to unlock this tier.")]
        public double unlockRevenueThreshold;

        [Header("Reward")]
        [Tooltip("Permanent multiplier applied to ALL earnings (click + passive) once unlocked. e.g. 1.5 = +50%.")]
        public double globalEarningsMultiplier = 1.0;
    }
}
