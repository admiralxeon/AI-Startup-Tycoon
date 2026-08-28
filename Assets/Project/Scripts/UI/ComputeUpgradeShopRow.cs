using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;
using AIStartupTycoon.Utils;
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
        public UIRoundedGraphic iconBadge; // swapped gold/purple/muted to reflect state
        public TMP_Text nameLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text effectLabel; // e.g. "Passive x1.75"
        public TMP_Text costLabel;
        public UIRoundedGraphic buyButtonGraphic;
        public Button buyButton;
        public GameObject lockedOverlay;
        public TMP_Text lockedRequirementLabel; // caption on the locked overlay itself
        public GameObject purchasedOverlay;

        [Header("State Materials")]
        public Material iconInstalledMat;
        public Material iconAvailableMat;
        public Material iconLockedMat;
        public Material buttonAffordableMat;
        public Material buttonUnaffordableMat;

        private ComputeUpgradeData _data;
        private bool _purchased;

        public void Initialize(ComputeUpgradeData data)
        {
            _data = data;
            if (icon != null) icon.sprite = data.icon;
            nameLabel.text = data.upgradeName;
            descriptionLabel.text = data.description;
            if (effectLabel != null) effectLabel.text = GetEffectText(data);
            costLabel.text = $"${(BigNumber)data.cost}";
            buyButton.onClick.AddListener(OnBuyClicked);
            Refresh();
            Canvas.ForceUpdateCanvases();
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
            bool affordable = CurrencyManager.Instance != null && CurrencyManager.Instance.CurrentRevenue.ToDouble() >= _data.cost;

            if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked && !_purchased);
            if (lockedRequirementLabel != null) lockedRequirementLabel.text = $"Unlocks at ${(BigNumber)_data.unlockRevenueThreshold}";
            buyButton.gameObject.SetActive(unlocked && !_purchased);
            buyButton.interactable = unlocked && !_purchased && affordable;
            if (purchasedOverlay != null) purchasedOverlay.SetActive(_purchased);

            if (iconBadge != null)
                iconBadge.material = _purchased ? iconInstalledMat : (unlocked ? iconAvailableMat : iconLockedMat);
            if (buyButtonGraphic != null && unlocked && !_purchased)
                buyButtonGraphic.material = affordable ? buttonAffordableMat : buttonUnaffordableMat;
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

        private static string GetEffectText(ComputeUpgradeData data)
        {
            string label = data.effectType switch
            {
                UpgradeEffectType.ClickPowerMultiplier => "Tap power",
                UpgradeEffectType.PassiveOutputMultiplier => "Passive",
                UpgradeEffectType.GlobalMultiplier => "All earnings",
                _ => "Effect"
            };
            return $"{label} x{data.multiplierValue:0.##}";
        }
    }
}
