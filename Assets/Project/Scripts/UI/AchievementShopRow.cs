using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Data;
using AIStartupTycoon.Systems;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// A single tile in the Achievements list. Read-only (no buy button) - achievements
    /// unlock automatically via AchievementManager polling stats, this just displays
    /// locked/unlocked state and progress toward the next one.
    /// </summary>
    public class AchievementShopRow : MonoBehaviour
    {
        [Header("UI References (wire up on the prefab)")]
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text descriptionLabel;
        public GameObject lockedOverlay;
        public Image progressFill; // Image with Fill Amount type = Horizontal, lives on the locked overlay
        public GameObject unlockedOverlay;

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

            if (lockedOverlay != null) lockedOverlay.SetActive(!_unlocked);
            if (unlockedOverlay != null) unlockedOverlay.SetActive(_unlocked);
            if (progressFill != null) progressFill.fillAmount = AchievementManager.Instance.GetProgress(_data);
        }
    }
}
