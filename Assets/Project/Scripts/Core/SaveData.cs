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

        // Reputation upgrades - permanent, never cleared by an IPO reset
        public List<string> purchasedReputationUpgradeNames = new List<string>();

        // Offline earnings
        public string lastQuitTimestampUtc;

        // Achievement tracking
        public long totalClicks;
        public int ipoCount;
        public List<string> unlockedAchievementNames = new List<string>();

        // Onboarding - runs once per save file
        public bool onboardingCompleted;

        // Daily login reward streak
        public string dailyLoginLastClaimedDateUtc;
        public int dailyLoginNextRewardDay = 1;

        // Quest slots - parallel arrays, one entry per slot
        public List<string> questTemplateNames = new List<string>();
        public List<double> questSnapshotValues = new List<double>();
        public List<string> questExpiresAtUtc = new List<string>();

        // In-app purchases
        public bool adsRemoved;
        public List<string> ownedIAPProductIds = new List<string>();

        public int saveVersion = 1; // bump if you change this schema, so LoadGame can migrate old saves
    }
}