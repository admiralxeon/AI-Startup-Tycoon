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
    /// Shows the full 7-day ladder: days before NextRewardDay read as claimed (this pass through
    /// the cycle), NextRewardDay itself is claimable now, everything after is locked. A wrap
    /// back to day 1 after completing day 7 correctly reads as "nothing claimed yet" - a fresh
    /// lap, not a bug.
    /// </summary>
    public class DailyRewardPopupController : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        [Header("Content")]
        public TMP_Text titleLabel;
        public TMP_Text subtitleLabel;
        public DailyRewardTileView[] tiles; // exactly rewardCycle.Count, in day order
        public Button claimButton;
        public TMP_Text claimButtonLabel;
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

            int cycleLength = mgr.rewardCycle != null ? mgr.rewardCycle.Count : 0;
            if (subtitleLabel != null)
                subtitleLabel.text = $"Day {mgr.NextRewardDay} of {cycleLength} · miss a day and the ladder restarts";

            RefreshTiles(mgr);

            if (claimButtonLabel != null) claimButtonLabel.text = $"CLAIM {BuildShortRewardText(reward)}";

            if (panelRoot != null) panelRoot.SetActive(true);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }

        private void RefreshTiles(DailyLoginManager mgr)
        {
            if (tiles == null || mgr.rewardCycle == null) return;
            for (int i = 0; i < tiles.Length && i < mgr.rewardCycle.Count; i++)
            {
                int day = i + 1;
                DailyRewardData data = mgr.rewardCycle[i];
                DailyRewardTileState state = day < mgr.NextRewardDay ? DailyRewardTileState.Claimed
                    : day == mgr.NextRewardDay ? DailyRewardTileState.Current
                    : DailyRewardTileState.Locked;

                string dayText = string.IsNullOrEmpty(data.dayLabel) ? $"D{day}" : data.dayLabel;
                tiles[i].SetState(state, dayText, BuildShortRewardText(data));
            }
        }

        private static string BuildShortRewardText(DailyRewardData reward)
        {
            if (reward.cashReward > 0) return $"${(BigNumber)reward.cashReward}";
            if (reward.reputationReward > 0) return $"{reward.reputationReward:0.#} REP";
            if (reward.temporaryEarningsMultiplier != 1.0) return $"{reward.temporaryEarningsMultiplier:0.#}x";
            return "—";
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
