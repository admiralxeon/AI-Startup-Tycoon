using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Top-level bottom nav: HQ / Shop / Quests / Awards / Settings. Same shape as
    /// ShopTabController (buttons + panel roots + active-state visuals) but one level
    /// up, switching between whole screens instead of shop sub-tabs.
    /// </summary>
    public class MainNavController : MonoBehaviour
    {
        [System.Serializable]
        public class NavEntry
        {
            public string label;
            public Button button;
            public GameObject screenRoot;
            public UIRoundedGraphic dot;   // the small rounded-square background slot, recolored when active
            public Image icon;             // glyph layered on top of dot, tinted the same as labelText
            public TMP_Text labelText;
        }

        public NavEntry[] entries;
        public Material activeDotMaterial;
        public Color inactiveDotColor = new Color(1f, 1f, 1f, 0.06f);
        public Color activeLabelColor = new Color(0.78f, 0.82f, 0.99f); // #C7D2FE
        public Color inactiveLabelColor = new Color(0.39f, 0.45f, 0.55f); // #64748B

        private int _current = -1;

        private void Start()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                int idx = i;
                if (entries[i].button != null) entries[i].button.onClick.AddListener(() => Show(idx));
            }
            Show(0);
        }

        public void Show(int index)
        {
            if (index < 0 || index >= entries.Length) return;
            _current = index;
            for (int i = 0; i < entries.Length; i++)
            {
                bool on = i == index;
                if (entries[i].screenRoot != null) entries[i].screenRoot.SetActive(on);
                if (entries[i].dot != null)
                {
                    if (on && activeDotMaterial != null) entries[i].dot.material = activeDotMaterial;
                    entries[i].dot.color = on ? Color.white : inactiveDotColor;
                }
                if (entries[i].labelText != null)
                    entries[i].labelText.color = on ? activeLabelColor : inactiveLabelColor;
                if (entries[i].icon != null)
                    entries[i].icon.color = on ? activeLabelColor : inactiveLabelColor;
            }
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlaySwitch();
        }
    }
}
