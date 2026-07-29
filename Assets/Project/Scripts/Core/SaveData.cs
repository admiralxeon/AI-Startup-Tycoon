using System;
using System.Collections.Generic;

namespace AIStartupTycoon.Core
{
    [Serializable]
    public class SaveData
    {
        // Currency state
        public double revenueMantissa;
        public int revenueExponent;
        public double lifetimeRevenueMantissa;
        public int lifetimeRevenueExponent;
        public double reputation;

        // Multipliers (persist so a mid-run save doesn't lose purchased upgrade effects)
        public double clickPowerBase;
        public double globalEarningsMultiplier;
        public double passiveOutputMultiplier;
        public double reputationMultiplier;

        // Engineer ownership - parallel arrays since JsonUtility can't serialize Dictionary directly
        public List<string> ownedEngineerNames = new List<string>();
        public List<int> ownedEngineerCounts = new List<int>();

        // Unlocked model tiers / purchased upgrades - store by asset name
        public List<string> unlockedModelTierNames = new List<string>();
        public List<string> purchasedUpgradeNames = new List<string>();

        // Offline earnings
        public string lastQuitTimestampUtc;

        public int saveVersion = 1; // bump if you change this schema, so LoadGame can migrate old saves
    }
}