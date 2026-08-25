using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// One row in the real-money Store tab. Consumables (cash packs) stay buyable forever;
    /// non-consumables (remove ads, permanent boost, starter bundle) switch to an "Owned"
    /// state once purchased, same purchased/locked pattern ComputeUpgradeShopRow uses for
    /// the in-game-currency shop.
    /// </summary>
    public class IAPShopRow : MonoBehaviour
    {
        [Header("UI References (wire up on the prefab)")]
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text priceLabel;
        public Button buyButton;
        public GameObject ownedOverlay;

        private IAPProductData _data;

        public void Initialize(IAPProductData data)
        {
            _data = data;
            if (icon != null) icon.sprite = data.icon;
            if (nameLabel != null) nameLabel.text = data.displayName;
            if (descriptionLabel != null) descriptionLabel.text = data.description;
            buyButton.onClick.AddListener(OnBuyClicked);
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (_data == null || IAPManager.Instance == null) return;

            bool owned = _data.kind == IAPProductKind.NonConsumable && IAPManager.Instance.IsOwned(_data);
            if (ownedOverlay != null) ownedOverlay.SetActive(owned);
            buyButton.interactable = !owned && IAPManager.Instance.IsInitialized;

            if (priceLabel != null)
                priceLabel.text = owned ? "OWNED" : IAPManager.Instance.GetPriceString(_data);
        }

        private void OnBuyClicked()
        {
            if (IAPManager.Instance == null) return;
            IAPManager.Instance.BuyProduct(_data);
        }
    }
}
