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
        public Button prestigeTabButton;
        public Button achievementsTabButton;
        public Button questsTabButton;
        public Button storeTabButton;

        [Header("Panels (the existing Engineer/Upgrade/Prestige/Achievements/Quests/Store ScrollViews)")]
        public GameObject teamPanel;
        public GameObject computePanel;
        public GameObject prestigePanel;
        public GameObject achievementsPanel;
        public GameObject questsPanel;
        public GameObject storePanel;

        [Header("Tab Visual States")]
        [Tooltip("Applied to the active tab button's Image component.")]
        public Color activeTabColor = new Color(0.29f, 0.29f, 0.87f); // indigo, matches mockup
        [Tooltip("Applied to the inactive tab button's Image component.")]
        public Color inactiveTabColor = new Color(0.3f, 0.3f, 0.35f);

        [Header("Tab Badge (optional)")]
        public GameObject computeTabBadge;
        public Text computeTabBadgeLabel;
        public GameObject achievementsTabBadge;
        public Text achievementsTabBadgeLabel;
        public GameObject questsTabBadge;
        public Text questsTabBadgeLabel;

        private void Start()
        {
            teamTabButton.onClick.AddListener(ShowTeamTab);
            computeTabButton.onClick.AddListener(ShowComputeTab);
            if (prestigeTabButton != null) prestigeTabButton.onClick.AddListener(ShowPrestigeTab);
            if (achievementsTabButton != null) achievementsTabButton.onClick.AddListener(ShowAchievementsTab);
            if (questsTabButton != null) questsTabButton.onClick.AddListener(ShowQuestsTab);
            if (storeTabButton != null) storeTabButton.onClick.AddListener(ShowStoreTab);
            ShowTeamTab(); // default open tab
        }

        private void Update()
        {
            RefreshBadge();
        }

        private void RefreshBadge()
        {
            if (computeTabBadge != null && Core.GameManager.Instance != null)
            {
                int count = Core.GameManager.Instance.GetPurchasedUpgradeCount();
                computeTabBadge.SetActive(count > 0);
                if (computeTabBadgeLabel != null) computeTabBadgeLabel.text = count.ToString();
            }

            if (achievementsTabBadge != null && Systems.AchievementManager.Instance != null)
            {
                int unlocked = Systems.AchievementManager.Instance.GetUnlockedCount();
                achievementsTabBadge.SetActive(unlocked > 0);
                if (achievementsTabBadgeLabel != null) achievementsTabBadgeLabel.text = unlocked.ToString();
            }

            if (questsTabBadge != null && Systems.QuestManager.Instance != null)
            {
                int readyCount = 0;
                for (int i = 0; i < Systems.QuestManager.Instance.slotCount; i++)
                    if (Systems.QuestManager.Instance.IsSlotComplete(i)) readyCount++;

                questsTabBadge.SetActive(readyCount > 0);
                if (questsTabBadgeLabel != null) questsTabBadgeLabel.text = readyCount.ToString();
            }
        }

        public void ShowTeamTab() => ShowTab(teamPanel, teamTabButton);
        public void ShowComputeTab() => ShowTab(computePanel, computeTabButton);
        public void ShowPrestigeTab() => ShowTab(prestigePanel, prestigeTabButton);
        public void ShowAchievementsTab() => ShowTab(achievementsPanel, achievementsTabButton);
        public void ShowQuestsTab() => ShowTab(questsPanel, questsTabButton);
        public void ShowStoreTab() => ShowTab(storePanel, storeTabButton);

        private void ShowTab(GameObject panelToShow, Button buttonToActivate)
        {
            SetPanelActive(teamPanel, panelToShow);
            SetPanelActive(computePanel, panelToShow);
            SetPanelActive(prestigePanel, panelToShow);
            SetPanelActive(achievementsPanel, panelToShow);
            SetPanelActive(questsPanel, panelToShow);
            SetPanelActive(storePanel, panelToShow);

            SetTabVisual(teamTabButton, teamTabButton == buttonToActivate);
            SetTabVisual(computeTabButton, computeTabButton == buttonToActivate);
            SetTabVisual(prestigeTabButton, prestigeTabButton == buttonToActivate);
            SetTabVisual(achievementsTabButton, achievementsTabButton == buttonToActivate);
            SetTabVisual(questsTabButton, questsTabButton == buttonToActivate);
            SetTabVisual(storeTabButton, storeTabButton == buttonToActivate);

            if (Core.UIAudioManager.Instance != null) Core.UIAudioManager.Instance.PlaySwitch();
        }

        private static void SetPanelActive(GameObject panel, GameObject panelToShow)
        {
            if (panel != null) panel.SetActive(panel == panelToShow);
        }

        private void SetTabVisual(Button button, bool active)
        {
            if (button == null) return;
            Image img = button.GetComponent<Image>();
            if (img != null) img.color = active ? activeTabColor : inactiveTabColor;
        }
    }
}
