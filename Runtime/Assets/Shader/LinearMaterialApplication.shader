Shader "Custom/RadialMaterialApplication"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Progress ("Progress", Range(0, 1)) = 0
        _Center ("Center", Vector) = (0,0,0,0)
        _Radius ("Max Radius", Float) = 1.0
        _GridColor ("Grid Color", Color) = (1,1,0,1)
        _GridSize ("Grid Size", Float) = 1.0
        _GridLineWidth ("Grid Line Width", Range(0.01, 0.1)) = 0.02
        _ShowGrid ("Show Grid", Range(0, 1)) = 1
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
        fixed4 _GridColor;
        float _GridSize;
        float _GridLineWidth;
        float _ShowGrid;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        float GetGridFactor(float3 worldPos, float3 worldNormal, float gridSize, float lineWidth)
        {
            // Determine which plane to project onto based on surface normal
            float3 absNormal = abs(worldNormal);
            float2 gridCoord;
            
            // Use the two coordinates perpendicular to the dominant normal direction
            if (absNormal.y > absNormal.x && absNormal.y > absNormal.z)
            {
                // Horizontal surface (floor/ceiling) - use X and Z
                gridCoord = float2(worldPos.x, worldPos.z) / gridSize;
            }
            else if (absNormal.x > absNormal.z)
            {
                // Vertical surface facing X direction (wall) - use Y and Z
                gridCoord = float2(worldPos.y, worldPos.z) / gridSize;
            }
            else
            {
                // Vertical surface facing Z direction (wall) - use X and Y
                gridCoord = float2(worldPos.x, worldPos.y) / gridSize;
            }
            
            // Get fractional part of grid coordinates
            float2 gridFract = frac(abs(gridCoord));
            
            // Calculate distance to nearest grid line
            float2 distToLine = min(gridFract, 1.0 - gridFract);
            float minDistToLine = min(distToLine.x, distToLine.y);
            
            // Create grid lines
            float gridFactor = 1.0 - smoothstep(0.0, lineWidth, minDistToLine);
            
            return gridFactor;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            
            float3 objectSpacePos = mul(unity_WorldToObject, float4(IN.worldPos, 1.0)).xyz;
            float distanceFromCenter = distance(objectSpacePos, _Center);
            float normalizedDistance = distanceFromCenter / _Radius;
            
            float threshold = _Progress;
            
            if (normalizedDistance < threshold)
            {
                // Calculate grid pattern
                float gridFactor = GetGridFactor(IN.worldPos, IN.worldNormal, _GridSize, _GridLineWidth);
                
                // Blend between base material and grid color
                float3 finalColor = lerp(c.rgb, _GridColor.rgb, gridFactor * _ShowGrid);
                float finalAlpha = max(c.a, gridFactor * _GridColor.a * _ShowGrid);
                
                o.Albedo = finalColor;
                o.Alpha = finalAlpha;
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