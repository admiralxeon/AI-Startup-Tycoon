using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Drives every combo-progress visual on the HQ tap orb: the conic ring around it
    /// AND the linear "COMBO ... tap faster" bar beneath it (the mock's Direction B
    /// pairs both). Both track ClickComboManager.ComboCount / maxComboSteps and share
    /// the same cool/hot recolor threshold. Purely a presentation layer over
    /// ClickComboManager, same pattern as the old ComboMeterController.
    /// </summary>
    public class ComboRingController : MonoBehaviour
    {
        [Header("Ring (around the orb)")]
        public Image ring; // Image Type=Filled, Method=Radial360, Origin=Top, Clockwise
        //public GameObject haloGlow; // optional halo shown only while combo > 0

        [Header("Linear bar (below the orb)")]
        public Image linearFill; // Image Type=Filled, Method=Horizontal, Origin=Left
        public TMP_Text comboStatusLabel; // right-aligned: "tap faster" or "x1.15 · 5"

        [Header("Colors")]
        public Color coolColor = new Color(0.49f, 0.83f, 0.99f); // #7DD3FC
        public Color hotColor = new Color(0.98f, 0.75f, 0.14f);  // #FBBF24
        public Color idleColor = new Color(0.28f, 0.34f, 0.42f); // #475569
        public int hotThresholdSteps = 20;

        private void Start()
        {
            if (ring != null) ring.fillAmount = 0f;
            if (linearFill != null) linearFill.fillAmount = 0f;
            //if (haloGlow != null) haloGlow.SetActive(false);
            Refresh(0, 1.0);

            if (ClickComboManager.Instance != null)
                ClickComboManager.Instance.OnComboChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (ClickComboManager.Instance != null)
                ClickComboManager.Instance.OnComboChanged -= Refresh;
        }

        private void Refresh(int comboCount, double multiplier)
        {
            int maxSteps = ClickComboManager.Instance != null ? ClickComboManager.Instance.maxComboSteps : 40;
            float pct = maxSteps > 0 ? Mathf.Clamp01((float)comboCount / maxSteps) : 0f;
            bool comboOn = comboCount > 0;
            Color c = !comboOn ? idleColor : (comboCount > hotThresholdSteps ? hotColor : coolColor);

            if (ring != null)
            {
                ring.fillAmount = pct;
                ring.color = c;
            }
            //if (haloGlow != null) haloGlow.SetActive(comboOn);

            if (linearFill != null)
            {
                linearFill.fillAmount = Mathf.Max(0.02f, pct);
                linearFill.color = c;
            }
            if (comboStatusLabel != null)
            {
                comboStatusLabel.text = comboOn ? $"x{multiplier:0.00} · {comboCount}" : "tap faster";
                comboStatusLabel.color = c;
            }
        }
    }
}
