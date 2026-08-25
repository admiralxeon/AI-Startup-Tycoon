using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIStartupTycoon.Core;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Shows a "Welcome back! +$X while you were away" popup once per launch, driven
    /// by GameManager.OnOfflineEarningsApplied. GameManager defers that event by one
    /// frame specifically so this component's Start() has time to subscribe first.
    /// </summary>
    public class WelcomeBackPopupController : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panelRoot;
        public TMP_Text earnedLabel;
        public TMP_Text awayLabel;
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

            if (earnedLabel != null) earnedLabel.text = $"+${earned}";
            if (awayLabel != null) awayLabel.text = $"while you were away for {FormatDuration(secondsAway)}";

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
