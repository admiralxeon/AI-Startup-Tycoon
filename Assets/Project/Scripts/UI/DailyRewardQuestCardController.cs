using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// A quest-row-styled entry point for the daily login reward, shown inline at the
    /// bottom of the Quests list whenever DailyLoginManager.CanClaimToday is true - a
    /// second, always-visible way to reach the same reward DailyRewardPopupController
    /// already surfaces automatically on load. Lives as a static scene child of the quest
    /// list container - ShopPanelController.SpawnQuestRows() pushes it below the freshly
    /// spawned quest rows so it lands at the bottom of the list, matching the mockup.
    /// </summary>
    public class DailyRewardQuestCardController : MonoBehaviour
    {
        public TMP_Text dayBadgeLabel;
        public TMP_Text subtitleLabel;
        public Button claimButton;

        private void Start()
        {
            if (claimButton != null) claimButton.onClick.AddListener(OnClaimClicked);
            if (DailyLoginManager.Instance != null) DailyLoginManager.Instance.OnStateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (DailyLoginManager.Instance != null) DailyLoginManager.Instance.OnStateChanged -= Refresh;
        }

        private void Refresh()
        {
            DailyLoginManager mgr = DailyLoginManager.Instance;
            bool show = mgr != null && mgr.CanClaimToday && mgr.PeekNextReward() != null;
            gameObject.SetActive(show);
            if (!show) return;

            DailyRewardData reward = mgr.PeekNextReward();
            int cycleLength = mgr.rewardCycle != null ? mgr.rewardCycle.Count : 0;
            if (dayBadgeLabel != null) dayBadgeLabel.text = mgr.NextRewardDay.ToString();
            if (subtitleLabel != null) subtitleLabel.text = $"Day {mgr.NextRewardDay} of {cycleLength} · {BuildRewardSummary(reward)} waiting";
        }

        private static string BuildRewardSummary(DailyRewardData reward)
        {
            if (reward.cashReward > 0) return $"${(BigNumber)reward.cashReward}";
            if (reward.reputationReward > 0) return $"{reward.reputationReward:N1} Reputation";
            if (reward.temporaryEarningsMultiplier != 1.0) return $"{reward.temporaryEarningsMultiplier:0.#}x earnings";
            return "a reward";
        }

        private void OnClaimClicked()
        {
            if (DailyLoginManager.Instance == null || !DailyLoginManager.Instance.TryClaim()) return;
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }
    }
}
