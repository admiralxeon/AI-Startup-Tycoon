using UnityEngine;
using TMPro;
using AIStartupTycoon.Core;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Live "value per tap" readout shown inside the HQ tap orb, mirroring the same
    /// formula CurrencyManager.EarnFromClick() uses (minus the combo multiplier - this
    /// shows the baseline so the number doesn't jitter on every tap; the floating "+$"
    /// popup from ClickJuiceController already shows the combo-boosted actual amount).
    /// </summary>
    public class TapValueLabel : MonoBehaviour
    {
        public TMP_Text label;
        public string format = "${0}";

        private void Update()
        {
            if (label == null || CurrencyManager.Instance == null) return;
            var cm = CurrencyManager.Instance;
            double amount = cm.ClickPowerBase * cm.GlobalEarningsMultiplier * cm.ReputationMultiplier;
            label.text = string.Format(format, (BigNumber)amount);
        }
    }
}
