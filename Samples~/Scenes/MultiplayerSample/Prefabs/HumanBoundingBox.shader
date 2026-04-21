Shader "Custom/AR/HumanBoundingBox"
{
    Properties
    {
        _EdgeColor ("Edge Color", Color) = (0,1,1,1)
        _FillColor ("Fill Color", Color) = (0,1,1,0.05)

        _Thickness ("Edge Thickness", Range(0.001,0.05)) = 0.01
        _GlowSize ("Glow Size", Range(0.001,0.1)) = 0.02
        _GlowIntensity ("Glow Intensity", Range(0,5)) = 2

        _CutSize ("Cut Corner Size", Range(0.01,0.3)) = 0.12
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            float4 _EdgeColor;
            float4 _FillColor;

            float _Thickness;
            float _GlowSize;
            float _GlowIntensity;
            float _CutSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float rectBorder(float2 uv)
            {
                float left   = smoothstep(_Thickness, 0, uv.x);
                float right  = smoothstep(_Thickness, 0, 1-uv.x);
                float bottom = smoothstep(_Thickness, 0, uv.y);
                float top    = smoothstep(_Thickness, 0, 1-uv.y);

                return max(max(left,right),max(top,bottom));
            }

            float rectGlow(float2 uv)
            {
                float left   = smoothstep(_GlowSize, 0, uv.x);
                float right  = smoothstep(_GlowSize, 0, 1-uv.x);
                float bottom = smoothstep(_GlowSize, 0, uv.y);
                float top    = smoothstep(_GlowSize, 0, 1-uv.y);

                return max(max(left,right),max(top,bottom));
            }

            float cutMask(float2 uv)
            {
                return step(_CutSize, uv.x + (1-uv.y));
            }

            float diagonalEdge(float2 uv)
            {
                float d = abs((uv.x + (1-uv.y)) - _CutSize);
                return smoothstep(_Thickness, 0, d);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float border = rectBorder(uv);
                float glow = rectGlow(uv);

                float cut = cutMask(uv);
                float diag = diagonalEdge(uv);

                float edge = max(border * cut, diag);

                float glowPulse = (1 + sin(_Time.y * 2) * 0.25);

                float glowValue = glow * _GlowIntensity * glowPulse * edge;

                float fillMask = step(_Thickness, uv.x) *
                                 step(_Thickness, uv.y) *
                                 step(_Thickness, 1-uv.x) *
                                 step(_Thickness, 1-uv.y) *
                                 cut;

                float4 fill = _FillColor * fillMask;

                float4 edgeColor = float4(_EdgeColor.rgb * (edge + glowValue), edge);

                return fill + edgeColor;
            }

            ENDCG
        }
    }
}