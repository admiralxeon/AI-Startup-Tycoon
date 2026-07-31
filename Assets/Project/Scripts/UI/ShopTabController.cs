using UnityEngine;
using UnityEngine.UI;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Switches between the Team (Engineer) and Compute (Upgrade) shop panels,
    /// matching mockup 1A/1B's single-panel tab toggle instead of two side-by-side
    /// scroll lists. Both panels already exist (from ShopPanelController) - this
    /// just controls which one is visible.
    /// </summary>
    public class ShopTabController : MonoBehaviour
    {
        [Header("Tab Buttons")]
        public Button teamTabButton;
        public Button computeTabButton;

        [Header("Panels (the existing Engineer/Upgrade ScrollViews)")]
        public GameObject teamPanel;
        public GameObject computePanel;

        [Header("Tab Visual States")]
        [Tooltip("Applied to the active tab button's Image component.")]
        public Color activeTabColor = new Color(0.29f, 0.29f, 0.87f); // indigo, matches mockup
        [Tooltip("Applied to the inactive tab button's Image component.")]
        public Color inactiveTabColor = new Color(0.3f, 0.3f, 0.35f);

        [Header("Tab Badge (optional)")]
        public GameObject computeTabBadge;
        public Text computeTabBadgeLabel;

        private void Start()
        {
            teamTabButton.onClick.AddListener(ShowTeamTab);
            computeTabButton.onClick.AddListener(ShowComputeTab);
            ShowTeamTab(); // default open tab
        }

        private void Update()
        {
            RefreshBadge();
        }

        private void RefreshBadge()
        {
            if (computeTabBadge == null || Core.GameManager.Instance == null) return;
            int count = Core.GameManager.Instance.GetPurchasedUpgradeCount();
            computeTabBadge.SetActive(count > 0);
            if (computeTabBadgeLabel != null) computeTabBadgeLabel.text = count.ToString();
        }

        public void ShowTeamTab()
        {
            teamPanel.SetActive(true);
            computePanel.SetActive(false);
            SetTabVisual(teamTabButton, active: true);
            SetTabVisual(computeTabButton, active: false);
        }

        public void ShowComputeTab()
        {
            teamPanel.SetActive(false);
            computePanel.SetActive(true);
            SetTabVisual(teamTabButton, active: false);
            SetTabVisual(computeTabButton, active: true);
        }

        private void SetTabVisual(Button button, bool active)
        {
            Image img = button.GetComponent<Image>();
            if (img != null) img.color = active ? activeTabColor : inactiveTabColor;
        }
    }
}