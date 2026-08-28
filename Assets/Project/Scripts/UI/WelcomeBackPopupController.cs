using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIStartupTycoon.Core;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Shows a "while you were out" popup once per launch, driven by
    /// GameManager.OnOfflineEarningsApplied. GameManager defers that event by one
    /// frame specifically so this component's Start() has time to subscribe first.
    /// </summary>
    public class WelcomeBackPopupController : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panelRoot;
        public TMP_Text earnedLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text rateLabel;
        public TMP_Text cappedAtLabel;
        public Button continueButton;

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            GameManager.Instance.OnOfflineEarningsApplied += ShowWelcomeBack;
            if (continueButton != null) continueButton.onClick.AddListener(Dismiss);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnOfflineEarningsApplied -= ShowWelcomeBack;
        }

        private void ShowWelcomeBack(BigNumber earned, double secondsAway)
        {
            if (earned.ToDouble() <= 0) return; // fresh save, or no passive income yet - nothing to celebrate

            GameManager gm = GameManager.Instance;
            int ratePercent = Mathf.RoundToInt(gm.offlineEarningsRate * 100f);
            int capHours = Mathf.RoundToInt(gm.maxOfflineHours);

            if (earnedLabel != null) earnedLabel.text = $"+${earned}";
            if (descriptionLabel != null)
                descriptionLabel.text = $"Your engineers shipped for {FormatDuration(secondsAway)}. Offline output runs at {ratePercent}%, capped at {capHours} hours.";

            if (rateLabel != null)
            {
                BigNumber effectiveRate = (BigNumber)(gm.GetTotalPassiveOutput().ToDouble() * gm.offlineEarningsRate);
                rateLabel.text = $"${effectiveRate}/s";
            }
            if (cappedAtLabel != null) cappedAtLabel.text = $"{capHours}h 00m";

            if (panelRoot != null) panelRoot.SetActive(true);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }

        private void Dismiss()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private static string FormatDuration(double seconds)
        {
            int totalMinutes = Mathf.Max(1, Mathf.RoundToInt((float)(seconds / 60.0)));
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            if (hours <= 0) return $"{minutes}m";
            return $"{hours}h {minutes}m";
        }
    }
}
