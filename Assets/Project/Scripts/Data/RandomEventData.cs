using UnityEngine;

namespace AIStartupTycoon.Data
{
    /// <summary>
    /// Flavor-only random event popup (e.g. "A competitor cloned your product overnight").
    /// Effects are optional and always temporary, applied on top of existing stats -
    /// this system intentionally introduces NO new mechanics, per the GDD.
    /// </summary>
    [CreateAssetMenu(fileName = "Event_", menuName = "AIStartupTycoon/RandomEvent")]
    public class RandomEventData : ScriptableObject
    {
        [Header("Content")]
        public string headline;
        [TextArea] public string bodyText;
        public Sprite illustration;
        [Tooltip("Flavor text for the dismiss button, e.g. 'SHIP FASTER, THEN'. Leave blank to use a generic default.")]
        public string dismissButtonText;

        [Header("Effect (optional - leave at 1.0 for pure flavor, no gameplay impact)")]
        [Tooltip("Temporary multiplier to global earnings while active. 1.0 = no effect.")]
        public double temporaryEarningsMultiplier = 1.0;

        [Tooltip("How long the effect lasts, in seconds. Ignored if multiplier is 1.0.")]
        public float durationSeconds = 30f;

        [Header("Weighting")]
        [Tooltip("Relative chance of this event being picked vs others. Higher = more common.")]
        public float weight = 1f;

        [Tooltip("Minimum lifetime revenue before this event can trigger, to avoid gating early game.")]
        public double minRevenueThreshold = 0;
    }
}