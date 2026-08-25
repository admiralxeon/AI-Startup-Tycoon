using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Auto-shows once an unclaimed reward is available, mirroring WelcomeBackPopupController's
    /// "surface it, don't make the player go find it" pattern. Dismissing without claiming just
    /// means it reappears the next time DailyLoginManager's state is checked (next launch) -
    /// the reward itself doesn't expire, it just sits there claimable for the rest of the day.
    /// </summary>
    public class DailyRewardPopupController : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Content")]
        public TMP_Text dayLabel;
        public Image icon;
        public TMP_Text rewardSummaryLabel;
        public Button claimButton;
        public Button closeButton; // dismiss without claiming

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (claimButton != null) claimButton.onClick.AddListener(OnClaimPressed);
            if (closeButton != null) closeButton.onClick.AddListener(Dismiss);

            if (DailyLoginManager.Instance != null)
                DailyLoginManager.Instance.OnStateChanged += TryShow;
        }

        private void OnDestroy()
        {
            if (DailyLoginManager.Instance != null)
                DailyLoginManager.Instance.OnStateChanged -= TryShow;
        }

        private void TryShow()
        {
            DailyLoginManager mgr = DailyLoginManager.Instance;
            if (mgr == null || !mgr.CanClaimToday) return;

            DailyRewardData reward = mgr.PeekNextReward();
            if (reward == null) return;

            if (dayLabel != null)
                dayLabel.text = string.IsNullOrEmpty(reward.dayLabel) ? $"Day {mgr.NextRewardDay}" : reward.dayLabel;
            if (icon != null && reward.icon != null) icon.sprite = reward.icon;
            if (rewardSummaryLabel != null) rewardSummaryLabel.text = BuildSummary(reward);

            if (panelRoot != null) panelRoot.SetActive(true);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }

        private static string BuildSummary(DailyRewardData reward)
        {
            var parts = new List<string>();
            if (reward.cashReward > 0) parts.Add($"+${(BigNumber)reward.cashReward}");
            if (reward.reputationReward > 0) parts.Add($"+{reward.reputationReward:N1} Reputation");
            if (reward.temporaryEarningsMultiplier != 1.0)
                parts.Add($"{reward.temporaryEarningsMultiplier:0.0}x earnings for {reward.boostDurationSeconds:N0}s");
            return string.Join("   •   ", parts);
        }

        private void OnClaimPressed()
        {
            if (DailyLoginManager.Instance == null || !DailyLoginManager.Instance.TryClaim()) return;
            if (panelRoot != null) panelRoot.SetActive(false);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }

        private void Dismiss()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }
    }
}
