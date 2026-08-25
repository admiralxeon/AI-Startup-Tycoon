using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AIStartupTycoon.UI
{
    [Serializable]
    public class BackButtonTarget
    {
        [Tooltip("The panel/popup this entry represents. Its activeInHierarchy state is how the handler knows whether this entry is currently 'open'.")]
        public GameObject panelRoot;
        [Tooltip("Optional: if set, pressing back invokes this button instead of directly deactivating panelRoot - use this when closing needs more than SetActive(false) (e.g. the portrait Shop toggle also syncs a scrim and a button label).")]
        public Button closeButton;
    }

    /// <summary>
    /// Handles the Android hardware/gesture back button (Unity maps it to KeyCode.Escape).
    /// Closes whichever tracked panel is currently open, highest-priority entry first. If
    /// nothing in the list is open, shows a confirm-to-exit panel instead of quitting
    /// immediately (a bare accidental back-press shouldn't kill the app); pressing back again
    /// while that confirm is already up exits directly, the standard Android "press back twice"
    /// pattern. Deliberately does not include OnboardingController's panel: the first-launch
    /// tutorial shouldn't be back-button-dismissable.
    /// </summary>
    public class AndroidBackButtonHandler : MonoBehaviour
    {
        [Header("Checked top-to-bottom; the first one found open is closed")]
        public List<BackButtonTarget> targetsInPriorityOrder;

        [Header("Exit Confirmation (shown when nothing above is open)")]
        public GameObject exitConfirmPanel;
        public Button exitConfirmYesButton;
        public Button exitConfirmCancelButton;

        private void Start()
        {
            if (exitConfirmYesButton != null) exitConfirmYesButton.onClick.AddListener(Application.Quit);
            if (exitConfirmCancelButton != null) exitConfirmCancelButton.onClick.AddListener(() => exitConfirmPanel.SetActive(false));
            if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            foreach (var target in targetsInPriorityOrder)
            {
                if (target.panelRoot == null || !target.panelRoot.activeInHierarchy) continue;

                if (target.closeButton != null) target.closeButton.onClick.Invoke();
                else target.panelRoot.SetActive(false);
                return; // only ever close the topmost open panel per press
            }

            if (exitConfirmPanel != null)
            {
                if (exitConfirmPanel.activeInHierarchy) { Application.Quit(); return; } // pressed back twice - exit directly
                exitConfirmPanel.SetActive(true);
                return;
            }

            Application.Quit(); // no exit confirm wired - fall back to the old immediate-quit behavior
        }
    }
}
