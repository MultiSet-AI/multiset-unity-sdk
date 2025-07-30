Shader "MultiSet/PathArrow"
{
    Properties
    {
        _MainTex ("Arrow Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _ArrowSize ("Arrow Size", Range(0.1, 10.0)) = 1.0
        _ArrowSpacing ("Arrow Spacing", Range(0.1, 10.0)) = 2.0
        _Intensity ("Intensity", Range(0.1, 5.0)) = 1.0
        _ScrollSpeed ("Scroll Speed", Range(-2.0, 2.0)) = 0.0
        _PathLength ("Path Length", Float) = 1.0
    }
    
    SubShader
    {
        Tags { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        LOD 100
        Lighting Off
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _ArrowSize;
            float _ArrowSpacing;
            float _Intensity;
            float _ScrollSpeed;
            float _PathLength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Convert UV.x (normalized) into real-world distance along path
                float distAlongPath = v.uv.x * _PathLength;

                // Repeat arrows every _ArrowSpacing units in world space
                float uvRepeat = distAlongPath / _ArrowSpacing;

                // Scroll based on time
                uvRepeat += _Time.y * _ScrollSpeed;

                // Final UV scaled by arrow size
                o.uv = float2(uvRepeat, v.uv.y) * (_MainTex_ST.xy / _ArrowSize);

                o.color = v.color * _Color;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Repeat the arrow using frac()
                fixed4 col = tex2D(_MainTex, frac(i.uv)) * i.color * _Intensity;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    Fallback "Transparent/VertexLit"
}