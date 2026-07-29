using UnityEngine;
using TMPro;
using AIStartupTycoon.Core;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Persistent top-bar revenue display. Separate from the shop panel and any
    /// test harness so it survives regardless of what other UI gets toggled.
    /// </summary>
    public class TopBarController : MonoBehaviour
    {
        public TMP_Text revenueLabel;

        private void Start()
        {
            CurrencyManager.Instance.OnRevenueChanged += OnRevenueChanged;
            OnRevenueChanged(CurrencyManager.Instance.CurrentRevenue); // initial paint
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnRevenueChanged -= OnRevenueChanged;
        }

        private void OnRevenueChanged(BigNumber newRevenue)
        {
            revenueLabel.text = $"${newRevenue}";
        }
    }
}