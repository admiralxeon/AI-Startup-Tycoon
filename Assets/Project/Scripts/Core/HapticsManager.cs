using UnityEngine;

namespace AIStartupTycoon.Core
{
    /// <summary>
    /// Gates the game's Handheld.Vibrate() calls (achievement unlocks, IPO completion)
    /// behind a player-toggleable preference. Persists via PlayerPrefs like
    /// UIAudioManager's Music/SFX toggles - a device-level preference, not save progress.
    /// </summary>
    public class HapticsManager : MonoBehaviour
    {
        public static HapticsManager Instance { get; private set; }

        private const string HapticsEnabledKey = "AIST_HapticsEnabled";

        public bool HapticsEnabled { get; private set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            HapticsEnabled = PlayerPrefs.GetInt(HapticsEnabledKey, 1) == 1;
        }

        public void SetHapticsEnabled(bool isEnabled)
        {
            HapticsEnabled = isEnabled;
            PlayerPrefs.SetInt(HapticsEnabledKey, isEnabled ? 1 : 0);
        }

        public void Vibrate()
        {
            if (HapticsEnabled) Handheld.Vibrate();
        }
    }
}
