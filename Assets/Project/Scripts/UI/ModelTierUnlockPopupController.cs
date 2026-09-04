using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Celebratory popup for the handful of model-tier unlocks marked isMilestone (e.g. the
    /// Transformer tier) - everything else unlocks silently via GameManager.OnModelTierUnlocked,
    /// which nothing else in the UI currently listens to. Reuses the same dim-scrim + gold
    /// modal-card + halo language as the IPO and daily-reward popups.
    /// </summary>
    public class ModelTierUnlockPopupController : MonoBehaviour
    {
        [Header("Panel Root")]
        public GameObject panelRoot;

        [Header("Content")]
        public TMP_Text tierNameLabel;
        public TMP_Text flavorTextLabel;
        public Button dismissButton;

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            dismissButton.onClick.AddListener(OnDismissPressed);

            if (GameManager.Instance != null)
                GameManager.Instance.OnModelTierUnlocked += OnModelTierUnlocked;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnModelTierUnlocked -= OnModelTierUnlocked;
        }

        private void OnModelTierUnlocked(ModelTierData tier)
        {
            if (tier == null || !tier.isMilestone) return;

            tierNameLabel.text = tier.tierName;
            flavorTextLabel.text = tier.unlockFlavorText;
            panelRoot.SetActive(true);

            if (HapticsManager.Instance != null) HapticsManager.Instance.Vibrate();
        }

        private void OnDismissPressed()
        {
            panelRoot.SetActive(false);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }
    }
}
