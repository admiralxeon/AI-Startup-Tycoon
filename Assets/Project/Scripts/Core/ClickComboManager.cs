using System;
using UnityEngine;

namespace AIStartupTycoon.Core
{
    /// <summary>
    /// Rewards rapid, sustained clicking with an escalating short-lived multiplier -
    /// the "why tap instead of walk away" hook. Applies only to click earnings
    /// (CurrencyManager.EarnFromClick), never to passive income, so it's purely a
    /// bonus for active play and can't be used to bypass idle-game pacing.
    /// </summary>
    public class ClickComboManager : MonoBehaviour
    {
        public static ClickComboManager Instance { get; private set; }

        [Header("Tuning")]
        [Tooltip("Clicks must land within this many seconds of the previous one to keep the combo alive.")]
        public float comboWindowSeconds = 0.6f;
        [Tooltip("Multiplier bonus added per combo step (e.g. 0.03 = +3% per consecutive click).")]
        public double bonusPerStep = 0.03;
        [Tooltip("Combo step count at which the bonus caps out.")]
        public int maxComboSteps = 40; // caps bonus at +120% (3.2x total) with the defaults above

        public int ComboCount { get; private set; }
        public double ComboMultiplier { get; private set; } = 1.0;

        /// <summary>Fires whenever combo count or multiplier changes, including the reset to 0.</summary>
        public event Action<int, double> OnComboChanged;

        private float _lastClickTime = float.NegativeInfinity;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (ComboCount > 0 && Time.unscaledTime - _lastClickTime > comboWindowSeconds)
                ResetCombo();
        }

        /// <summary>Call once per click, before applying it to currency. Returns the
        /// multiplier CurrencyManager should apply to that click's earnings.</summary>
        public double RegisterClick()
        {
            float now = Time.unscaledTime;
            bool withinWindow = now - _lastClickTime <= comboWindowSeconds;
            _lastClickTime = now;

            ComboCount = withinWindow ? Mathf.Min(ComboCount + 1, maxComboSteps) : 0;
            ComboMultiplier = 1.0 + ComboCount * bonusPerStep;
            OnComboChanged?.Invoke(ComboCount, ComboMultiplier);
            return ComboMultiplier;
        }

        private void ResetCombo()
        {
            ComboCount = 0;
            ComboMultiplier = 1.0;
            OnComboChanged?.Invoke(ComboCount, ComboMultiplier);
        }
    }
}
