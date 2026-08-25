using UnityEngine;
using TMPro;
using AIStartupTycoon.Core;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Sole owner of the header cash label's text: smoothly counts toward
    /// CurrencyManager.CurrentRevenue instead of snapping instantly, the classic idle-game
    /// "ticking" number feel. ClickAndRevenueController and MainScreenStatsController used
    /// to both write this same label directly (redundant, and would have fought this
    /// animation every frame) - that responsibility now lives here exclusively.
    /// </summary>
    public class AnimatedCashLabel : MonoBehaviour
    {
        public TMP_Text label;

        [Tooltip("Higher = catches up to the real value faster. Exponential, so it closes the same fraction of the gap per second regardless of how large the jump is (a $5 click and a $5M offline-earnings payout both feel equally snappy).")]
        public float catchUpSpeed = 8f;

        private double _displayed;

        private void Start()
        {
            if (label == null) return;
            _displayed = CurrencyManager.Instance.CurrentRevenue.ToDouble();
            Render();
        }

        private void Update()
        {
            if (label == null) return;

            double target = CurrencyManager.Instance.CurrentRevenue.ToDouble();
            float t = 1f - Mathf.Exp(-catchUpSpeed * Time.deltaTime);
            _displayed += (target - _displayed) * t;

            // Snap once within a negligible fraction of the target - otherwise floating point
            // means it technically never quite arrives, chasing forever for no visible gain.
            if (System.Math.Abs(target - _displayed) < System.Math.Max(0.01, target * 0.0001))
                _displayed = target;

            Render();
        }

        private void Render()
        {
            label.text = $"${(BigNumber)_displayed}";
        }
    }
}
