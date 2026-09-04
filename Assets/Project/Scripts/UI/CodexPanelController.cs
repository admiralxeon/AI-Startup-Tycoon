using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIStartupTycoon.Core;
using AIStartupTycoon.Data;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// A running glossary of the AI concepts the game has taught so far, one entry per
    /// model tier. Unlocked tiers show their real name and flavor text (the same copy
    /// the tier-unlock popup showed in the moment); locked ones show "???" and the
    /// revenue target, so the list itself previews what's still ahead. Rebuilt fresh
    /// every time it's opened rather than cached/event-driven, since it's a low-frequency
    /// reference screen, not something that needs to live-update while open.
    /// </summary>
    public class CodexPanelController : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;
        public Button closeButton;

        [Header("Rows")]
        public Transform entryContainer;
        public GameObject entryRowTemplate; // inactive template, instantiated per entry

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            closeButton.onClick.AddListener(Close);
        }

        public void Show()
        {
            BuildRows();
            panelRoot.SetActive(true);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }

        private void Close()
        {
            panelRoot.SetActive(false);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }

        private void BuildRows()
        {
            for (int i = entryContainer.childCount - 1; i >= 0; i--)
                Destroy(entryContainer.GetChild(i).gameObject);

            var gm = GameManager.Instance;
            if (gm == null || gm.allModelTiers == null) return;

            var ordered = new List<ModelTierData>(gm.allModelTiers);
            ordered.Sort((a, b) => a.tierOrder.CompareTo(b.tierOrder));

            foreach (var tier in ordered)
            {
                var row = Instantiate(entryRowTemplate, entryContainer);
                row.SetActive(true);

                bool unlocked = gm.IsModelTierUnlocked(tier);
                var nameLabel = row.transform.Find("NameLabel").GetComponent<TMP_Text>();
                var bodyLabel = row.transform.Find("BodyLabel").GetComponent<TMP_Text>();

                nameLabel.text = unlocked ? tier.tierName : "???";
                bodyLabel.text = unlocked
                    ? tier.unlockFlavorText
                    : $"Unlocks at ${(BigNumber)tier.unlockRevenueThreshold} lifetime revenue.";
            }
        }
    }
}
