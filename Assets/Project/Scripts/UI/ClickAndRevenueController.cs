using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Minimal persistent UI controller: just the click button and the top-level
    /// revenue counter. Replaces TestUIController now that hiring is handled by
    /// ShopPanelController + per-row EngineerShopRow components.
    /// </summary>
    public class ClickAndRevenueController : MonoBehaviour
    {
        [Header("Click")]
        public Button clickButton;
        public TMP_Text revenueLabel; // swap for TMP_Text if using TextMeshPro

        private void Start()
        {
            clickButton.onClick.AddListener(OnClickButtonPressed);
            CurrencyManager.Instance.OnRevenueChanged += OnRevenueChanged;

            revenueLabel.text = $"${CurrencyManager.Instance.CurrentRevenue}";
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnRevenueChanged -= OnRevenueChanged;
        }

        private void OnClickButtonPressed()
        {
            CurrencyManager.Instance.EarnFromClick();
        }

        private void OnRevenueChanged(BigNumber newRevenue)
        {
            revenueLabel.text = $"${newRevenue}";
        }
    }
}