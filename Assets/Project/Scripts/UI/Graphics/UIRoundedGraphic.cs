using UnityEngine;
using UnityEngine.UI;

namespace AIStartupTycoon.UI
{
    /// <summary>
    /// Drop-in replacement for Image that renders a rounded rect (and, paired with the
    /// UI/RoundedGradient shader, a gradient fill + hairline border) via a single quad.
    /// Corner radius and border width are packed into the mesh's UV1/UV2 channels per
    /// instance, so many UIRoundedGraphics can share one Material (one per design-system
    /// "role" - see the shared materials under Assets/Project/Art/UI/Materials) and still
    /// batch, instead of needing a Material instance per element.
    /// Gradient colors/angle live on the Material itself (shared per role) since the mock
    /// never varies a single button's own gradient at runtime - only which role/state
    /// Material is assigned.
    /// </summary>
    [AddComponentMenu("UI/Rounded Graphic")]
    public class UIRoundedGraphic : MaskableGraphic
    {
        [Tooltip("Corner radius in local (RectTransform) units.")]
        public float cornerRadius = 16f;

        [Tooltip("Hairline border width in local units. 0 = no border (color/alpha still driven by the material's _BorderColor if > 0 width elsewhere).")]
        public float borderWidth = 0f;

        [Tooltip("Extends the mesh this many local units beyond the visible rounded rect on every side, so a Material's _ShadowOffset/_ShadowColor has room to draw into without being clipped at the rect edge. 0 = no shadow draws outside the rect (safe default, matches pre-shadow behavior). Set to roughly max(|shadowOffset|) + a few px of softness.")]
        public float shadowPadding = 0f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = GetPixelAdjustedRect();
            float w = r.width;
            float h = r.height;
            Vector2 size = new Vector2(w, h);
            float pad = shadowPadding;

            Vector2[] corners =
            {
                new Vector2(r.xMin - pad, r.yMin - pad), // bottom-left
                new Vector2(r.xMin - pad, r.yMax + pad), // top-left
                new Vector2(r.xMax + pad, r.yMax + pad), // top-right
                new Vector2(r.xMax + pad, r.yMin - pad), // bottom-right
            };

            for (int i = 0; i < 4; i++)
            {
                Vector2 corner = corners[i];
                // Fraction of the ORIGINAL (unpadded) rect - deliberately allowed to fall
                // outside [0,1] for the padded corners. The shader only ever uses this to
                // reconstruct a real local-unit position (rectUV.xy - 0.5) * rectUV.zw, so
                // values outside 0-1 still interpolate to the correct local position in the
                // padding margin; it never needs to be a "true" 0-1 UV.
                float u = (corner.x - r.xMin) / w;
                float v = (corner.y - r.yMin) / h;

                var vert = UIVertex.simpleVert;
                vert.position = corner;
                vert.color = color;
                vert.uv0 = new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
                // uv1 (rectUV in the shader): xy = fraction across the ORIGINAL rect (see above), zw = rect size in local units.
                vert.uv1 = new Vector4(u, v, size.x, size.y);
                // uv2 (shapeParams): x = corner radius, y = border width.
                vert.uv2 = new Vector4(cornerRadius, borderWidth, 0f, 0f);
                vh.AddVert(vert);
            }

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            cornerRadius = Mathf.Max(0f, cornerRadius);
            borderWidth = Mathf.Max(0f, borderWidth);
            shadowPadding = Mathf.Max(0f, shadowPadding);
        }
#endif
    }
}
