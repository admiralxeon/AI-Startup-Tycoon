using UnityEngine;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Spawns one EngineerShopRow per EngineerData and one ComputeUpgradeShopRow per
    /// ComputeUpgradeData under their respective content containers (typically each
    /// container is the content of a ScrollRect for a scrollable list).
    /// Attach this to a manager GameObject and wire references in the Inspector.
    /// </summary>
    public class ShopPanelController : MonoBehaviour
    {
        [Header("Prefabs")]
        public EngineerShopRow engineerRowPrefab;
        public ComputeUpgradeShopRow upgradeRowPrefab;
        public ReputationUpgradeShopRow reputationUpgradeRowPrefab;
        public AchievementShopRow achievementRowPrefab;
        public QuestShopRow questRowPrefab;
        public IAPShopRow iapRowPrefab;
        public ModelTierShopRow modelTierRowPrefab;

        [Header("Containers (assign the 'Content' object of each ScrollRect)")]
        public Transform engineerListContainer;
        public Transform upgradeListContainer;
        public Transform reputationUpgradeListContainer;
        public Transform achievementListContainer;
        public Transform questListContainer;
        public Transform storeListContainer;
        public Transform modelTierListContainer;

        private void Start()
        {
            SpawnEngineerRows();
            SpawnUpgradeRows();
            SpawnReputationUpgradeRows();
            SpawnAchievementRows();
            SpawnIAPRows();
            SpawnModelTierRows();
            // Deferred one frame: unlike the other Spawn* calls above (whose data comes from
            // Inspector-serialized fields, ready the instant their singleton's Awake() runs),
            // QuestManager also depends on GameManager.LoadGame() having restored slot state,
            // which isn't guaranteed to have happened yet at this point in the frame.
            StartCoroutine(SpawnQuestRowsNextFrame());
        }

        private System.Collections.IEnumerator SpawnQuestRowsNextFrame()
        {
            yield return null;
            SpawnQuestRows();
        }

        private void SpawnEngineerRows()
        {
            if (GameManager.Instance == null || GameManager.Instance.allEngineers == null) return;

            foreach (EngineerData engineer in GameManager.Instance.allEngineers)
            {
                EngineerShopRow row = Instantiate(engineerRowPrefab, engineerListContainer);
                row.Initialize(engineer);
            }
        }

        private void SpawnUpgradeRows()
        {
            if (GameManager.Instance == null || GameManager.Instance.allComputeUpgrades == null) return;

            foreach (ComputeUpgradeData upgrade in GameManager.Instance.allComputeUpgrades)
            {
                ComputeUpgradeShopRow row = Instantiate(upgradeRowPrefab, upgradeListContainer);
                row.Initialize(upgrade);
            }
        }

        private void SpawnReputationUpgradeRows()
        {
            if (GameManager.Instance == null || GameManager.Instance.allReputationUpgrades == null) return;

            foreach (ReputationUpgradeData upgrade in GameManager.Instance.allReputationUpgrades)
            {
                ReputationUpgradeShopRow row = Instantiate(reputationUpgradeRowPrefab, reputationUpgradeListContainer);
                row.Initialize(upgrade);
            }
        }

        private void SpawnAchievementRows()
        {
            if (Systems.AchievementManager.Instance == null || Systems.AchievementManager.Instance.allAchievements == null) return;

            foreach (var achievement in Systems.AchievementManager.Instance.allAchievements)
            {
                AchievementShopRow row = Instantiate(achievementRowPrefab, achievementListContainer);
                row.Initialize(achievement);
            }
        }

        /// <summary>One row per SLOT INDEX, not per QuestData asset - QuestManager rerolls
        /// each slot's content over time, and the row polls by index to follow along.</summary>
        private void SpawnQuestRows()
        {
            if (Systems.QuestManager.Instance == null || questRowPrefab == null) return;

            for (int i = 0; i < Systems.QuestManager.Instance.slotCount; i++)
            {
                QuestShopRow row = Instantiate(questRowPrefab, questListContainer);
                row.Initialize(i);
            }

            // The "daily reward ready" card is a static scene child of this same container
            // (not spawned here), so it starts out ahead of these freshly-instantiated rows
            // in sibling order - push it below them to match the mockup's bottom-of-list spot.
            Transform dailyRewardCard = questListContainer.Find("DailyRewardQuestCard");
            if (dailyRewardCard != null) dailyRewardCard.SetAsLastSibling();
        }

        private void SpawnIAPRows()
        {
            if (IAPManager.Instance == null || IAPManager.Instance.allProducts == null || iapRowPrefab == null) return;

            foreach (var product in IAPManager.Instance.allProducts)
            {
                if (product == null) continue;
                IAPShopRow row = Instantiate(iapRowPrefab, storeListContainer);
                row.Initialize(product);
            }
        }

        private void SpawnModelTierRows()
        {
            if (GameManager.Instance == null || GameManager.Instance.allModelTiers == null || modelTierRowPrefab == null) return;

            foreach (ModelTierData tier in GameManager.Instance.allModelTiers)
            {
                ModelTierShopRow row = Instantiate(modelTierRowPrefab, modelTierListContainer);
                row.Initialize(tier);
            }
        }
    }
}