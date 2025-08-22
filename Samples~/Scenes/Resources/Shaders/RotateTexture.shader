Shader "Unlit/RotateTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            // These directives MUST be on separate lines.
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
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // The core of the 90-degree counter-clockwise rotation:
                // The new u-coordinate becomes the old v-coordinate.
                // The new v-coordinate becomes 1 minus the old u-coordinate.
                o.uv = float2(v.uv.y, 1.0 - v.uv.x);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture with the newly rotated UV coordinates
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}