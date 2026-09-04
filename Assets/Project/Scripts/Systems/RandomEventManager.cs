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

        [Header("Fight Back")]
        [Tooltip("Real taps during a NEGATIVE event's effect window shave this many seconds off its remaining duration - the only way a player can push back against a bad event instead of just waiting it out. Positive events are never shortened this way (no reason to rush a bonus).")]
        public float fightBackReductionPerTap = 0.5f;

        public System.Action<RandomEventData> OnEventTriggered;
        public System.Action OnEventEffectEnded;

        /// <summary>Lifetime count of triggered events (not reset by IPO) - drives the
        /// RandomEventsSeen achievement type.</summary>
        public int TotalEventsTriggered { get; private set; }

        /// <summary>Seconds left on the current effect, ticking down in real time - UI can
        /// poll this for a live countdown. 0 when no effect is active.</summary>
        public float RemainingEffectSeconds { get; private set; }

        /// <summary>True only while a NEGATIVE effect is actively running - the window
        /// during which fight-back taps do anything.</summary>
        public bool IsNegativeEffectActive { get; private set; }

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
            TotalEventsTriggered++;
            OnEventTriggered?.Invoke(evt);

            if (evt.temporaryEarningsMultiplier != 1.0)
                StartCoroutine(ApplyTemporaryEffect(evt));
            else
                _eventActive = false; // pure flavor, no effect to wait out
        }

        private IEnumerator ApplyTemporaryEffect(RandomEventData evt)
        {
            CurrencyManager.Instance.GlobalEarningsMultiplier *= evt.temporaryEarningsMultiplier;

            RemainingEffectSeconds = evt.durationSeconds;
            IsNegativeEffectActive = evt.temporaryEarningsMultiplier < 1.0;

            // Manually ticked (instead of WaitForSeconds) so RegisterFightBackTap() can
            // shave time off a negative event while it's running.
            while (RemainingEffectSeconds > 0f)
            {
                RemainingEffectSeconds -= Time.deltaTime;
                yield return null;
            }

            CurrencyManager.Instance.GlobalEarningsMultiplier /= evt.temporaryEarningsMultiplier;
            _eventActive = false;
            IsNegativeEffectActive = false;
            RemainingEffectSeconds = 0f;
            OnEventEffectEnded?.Invoke();
        }

        /// <summary>Call this once per real player tap (see CurrencyManager.EarnFromClick).
        /// A no-op unless a negative event's effect is currently running - positive events
        /// and pure-flavor events are unaffected.</summary>
        public void RegisterFightBackTap()
        {
            if (!IsNegativeEffectActive) return;
            RemainingEffectSeconds = Mathf.Max(0f, RemainingEffectSeconds - fightBackReductionPerTap);
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

        /// <summary>Restores the lifetime count from a save file. Only ever raises the
        /// value - never call this to reset it.</summary>
        public void LoadEventsTriggeredCount(int savedCount)
        {
            if (savedCount > TotalEventsTriggered) TotalEventsTriggered = savedCount;
        }
    }
}