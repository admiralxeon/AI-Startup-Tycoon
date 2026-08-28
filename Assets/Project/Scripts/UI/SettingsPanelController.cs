using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Settings screen: a permanent bottom-nav tab (see MainNavController), not a
    /// dismissible modal - it has no open/close of its own, just music/SFX/haptics/
    /// idle-notification toggles, replay-tutorial, and a reset-progress action (gated
    /// behind its own confirm step, since it's destructive and irreversible).
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;
        [Tooltip("Used to switch back to the HQ tab before starting the tutorial replay, since this screen no longer has its own close/dismiss.")]
        public MainNavController mainNavController;

        [Header("Toggles")]
        public Button musicToggleButton;
        public ToggleSwitchView musicToggleView;
        public Button sfxToggleButton;
        public ToggleSwitchView sfxToggleView;
        public Button hapticsToggleButton;
        public ToggleSwitchView hapticsToggleView;
        public Button idleNotifToggleButton;
        public ToggleSwitchView idleNotifToggleView;

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

        private void Start()
        {
            musicToggleButton.onClick.AddListener(ToggleMusic);
            sfxToggleButton.onClick.AddListener(ToggleSfx);
            if (hapticsToggleButton != null) hapticsToggleButton.onClick.AddListener(ToggleHaptics);
            if (idleNotifToggleButton != null) idleNotifToggleButton.onClick.AddListener(ToggleIdleNotifications);
            replayTutorialButton.onClick.AddListener(ReplayTutorial);
            resetProgressButton.onClick.AddListener(() => resetConfirmRoot.SetActive(true));
            if (exitGameButton != null) exitGameButton.onClick.AddListener(OpenExitConfirm);
            resetConfirmButton.onClick.AddListener(ConfirmReset);
            resetCancelButton.onClick.AddListener(() => resetConfirmRoot.SetActive(false));

            if (resetConfirmRoot != null) resetConfirmRoot.SetActive(false);
            RefreshToggles();
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

        private void ToggleHaptics()
        {
            if (HapticsManager.Instance == null) return;
            bool turningOn = !HapticsManager.Instance.HapticsEnabled;
            HapticsManager.Instance.SetHapticsEnabled(turningOn);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
            if (turningOn) HapticsManager.Instance.Vibrate(); // felt confirmation only when turning on
            RefreshToggles();
        }

        private void ToggleIdleNotifications()
        {
            if (NotificationManager.Instance == null) return;
            NotificationManager.Instance.SetNotificationsEnabled(!NotificationManager.Instance.NotificationsEnabled);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
            RefreshToggles();
        }

        private void RefreshToggles()
        {
            if (musicToggleView != null) musicToggleView.SetState(UIAudioManager.Instance.MusicEnabled);
            if (sfxToggleView != null) sfxToggleView.SetState(UIAudioManager.Instance.SfxEnabled);
            if (hapticsToggleView != null && HapticsManager.Instance != null) hapticsToggleView.SetState(HapticsManager.Instance.HapticsEnabled);
            if (idleNotifToggleView != null && NotificationManager.Instance != null) idleNotifToggleView.SetState(NotificationManager.Instance.NotificationsEnabled);
        }

        private void ReplayTutorial()
        {
            if (mainNavController != null) mainNavController.Show(0); // back to HQ, so the tutorial highlights land on real targets
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
