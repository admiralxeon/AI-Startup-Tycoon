using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AIStartupTycoon.Data;
using AIStartupTycoon.Core;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.Systems
{
    /// <summary>
    /// Runtime state for one quest slot. Not a ScriptableObject - "template" points at the
    /// shared QuestData asset being attempted, everything else is this attempt's own progress.
    /// </summary>
    [Serializable]
    public class QuestSlotState
    {
        public QuestData template;
        public double startSnapshotValue; // the tracked stat's value when this quest was assigned
        public DateTime expiresAtUtc;
    }

    /// <summary>
    /// Keeps a fixed number of quest slots filled with randomly-picked QuestData templates,
    /// each with its own countdown and delta-progress tracking. A completed slot stays
    /// claimable indefinitely (no forced expiry after the target is hit) - only an
    /// UNCOMPLETED slot gets rerolled once its time limit passes. Claiming grants the reward
    /// and immediately rolls a new quest into that slot, so there are always up to
    /// slotCount quests available. Slot state persists through GameManager same as every
    /// other save field.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Data")]
        public List<QuestData> allQuestTemplates;

        [Header("Slots")]
        public int slotCount = 3;

        public event Action OnSlotsChanged;
        public event Action<QuestData> OnQuestClaimed;

        private QuestSlotState[] _slots;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _slots = new QuestSlotState[Mathf.Max(1, slotCount)];
        }

        private void Start()
        {
            // Deferred one frame so GameManager.Start() -> LoadGame() has definitely restored
            // any saved slot state first, same guard every other manager here uses.
            StartCoroutine(FillEmptySlotsNextFrame());
        }

        private IEnumerator FillEmptySlotsNextFrame()
        {
            yield return null;
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] == null) AssignNewQuest(i);
            OnSlotsChanged?.Invoke();
        }

        private void Update()
        {
            if (_slots == null) return;

            bool changed = false;
            for (int i = 0; i < _slots.Length; i++)
            {
                QuestSlotState slot = _slots[i];
                if (slot == null) continue;
                if (IsSlotComplete(i)) continue; // let the player claim it - no forced expiry once earned
                if (DateTime.UtcNow < slot.expiresAtUtc) continue;

                AssignNewQuest(i); // time ran out without completing - roll a fresh attempt
                changed = true;
            }
            if (changed) OnSlotsChanged?.Invoke();
        }

        public QuestData GetSlotTemplate(int slot) => IsValidSlot(slot) ? _slots[slot]?.template : null;

        public double GetSlotProgressRaw(int slot)
        {
            if (!IsValidSlot(slot) || _slots[slot] == null) return 0;
            QuestSlotState s = _slots[slot];
            double current = GetCurrentStatValue(s.template.requirementType);
            return Math.Max(0, current - s.startSnapshotValue);
        }

        public float GetSlotProgress01(int slot)
        {
            if (!IsValidSlot(slot) || _slots[slot] == null) return 0f;
            double target = _slots[slot].template.targetAmount;
            if (target <= 0) return 1f;
            return Mathf.Clamp01((float)(GetSlotProgressRaw(slot) / target));
        }

        public bool IsSlotComplete(int slot)
        {
            if (!IsValidSlot(slot) || _slots[slot] == null) return false;
            return GetSlotProgressRaw(slot) >= _slots[slot].template.targetAmount;
        }

        public float GetSlotRemainingSeconds(int slot)
        {
            if (!IsValidSlot(slot) || _slots[slot] == null) return 0f;
            return Mathf.Max(0f, (float)(_slots[slot].expiresAtUtc - DateTime.UtcNow).TotalSeconds);
        }

        public bool TryClaimSlot(int slot)
        {
            if (!IsValidSlot(slot) || !IsSlotComplete(slot)) return false;

            QuestData template = _slots[slot].template;
            GrantReward(template);
            OnQuestClaimed?.Invoke(template);

            AssignNewQuest(slot);
            OnSlotsChanged?.Invoke();

            if (GameManager.Instance != null) GameManager.Instance.SaveGame();
            return true;
        }

        private bool IsValidSlot(int slot) => _slots != null && slot >= 0 && slot < _slots.Length;

        private double GetCurrentStatValue(QuestRequirementType type)
        {
            var cm = CurrencyManager.Instance;
            var gm = GameManager.Instance;
            if (cm == null || gm == null) return 0;

            switch (type)
            {
                case QuestRequirementType.RevenueEarned: return cm.LifetimeRevenue.ToDouble();
                case QuestRequirementType.ClicksMade: return cm.TotalClicks;
                case QuestRequirementType.EngineersHired: return gm.GetTotalHeadcount();
                case QuestRequirementType.UpgradesPurchased: return gm.GetPurchasedUpgradeCount();
                default: return 0;
            }
        }

        private void AssignNewQuest(int slot)
        {
            QuestData picked = PickTemplate();
            if (picked == null) { _slots[slot] = null; return; }

            _slots[slot] = new QuestSlotState
            {
                template = picked,
                startSnapshotValue = GetCurrentStatValue(picked.requirementType),
                expiresAtUtc = DateTime.UtcNow.AddSeconds(picked.timeLimitSeconds)
            };
        }

        private QuestData PickTemplate()
        {
            if (allQuestTemplates == null || allQuestTemplates.Count == 0 || CurrencyManager.Instance == null) return null;

            double lifetimeRevenue = CurrencyManager.Instance.LifetimeRevenue.ToDouble();
            List<QuestData> eligible = allQuestTemplates
                .Where(q => q != null && lifetimeRevenue >= q.minRevenueThreshold)
                .ToList();
            if (eligible.Count == 0) return null;

            float totalWeight = eligible.Sum(q => q.weight);
            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var q in eligible)
            {
                cumulative += q.weight;
                if (roll <= cumulative) return q;
            }
            return eligible[eligible.Count - 1]; // fallback, floating point safety
        }

        private void GrantReward(QuestData reward)
        {
            if (reward.cashReward > 0)
                CurrencyManager.Instance.GrantCash(new BigNumber(reward.cashReward, 0));

            if (reward.reputationReward > 0)
                CurrencyManager.Instance.GrantReputation(reward.reputationReward);

            if (reward.temporaryEarningsMultiplier != 1.0)
                StartCoroutine(ApplyTemporaryBoost(reward));
        }

        private IEnumerator ApplyTemporaryBoost(QuestData reward)
        {
            CurrencyManager.Instance.GlobalEarningsMultiplier *= reward.temporaryEarningsMultiplier;
            yield return new WaitForSeconds(reward.boostDurationSeconds);
            CurrencyManager.Instance.GlobalEarningsMultiplier /= reward.temporaryEarningsMultiplier;
        }

        // --- Save/Load support (called from GameManager, which owns the save file) ---

        public (List<string> templateNames, List<double> snapshots, List<string> expiries) GetSaveState()
        {
            var names = new List<string>();
            var snapshots = new List<double>();
            var expiries = new List<string>();

            foreach (var slot in _slots)
            {
                names.Add(slot?.template != null ? slot.template.name : "");
                snapshots.Add(slot?.startSnapshotValue ?? 0);
                expiries.Add(slot != null ? slot.expiresAtUtc.ToString("o") : "");
            }
            return (names, snapshots, expiries);
        }

        /// <summary>Restores slot state from save. Any slot that fails to resolve (missing
        /// template asset, corrupt/absent save data, or the save predates this system) is
        /// left null - the Start() coroutine's deferred fill pass rolls a fresh quest into it.</summary>
        public void LoadSaveState(List<string> templateNames, List<double> snapshots, List<string> expiries)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                bool hasData = templateNames != null && i < templateNames.Count && !string.IsNullOrEmpty(templateNames[i]);
                if (!hasData) { _slots[i] = null; continue; }

                QuestData template = allQuestTemplates?.FirstOrDefault(q => q != null && q.name == templateNames[i]);
                if (template == null) { _slots[i] = null; continue; }

                DateTime expiry = i < expiries.Count && DateTime.TryParse(expiries[i], null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed)
                    ? parsed
                    : DateTime.UtcNow.AddSeconds(template.timeLimitSeconds);

                _slots[i] = new QuestSlotState
                {
                    template = template,
                    startSnapshotValue = i < snapshots.Count ? snapshots[i] : 0,
                    expiresAtUtc = expiry
                };
            }
        }
    }
}
