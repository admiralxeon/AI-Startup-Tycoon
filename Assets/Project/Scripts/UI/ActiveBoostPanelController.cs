using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Displays currently active temporary boosts in the Company column (mockup's
    /// "ACTIVE BOOSTS" panel). Currently wired to RewardedBoostController's ad-based
    /// 2x earnings boost; add more entries here later (e.g. a Hype Cycle consumable)
    /// following the same pattern - one row shown/hidden per boost source.
    /// </summary>
    public class ActiveBoostsPanelController : MonoBehaviour
    {
        [Header("Boost Sources")]
        public RewardedBoostController adBoost;

        [Header("Ad Boost Row")]
        public GameObject adBoostRow;
        public TextMeshProUGUI adBoostLabel;

        [Header("Empty State")]
        public GameObject noActiveBoostsLabel; // "No other boosts running"

        private void Start()
        {
            if (adBoost != null) adBoost.OnBoostStateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (adBoost != null) adBoost.OnBoostStateChanged -= Refresh;
        }

        private void Update()
        {
            // Countdown text needs per-frame refresh while active; cheap at this scale.
            if (adBoost != null && adBoost.IsBoostActive)
                UpdateAdBoostLabel();
        }

        private void Refresh()
        {
            bool adBoostActive = adBoost != null && adBoost.IsBoostActive;

            if (adBoostRow != null) adBoostRow.SetActive(adBoostActive);
            if (adBoostActive) UpdateAdBoostLabel();

            // Extend this condition as more boost sources are added.
            bool anyActive = adBoostActive;
            if (noActiveBoostsLabel != null) noActiveBoostsLabel.SetActive(!anyActive);
        }

        private void UpdateAdBoostLabel()
        {
            if (adBoostLabel == null) return;
            int seconds = Mathf.CeilToInt(adBoost.RemainingBoostSeconds);
            adBoostLabel.text = $"2x Earnings - {seconds}s remaining";
        }
    }
}