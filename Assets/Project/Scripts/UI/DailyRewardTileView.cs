using TMPro;
using UnityEngine;

namespace AIStartupTycoon.UI
{
    public enum DailyRewardTileState { Claimed, Current, Locked }

    /// <summary>One day's tile in the Daily Reward ladder grid.</summary>
    public class DailyRewardTileView : MonoBehaviour
    {
        public UIRoundedGraphic background;
        public TMP_Text dayLabel;
        public TMP_Text rewardLabel;
        [Tooltip("A small dot graphic shown in the corner when claimed - not a text glyph, since the Baloo2 font has no checkmark character.")]
        public GameObject checkmark;

        public Material claimedMaterial;
        public Material currentMaterial;
        public Material lockedMaterial;

        public void SetState(DailyRewardTileState state, string dayText, string rewardText)
        {
            dayLabel.text = dayText;
            rewardLabel.text = rewardText;
            if (checkmark != null) checkmark.SetActive(state == DailyRewardTileState.Claimed);

            if (background == null) return;
            background.material = state switch
            {
                DailyRewardTileState.Claimed => claimedMaterial,
                DailyRewardTileState.Current => currentMaterial,
                _ => lockedMaterial
            };
        }
    }
}
