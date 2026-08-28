using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using AIStartupTycoon.Utils;
using AIStartupTycoon.Data;
using TMPro;

namespace AIStartupTycoon.UI
{
   
    public class MainScreenStatsController : MonoBehaviour
    {
        [Header("Header")]
        public TMP_Text stageLabel;
        public TMP_Text passiveRateLabel;
        public TMP_Text valuationLabel;
        public TMP_Text reputationLabel;

        [Header("Level Badge (cosmetic only - not a real game system)")]
        // Purely a UI flourish derived from lifetime revenue, same idea as stageLabel above
        // (Seed/Series A/B/C) but presented as a level number + progress ring for the chunky
        // HUD's level badge. No save data, no economy effect, nothing else reads this.
        public TMP_Text levelLabel;
        public Image levelXpFill; // Image with Fill Amount type = Horizontal

        [Header("Top Bar Headcount Chip")]
        // Separate from headcountLabel below (that one lives in the Company stats grid) -
        // the mockup also shows headcount as its own top-bar chip next to Reputation.
        public TMP_Text headcountChipLabel;

        [Header("Office Card Badges")]
        // "OFFICE - FLOOR N" (purely cosmetic, derived from headcount) and the current
        // (highest unlocked) model tier + its multiplier, e.g. "Vision Model - x1.75".
        public TMP_Text officeFloorLabel;
        public TMP_Text officeTierLabel;

        [Header("Company Column")]
        public TMP_Text featuresLabel;
        public TMP_Text headcountLabel;
        public TMP_Text burnRateLabel;
        public TMP_Text multiplierLabel;

        [Header("Next Milestone")]
        public TMP_Text nextMilestoneNameLabel;
        public Image nextMilestoneProgressFill; // Image with Fill Amount type = Horizontal
        public TMP_Text nextMilestoneDetailLabel;    // e.g. "$1.28M of $2.00M valuation - unlocks Custom Silicon"

        [Header("Tuning")]
        public double valuationMultiplier = 1.5;
        public double flavorBurnPerHead = 70; // purely cosmetic, not deducted from real currency
        public double seriesAThreshold = 1000000;
        public double seriesBThreshold = 50000000;
        public double seriesCThreshold = 500000000;

        private void OnEnable()
        {
            // Instance can be null here on the editor's exit-play-mode teardown pass, which
            // re-enables scene objects after CurrencyManager has already been torn down.
            if (CurrencyManager.Instance == null) return;
            CurrencyManager.Instance.OnRevenueChanged += HandleCurrencyChanged;
            CurrencyManager.Instance.OnLifetimeRevenueChanged += HandleCurrencyChanged;
        }

        private void OnDisable()
        {
            if (CurrencyManager.Instance == null) return;
            CurrencyManager.Instance.OnRevenueChanged -= HandleCurrencyChanged;
            CurrencyManager.Instance.OnLifetimeRevenueChanged -= HandleCurrencyChanged;
        }

        private void HandleCurrencyChanged(BigNumber _) => RefreshAll();

        private void Start() => RefreshAll();

        private void Update() => RefreshRates(); // multipliers/boosts can change every frame

        private void RefreshAll()
        {
            RefreshHeader();
            RefreshLevelBadge();
            RefreshCompanyColumn();
            RefreshMilestone();
            RefreshRates();
            RefreshOfficeBadges();
        }

        private void RefreshOfficeBadges()
        {
            var gm = GameManager.Instance;
            int headcount = gm.GetTotalHeadcount();

            if (headcountChipLabel != null) headcountChipLabel.text = headcount.ToString("N0");

            if (officeFloorLabel != null)
            {
                int floor = Mathf.Clamp(headcount / 12 + 1, 1, 9);
                officeFloorLabel.text = $"OFFICE · FLOOR {floor}";
            }

            if (officeTierLabel != null && gm.allModelTiers != null)
            {
                ModelTierData current = null;
                foreach (var tier in gm.allModelTiers)
                {
                    if (!gm.IsModelTierUnlocked(tier)) continue;
                    if (current == null || tier.unlockRevenueThreshold > current.unlockRevenueThreshold)
                        current = tier;
                }
                officeTierLabel.text = current != null
                    ? $"{current.tierName} · x{current.globalEarningsMultiplier:0.##}"
                    : "";
            }
        }

        private void RefreshLevelBadge()
        {
            if (levelLabel == null && levelXpFill == null) return;

            double lifetime = CurrencyManager.Instance.LifetimeRevenue.ToDouble();
            double progress = System.Math.Log10(System.Math.Max(10, lifetime)) * 2.4;
            int level = (int)Mathf.Clamp(Mathf.Floor((float)progress) - 2, 1, 99);
            float xp = Mathf.Clamp01((float)(progress % 1.0));

            if (levelLabel != null) levelLabel.text = level.ToString();
            if (levelXpFill != null) levelXpFill.fillAmount = Mathf.Max(0.06f, xp);
        }

        private void RefreshHeader()
        {
            var cm = CurrencyManager.Instance;
            // Cash itself is animated by AnimatedCashLabel, which owns that label exclusively.
            valuationLabel.text = $"${(BigNumber)(cm.LifetimeRevenue.ToDouble() * valuationMultiplier)}";
            reputationLabel.text = $"{cm.Reputation:N0}";
            stageLabel.text = GetStageLabel(cm.LifetimeRevenue.ToDouble());
        }

        private void RefreshCompanyColumn()
        {
            var cm = CurrencyManager.Instance;
            var gm = GameManager.Instance;

            featuresLabel.text = cm.TotalClicks.ToString("N0");

            int headcount = gm.GetTotalHeadcount();
            headcountLabel.text = headcount.ToString("N0");

            double burn = headcount * flavorBurnPerHead;
            burnRateLabel.text = $"${(BigNumber)burn}/s";
        }

        private void RefreshMilestone()
        {
            var (tier, progress) = GameManager.Instance.GetNextMilestone();

            if (tier == null)
            {
                nextMilestoneNameLabel.text = "All tiers unlocked";
                if (nextMilestoneProgressFill != null) nextMilestoneProgressFill.fillAmount = 1f;
                if (nextMilestoneDetailLabel != null) nextMilestoneDetailLabel.text = "";
                return;
            }

            nextMilestoneNameLabel.text = tier.tierName;
            if (nextMilestoneProgressFill != null) nextMilestoneProgressFill.fillAmount = progress;

            if (nextMilestoneDetailLabel != null)
            {
                double lifetime = CurrencyManager.Instance.LifetimeRevenue.ToDouble();
                nextMilestoneDetailLabel.text =
                    $"{(BigNumber)lifetime} of {(BigNumber)tier.unlockRevenueThreshold} lifetime revenue";
            }
        }

        private void RefreshRates()
        {
            var cm = CurrencyManager.Instance;
            var gm = GameManager.Instance;

            BigNumber baseOutput = gm.GetTotalPassiveOutput();
            double rateMultiplier = cm.PassiveOutputMultiplier * cm.GlobalEarningsMultiplier * cm.ReputationMultiplier;
            passiveRateLabel.text = $"+{baseOutput * rateMultiplier}/sec";

            multiplierLabel.text = $"x{rateMultiplier:0.0}";
        }

        private string GetStageLabel(double lifetimeRevenue)
        {
            if (lifetimeRevenue >= seriesCThreshold) return "SERIES C";
            if (lifetimeRevenue >= seriesBThreshold) return "SERIES B";
            if (lifetimeRevenue >= seriesAThreshold) return "SERIES A";
            return "SEED STAGE";
        }
    }
}