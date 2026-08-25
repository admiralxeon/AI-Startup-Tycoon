using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Systems;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// One quest slot's row. Unlike EngineerShopRow/ComputeUpgradeShopRow (one row per data
    /// asset), this is one row per SLOT INDEX - the QuestData it displays changes over time
    /// as QuestManager rerolls that slot, so it polls by index rather than holding a fixed
    /// data reference.
    /// </summary>
    public class QuestShopRow : MonoBehaviour
    {
        [Header("UI References (wire up on the prefab)")]
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text progressLabel;      // e.g. "3 / 10"
        public Image progressFill;          // Image with Fill Amount type = Horizontal
        public TMP_Text timeRemainingLabel;
        public Button claimButton;
        public GameObject readyToClaimOverlay; // optional highlight shown once complete

        private int _slotIndex;

        public void Initialize(int slotIndex)
        {
            _slotIndex = slotIndex;
            claimButton.onClick.AddListener(OnClaimClicked);
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (QuestManager.Instance == null) return;

            var data = QuestManager.Instance.GetSlotTemplate(_slotIndex);
            if (data == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);

            if (icon != null) icon.sprite = data.icon;
            if (nameLabel != null) nameLabel.text = data.questName;

            double raw = QuestManager.Instance.GetSlotProgressRaw(_slotIndex);
            double target = data.targetAmount;
            if (descriptionLabel != null) descriptionLabel.text = string.Format(data.descriptionFormat, target);
            if (progressLabel != null) progressLabel.text = $"{FormatNumber(raw)} / {FormatNumber(target)}";
            if (progressFill != null) progressFill.fillAmount = QuestManager.Instance.GetSlotProgress01(_slotIndex);

            bool complete = QuestManager.Instance.IsSlotComplete(_slotIndex);
            if (readyToClaimOverlay != null) readyToClaimOverlay.SetActive(complete);
            claimButton.interactable = complete;

            if (timeRemainingLabel != null)
                timeRemainingLabel.text = complete ? "Ready!" : FormatTime(QuestManager.Instance.GetSlotRemainingSeconds(_slotIndex));
        }

        private void OnClaimClicked()
        {
            if (QuestManager.Instance == null || !QuestManager.Instance.TryClaimSlot(_slotIndex)) return;
            if (Core.UIAudioManager.Instance != null) Core.UIAudioManager.Instance.PlayTap();
        }

        private static string FormatNumber(double amount)
        {
            if (amount >= 1000000.0) return (amount / 1000000.0).ToString("0.##") + "M";
            if (amount >= 1000.0) return (amount / 1000.0).ToString("0.##") + "K";
            return amount.ToString("0.#");
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(seconds);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int secs = totalSeconds % 60;
            return hours > 0 ? $"{hours}h {minutes}m" : minutes > 0 ? $"{minutes}m {secs}s" : $"{secs}s";
        }
    }
}
