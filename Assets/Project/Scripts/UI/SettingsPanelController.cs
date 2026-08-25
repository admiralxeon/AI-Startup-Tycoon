using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Settings modal: music/SFX toggles, replay-tutorial, and a reset-progress action
    /// (gated behind its own confirm step, since it's destructive and irreversible).
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;
        public Button openButton;
        public Button closeButton;

        [Header("Audio Toggles")]
        public Button musicToggleButton;
        public Image musicToggleImage;
        public TMP_Text musicToggleLabel;
        public Button sfxToggleButton;
        public Image sfxToggleImage;
        public TMP_Text sfxToggleLabel;

        [Header("Actions")]
        public Button replayTutorialButton;
        public Button resetProgressButton;
        public Button exitGameButton;
        public OnboardingController onboardingController;
        [Tooltip("Shared with AndroidBackButtonHandler, which owns the Yes/Cancel wiring - this button just opens it.")]
        public GameObject exitConfirmPanel;

        [Header("Reset Confirm")]
        public GameObject resetConfirmRoot;
        public Button resetConfirmButton;
        public Button resetCancelButton;

        [Header("Toggle Colors")]
        public Color onColor = new Color(0.2f, 0.7f, 0.35f);
        public Color offColor = new Color(0.35f, 0.37f, 0.42f);

        private void Start()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            musicToggleButton.onClick.AddListener(ToggleMusic);
            sfxToggleButton.onClick.AddListener(ToggleSfx);
            replayTutorialButton.onClick.AddListener(ReplayTutorial);
            resetProgressButton.onClick.AddListener(() => resetConfirmRoot.SetActive(true));
            if (exitGameButton != null) exitGameButton.onClick.AddListener(OpenExitConfirm);
            resetConfirmButton.onClick.AddListener(ConfirmReset);
            resetCancelButton.onClick.AddListener(() => resetConfirmRoot.SetActive(false));

            panelRoot.SetActive(false);
            if (resetConfirmRoot != null) resetConfirmRoot.SetActive(false);
            RefreshToggles();
        }

        private void Open()
        {
            panelRoot.SetActive(true);
            RefreshToggles();
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }

        public void Close()
        {
            panelRoot.SetActive(false);
            if (resetConfirmRoot != null) resetConfirmRoot.SetActive(false);
            if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }

        private void ToggleMusic()
        {
            UIAudioManager.Instance.SetMusicEnabled(!UIAudioManager.Instance.MusicEnabled);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
            RefreshToggles();
        }

        private void ToggleSfx()
        {
            bool turningOn = !UIAudioManager.Instance.SfxEnabled;
            UIAudioManager.Instance.SetSfxEnabled(turningOn);
            if (turningOn && UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap(); // audible confirmation only when turning on
            RefreshToggles();
        }

        private void RefreshToggles()
        {
            bool music = UIAudioManager.Instance.MusicEnabled;
            bool sfx = UIAudioManager.Instance.SfxEnabled;

            if (musicToggleLabel != null) musicToggleLabel.text = music ? "ON" : "OFF";
            if (musicToggleImage != null) musicToggleImage.color = music ? onColor : offColor;
            if (sfxToggleLabel != null) sfxToggleLabel.text = sfx ? "ON" : "OFF";
            if (sfxToggleImage != null) sfxToggleImage.color = sfx ? onColor : offColor;
        }

        private void ReplayTutorial()
        {
            Close();
            if (onboardingController != null) onboardingController.Begin();
        }

        private void ConfirmReset()
        {
            SaveSystem.DeleteSave();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OpenExitConfirm()
        {
            // exitConfirmPanel is a sibling of the settings content under panelRoot, not a
            // child of it - calling Close() here would deactivate panelRoot itself, and
            // activating a child of an inactive parent has no visible effect (activeSelf
            // becomes true but activeInHierarchy stays false until panelRoot reopens).
            if (exitConfirmPanel != null) exitConfirmPanel.SetActive(true);
        }
    }
}
