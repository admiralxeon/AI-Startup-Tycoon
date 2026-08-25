using UnityEngine;
using TMPro;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Small on-screen readout for ClickComboManager: shows the current combo and
    /// its multiplier while the player is actively tapping, hides itself once the
    /// combo drops back to 0. Purely reactive - all combo logic lives in
    /// ClickComboManager, this just renders its state.
    /// </summary>
    public class ComboMeterController : MonoBehaviour
    {
        [Header("Refs")]
        public GameObject root; // whole meter, hidden when no combo is active
        public TMP_Text comboLabel;

        [Header("Format")]
        [Tooltip("e.g. \"COMBO x{0} - +{1}%\"")]
        public string format = "COMBO x{0}  +{1}%";

        private void Start()
        {
            if (root != null) root.SetActive(false);

            if (ClickComboManager.Instance != null)
                ClickComboManager.Instance.OnComboChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (ClickComboManager.Instance != null)
                ClickComboManager.Instance.OnComboChanged -= Refresh;
        }

        private void Refresh(int comboCount, double multiplier)
        {
            bool active = comboCount > 0;
            if (root != null) root.SetActive(active);
            if (!active || comboLabel == null) return;

            double bonusPercent = (multiplier - 1.0) * 100.0;
            comboLabel.text = string.Format(format, comboCount, bonusPercent.ToString("0"));
        }
    }
}
