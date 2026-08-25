using UnityEngine;

namespace AIStartupTycoon.Core
{
    /// <summary>
    /// Central one-shot UI sound player, plus background music playback. Each SFX category
    /// picks a random clip from its array (when more than one is assigned) so repeated
    /// presses don't sound identical every time. Attach to a persistent GameObject (e.g.
    /// Managers). Music/SFX enabled state persists via PlayerPrefs rather than the game's
    /// own SaveData - these are device-level preferences, not gameplay progress, so a
    /// "Reset Progress" action shouldn't also silently re-enable audio the player muted.
    /// </summary>
    public class UIAudioManager : MonoBehaviour
    {
        public static UIAudioManager Instance { get; private set; }

        [Header("Clip Sets")]
        public AudioClip[] clickClips;   // main "Ship Feature" click button
        public AudioClip[] switchClips;  // tab switches (Engineers / Upgrades)
        public AudioClip[] tapClips;     // generic buttons: Buy, Confirm, Cancel, Dismiss

        [Range(0f, 1f)] public float volume = 0.8f;

        [Header("Background Music")]
        public AudioClip backgroundMusic;
        [Range(0f, 1f)] public float musicVolume = 0.35f; // ambience should sit well under SFX

        private const string MusicEnabledKey = "AIST_MusicEnabled";
        private const string SfxEnabledKey = "AIST_SfxEnabled";

        public bool MusicEnabled { get; private set; } = true;
        public bool SfxEnabled { get; private set; } = true;

        private AudioSource _source;      // one-shot SFX, via PlayOneShot
        private AudioSource _musicSource; // separate source: needs to loop independently of SFX

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
            SfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;

            _source = gameObject.GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;
            _musicSource.loop = true;
            _musicSource.volume = musicVolume;
            _musicSource.mute = !MusicEnabled;
        }

        private void Start()
        {
            if (backgroundMusic != null)
            {
                _musicSource.clip = backgroundMusic;
                _musicSource.Play();
            }
        }

        public void SetMusicEnabled(bool isEnabled)
        {
            MusicEnabled = isEnabled;
            _musicSource.mute = !isEnabled;
            PlayerPrefs.SetInt(MusicEnabledKey, isEnabled ? 1 : 0);
        }

        public void SetSfxEnabled(bool isEnabled)
        {
            SfxEnabled = isEnabled;
            PlayerPrefs.SetInt(SfxEnabledKey, isEnabled ? 1 : 0);
        }

        private void PlayRandom(AudioClip[] clips)
        {
            if (!SfxEnabled || clips == null || clips.Length == 0 || _source == null) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null) _source.PlayOneShot(clip, volume);
        }

        public void PlayClick() => PlayRandom(clickClips);
        public void PlaySwitch() => PlayRandom(switchClips);
        public void PlayTap() => PlayRandom(tapClips);
    }
}
