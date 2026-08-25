using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using AIStartupTycoon.Utils;
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
            RefreshCompanyColumn();
            RefreshMilestone();
            RefreshRates();
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

            nextMilestoneNameLabel.text = tier.tierName.ToUpper();
            if (nextMilestoneProgressFill != null) nextMilestoneProgressFill.fillAmount = progress;

            if (nextMilestoneDetailLabel != null)
            {
                double lifetime = CurrencyManager.Instance.LifetimeRevenue.ToDouble();
                nextMilestoneDetailLabel.text =
                    $"{(BigNumber)lifetime} of {(BigNumber)tier.unlockRevenueThreshold} revenue - unlocks {tier.tierName}";
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