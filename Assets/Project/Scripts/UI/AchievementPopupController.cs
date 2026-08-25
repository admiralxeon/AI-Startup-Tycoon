using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Data;
using AIStartupTycoon.Systems;
using AIStartupTycoon.Core;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Toast-style popup celebrating a newly unlocked achievement. Unlike EventPopupController
    /// (a blocking modal) this auto-dismisses after displayDuration, and queues unlocks that
    /// arrive back-to-back (e.g. loading a save that crosses several thresholds in one frame)
    /// so each gets its own moment instead of overwriting the label mid-toast.
    /// </summary>
    public class AchievementPopupController : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panelRoot;
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text descriptionLabel;

        [Header("Timing")]
        public float displayDuration = 3.5f;
        public float gapBetweenToasts = 0.3f;

        private readonly Queue<AchievementData> _pending = new Queue<AchievementData>();
        private bool _showing;

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            AchievementManager.Instance.OnAchievementUnlocked += Enqueue;
        }

        private void OnDestroy()
        {
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked -= Enqueue;
        }

        private void Enqueue(AchievementData achievement)
        {
            _pending.Enqueue(achievement);
            if (!_showing) StartCoroutine(ShowQueue());
        }

        private IEnumerator ShowQueue()
        {
            _showing = true;
            while (_pending.Count > 0)
            {
                AchievementData achievement = _pending.Dequeue();

                if (icon != null) icon.sprite = achievement.icon;
                if (nameLabel != null) nameLabel.text = achievement.achievementName;
                if (descriptionLabel != null) descriptionLabel.text = achievement.description;

                if (panelRoot != null) panelRoot.SetActive(true);
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
                Handheld.Vibrate(); // rare/celebratory - unlike click feedback, safe to fire every time

                yield return new WaitForSeconds(displayDuration);

                if (panelRoot != null) panelRoot.SetActive(false);
                yield return new WaitForSeconds(gapBetweenToasts); // brief gap so back-to-back toasts read as distinct
            }
            _showing = false;
        }
    }
}
