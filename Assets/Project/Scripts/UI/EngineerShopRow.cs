using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// A single row in the Engineer shop list. One prefab instance per EngineerData,
    /// spawned and managed by ShopPanelController. Self-updates its own cost/owned
    /// display each frame rather than requiring the parent to push updates per-row.
    /// </summary>
    public class EngineerShopRow : MonoBehaviour
    {
        [Header("UI References (wire up on the prefab)")]
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text costLabel;
        public TMP_Text ownedLabel;
        public TMP_Text outputLabel;
        public Button buyButton;
        public GameObject lockedOverlay; // shown/hidden based on unlock state

        private EngineerData _data;

        public void Initialize(EngineerData data)
        {
            _data = data;
            if (icon != null) icon.sprite = data.icon;
            nameLabel.text = data.engineerName;
            buyButton.onClick.AddListener(OnBuyClicked);
            Refresh();
        }

        private void Update()
        {
            // Simple per-row polling. Fine at this list scale (a handful of rows);
            // if this ever grows to dozens of rows, switch to event-driven refresh only.
            Refresh();
        }

        private void Refresh()
        {
            if (_data == null || GameManager.Instance == null) return;

            bool unlocked = GameManager.Instance.IsEngineerUnlocked(_data);
            if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);
            buyButton.interactable = unlocked;

            int owned = GameManager.Instance.GetOwnedCount(_data);
            double cost = _data.GetCostForUnit(owned);

            costLabel.text = unlocked ? $"${cost:N0}" : $"Unlocks at ${_data.unlockRevenueThreshold:N0}";
            ownedLabel.text = $"x{owned}";
            outputLabel.text = $"{_data.GetOutputForCount(owned):N1}/sec";
        }

        private void OnBuyClicked()
        {
            GameManager.Instance.TryHireEngineer(_data);
        }
    }
}