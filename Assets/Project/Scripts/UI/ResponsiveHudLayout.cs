using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Repositions the main HUD for portrait aspects. The HUD was built assuming a
    /// wide/landscape canvas (Company Panel and Shop Panel alone span ~1500 reference
    /// units against a 390-wide reference resolution), so portrait needs a genuinely
    /// different arrangement, not just a rescale: a compact 2-row top bar, a smaller
    /// click button, and the Shop moved behind a toggle button (there isn't enough
    /// vertical room to show Company stats + Shop + click button all at once in a
    /// single portrait screen without scrolling).
    /// Landscape values are captured at Awake and restored exactly, so this never
    /// permanently alters the landscape layout.
    /// </summary>
    public class ResponsiveHudLayout : MonoBehaviour
    {
        [Header("Header")]
        public RectTransform logo;
        public RectTransform title;
        public RectTransform subtitle;

        [Header("Top Bar Stats")]
        public RectTransform revenueCard;
        public RectTransform revenueLabel;
        public RectTransform ipoButton;
        public RectTransform passiveRateBadge;
        public RectTransform valuation;
        public RectTransform reputation;

        [Header("Main Content")]
        public RectTransform clickButton;
        public RectTransform watchAdButton;
        public RectTransform companyPanelBG;
        public RectTransform shopPanelUI;

        [Header("IPO Screen (authored for landscape - 520 wide, wider than the ~390-475 portrait canvas)")]
        public RectTransform ipoDialogCard;
        public RectTransform ipoResultsCard;

        [Header("Portrait-only Shop Toggle")]
        public GameObject shopToggleButton;
        public TMP_Text shopToggleLabel;

        [Header("Store Toggle (visible in both orientations - Store tab button itself is hidden)")]
        public RectTransform storeToggleButton;

        private Image _shopBackground;
        private Color _shopBackgroundOriginalColor;
        private GameObject _portraitScrim;

        private struct Original
        {
            public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta;
            public Vector3 localScale;
        }

        private RectTransform[] _tracked;
        private Original[] _originals;
        private bool _isPortrait;
        private int _lastWidth, _lastHeight;
        private float _titleOriginalFontSize, _subtitleOriginalFontSize, _revenueLabelOriginalFontSize;
        private TextWrappingModes _titleOriginalWrap, _subtitleOriginalWrap;
        private TMP_Text _titleText, _subtitleText, _revenueText;

        private void Awake() => EnsureInitialized();

        // Guarded so it's safe to call from the debug test hooks too, in case AddComponent
        // hasn't invoked Awake yet by the time a caller in the same editor tick uses them.
        private void EnsureInitialized()
        {
            // Checked on _originals, not _tracked: a domain reload (recompiling while
            // these objects exist in the editor) preserves _tracked because it's an
            // array of UnityEngine.Object references, but wipes _originals since it's a
            // plain struct array - checking only _tracked left this returning early
            // with _originals still null.
            if (_tracked != null && _originals != null) return;

            _tracked = new[]
            {
                logo, title, subtitle, revenueCard, revenueLabel, ipoButton,
                passiveRateBadge, valuation, reputation, clickButton, watchAdButton,
                companyPanelBG, shopPanelUI, ipoDialogCard, ipoResultsCard
            };
            _originals = new Original[_tracked.Length];
            for (int i = 0; i < _tracked.Length; i++)
            {
                var rt = _tracked[i];
                _originals[i] = new Original
                {
                    anchorMin = rt.anchorMin,
                    anchorMax = rt.anchorMax,
                    pivot = rt.pivot,
                    anchoredPosition = rt.anchoredPosition,
                    sizeDelta = rt.sizeDelta,
                    localScale = rt.localScale
                };
            }

            _titleText = title.GetComponent<TMP_Text>();
            _subtitleText = subtitle.GetComponent<TMP_Text>();
            _revenueText = revenueLabel.GetComponent<TMP_Text>();
            _titleOriginalFontSize = _titleText.fontSize;
            _subtitleOriginalFontSize = _subtitleText.fontSize;
            _revenueLabelOriginalFontSize = _revenueText.fontSize;
            _titleOriginalWrap = _titleText.textWrappingMode;
            _subtitleOriginalWrap = _subtitleText.textWrappingMode;

            _shopBackground = shopPanelUI.GetComponent<Image>();
            _shopBackgroundOriginalColor = _shopBackground.color;

            if (shopToggleButton != null)
            {
                var btn = shopToggleButton.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(ToggleShop);
            }

            if (storeToggleButton != null)
            {
                var btn = storeToggleButton.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(OpenStore);
            }
        }

        private void Start()
        {
            EnsureInitialized();
            Apply(Screen.width < Screen.height);
        }

        /// <summary>Editor-only test hooks for verifying the layout without Play mode.</summary>
        public void DebugApplyPortrait() { EnsureInitialized(); Apply(true); }
        public void DebugApplyLandscape() { EnsureInitialized(); Apply(false); }

        private void Update()
        {
            if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;
            Apply(Screen.width < Screen.height);
        }

        private void ToggleShop()
        {
            bool nowOpen = !shopPanelUI.gameObject.activeSelf;
            shopPanelUI.gameObject.SetActive(nowOpen);
            if (_portraitScrim != null) _portraitScrim.SetActive(nowOpen);
            if (shopToggleLabel != null) shopToggleLabel.text = nowOpen ? "CLOSE" : "SHOP";
        }

        /// <summary>Store gets its own persistent entry point instead of a 6th tab in the
        /// already-crowded shop tab bar - opens the shop (same as the Shop toggle would, if
        /// it's currently closed) and jumps straight to the Store tab.</summary>
        private void OpenStore()
        {
            if (!shopPanelUI.gameObject.activeSelf)
            {
                shopPanelUI.gameObject.SetActive(true);
                if (_isPortrait)
                {
                    if (_portraitScrim != null) _portraitScrim.SetActive(true);
                    if (shopToggleLabel != null) shopToggleLabel.text = "CLOSE";
                }
            }

            var tabController = shopPanelUI.GetComponent<ShopTabController>();
            if (tabController != null) tabController.ShowStoreTab();
        }

        private void Set(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, float scale = 1f)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one * scale;
        }

        // Repositions by top-left corner while leaving sizeDelta at its original value -
        // only scale shrinks it, so children using fixed (non-stretch) offsets stay put
        // relative to their parent instead of being pushed outside the new bounds.
        private void SetScaled(RectTransform rt, Vector2 anchorPivot, Vector2 topLeftPos, float scale)
        {
            rt.anchorMin = anchorPivot;
            rt.anchorMax = anchorPivot;
            rt.pivot = anchorPivot;
            rt.anchoredPosition = topLeftPos;
            rt.localScale = Vector3.one * scale;
        }

        private void Apply(bool portrait)
        {
            _isPortrait = portrait;
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            if (!portrait)
            {
                for (int i = 0; i < _tracked.Length; i++)
                {
                    var rt = _tracked[i];
                    var o = _originals[i];
                    rt.anchorMin = o.anchorMin;
                    rt.anchorMax = o.anchorMax;
                    rt.pivot = o.pivot;
                    rt.anchoredPosition = o.anchoredPosition;
                    rt.sizeDelta = o.sizeDelta;
                    rt.localScale = o.localScale;
                }
                _titleText.fontSize = _titleOriginalFontSize;
                _subtitleText.fontSize = _subtitleOriginalFontSize;
                _revenueText.fontSize = _revenueLabelOriginalFontSize;
                _titleText.textWrappingMode = _titleOriginalWrap;
                _subtitleText.textWrappingMode = _subtitleOriginalWrap;
                _shopBackground.color = _shopBackgroundOriginalColor;
                if (_portraitScrim != null) _portraitScrim.SetActive(false);

                shopPanelUI.gameObject.SetActive(true);
                if (shopToggleButton != null) shopToggleButton.SetActive(false);
                return;
            }

            var TL = new Vector2(0f, 1f); // top-left anchor/pivot convention
            var TC = new Vector2(0.5f, 1f); // top-center - used consistently below so
            // every element shares ONE "distance from canvas top" coordinate frame.
            // (A previous version mixed this with center-anchored (0.5,0.5) positions
            // for the click/ad buttons, which put them at the wrong absolute height and
            // pushed the Company panel off the bottom of the screen.)

            Set(logo, TL, TL, TL, new Vector2(14f, -14f), new Vector2(44f, 44f));

            // Word wrap forced off: at small portrait widths TMP was wrapping the title
            // to 2 lines, which then overlapped the subtitle below it.
            Set(title, TL, TL, TL, new Vector2(66f, -20f), new Vector2(310f, 26f));
            _titleText.fontSize = 16f;
            _titleText.textWrappingMode = TextWrappingModes.NoWrap;
            Set(subtitle, TL, TL, TL, new Vector2(66f, -46f), new Vector2(310f, 16f));
            _subtitleText.fontSize = 10f;
            _subtitleText.textWrappingMode = TextWrappingModes.NoWrap;

            // 14px left margin, 14px gap, 14px right margin against the 390-wide reference -
            // matches the 14px grid used everywhere else in this header (logo, passiveRateBadge).
            // ipoButton is 2px wider than before (166->168) solely to make its right edge land
            // exactly on that same 14px margin instead of leaving a 16px gap.
            Set(revenueCard, TL, TL, TL, new Vector2(14f, -70f), new Vector2(180f, 54f));
            Set(revenueLabel, TL, TL, TL, new Vector2(14f, -70f), new Vector2(180f, 54f));
            _revenueText.fontSize = 24f;
            Set(ipoButton, TL, TL, TL, new Vector2(208f, -70f), new Vector2(168f, 54f));

            // Valuation/Reputation/PassiveRateBadge each have a caption+value child using
            // FIXED pixel offsets (not stretch anchors) relative to their parent pill, so
            // resizing the parent's sizeDelta directly pushes those children out of view.
            // Scaling the whole pill instead keeps every child correctly positioned.
            SetScaled(passiveRateBadge, TL, new Vector2(14f, -134f), 0.76f);
            SetScaled(valuation, TL, new Vector2(138f, -134f), 0.93f);
            SetScaled(reputation, TL, new Vector2(262f, -134f), 0.93f);

            Set(clickButton, TC, TC, TC, new Vector2(0f, -188f), new Vector2(200f, 200f));
            // Height trimmed from 56->48 (still meets Android's 48dp minimum touch target) solely
            // to open up breathing room below - its bottom edge was landing right on top of the
            // "Company" header text, since companyPanelBG is already packed tight against the
            // bottom of the screen and can't be pushed down further without clipping.
            Set(watchAdButton, TC, TC, TC, new Vector2(0f, -400f), new Vector2(240f, 48f));

            // companyPanelBG's parent ("Company Panel") is itself a small 100x100 box
            // sitting at canvas center, not a full-screen container - so a top-center
            // anchor here is relative to THAT box's top edge (372 units below the real
            // canvas top), not the screen. -472 canvas-relative becomes -100 once that
            // 372 offset is subtracted back out.
            Set(companyPanelBG, TC, TC, TC, new Vector2(0f, -100f), companyPanelBG.sizeDelta, 0.55f);
            // sizeDelta unchanged on purpose - scale handles the resize so every nested
            // stat card keeps its correct relative position without touching each one.

            var C = new Vector2(0.5f, 0.5f);

            // Both cards are already center-anchored at (0,0), so scaling in place keeps them
            // centered - same technique as shopPanelUI below. At 520 wide, authored for a
            // landscape canvas, they overflowed past both edges of the ~390-475-wide portrait
            // canvas; 0.7 brings them to ~364 wide, comfortably within it.
            if (ipoDialogCard != null) Set(ipoDialogCard, C, C, C, Vector2.zero, ipoDialogCard.sizeDelta, 0.7f);
            if (ipoResultsCard != null) Set(ipoResultsCard, C, C, C, Vector2.zero, ipoResultsCard.sizeDelta, 0.7f);

            Set(shopPanelUI, C, C, C, Vector2.zero, shopPanelUI.sizeDelta, 0.85f);
            shopPanelUI.gameObject.SetActive(false);

            // Shop Panel's own background is a ~69%-opacity tint (fine in landscape,
            // where it just sits beside other content) - as a portrait modal it needs
            // to actually hide what's behind it, both via its own near-opaque
            // background and a dedicated full-screen scrim placed just behind it.
            Color c = _shopBackgroundOriginalColor;
            _shopBackground.color = new Color(c.r, c.g, c.b, 1f);
            EnsurePortraitScrim();

            if (shopToggleButton != null)
            {
                shopToggleButton.SetActive(true);
                if (shopToggleLabel != null) shopToggleLabel.text = "SHOP";
            }
        }

        private void EnsurePortraitScrim()
        {
            if (_portraitScrim != null)
            {
                _portraitScrim.SetActive(false);
                return;
            }

            _portraitScrim = new GameObject("PortraitScrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _portraitScrim.transform.SetParent(shopPanelUI.parent, false);
            _portraitScrim.transform.SetSiblingIndex(shopPanelUI.GetSiblingIndex()); // just behind the shop panel

            RectTransform rt = (RectTransform)_portraitScrim.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = _portraitScrim.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.75f);

            var button = _portraitScrim.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(ToggleShop); // tapping outside the panel closes it

            _portraitScrim.SetActive(false);
        }
    }
}
