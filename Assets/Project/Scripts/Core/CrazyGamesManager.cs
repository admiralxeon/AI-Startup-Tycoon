using UnityEngine;
using CrazyGames;

namespace AIStartupTycoon.Core
{
    /// <summary>
    /// Central wrapper around the CrazyGames SDK (v5.31.0). All SDK calls in the game
    /// should go through this class, not called directly elsewhere - keeps any future
    /// SDK API changes isolated to one file.
    ///
    /// IMPORTANT: verify these exact method/callback signatures against
    /// Assets/CrazySDK/Demo/AdModule and GameModule before relying on this compiling
    /// as-is - SDK versions have shifted this API before.
    /// </summary>
    public class CrazyGamesManager : MonoBehaviour
    {
        public static CrazyGamesManager Instance { get; private set; }

        private bool _isGameplayActive;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Gameplay is considered "started" as soon as the main scene loads
            // and the player can interact. Called once here; NotifyGameplayStop/Start
            // should also be called around menu opens/closes and IPO transitions.
            NotifyGameplayStart();
        }

        // --- Gameplay start/stop tracking ---

        public void NotifyGameplayStart()
        {
            if (_isGameplayActive) return;
            _isGameplayActive = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            CrazySDK.Game.GameplayStart();
#else
            Debug.Log("[CrazyGamesManager] GameplayStart (editor stub - no SDK call made)");
#endif
        }

        

        public void NotifyGameplayStop()
        {
            if (!_isGameplayActive) return;
            _isGameplayActive = false;

#if UNITY_WEBGL && !UNITY_EDITOR
            CrazySDK.Game.GameplayStop();
#else
            Debug.Log("[CrazyGamesManager] GameplayStop (editor stub - no SDK call made)");
#endif
        }

        // --- Ads ---

        public void RequestRewardedAd(System.Action onRewardGranted, System.Action onFailed = null)
        {
            NotifyGameplayStop(); // pause gameplay tracking while an ad plays
            Time.timeScale = 0f;

#if UNITY_WEBGL && !UNITY_EDITOR
            CrazySDK.Ad.RequestAd(
                CrazyAdType.Rewarded,
                adStarted: () => Debug.Log("[CrazyGamesManager] Rewarded ad started"),
                adError: (error) =>
                {
                    Debug.LogWarning($"[CrazyGamesManager] Rewarded ad error: {error}");
                    Time.timeScale = 1f;
                    NotifyGameplayStart();
                    onFailed?.Invoke();
                },
                adFinished: () =>
                {
                    Time.timeScale = 1f;
                    NotifyGameplayStart();
                    onRewardGranted?.Invoke();
                }
            );
#else
            // Editor stub: simulate a successful ad immediately so the reward flow
            // is testable in-editor without a real browser/SDK context.
            Debug.Log("[CrazyGamesManager] Rewarded ad (editor stub - auto-granting reward)");
            Time.timeScale = 1f;
            NotifyGameplayStart();
            onRewardGranted?.Invoke();
#endif
        }

        /// <summary>
        /// Requests a midgame (non-rewarded) ad. Use sparingly - e.g. once on IPO,
        /// never stacked with other interruptions. No reward callback needed since
        /// this doesn't grant anything, it's just a break point.
        /// </summary>
        public void RequestMidgameAd(System.Action onComplete = null)
        {
            NotifyGameplayStop();
            Time.timeScale = 0f;

#if UNITY_WEBGL && !UNITY_EDITOR
            CrazySDK.Ad.RequestAd(
                CrazyAdType.Midgame,
                adStarted: () => Debug.Log("[CrazyGamesManager] Midgame ad started"),
                adError: (error) =>
                {
                    Debug.LogWarning($"[CrazyGamesManager] Midgame ad error: {error}");
                    Time.timeScale = 1f;
                    NotifyGameplayStart();
                    onComplete?.Invoke();
                },
                adFinished: () =>
                {
                    Time.timeScale = 1f;
                    NotifyGameplayStart();
                    onComplete?.Invoke();
                }
            );
#else
            Debug.Log("[CrazyGamesManager] Midgame ad (editor stub - skipping)");
            Time.timeScale = 1f;
            NotifyGameplayStart();
            onComplete?.Invoke();
#endif
        }

        private void OnApplicationQuit()
        {
            NotifyGameplayStop();
        }
    }
}