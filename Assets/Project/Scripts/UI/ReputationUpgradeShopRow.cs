using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// A single row in the Prestige shop list - permanent upgrades bought with
    /// Reputation. Unlike ComputeUpgradeShopRow, these are never reset by IPO,
    /// so "purchased" is a permanent terminal state for the whole save file.
    /// </summary>
    public class ReputationUpgradeShopRow : MonoBehaviour
    {
        [Header("UI References (wire up on the prefab)")]
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text costLabel;
        public Button buyButton;
        public GameObject lockedOverlay;    // shown if prerequisite not yet purchased
        public TMP_Text lockedRequirementLabel; // caption on the locked overlay itself
        public GameObject purchasedOverlay; // shown once bought - permanent

        private ReputationUpgradeData _data;
        private bool _purchased;

        public void Initialize(ReputationUpgradeData data)
        {
            _data = data;
            if (icon != null) icon.sprite = data.icon;
            nameLabel.text = data.upgradeName;
            descriptionLabel.text = data.description;
            costLabel.text = $"{data.reputationCost:N1} Rep";
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

            _purchased = GameManager.Instance.IsReputationUpgradePurchased(_data);
            bool unlocked = GameManager.Instance.IsReputationUpgradeUnlocked(_data);

            if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked && !_purchased);
            if (lockedRequirementLabel != null && _data.prerequisite != null)
                lockedRequirementLabel.text = $"Requires: {_data.prerequisite.upgradeName}";
            if (purchasedOverlay != null) purchasedOverlay.SetActive(_purchased);
            buyButton.interactable = unlocked && !_purchased;
        }

        private void OnBuyClicked()
        {
            if (GameManager.Instance.TryPurchaseReputationUpgrade(_data))
            {
                _purchased = true;
                buyButton.interactable = false;
                Refresh();
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
            }
        }
    }
}
