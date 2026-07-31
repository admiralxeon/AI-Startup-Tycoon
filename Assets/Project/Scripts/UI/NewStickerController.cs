using UnityEngine;
using AIStartupTycoon.Data;
using AIStartupTycoon.Systems;
using TMPro;    

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Persistent small ticker box showing the most recent random event's headline
    /// + body, matching the mockup's "News Ticker" panel. Purely a passive display -
    /// does not replace EventPopupController's modal, both can react to the same
    /// RandomEventManager.OnEventTriggered event independently.
    /// </summary>
    public class NewsTickerController : MonoBehaviour
    {
        public GameObject tickerRoot;
        public TMP_Text tickerText;

        private void Start()
        {
            RandomEventManager.Instance.OnEventTriggered += ShowLatest;
            if (tickerRoot != null) tickerRoot.SetActive(false); // hidden until first event fires
        }

        private void OnDestroy()
        {
            if (RandomEventManager.Instance != null)
                RandomEventManager.Instance.OnEventTriggered -= ShowLatest;
        }

        private void ShowLatest(RandomEventData evt)
        {
            if (tickerRoot != null) tickerRoot.SetActive(true);
            tickerText.text = $"{evt.headline}. {evt.bodyText}";
        }
    }
}