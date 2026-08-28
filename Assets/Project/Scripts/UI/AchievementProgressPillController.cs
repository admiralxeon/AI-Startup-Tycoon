using TMPro;
using UnityEngine;
using AIStartupTycoon.Systems;

namespace AIStartupTycoon.UI
{
    /// <summary>Shows "unlocked/total" for the Trophy Case header pill.</summary>
    public class AchievementProgressPillController : MonoBehaviour
    {
        public TMP_Text label;

        private void Update()
        {
            if (label == null || AchievementManager.Instance == null) return;
            int total = AchievementManager.Instance.allAchievements != null ? AchievementManager.Instance.allAchievements.Count : 0;
            label.text = $"{AchievementManager.Instance.GetUnlockedCount()}/{total}";
        }
    }
}
