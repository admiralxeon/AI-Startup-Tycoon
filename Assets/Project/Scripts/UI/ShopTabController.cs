using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        public Button modelsTabButton;

        [Header("Panels (the existing Engineer/Upgrade/Prestige/Achievements/Quests/Store/Models ScrollViews)")]
        public GameObject teamPanel;
        public GameObject computePanel;
        public GameObject prestigePanel;
        public GameObject achievementsPanel;
        public GameObject questsPanel;
        public GameObject storePanel;
        public GameObject modelsPanel;

        [Header("Tab Visual States")]
        [Tooltip("Applied to the active tab button's Image component.")]
        public Color activeTabColor = new Color(0.29f, 0.29f, 0.87f); // indigo, matches mockup
        [Tooltip("Applied to the inactive tab button's Image component.")]
        public Color inactiveTabColor = new Color(0.3f, 0.3f, 0.35f);
        public Color activeLabelColor = Color.white;
        public Color inactiveLabelColor = new Color(0.39f, 0.45f, 0.55f); // #64748B

        [Header("Category Subtitle (optional)")]
        public TMP_Text categorySubtitleLabel;
        [TextArea] public string teamSubtitle = "Engineers ship whether the app is open or not. Cost climbs 15% per hire.";
        [TextArea] public string computeSubtitle = "Compute upgrades boost every engineer's output at once.";
        [TextArea] public string modelsSubtitle = "Unlock stronger models as your lifetime revenue grows.";
        [TextArea] public string prestigeSubtitle = "Spend reputation on permanent, run-spanning bonuses.";

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
            if (modelsTabButton != null) modelsTabButton.onClick.AddListener(ShowModelsTab);
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

        public void ShowTeamTab() => ShowTab(teamPanel, teamTabButton, teamSubtitle);
        public void ShowComputeTab() => ShowTab(computePanel, computeTabButton, computeSubtitle);
        public void ShowPrestigeTab() => ShowTab(prestigePanel, prestigeTabButton, prestigeSubtitle);
        public void ShowAchievementsTab() => ShowTab(achievementsPanel, achievementsTabButton, "");
        public void ShowQuestsTab() => ShowTab(questsPanel, questsTabButton, "");
        public void ShowStoreTab() => ShowTab(storePanel, storeTabButton, "");
        public void ShowModelsTab() => ShowTab(modelsPanel, modelsTabButton, modelsSubtitle);

        private void ShowTab(GameObject panelToShow, Button buttonToActivate, string subtitle)
        {
            if (categorySubtitleLabel != null) categorySubtitleLabel.text = subtitle;
            SetPanelActive(teamPanel, panelToShow);
            SetPanelActive(computePanel, panelToShow);
            SetPanelActive(prestigePanel, panelToShow);
            SetPanelActive(achievementsPanel, panelToShow);
            SetPanelActive(questsPanel, panelToShow);
            SetPanelActive(storePanel, panelToShow);
            SetPanelActive(modelsPanel, panelToShow);

            SetTabVisual(teamTabButton, teamTabButton == buttonToActivate);
            SetTabVisual(computeTabButton, computeTabButton == buttonToActivate);
            SetTabVisual(prestigeTabButton, prestigeTabButton == buttonToActivate);
            SetTabVisual(achievementsTabButton, achievementsTabButton == buttonToActivate);
            SetTabVisual(questsTabButton, questsTabButton == buttonToActivate);
            SetTabVisual(storeTabButton, storeTabButton == buttonToActivate);
            SetTabVisual(modelsTabButton, modelsTabButton == buttonToActivate);

            if (Core.UIAudioManager.Instance != null) Core.UIAudioManager.Instance.PlaySwitch();
        }

        private static void SetPanelActive(GameObject panel, GameObject panelToShow)
        {
            if (panel != null) panel.SetActive(panel == panelToShow);
        }

        private void SetTabVisual(Button button, bool active)
        {
            if (button == null) return;
            Graphic img = button.GetComponent<Graphic>();
            if (img != null) img.color = active ? activeTabColor : inactiveTabColor;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = active ? activeLabelColor : inactiveLabelColor;
        }
    }
}
