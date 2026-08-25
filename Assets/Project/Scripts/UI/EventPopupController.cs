using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Data;
using AIStartupTycoon.Systems;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Displays the random event popup (mockup 1D: dimmed background, headline,
    /// body text, effect chip, single dismiss button). Subscribes to
    /// RandomEventManager.OnEventTriggered and shows/hides itself accordingly.
    /// </summary>
    public class EventPopupController : MonoBehaviour
    {
        [Header("Panel Root")]
        public GameObject panelRoot; // includes the dim/blur background overlay

        [Header("Content")]
        public Image illustration;
        public TMP_Text headlineLabel;
        public TMP_Text bodyLabel;
        public TMP_Text effectChipLabel;      // e.g. "Revenue -15% for the next 60 seconds"
        public GameObject effectChipRoot;  // hide entirely if event has no numeric effect
        public Button dismissButton;
        public TMP_Text dismissButtonLabel;    // mockup uses flavor text like "SHIP FASTER, THEN"

        private void Start()
        {
            dismissButton.onClick.AddListener(OnDismissPressed);
            if (panelRoot != null) panelRoot.SetActive(false);

            RandomEventManager.Instance.OnEventTriggered += ShowEvent;
        }

        private void OnDestroy()
        {
            if (RandomEventManager.Instance != null)
                RandomEventManager.Instance.OnEventTriggered -= ShowEvent;
        }

        private void ShowEvent(RandomEventData evt)
        {
            headlineLabel.text = evt.headline;
            bodyLabel.text = evt.bodyText;

            bool hasEffect = evt.temporaryEarningsMultiplier != 1.0;

            if (illustration != null && evt.illustration != null)
            {
                illustration.sprite = evt.illustration;
                // Tints the icon by outcome so it reads at a glance, same sign logic as the effect chip below.
                illustration.color = !hasEffect
                    ? new Color32(210, 220, 235, 255)
                    : evt.temporaryEarningsMultiplier >= 1.0
                        ? new Color32(120, 220, 130, 255)
                        : new Color32(255, 122, 90, 255);
            }

            if (effectChipRoot != null) effectChipRoot.SetActive(hasEffect);

            if (hasEffect && effectChipLabel != null)
            {
                double percent = (evt.temporaryEarningsMultiplier - 1.0) * 100.0;
                bool positive = percent >= 0;
                string sign = positive ? "+" : "";
                effectChipLabel.text = $"Revenue {sign}{percent:N0}% for the next {evt.durationSeconds:N0} seconds";
                effectChipLabel.color = positive
                    ? new Color32(120, 220, 130, 255)
                    : new Color32(255, 122, 90, 255);
            }

            if (dismissButtonLabel != null)
            {
                dismissButtonLabel.text = string.IsNullOrEmpty(evt.dismissButtonText)
                    ? "OK, NOTED"
                    : evt.dismissButtonText;
            }

            panelRoot.SetActive(true);
        }

        private void OnDismissPressed()
        {
            RandomEventManager.Instance.AcknowledgeEvent();
            panelRoot.SetActive(false);
            if (Core.UIAudioManager.Instance != null) Core.UIAudioManager.Instance.PlayTap();
        }
    }
}