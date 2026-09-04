using UnityEngine;
using TMPro;
using AIStartupTycoon.Systems;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Small persistent HUD chip shown only while a negative random event's effect is
    /// running - gives the player a live countdown and signals that tapping shortens it
    /// (see RandomEventManager.RegisterFightBackTap). Purely a passive display; the
    /// actual reduction happens automatically whenever CurrencyManager.EarnFromClick fires.
    /// </summary>
    public class FightBackIndicatorController : MonoBehaviour
    {
        public GameObject chipRoot;
        public TMP_Text label;

        private void Update()
        {
            var mgr = RandomEventManager.Instance;
            bool show = mgr != null && mgr.IsNegativeEffectActive;
            if (chipRoot != null) chipRoot.SetActive(show);
            if (show && label != null)
                label.text = $"RIVAL EVENT · {Mathf.CeilToInt(mgr.RemainingEffectSeconds)}s · TAP TO FIGHT BACK";
        }
    }
}
