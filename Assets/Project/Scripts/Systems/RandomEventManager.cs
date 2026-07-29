using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AIStartupTycoon.Data;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.Systems
{
    /// <summary>
    /// Periodically picks and triggers a RandomEventData based on weighted selection,
    /// respecting each event's minRevenueThreshold. Fires OnEventTriggered for the UI
    /// to display; applies/reverts the temporary earnings multiplier automatically.
    /// </summary>
    public class RandomEventManager : MonoBehaviour
    {
        public static RandomEventManager Instance { get; private set; }

        [Header("Data")]
        public List<RandomEventData> allEvents;

        [Header("Timing")]
        [Tooltip("Minimum seconds between events.")]
        public float minIntervalSeconds = 90f;
        [Tooltip("Maximum seconds between events.")]
        public float maxIntervalSeconds = 240f;

        public System.Action<RandomEventData> OnEventTriggered;
        public System.Action OnEventEffectEnded;

        private bool _eventActive;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            StartCoroutine(EventLoop());
        }

        private IEnumerator EventLoop()
        {
            while (true)
            {
                float wait = Random.Range(minIntervalSeconds, maxIntervalSeconds);
                yield return new WaitForSeconds(wait);

                if (_eventActive) continue; // skip this cycle if one is already showing

                RandomEventData chosen = PickEvent();
                if (chosen != null)
                    TriggerEvent(chosen);
            }
        }

        private RandomEventData PickEvent()
        {
            double lifetimeRevenue = CurrencyManager.Instance.LifetimeRevenue.ToDouble();

            List<RandomEventData> eligible = allEvents
                .Where(e => lifetimeRevenue >= e.minRevenueThreshold)
                .ToList();

            if (eligible.Count == 0) return null;

            float totalWeight = eligible.Sum(e => e.weight);
            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var evt in eligible)
            {
                cumulative += evt.weight;
                if (roll <= cumulative) return evt;
            }

            return eligible[eligible.Count - 1]; // fallback, floating point safety
        }

        private void TriggerEvent(RandomEventData evt)
        {
            _eventActive = true;
            OnEventTriggered?.Invoke(evt);

            if (evt.temporaryEarningsMultiplier != 1.0)
                StartCoroutine(ApplyTemporaryEffect(evt));
            else
                _eventActive = false; // pure flavor, no effect to wait out
        }

        private IEnumerator ApplyTemporaryEffect(RandomEventData evt)
        {
            CurrencyManager.Instance.GlobalEarningsMultiplier *= evt.temporaryEarningsMultiplier;

            yield return new WaitForSeconds(evt.durationSeconds);

            CurrencyManager.Instance.GlobalEarningsMultiplier /= evt.temporaryEarningsMultiplier;
            _eventActive = false;
            OnEventEffectEnded?.Invoke();
        }

        /// <summary>Call this from the popup's dismiss button - doesn't cancel the
        /// effect (it keeps running per its duration), just closes the modal.</summary>
        public void AcknowledgeEvent()
        {
            // Intentionally does nothing to game state - the effect duration runs
            // independently of whether the player has dismissed the popup.
            // This method exists as a clear hook point if you want to add
            // analytics/logging on dismissal later.
        }
    }
}