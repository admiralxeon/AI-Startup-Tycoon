using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIStartupTycoon.Core;

namespace AIStartupTycoon.UI
{
    [Serializable]
    public class OnboardingStep
    {
        [Tooltip("Element to highlight in the default/landscape layout.")]
        public RectTransform target;
        [Tooltip("Optional alternate target used instead when the HUD is in portrait mode - e.g. a toggle button that only exists there (see ResponsiveHudLayout). Leave empty if the step's target is the same in both orientations.")]
        public RectTransform portraitTarget;
        [TextArea] public string title;
        [TextArea] public string body;
    }

    /// <summary>
    /// First-launch coach-mark sequence: dims the screen, draws a highlight frame around
    /// each step's target UI element, and shows a tooltip with Next/Skip. The target's
    /// screen position is read live every step (via world corners, same technique
    /// regardless of Canvas render mode) rather than cached, so the highlight correctly
    /// tracks wherever ResponsiveHudLayout has currently positioned that element in
    /// either portrait or landscape. Runs once per save file - completion is persisted
    /// through GameManager same as every other piece of save state.
    /// </summary>
    public class OnboardingController : MonoBehaviour
    {
        [Header("Steps (wire targets + copy in the Inspector)")]
        public List<OnboardingStep> steps;

        [Header("Overlay (panelRoot's own Image should be the full-screen dim scrim)")]
        public GameObject panelRoot;
        public RectTransform highlightFrame; // an Image repositioned/resized each step to frame the current target
        public float highlightPadding = 8f;

        [Header("Tooltip")]
        public RectTransform tooltipPanel;
        public TMP_Text titleLabel;
        public TMP_Text bodyLabel;
        public TMP_Text stepCounterLabel;
        public Button nextButton;
        public TMP_Text nextButtonLabel;
        public Button skipButton;

        private Canvas _canvas;
        private int _currentStep = -1;

        private void Start()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (panelRoot != null) panelRoot.SetActive(false);

            nextButton.onClick.AddListener(Advance);
            if (skipButton != null) skipButton.onClick.AddListener(Complete);

            // Deferred one frame so GameManager.Start() -> LoadGame() has definitely run
            // first, regardless of Unity's arbitrary Start() ordering between scripts -
            // same guard GameManager uses before firing OnOfflineEarningsApplied.
            StartCoroutine(BeginNextFrameIfNeeded());
        }

        private IEnumerator BeginNextFrameIfNeeded()
        {
            yield return null;
            if (GameManager.Instance != null && !GameManager.Instance.OnboardingCompleted && steps.Count > 0)
                Begin();
        }

        public void Begin()
        {
            // Marked complete the moment it starts, not when it finishes - this is meant to be
            // a strict "once ever" first-launch experience, so stopping Play mode (or quitting)
            // partway through must not bring it back next time. CompleteOnboarding() is
            // idempotent, so replaying it later from Settings doesn't re-trigger anything.
            if (GameManager.Instance != null) GameManager.Instance.CompleteOnboarding();

            _currentStep = -1;
            if (panelRoot != null) panelRoot.SetActive(true);
            Advance();
        }

        private void Advance()
        {
            _currentStep++;
            if (_currentStep >= steps.Count) { Complete(); return; }
            ShowStep(steps[_currentStep]);
        }

        private void Complete()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.CompleteOnboarding();
        }

        private void ShowStep(OnboardingStep step)
        {
            RectTransform target = ResolveTarget(step);
            if (target == null) { Advance(); return; } // misconfigured step - skip rather than soft-lock the player

            PositionHighlight(target);
            PositionTooltip(target);

            if (titleLabel != null) titleLabel.text = step.title;
            if (bodyLabel != null) bodyLabel.text = step.body;
            if (stepCounterLabel != null) stepCounterLabel.text = $"{_currentStep + 1}/{steps.Count}";
            if (nextButtonLabel != null) nextButtonLabel.text = _currentStep == steps.Count - 1 ? "GOT IT" : "NEXT";

            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayTap();
        }

        private RectTransform ResolveTarget(OnboardingStep step)
        {
            bool portrait = Screen.width < Screen.height;
            return (portrait && step.portraitTarget != null) ? step.portraitTarget : step.target;
        }

        private Camera OverlayCamera()
        {
            return (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
        }

        // World-space corners -> screen point -> local point in `relativeTo`. Works
        // regardless of how deeply nested or scaled the target is (e.g. companyPanelBG's
        // children, which ResponsiveHudLayout scales by 0.55x in portrait) since world
        // corners already bake in every ancestor's scale.
        private Vector2 WorldToLocal(RectTransform relativeTo, Vector3 worldPoint)
        {
            Camera cam = OverlayCamera();
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(relativeTo, screenPoint, cam, out Vector2 local);
            // ScreenPointToLocalPointInRectangle returns a point relative to relativeTo's pivot
            // (e.g. centered at (0,0) for the default (0.5,0.5)-pivot overlay) - subtracting
            // rect.min re-bases it to the rect's bottom-left corner, matching the
            // anchorMin=anchorMax=pivot=(0,0) convention highlightFrame/tooltipPanel use below.
            // Without this, every position was off by roughly half the overlay's width/height.
            return local - relativeTo.rect.min;
        }

        private void PositionHighlight(RectTransform target)
        {
            if (highlightFrame == null) return;
            RectTransform overlay = highlightFrame.parent as RectTransform;

            Vector3[] corners = new Vector3[4]; // GetWorldCorners order: 0=BL, 1=TL, 2=TR, 3=BR
            target.GetWorldCorners(corners);

            Vector2 min = WorldToLocal(overlay, corners[0]);
            Vector2 max = WorldToLocal(overlay, corners[2]);

            highlightFrame.anchorMin = highlightFrame.anchorMax = Vector2.zero;
            highlightFrame.pivot = Vector2.zero;
            highlightFrame.anchoredPosition = min - new Vector2(highlightPadding, highlightPadding);
            highlightFrame.sizeDelta = (max - min) + new Vector2(highlightPadding, highlightPadding) * 2f;
        }

        private void PositionTooltip(RectTransform target)
        {
            if (tooltipPanel == null) return;
            RectTransform overlay = tooltipPanel.parent as RectTransform;

            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 bottomLeft = WorldToLocal(overlay, corners[0]);
            Vector2 topLeft = WorldToLocal(overlay, corners[1]);
            Vector2 topRight = WorldToLocal(overlay, corners[2]);

            float targetCenterY = (topLeft.y + bottomLeft.y) * 0.5f;
            bool targetInUpperHalf = targetCenterY > overlay.rect.height * 0.5f; // both sides now bottom-left-relative, per WorldToLocal

            const float gap = 16f;
            const float screenPadding = 12f;

            tooltipPanel.anchorMin = tooltipPanel.anchorMax = Vector2.zero;
            tooltipPanel.pivot = new Vector2(0f, targetInUpperHalf ? 1f : 0f);

            float y = targetInUpperHalf ? bottomLeft.y - gap : topLeft.y + gap;

            float targetCenterX = (topLeft.x + topRight.x) * 0.5f;
            float x = Mathf.Clamp(
                targetCenterX - tooltipPanel.sizeDelta.x * 0.5f,
                screenPadding,
                overlay.rect.width - tooltipPanel.sizeDelta.x - screenPadding);

            tooltipPanel.anchoredPosition = new Vector2(x, y);
        }
    }
}
