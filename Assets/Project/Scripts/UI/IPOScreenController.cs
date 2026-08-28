using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Controls the IPO / Prestige screen: shows projected Reputation gain, confirms
    /// the reset, then calls GameManager.ExecuteIPO(). Screen should be a full-panel
    /// overlay, hidden by default and shown via ShowPanel() (e.g. from an "IPO" button
    /// elsewhere).
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
        public GameObject minimumRevenueWarningBG;  // its card background, toggled alongside it

        [Header("Buttons")]
        public Button openIPOButton;   // e.g. a persistent "IPO" button on the main screen
        public Button confirmIPOButton;
        public Button cancelButton;

        [Header("Config")]
        [Tooltip("Minimum lifetime revenue before IPO is allowed. Tune during playtesting.")]
        public double minimumLifetimeRevenueToIPO = 50000;

        [Header("Confirm Dialog Grouping")]
        public GameObject confirmContentRoot; // the ask-to-confirm dialog contents

        [Header("Results / Celebration")]
        public GameObject resultsContentRoot;
        public TMP_Text resultsReputationGainedLabel;
        public TMP_Text resultsTotalReputationLabel;
        public Button resultsContinueButton;

        private void Start()
        {
            if (openIPOButton != null) openIPOButton.onClick.AddListener(ShowPanel);
            confirmIPOButton.onClick.AddListener(OnConfirmPressed);
            cancelButton.onClick.AddListener(OnCancelPressed);
            if (resultsContinueButton != null) resultsContinueButton.onClick.AddListener(OnResultsContinuePressed);

            if (panelRoot != null) panelRoot.SetActive(false);
            if (resultsContentRoot != null) resultsContentRoot.SetActive(false);
        }

        public void ShowPanel()
        {
            panelRoot.SetActive(true);
            if (confirmContentRoot != null) confirmContentRoot.SetActive(true);
            if (resultsContentRoot != null) resultsContentRoot.SetActive(false);
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
            confirmIPOButton.gameObject.SetActive(eligible);

            if (minimumRevenueWarningLabel != null)
            {
                minimumRevenueWarningLabel.gameObject.SetActive(!eligible);
                if (!eligible)
                    minimumRevenueWarningLabel.text = $"REACH ${minimumLifetimeRevenueToIPO:N0} LIFETIME REVENUE";
            }
            if (minimumRevenueWarningBG != null) minimumRevenueWarningBG.SetActive(!eligible);
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

        /// <summary>Public so the header's back-arrow button can share this same handler
        /// instead of duplicating dismiss logic.</summary>
        public void OnCancelPressed()
        {
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
            HidePanel();
        }

        private void OnConfirmPressed()
        {
            confirmIPOButton.interactable = false; // prevent double-clicks
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();

            double reputationGained = GameManager.Instance.ExecuteIPO();
            ShowResults(reputationGained);
            Debug.Log($"[IPOScreenController] IPO complete. Reputation gained: {reputationGained:N1}");
        }

        private void ShowResults(double reputationGained)
        {
            if (HapticsManager.Instance != null) HapticsManager.Instance.Vibrate(); // IPO is a major, infrequent milestone - worth the haptic hit

            if (confirmContentRoot != null) confirmContentRoot.SetActive(false);
            confirmIPOButton.interactable = true; // reset for next time the panel opens

            if (resultsContentRoot != null) resultsContentRoot.SetActive(true);
            if (resultsReputationGainedLabel != null)
                resultsReputationGainedLabel.text = $"+{reputationGained:N1} Reputation Earned";
            if (resultsTotalReputationLabel != null)
                resultsTotalReputationLabel.text = $"Total Reputation: {CurrencyManager.Instance.Reputation:N1}";
        }

        private void OnResultsContinuePressed()
        {
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
            HidePanel();
        }
    }
}
