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

        [Header("Containers (assign the 'Content' object of each ScrollRect)")]
        public Transform engineerListContainer;
        public Transform upgradeListContainer;

        private void Start()
        {
            SpawnEngineerRows();
            SpawnUpgradeRows();
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
    }
}