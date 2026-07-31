using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Handles the "Watch Ad for 2x Earnings" button: requests a rewarded ad via
    /// CrazyGamesManager, and on success applies a temporary global earnings
    /// multiplier for a set duration. Attach to a dedicated UI button GameObject.
    /// </summary>
    public class RewardedBoostController : MonoBehaviour
    {
        [Header("UI")]
        public Button watchAdButton;
        public TextMeshProUGUI  buttonLabel;          // shows "Watch Ad: 2x Earnings" or a countdown
        public GameObject cooldownOverlay; // optional: shown while boost or cooldown active

        [Header("Boost Settings")]
        public double boostMultiplier = 2.0;
        public float boostDurationSeconds = 1800f; // 30 min
        public float cooldownSeconds = 60f;         // prevents instant re-trigger spam

        public bool IsBoostActive => _boostActive;
        public float RemainingBoostSeconds { get; private set; }
        public System.Action OnBoostStateChanged;

        private bool _boostActive;
        private bool _onCooldown;

        private void Start()
        {
            watchAdButton.onClick.AddListener(OnWatchAdButtonPressed);
            RefreshButtonState();
        }

        public void OnWatchAdButtonPressed()
        {
            if (_boostActive || _onCooldown) return;

            CrazyGamesManager.Instance.RequestRewardedAd(
                onRewardGranted: () =>
                {
                    StartCoroutine(ApplyBoost());
                },
                onFailed: () =>
                {
                    // Optional: surface a toast/message here, e.g. "Ad unavailable, try again later."
                    Debug.Log("[RewardedBoostController] Ad failed or was skipped - no boost granted.");
                }
            );
        }

        private IEnumerator ApplyBoost()
        {
            _boostActive = true;
            OnBoostStateChanged?.Invoke();
            RefreshButtonState();

            CurrencyManager.Instance.GlobalEarningsMultiplier *= boostMultiplier;

            float remaining = boostDurationSeconds;
            while (remaining > 0f)
            {
                RemainingBoostSeconds = remaining;
                if (buttonLabel != null)
                    buttonLabel.text = $"2x Active: {Mathf.CeilToInt(remaining)}s";
                remaining -= Time.deltaTime;
                yield return null;
            }

            CurrencyManager.Instance.GlobalEarningsMultiplier /= boostMultiplier;
            _boostActive = false;
            RemainingBoostSeconds = 0f;
            OnBoostStateChanged?.Invoke();

            StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            _onCooldown = true;
            RefreshButtonState();

            float remaining = cooldownSeconds;
            while (remaining > 0f)
            {
                if (buttonLabel != null)
                    buttonLabel.text = $"Available in {Mathf.CeilToInt(remaining)}s";
                remaining -= Time.deltaTime;
                yield return null;
            }

            _onCooldown = false;
            RefreshButtonState();
        }

        private void RefreshButtonState()
        {
            watchAdButton.interactable = !_boostActive && !_onCooldown;
            if (cooldownOverlay != null)
                cooldownOverlay.SetActive(_boostActive || _onCooldown);

            if (!_boostActive && !_onCooldown && buttonLabel != null)
                buttonLabel.text = "Watch Ad: 2x Earnings (30 min)";
        }
    }
}