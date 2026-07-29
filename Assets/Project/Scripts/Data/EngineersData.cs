using UnityEngine;

namespace AIStartupTycoon.Data
{

    [CreateAssetMenu(fileName = "Engineer_", menuName = "AIStartupTycoon/Engineer")]
    public class EngineerData : ScriptableObject
    {
        [Header("Identity")]
        public string engineerName;
        [TextArea] public string flavorText;
        public Sprite icon;
        public int tierOrder; // 0 = Intern, 1 = Junior Dev, etc. Used for sorting/unlock order.

        [Header("Economy")]
        [Tooltip("Base cost to hire the FIRST unit of this engineer.")]
        public double baseCost;

        [Tooltip("Cost multiplier applied per unit already owned. ~1.15 is a standard idle-game curve.")]
        public double costGrowthRate = 1.15;

        [Tooltip("Base $/sec this engineer generates per unit owned.")]
        public double baseOutputPerSecond;

        [Header("Unlock Condition")]
        [Tooltip("Total lifetime revenue required before this engineer becomes purchasable. 0 = unlocked from start.")]
        public double unlockRevenueThreshold;

        public double GetCostForUnit(int currentlyOwned)
        {
            return baseCost * System.Math.Pow(costGrowthRate, currentlyOwned);
        }

        
        public double GetOutputForCount(int ownedCount)
        {
            return baseOutputPerSecond * ownedCount;
        }
    }
}