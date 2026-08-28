using UnityEngine;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Nudges this RectTransform in from one screen edge to clear a notch/camera-cutout
    /// (top) or Android's gesture nav bar (bottom), without touching its designed
    /// height or full-width stretch - unlike anchoring directly to Screen.safeArea
    /// (meant for a full-bleed content container), this only offsets the bar inward
    /// from whichever edge it's already pinned to. Re-applies whenever the safe area
    /// changes (rotation, foldable state, multi-window resize).
    /// </summary>
    public class SafeAreaEdgeInset : MonoBehaviour
    {
        public enum Edge { Top, Bottom }
        public Edge edge;

        private RectTransform _rect;
        private Canvas _canvas;
        private float _baseAnchoredY;
        private Rect _lastSafeArea = new Rect(-1, -1, -1, -1);

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _canvas = _rect.GetComponentInParent<Canvas>();
            _baseAnchoredY = _rect.anchoredPosition.y;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea) Apply();
        }

        private void Apply()
        {
            _lastSafeArea = Screen.safeArea;

            float insetPixels = edge == Edge.Top
                ? Screen.height - Screen.safeArea.yMax
                : Screen.safeArea.yMin;

            float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
            float insetLocal = insetPixels / Mathf.Max(scaleFactor, 0.0001f);

            Vector2 pos = _rect.anchoredPosition;
            pos.y = edge == Edge.Top ? _baseAnchoredY - insetLocal : _baseAnchoredY + insetLocal;
            _rect.anchoredPosition = pos;
        }
    }
}
