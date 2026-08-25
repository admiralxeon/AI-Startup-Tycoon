using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Data;
using AIStartupTycoon.Systems;
using AIStartupTycoon.Core;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Toast celebrating a claimed quest - same auto-dismiss/queueing pattern as
    /// AchievementPopupController, so back-to-back claims (e.g. claiming two ready slots
    /// in quick succession) each get their own moment instead of overwriting one another.
    /// </summary>
    public class QuestPopupController : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panelRoot;
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text rewardLabel;

        [Header("Timing")]
        public float displayDuration = 3f;
        public float gapBetweenToasts = 0.3f;

        private readonly Queue<QuestData> _pending = new Queue<QuestData>();
        private bool _showing;

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (QuestManager.Instance != null) QuestManager.Instance.OnQuestClaimed += Enqueue;
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null) QuestManager.Instance.OnQuestClaimed -= Enqueue;
        }

        private void Enqueue(QuestData quest)
        {
            _pending.Enqueue(quest);
            if (!_showing) StartCoroutine(ShowQueue());
        }

        private IEnumerator ShowQueue()
        {
            _showing = true;
            while (_pending.Count > 0)
            {
                QuestData quest = _pending.Dequeue();

                if (icon != null) icon.sprite = quest.icon;
                if (nameLabel != null) nameLabel.text = quest.questName;
                if (rewardLabel != null) rewardLabel.text = BuildRewardSummary(quest);

                if (panelRoot != null) panelRoot.SetActive(true);
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();

                yield return new WaitForSeconds(displayDuration);

                if (panelRoot != null) panelRoot.SetActive(false);
                yield return new WaitForSeconds(gapBetweenToasts);
            }
            _showing = false;
        }

        private static string BuildRewardSummary(QuestData quest)
        {
            var parts = new List<string>();
            if (quest.cashReward > 0) parts.Add($"+${(Utils.BigNumber)quest.cashReward}");
            if (quest.reputationReward > 0) parts.Add($"+{quest.reputationReward:N1} Reputation");
            if (quest.temporaryEarningsMultiplier != 1.0)
                parts.Add($"{quest.temporaryEarningsMultiplier:0.0}x earnings for {quest.boostDurationSeconds:N0}s");
            return string.Join("   •   ", parts);
        }
    }
}
