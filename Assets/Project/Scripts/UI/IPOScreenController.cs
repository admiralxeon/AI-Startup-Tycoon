using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Controls the IPO / Prestige screen: shows projected Reputation gain,
    /// confirms the reset via CrazyGamesManager's midgame ad hook, then calls
    /// GameManager.ExecuteIPO(). Screen should be a full-panel overlay, hidden
    /// by default and shown via ShowPanel() (e.g. from an "IPO" button elsewhere).
    /// </summary>
    public class IPOScreenController : MonoBehaviour
    {
        [Header("Panel Root")]
        public GameObject panelRoot; // the whole IPO screen, toggled on/off

        [Header("Display")]
        public TMP_Text currentLifetimeRevenueLabel;
        public TMP_Text projectedReputationLabel;
        public TMP_Text currentReputationLabel;
        public TMP_Text minimumRevenueWarningLabel; // shown if not yet eligible

        [Header("Buttons")]
        public Button openIPOButton;   // e.g. a persistent "IPO" button on the main screen
        public Button confirmIPOButton;
        public Button cancelButton;

        [Header("Config")]
        [Tooltip("Minimum lifetime revenue before IPO is allowed. Tune during playtesting.")]
        public double minimumLifetimeRevenueToIPO = 50000;

        private void Start()
        {
            if (openIPOButton != null) openIPOButton.onClick.AddListener(ShowPanel);
            confirmIPOButton.onClick.AddListener(OnConfirmPressed);
            cancelButton.onClick.AddListener(HidePanel);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void ShowPanel()
        {
            panelRoot.SetActive(true);
            RefreshDisplay();
        }

        public void HidePanel()
        {
            panelRoot.SetActive(false);
        }

        private void RefreshDisplay()
        {
            double lifetimeRevenue = CurrencyManager.Instance.LifetimeRevenue.ToDouble();
            bool eligible = lifetimeRevenue >= minimumLifetimeRevenueToIPO;

            currentLifetimeRevenueLabel.text = $"Lifetime Revenue: ${CurrencyManager.Instance.LifetimeRevenue}";
            currentReputationLabel.text = $"Current Reputation: {CurrencyManager.Instance.Reputation:N1}";

            double projectedGain = CalculateProjectedReputation(lifetimeRevenue);
            projectedReputationLabel.text = eligible
                ? $"Reputation Gain: +{projectedGain:N1}"
                : "Not yet eligible";

            confirmIPOButton.interactable = eligible;

            if (minimumRevenueWarningLabel != null)
            {
                minimumRevenueWarningLabel.gameObject.SetActive(!eligible);
                if (!eligible)
                    minimumRevenueWarningLabel.text = $"Reach ${minimumLifetimeRevenueToIPO:N0} lifetime revenue to IPO";
            }
        }

        /// <summary>
        /// Mirrors CurrencyManager's internal formula, duplicated here only for
        /// display purposes (showing the projection before commit). Keep in sync
        /// if the formula in CurrencyManager.CalculateReputationGain changes.
        /// </summary>
        private double CalculateProjectedReputation(double lifetimeRevenue)
        {
            return System.Math.Sqrt(lifetimeRevenue / 10000.0);
        }

        private void OnConfirmPressed()
        {
            confirmIPOButton.interactable = false; // prevent double-clicks during the ad

            CrazyGamesManager.Instance.RequestMidgameAd(onComplete: () =>
            {
                double reputationGained = GameManager.Instance.ExecuteIPO();
                HidePanel();

                // TODO: show a results/celebration screen here with reputationGained,
                // e.g. "You gained +12.4 Reputation! Starting your next company..."
                Debug.Log($"[IPOScreenController] IPO complete. Reputation gained: {reputationGained:N1}");
            });
        }
    }
}
