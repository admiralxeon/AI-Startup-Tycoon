using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;

namespace AIStartupTycoon.UI
{
   
    public class TestUIController : MonoBehaviour
    {
        [Header("Click")]
        public Button clickButton;
        public TMP_Text revenueLabel; // swap for TMP_Text if using TextMeshPro

        [Header("Hire (test with one engineer first)")]
        public EngineerData testEngineer;
        public Button hireButton;
        public TMP_Text hireCostLabel;
        public TMP_Text ownedCountLabel;

        [Header("Passive Income Display")]
        public TMP_Text passivePerSecondLabel;

        private void Start()
        {
            clickButton.onClick.AddListener(OnClickButtonPressed);
            hireButton.onClick.AddListener(OnHireButtonPressed);

            CurrencyManager.Instance.OnRevenueChanged += OnRevenueChanged;
            GameManager.Instance.OnEngineerCountChanged += OnEngineerCountChanged;

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnRevenueChanged -= OnRevenueChanged;
            if (GameManager.Instance != null)
                GameManager.Instance.OnEngineerCountChanged -= OnEngineerCountChanged;
        }

        private void Update()
        {
            // Cheap polling for hire button interactability + passive display.
            // Fine for a test harness; the real UI should be event-driven only.
            if (testEngineer != null)
            {
                bool unlocked = GameManager.Instance.IsEngineerUnlocked(testEngineer);
                hireButton.interactable = unlocked;

                double cost = testEngineer.GetCostForUnit(GameManager.Instance.GetOwnedCount(testEngineer));
                hireCostLabel.text = unlocked ? $"Hire: ${cost:N0}" : $"Locked (need ${testEngineer.unlockRevenueThreshold:N0})";
            }
        }

        private void OnClickButtonPressed()
        {
            CurrencyManager.Instance.EarnFromClick();
        }

        private void OnHireButtonPressed()
        {
            if (testEngineer == null) return;
            GameManager.Instance.TryHireEngineer(testEngineer);
        }

        private void OnRevenueChanged(Utils.BigNumber newRevenue)
        {
            revenueLabel.text = $"${newRevenue}";
        }

        private void OnEngineerCountChanged(EngineerData engineer, int newCount)
        {
            if (engineer == testEngineer)
                ownedCountLabel.text = $"Owned: {newCount}";
        }

        private void RefreshAll()
        {
            revenueLabel.text = $"${CurrencyManager.Instance.CurrentRevenue}";
            if (testEngineer != null)
                ownedCountLabel.text = $"Owned: {GameManager.Instance.GetOwnedCount(testEngineer)}";
        }
    }
}