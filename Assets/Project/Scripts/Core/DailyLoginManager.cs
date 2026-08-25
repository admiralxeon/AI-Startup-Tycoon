using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AIStartupTycoon.Data;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.Core
{
    /// <summary>
    /// Tracks consecutive daily logins against a repeating reward cycle (day N wraps back
    /// to the start after the last entry). The streak only advances once per calendar day
    /// (UTC) and only when the claim happens on the day immediately following the last one -
    /// missing a day resets back to Day 1 rather than losing progress permanently, the
    /// standard "restart the ladder, don't punish forever" pattern most idle games use.
    /// Claim state persists through GameManager same as every other save field.
    /// </summary>
    public class DailyLoginManager : MonoBehaviour
    {
        public static DailyLoginManager Instance { get; private set; }

        [Header("Reward Cycle (wraps back to the first entry after the last one)")]
        public List<DailyRewardData> rewardCycle;

        [Tooltip("Fallback boost duration if a reward's own boostDurationSeconds is left at 0.")]
        public float defaultBoostDurationSeconds = 300f;

        /// <summary>1-based day within rewardCycle that will be granted on the next claim.</summary>
        public int NextRewardDay { get; private set; } = 1;
        public bool CanClaimToday { get; private set; }

        /// <summary>Fires whenever claim availability is (re)computed - on load and after every claim.</summary>
        public event Action OnStateChanged;
        public event Action<DailyRewardData, int> OnRewardClaimed; // (reward, dayClaimed)

        private DateTime _lastClaimedDateUtc = DateTime.MinValue;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Deferred one frame so GameManager.Start() -> LoadGame() has definitely restored
            // our saved streak first, regardless of Unity's arbitrary Start() ordering between
            // scripts - same guard OnboardingController and GameManager's own offline-earnings
            // trigger use.
            StartCoroutine(RefreshNextFrame());
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            Refresh();
        }

        private void Refresh()
        {
            DateTime today = DateTime.UtcNow.Date;

            if (_lastClaimedDateUtc == DateTime.MinValue)
            {
                CanClaimToday = true; // never claimed before - Day 1 is always available
            }
            else
            {
                int daysSinceLastClaim = (today - _lastClaimedDateUtc).Days;
                if (daysSinceLastClaim <= 0)
                {
                    CanClaimToday = false; // already claimed today
                }
                else
                {
                    if (daysSinceLastClaim > 1) NextRewardDay = 1; // missed a day - restart the ladder
                    CanClaimToday = true;
                }
            }

            OnStateChanged?.Invoke();
        }

        /// <summary>The reward that would be granted if TryClaim() were called right now. Null if the cycle is empty.</summary>
        public DailyRewardData PeekNextReward()
        {
            if (rewardCycle == null || rewardCycle.Count == 0) return null;
            return rewardCycle[(NextRewardDay - 1) % rewardCycle.Count];
        }

        public bool TryClaim()
        {
            if (!CanClaimToday) return false;

            DailyRewardData reward = PeekNextReward();
            if (reward == null) return false;

            int claimedDay = NextRewardDay;
            GrantReward(reward);

            _lastClaimedDateUtc = DateTime.UtcNow.Date;
            NextRewardDay = (NextRewardDay % rewardCycle.Count) + 1;
            CanClaimToday = false;

            OnRewardClaimed?.Invoke(reward, claimedDay);
            OnStateChanged?.Invoke();

            if (GameManager.Instance != null) GameManager.Instance.SaveGame();
            return true;
        }

        private void GrantReward(DailyRewardData reward)
        {
            if (reward.cashReward > 0)
                CurrencyManager.Instance.GrantCash(new BigNumber(reward.cashReward, 0));

            if (reward.reputationReward > 0)
                CurrencyManager.Instance.GrantReputation(reward.reputationReward);

            if (reward.temporaryEarningsMultiplier != 1.0)
                StartCoroutine(ApplyTemporaryBoost(reward));
        }

        private IEnumerator ApplyTemporaryBoost(DailyRewardData reward)
        {
            float duration = reward.boostDurationSeconds > 0 ? reward.boostDurationSeconds : defaultBoostDurationSeconds;

            CurrencyManager.Instance.GlobalEarningsMultiplier *= reward.temporaryEarningsMultiplier;
            yield return new WaitForSeconds(duration);
            CurrencyManager.Instance.GlobalEarningsMultiplier /= reward.temporaryEarningsMultiplier;
        }

        // --- Save/Load support (called from GameManager, which owns the save file) ---

        public (string lastClaimedDateUtc, int nextRewardDay) GetSaveState()
        {
            string dateStr = _lastClaimedDateUtc == DateTime.MinValue ? null : _lastClaimedDateUtc.ToString("o");
            return (dateStr, NextRewardDay);
        }

        /// <summary>Restores streak state from save WITHOUT recomputing CanClaimToday - the
        /// Start() coroutine's deferred Refresh() does that once, after every manager's
        /// LoadGame() call (including this one) has had a chance to run.</summary>
        public void LoadSaveState(string lastClaimedDateUtc, int nextRewardDay)
        {
            _lastClaimedDateUtc = string.IsNullOrEmpty(lastClaimedDateUtc)
                ? DateTime.MinValue
                : DateTime.Parse(lastClaimedDateUtc, null, System.Globalization.DateTimeStyles.RoundtripKind).Date;
            NextRewardDay = nextRewardDay > 0 ? nextRewardDay : 1;
        }
    }
}
