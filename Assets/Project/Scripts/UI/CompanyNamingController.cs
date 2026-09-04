using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// The "name your company" moment - shown once at the end of onboarding, then shown
    /// again after every IPO (GameManager.ExecuteIPO clears CompanyName, framing the reset
    /// as starting the next company rather than continuing the old one). Same panel, same
    /// controller, both call sites just call Show() and listen for OnNameConfirmed.
    /// </summary>
    public class CompanyNamingController : MonoBehaviour
    {
        [Header("Panel Root")]
        public GameObject panelRoot;

        [Header("Naming View")]
        public GameObject namingContent;
        public TMP_Text titleLabel;
        public TMP_Text subtitleLabel;
        public TMP_InputField nameInputField;
        public Button confirmButton;

        [Header("Welcome Payoff View")]
        [Tooltip("Shown after confirming a name, before the panel actually closes - a beat that puts the name the player just typed back on screen instead of it just vanishing into a top-bar label.")]
        public GameObject welcomeContent;
        public TMP_Text welcomeCompanyNameLabel;
        public TMP_Text welcomeSubtitleLabel;
        public Button welcomeContinueButton;

        public event Action OnNameConfirmed;

        private const string DefaultName = "My Startup";
        private bool _isFirstRunPending;

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            confirmButton.onClick.AddListener(OnConfirmPressed);
            welcomeContinueButton.onClick.AddListener(OnWelcomeContinuePressed);
        }

        /// <summary>isFirstRun picks between the onboarding-flavored copy and the
        /// post-IPO one - same panel, same button, just a different headline.</summary>
        public void Show(bool isFirstRun)
        {
            _isFirstRunPending = isFirstRun;

            if (titleLabel != null)
                titleLabel.text = isFirstRun ? "NAME YOUR COMPANY" : "NAME THE NEXT ONE";
            if (subtitleLabel != null)
                subtitleLabel.text = isFirstRun
                    ? "This is how the world will know you."
                    : "Same expertise, new company. What's it called this time?";

            nameInputField.text = "";
            namingContent.SetActive(true);
            welcomeContent.SetActive(false);
            panelRoot.SetActive(true);
            nameInputField.Select();
            nameInputField.ActivateInputField();
        }

        private void OnConfirmPressed()
        {
            string chosen = nameInputField.text.Trim();
            if (string.IsNullOrEmpty(chosen)) chosen = DefaultName; // never block progress on an empty field

            GameManager.Instance.SetCompanyName(chosen);

            if (welcomeCompanyNameLabel != null) welcomeCompanyNameLabel.text = chosen;
            if (welcomeSubtitleLabel != null)
                welcomeSubtitleLabel.text = _isFirstRunPending
                    ? "Every great model needs a name behind it. Let's go build something the world hasn't trained on yet."
                    : "New company, same expertise. Let's make this one count.";

            namingContent.SetActive(false);
            welcomeContent.SetActive(true);

            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }

        private void OnWelcomeContinuePressed()
        {
            panelRoot.SetActive(false);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
            OnNameConfirmed?.Invoke();
        }
    }
}
