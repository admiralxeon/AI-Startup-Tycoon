using UnityEngine;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// A simple two-state toggle-switch visual: a pill-shaped track (color-coded on/off)
    /// with a circular knob that slides to the appropriate side. No animation - matches
    /// this project's existing "swap material/position outright" convention for state changes.
    /// </summary>
    public class ToggleSwitchView : MonoBehaviour
    {
        public UIRoundedGraphic track;
        public RectTransform knob;
        public Material onMaterial;
        public Material offMaterial;
        public float knobOnX = 18f;
        public float knobOffX = -18f;

        public void SetState(bool isOn)
        {
            if (track != null) track.material = isOn ? onMaterial : offMaterial;
            if (knob != null) knob.anchoredPosition = new Vector2(isOn ? knobOnX : knobOffX, knob.anchoredPosition.y);
        }
    }
}
