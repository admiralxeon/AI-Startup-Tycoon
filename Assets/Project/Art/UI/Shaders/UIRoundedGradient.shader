// Rounded-rect + up-to-3-stop linear gradient + hairline border for uGUI.
// One Material per visual "role" (see UIRoundedGraphic.cs) - corner radius, border
// width and the local rect size vary per-instance via UV channels (packed by
// UIRoundedGraphic.OnPopulateMesh), so many instances can still share one Material
// and batch normally. Stencil/ColorMask block copied from Unity's built-in UI/Default
// shader so masking (RectMask2D / Image masks) keeps working.
Shader "UI/RoundedGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorA ("Gradient Stop A", Color) = (0.22, 0.74, 0.97, 1)
        _ColorB ("Gradient Stop B", Color) = (0.39, 0.40, 0.95, 1)
        _ColorC ("Gradient Stop C", Color) = (0.65, 0.55, 0.98, 1)
        _StopB ("Stop B Position (0-1)", Range(0,1)) = 0.55
        _GradientAngle ("Gradient Angle (deg, CSS-style, 0=up)", Range(0,360)) = 135
        _BorderColor ("Border Color", Color) = (1, 1, 1, 0.1)
        _BorderSoftness ("Border AA Softness (px)", Float) = 1.0

        // Hard offset drop shadow, matching the mock's flat "plastic edge" shadows
        // (CSS box-shadow: 0 Npx 0 color, no blur). Alpha 0 (the default) means the
        // shadow term is fully skipped and existing materials render byte-identical
        // to before this was added - opt in per-Material by setting _ShadowColor.a > 0.
        // Requires UIRoundedGraphic.shadowPadding > 0 on the instance so the mesh
        // has room to draw into without being clipped at the rect edge.
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0)
        // Local space here follows Unity's own convention (Y increases upward), so to
        // push the shadow down like a CSS "0 Npx 0 color" box-shadow, use a NEGATIVE Y.
        _ShadowOffset ("Shadow Offset (local units, Y-up; use negative Y for a downward shadow)", Vector) = (0, 0, 0, 0)
        _ShadowSoftness ("Shadow AA Softness (px)", Float) = 1.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature __ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                // packed by UIRoundedGraphic: xy = local UV (0-1 across the rect),
                // zw = rect size in local units
                float4 rectUV   : TEXCOORD1;
                // x = corner radius (local units), y = border width (local units)
                float2 shapeParams : TEXCOORD2;
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                float4 color     : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 rectUV    : TEXCOORD1;
                float2 shapeParams : TEXCOORD2;
                float4 worldPosition : TEXCOORD3;
            };

            sampler2D _MainTex;
            fixed4 _ColorA, _ColorB, _ColorC, _BorderColor, _ShadowColor;
            float _StopB, _GradientAngle, _BorderSoftness, _ShadowSoftness;
            float4 _ShadowOffset;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                OUT.rectUV = v.rectUV;
                OUT.shapeParams = v.shapeParams;
                return OUT;
            }

            // Signed distance to a rounded box, centered at origin, half-extents b, corner radius r.
            float sdRoundBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 size = IN.rectUV.zw;
                float2 local = (IN.rectUV.xy - 0.5) * size; // centered local position, in local units
                float radius = min(IN.shapeParams.x, min(size.x, size.y) * 0.5);
                float border = IN.shapeParams.y;

                float dist = sdRoundBox(local, size * 0.5, radius);
                float aa = fwidth(dist) + 0.001;
                float shapeAlpha = 1.0 - smoothstep(0.0, aa, dist);

                // 3-stop linear gradient along a CSS-style angle (0deg = bottom-to-top).
                // Computed in real local units (not raw 0-1 UV) and normalized by the rect's
                // own half-diagonal projection along the gradient direction, so the gradient
                // spans corner-to-corner correctly regardless of the element's aspect ratio -
                // a wide/short card and a tall/narrow one both get a proper full-span gradient
                // instead of the wide one compressing it into a sharp band along its short axis.
                float rad = radians(_GradientAngle);
                float2 dir = float2(sin(rad), cos(rad));
                float2 halfSize = size * 0.5;
                float maxProj = abs(dir.x) * halfSize.x + abs(dir.y) * halfSize.y;
                float t = dot(local, dir) / (2.0 * max(maxProj, 0.0001)) + 0.5;
                t = saturate(t);
                fixed4 gradColor = t < _StopB
                    ? lerp(_ColorA, _ColorB, t / max(_StopB, 0.0001))
                    : lerp(_ColorB, _ColorC, (t - _StopB) / max(1.0 - _StopB, 0.0001));

                fixed4 texCol = tex2D(_MainTex, IN.texcoord);
                fixed4 col = gradColor * IN.color * texCol;

                if (border > 0.0001)
                {
                    float innerDist = dist + border;
                    float borderMask = smoothstep(0.0, aa, innerDist) * shapeAlpha;
                    col.rgb = lerp(col.rgb, _BorderColor.rgb, borderMask * _BorderColor.a);
                }

                // col.a already carries gradColor.a * IN.color.a * texCol.a from the `col =
                // gradColor * IN.color * texCol` line above (plus any border tint, which
                // doesn't touch alpha) - multiply in shapeAlpha to clip it to the rounded rect,
                // same as the original (pre-shadow) shader's `col.a *= shapeAlpha`.
                float fillAlpha = col.a * shapeAlpha;

                // Hard offset shadow, same rounded-box shape sampled at an offset position,
                // composited underneath the fill via standard "over" blending. When
                // _ShadowColor.a is 0 (unset materials) this whole block is a no-op and the
                // output is bit-identical to the pre-shadow shader.
                float shadowAlpha = 0.0;
                if (_ShadowColor.a > 0.001)
                {
                    float shadowDist = sdRoundBox(local - _ShadowOffset.xy, size * 0.5, radius);
                    float shadowAa = fwidth(shadowDist) + 0.001;
                    shadowAlpha = (1.0 - smoothstep(0.0, shadowAa + _ShadowSoftness, shadowDist)) * _ShadowColor.a;
                }

                col.rgb = col.rgb * fillAlpha + _ShadowColor.rgb * shadowAlpha * (1.0 - fillAlpha);
                col.a = fillAlpha + shadowAlpha * (1.0 - fillAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                clip(col.a - 0.001);
                return col;
            }
            ENDCG
        }
    }
}
