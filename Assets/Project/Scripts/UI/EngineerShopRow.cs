using UnityEngine;
using UnityEngine.UI;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;
using AIStartupTycoon.Utils;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// A single row in the Engineer shop list. One prefab instance per EngineerData,
    /// spawned and managed by ShopPanelController. Self-updates its own cost/owned
    /// display each frame rather than requiring the parent to push updates per-row.
    /// </summary>
    public class EngineerShopRow : MonoBehaviour
    {
        [Header("UI References (wire up on the prefab)")]
        public TMP_Text monogramLabel; // 2-3 letter avatar badge, e.g. "SR"
        public TMP_Text nameLabel;
        public TMP_Text ownedLabel; // small "x{n}" badge next to the name
        public TMP_Text descriptionLabel; // EngineerData.flavorText
        public TMP_Text rateLabel; // "{perUnit}/s each - {total}/s total"
        public TMP_Text costLabel; // lives on the buy button itself
        public Button buyButton;
        public GameObject lockedOverlay; // shown/hidden based on unlock state
        public TMP_Text lockedRequirementLabel; // caption on the locked overlay itself
        [Tooltip("Shown only when unlocked but not yet affordable - an ETA at the current passive rate, so a stalled shop doesn't read as 'stuck'.")]
        public TMP_Text timeToAffordLabel;

        private EngineerData _data;

        public void Initialize(EngineerData data)
        {
            _data = data;
            nameLabel.text = data.engineerName;
            if (monogramLabel != null) monogramLabel.text = GetMonogram(data.engineerName);
            if (descriptionLabel != null) descriptionLabel.text = data.flavorText;
            buyButton.onClick.AddListener(OnBuyClicked);
            Refresh();
        }

        private void Update()
        {
            // Simple per-row polling. Fine at this list scale (a handful of rows);
            // if this ever grows to dozens of rows, switch to event-driven refresh only.
            Refresh();
        }

        private void Refresh()
        {
            if (_data == null || GameManager.Instance == null) return;

            bool unlocked = GameManager.Instance.IsEngineerUnlocked(_data);
            if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);
            if (lockedRequirementLabel != null) lockedRequirementLabel.text = $"Unlocks at ${_data.unlockRevenueThreshold:N0} revenue";
            buyButton.interactable = unlocked;

            int owned = GameManager.Instance.GetOwnedCount(_data);
            double cost = _data.GetCostForUnit(owned);

            // Always show the real cost, even while locked - LockedOverlay (opaque enough to
            // fully hide row content behind it) is what communicates the unlock requirement;
            // this used to duplicate that same message here too, which the overlay's ~80%
            // opacity wasn't quite opaque enough to hide, so the two texts visibly collided.
            costLabel.text = $"${(BigNumber)cost}";
            ownedLabel.text = $"x{owned}";
            if (rateLabel != null)
                rateLabel.text = $"{(BigNumber)_data.baseOutputPerSecond}/s each · {(BigNumber)_data.GetOutputForCount(owned)}/s total";

            RefreshTimeToAfford(unlocked, cost);
        }

        private void RefreshTimeToAfford(bool unlocked, double cost)
        {
            if (timeToAffordLabel == null) return;

            double currentRevenue = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentRevenue.ToDouble() : 0;
            bool affordable = currentRevenue >= cost;

            if (!unlocked || affordable)
            {
                timeToAffordLabel.gameObject.SetActive(false);
                return;
            }

            timeToAffordLabel.gameObject.SetActive(true);
            double rate = ShopRowUtils.GetEffectivePassiveRate();
            timeToAffordLabel.text = rate > 0
                ? $"Affordable in {ShopRowUtils.FormatTimeToAfford((cost - currentRevenue) / rate)}"
                : "Keep tapping to afford";
        }

        private void OnBuyClicked()
        {
            if (GameManager.Instance.TryHireEngineer(_data))
            {
                if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
            }
        }

        private static string GetMonogram(string name)
        {
            switch (name)
            {
                case "Intern": return "IN";
                case "Junior Dev": return "JR";
                case "Senior Dev": return "SR";
                case "Staff Engineer": return "ST";
                case "10x Engineer": return "10X";
                default:
                    var words = name.Split(' ');
                    return words.Length >= 2
                        ? $"{words[0][0]}{words[1][0]}".ToUpper()
                        : name.Substring(0, Mathf.Min(2, name.Length)).ToUpper();
            }
        }
    }
}
