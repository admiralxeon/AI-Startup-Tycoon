using UnityEngine;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.UI
{
    /// <summary>Small shared helpers for shop row "time to afford" displays - kept in one
    /// place so every row computes the ETA against the exact same rate MainScreenStatsController
    /// shows in the Top Bar, rather than each row quietly drifting out of sync.</summary>
    public static class ShopRowUtils
    {
        /// <summary>Same formula as MainScreenStatsController.RefreshRates() - passive
        /// output/sec with every active multiplier applied.</summary>
        public static double GetEffectivePassiveRate()
        {
            var cm = CurrencyManager.Instance;
            var gm = GameManager.Instance;
            if (cm == null || gm == null) return 0;

            double baseOutput = gm.GetTotalPassiveOutput().ToDouble();
            return baseOutput * cm.PassiveOutputMultiplier * cm.GlobalEarningsMultiplier * cm.ReputationMultiplier;
        }

        public static string FormatTimeToAfford(double seconds)
        {
            if (seconds < 60) return $"~{Mathf.CeilToInt((float)seconds)}s";
            if (seconds < 3600) return $"~{Mathf.CeilToInt((float)seconds / 60f)}m";
            if (seconds < 86400) return $"~{Mathf.CeilToInt((float)seconds / 3600f)}h";
            return "a while";
        }
    }
}
