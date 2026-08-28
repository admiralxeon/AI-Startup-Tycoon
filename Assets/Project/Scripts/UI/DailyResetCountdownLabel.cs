using System;
using TMPro;
using UnityEngine;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Shows a live countdown to the next UTC day boundary - the same reset window
    /// DailyLoginManager's reward ladder uses, surfaced here as a "resets Xh Ym" pill.
    /// </summary>
    public class DailyResetCountdownLabel : MonoBehaviour
    {
        public TMP_Text label;

        private float _timer;

        private void OnEnable()
        {
            _timer = 0f;
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = 1f;
            Refresh();
        }

        private void Refresh()
        {
            if (label == null) return;
            DateTime now = DateTime.UtcNow;
            TimeSpan remaining = now.Date.AddDays(1) - now;
            label.text = $"resets {remaining.Hours}h {remaining.Minutes}m";
        }
    }
}
