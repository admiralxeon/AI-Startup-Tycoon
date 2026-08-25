using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// A single row in the Compute Upgrades list. Unlike EngineerShopRow, these are
    /// one-time purchases - once bought, the row shows a "Purchased" state permanently.
    /// </summary>
    public class ComputeUpgradeShopRow : MonoBehaviour
    {
        [Header("UI References (wire up on the prefab)")]
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text costLabel;
        public Button buyButton;
        public GameObject lockedOverlay;
        public TMP_Text lockedRequirementLabel; // caption on the locked overlay itself
        public GameObject purchasedOverlay;

        private ComputeUpgradeData _data;
        private bool _purchased;

        public void Initialize(ComputeUpgradeData data)
        {
            _data = data;
            if (icon != null) icon.sprite = data.icon;
            nameLabel.text = data.upgradeName;
            descriptionLabel.text = data.description;
            costLabel.text = $"${data.cost:N0}";
            buyButton.onClick.AddListener(OnBuyClicked);
            Refresh();
        }

        private void Update()
        {
            if (!_purchased) Refresh();
        }

        private void Refresh()
        {
            if (_data == null || GameManager.Instance == null) return;

            _purchased = GameManager.Instance.IsUpgradePurchased(_data);
            bool unlocked = GameManager.Instance.IsUpgradeUnlocked(_data);
            if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked && !_purchased);
            if (lockedRequirementLabel != null) lockedRequirementLabel.text = $"Unlocks at ${_data.unlockRevenueThreshold:N0} revenue";
            buyButton.interactable = unlocked && !_purchased;
            if (purchasedOverlay != null) purchasedOverlay.SetActive(_purchased);
        }

        private void OnBuyClicked()
        {
            if (GameManager.Instance.TryPurchaseUpgrade(_data))
            {
                _purchased = true;
                buyButton.interactable = false;
                Refresh();
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
            }
        }
    }
}