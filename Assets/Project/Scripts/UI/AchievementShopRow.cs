using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Data;
using AIStartupTycoon.Systems;
using AIStartupTycoon.Utils;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// A single tile in the Trophy Case list. Read-only (no buy button) - achievements
    /// unlock automatically via AchievementManager polling stats. Shows a "WON" pill once
    /// unlocked, otherwise a locked-state pill showing either a "current/target" count or
    /// the raw target value, depending on what reads better for that requirement type.
    /// </summary>
    public class AchievementShopRow : MonoBehaviour
    {
        [Header("UI References (wire up on the prefab)")]
        public Image icon;
        public UIRoundedGraphic iconBadge;
        public TMP_Text nameLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text statusLabel;
        public UIRoundedGraphic statusPillGraphic;

        [Header("State Materials")]
        public Material iconUnlockedMat;
        public Material iconLockedMat;
        public Material pillUnlockedMat;
        public Material pillLockedMat;

        private AchievementData _data;
        private bool _unlocked;

        public void Initialize(AchievementData data)
        {
            _data = data;
            if (icon != null) icon.sprite = data.icon;
            nameLabel.text = data.achievementName;
            descriptionLabel.text = data.description;
            Refresh();
        }

        private void Update()
        {
            if (!_unlocked) Refresh();
        }

        private void Refresh()
        {
            if (_data == null || AchievementManager.Instance == null) return;

            _unlocked = AchievementManager.Instance.IsUnlocked(_data);

            if (statusLabel != null) statusLabel.text = _unlocked ? "WON" : BuildLockedStatusText();
            if (statusPillGraphic != null) statusPillGraphic.material = _unlocked ? pillUnlockedMat : pillLockedMat;
            if (iconBadge != null) iconBadge.material = _unlocked ? iconUnlockedMat : iconLockedMat;
        }

        private string BuildLockedStatusText()
        {
            switch (_data.requirementType)
            {
                case AchievementRequirementType.LifetimeRevenue:
                    return $"${(BigNumber)_data.requirementValue}";
                case AchievementRequirementType.Reputation:
                    return $"{_data.requirementValue:0.#} REP";
                case AchievementRequirementType.IPOCount:
                    return "IPO";
                default:
                    double current = AchievementManager.Instance.GetCurrentRawValue(_data);
                    return $"{Mathf.FloorToInt((float)current)}/{_data.requirementValue:0}";
            }
        }
    }
}
