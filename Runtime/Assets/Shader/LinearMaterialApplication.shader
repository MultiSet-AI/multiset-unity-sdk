Shader "Custom/RadialMaterialApplication"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Progress ("Progress", Range(0, 1)) = 0
        _Center ("Center", Vector) = (0,0,0,0)
        _Radius ("Max Radius", Float) = 1.0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert alpha

        sampler2D _MainTex;
        fixed4 _Color;
        float _Progress;
        float3 _Center;
        float _Radius;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            
            float3 objectSpacePos = mul(unity_WorldToObject, float4(IN.worldPos, 1.0)).xyz;
            float distanceFromCenter = distance(objectSpacePos, _Center);
            float normalizedDistance = distanceFromCenter / _Radius;
            
            float threshold = _Progress;
            
            if (normalizedDistance < threshold)
            {
                o.Albedo = c.rgb;
                o.Alpha = c.a;
            }
            else
            {
                o.Albedo = float3(0,0,0);
                o.Alpha = 0;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}