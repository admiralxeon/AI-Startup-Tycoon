using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;
using AIStartupTycoon.Utils;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// A single row in the Models shop tab. Read-only (no buy button) - model tiers
    /// unlock automatically off lifetime revenue via GameManager.CheckModelTierUnlocks(),
    /// same pattern as AchievementShopRow.
    /// </summary>
    public class ModelTierShopRow : MonoBehaviour
    {
        [Header("UI References (wire up on the prefab)")]
        public Image icon;
        public UIRoundedGraphic iconBadge;
        public TMP_Text nameLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text multiplierLabel;
        public GameObject lockedOverlay;
        public TMP_Text lockedRequirementLabel;
        public GameObject unlockedOverlay;

        [Header("State Materials")]
        public Material iconUnlockedMat;
        public Material iconLockedMat;

        private ModelTierData _data;
        private bool _unlocked;

        public void Initialize(ModelTierData data)
        {
            _data = data;
            if (icon != null) icon.sprite = data.icon;
            nameLabel.text = data.tierName;
            if (descriptionLabel != null) descriptionLabel.text = data.unlockFlavorText;
            Refresh();
            Canvas.ForceUpdateCanvases();
        }

        private void Update()
        {
            if (!_unlocked) Refresh();
        }

        private void Refresh()
        {
            if (_data == null || GameManager.Instance == null) return;

            _unlocked = GameManager.Instance.IsModelTierUnlocked(_data);
            if (lockedOverlay != null) lockedOverlay.SetActive(!_unlocked);
            if (unlockedOverlay != null) unlockedOverlay.SetActive(_unlocked);
            if (lockedRequirementLabel != null)
                lockedRequirementLabel.text = $"At ${(BigNumber)_data.unlockRevenueThreshold} lifetime";
            if (multiplierLabel != null)
                multiplierLabel.text = _unlocked
                    ? $"x{_data.globalEarningsMultiplier:0.##} all earnings · live"
                    : $"x{_data.globalEarningsMultiplier:0.##} all earnings";
            if (iconBadge != null)
                iconBadge.material = _unlocked ? iconUnlockedMat : iconLockedMat;
        }
    }
}
