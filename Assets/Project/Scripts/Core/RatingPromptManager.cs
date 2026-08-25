using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Systems;

namespace AIStartupTycoon.Core
{
    /// <summary>
    /// Prompts for a Play Store rating once a meaningful milestone is hit (first IPO, or a
    /// handful of achievements unlocked - whichever comes first), and never again once
    /// answered with Rate Now or No Thanks. Uses a custom popup + Application.OpenURL to the
    /// store listing rather than Google's native In-App Review API - that API requires the
    /// Play Core library as a Gradle dependency, needing custom Android build template changes
    /// that can't be verified without a real device; this has no such dependency.
    /// </summary>
    public class RatingPromptManager : MonoBehaviour
    {
        private const string PromptedKey = "AIST_RatingPrompted";

        [Header("Trigger Thresholds (whichever is hit first)")]
        public int achievementsUnlockedThreshold = 3;

        [Header("UI")]
        public GameObject panelRoot;
        public Button rateNowButton;
        public Button maybeLaterButton;
        public Button noThanksButton;

        private bool _hasPrompted;
        private bool _suppressForSession; // "Maybe Later" - don't nag again until next launch

        private void Start()
        {
            _hasPrompted = PlayerPrefs.GetInt(PromptedKey, 0) == 1;
            if (panelRoot != null) panelRoot.SetActive(false);

            if (rateNowButton != null) rateNowButton.onClick.AddListener(OnRateNow);
            if (maybeLaterButton != null) maybeLaterButton.onClick.AddListener(OnMaybeLater);
            if (noThanksButton != null) noThanksButton.onClick.AddListener(OnNoThanks);
        }

        private void Update()
        {
            if (_hasPrompted || _suppressForSession) return;
            if (panelRoot == null || panelRoot.activeSelf) return;
            if (GameManager.Instance == null || AchievementManager.Instance == null) return;

            bool milestoneHit = GameManager.Instance.IPOCount >= 1
                || AchievementManager.Instance.GetUnlockedCount() >= achievementsUnlockedThreshold;
            if (!milestoneHit) return;

            panelRoot.SetActive(true);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }

        private void OnRateNow()
        {
            Application.OpenURL($"market://details?id={Application.identifier}");
            SetPromptedPermanently();
        }

        private void OnMaybeLater()
        {
            panelRoot.SetActive(false);
            _suppressForSession = true;
        }

        private void OnNoThanks()
        {
            SetPromptedPermanently();
        }

        private void SetPromptedPermanently()
        {
            _hasPrompted = true;
            PlayerPrefs.SetInt(PromptedKey, 1);
            panelRoot.SetActive(false);
        }
    }
}
