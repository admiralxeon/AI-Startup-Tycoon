using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Minimal persistent UI controller: just the click button. The revenue counter it used
    /// to also drive is now owned exclusively by AnimatedCashLabel (attached directly to that
    /// label), so both don't fight over the same text every frame.
    /// </summary>
    public class ClickAndRevenueController : MonoBehaviour
    {
        [Header("Click")]
        public Button clickButton;

        private void Start()
        {
            clickButton.onClick.AddListener(OnClickButtonPressed);
        }

        private void OnClickButtonPressed()
        {
            CurrencyManager.Instance.EarnFromClick();
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayClick();
        }
    }
}
