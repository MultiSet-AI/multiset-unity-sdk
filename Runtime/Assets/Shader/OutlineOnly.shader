Shader "Custom/UniversalOutlineOnly"
{
    Properties
    {
        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (0, 1, 1, 1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.1)) = 0.01
        _GlowIntensity ("Glow Intensity", Range(1, 10)) = 3
        
        [Header(Spark Animation)]
        _SparkSpeed ("Spark Speed", Range(1.0, 8.0)) = 4.0
        _SparkFrequency ("Spark Frequency", Range(2, 15)) = 8
        _SparkIntensity ("Spark Intensity", Range(1.0, 5.0)) = 3.0
        
        [Header(Rim Light)]
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 2.0
        _RimIntensity ("Rim Intensity", Range(0.0, 1.0)) = 0.7
    }
    
    // URP SubShader - Will be used if URP is active
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "Queue" = "Geometry+1"
            "RenderPipeline"="UniversalPipeline"
        }
        
        // Pass 1: Hide the original mesh completely
        Pass
        {
            Name "HideMesh"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            // Check if URP is available, fallback if not
            #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED) || defined(UNITY_PIPELINE_URP)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #define URP_AVAILABLE
            #else
                #include "UnityCG.cginc"
            #endif
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                #ifdef URP_AVAILABLE
                    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                    output.positionCS = positionInputs.positionCS;
                #else
                    output.positionCS = UnityObjectToClipPos(input.positionOS);
                #endif
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                return half4(0, 0, 0, 1);
            }
            ENDHLSL
        }
        
        // Pass 2: Spark animated outline edges
        Pass
        {
            Name "SparkOutlineEdges"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            // Check if URP is available, fallback if not
            #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED) || defined(UNITY_PIPELINE_URP)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #define URP_AVAILABLE
            #else
                #include "UnityCG.cginc"
                #include "Lighting.cginc"
            #endif
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
            };
            
            #ifdef URP_AVAILABLE
                CBUFFER_START(UnityPerMaterial)
                    half4 _OutlineColor;
                    float _OutlineWidth;
                    float _GlowIntensity;
                    float _SparkSpeed;
                    float _SparkFrequency;
                    float _SparkIntensity;
                    float _RimPower;
                    float _RimIntensity;
                CBUFFER_END
            #else
                float4 _OutlineColor;
                float _OutlineWidth;
                float _GlowIntensity;
                float _SparkSpeed;
                float _SparkFrequency;
                float _SparkIntensity;
                float _RimPower;
                float _RimIntensity;
            #endif
            
            // Simple hash function for pseudo-random values
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }
            
            // Noise function for organic sparkling
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                #ifdef URP_AVAILABLE
                    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                    
                    // Expand vertex along normal for outline effect
                    float3 normalWS = normalInputs.normalWS;
                    float3 positionWS = positionInputs.positionWS + normalWS * _OutlineWidth;
                    
                    output.positionCS = TransformWorldToHClip(positionWS);
                    output.positionWS = positionWS;
                    output.normalWS = normalWS;
                #else
                    // Expand vertex along normal
                    float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, input.normalOS));
                    float4 worldPos = mul(unity_ObjectToWorld, input.positionOS);
                    
                    worldPos.xyz += worldNormal * _OutlineWidth;
                    
                    output.positionCS = mul(UNITY_MATRIX_VP, worldPos);
                    output.normalWS = worldNormal;
                    output.positionWS = worldPos.xyz;
                #endif
                
                output.uv = input.uv;
                output.positionOS = input.positionOS.xyz;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // View direction for rim lighting
                #ifdef URP_AVAILABLE
                    float3 viewDir = GetWorldSpaceViewDir(input.positionWS);
                #else
                    float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                #endif
                viewDir = normalize(viewDir);
                
                // Rim lighting calculation
                float rim = 1.0 - saturate(dot(input.normalWS, viewDir));
                rim = pow(saturate(rim), _RimPower) * _RimIntensity;
                
                // Time-based animation
                float time = _Time.y * _SparkSpeed;
                
                // Primary spark layer
                float2 sparkCoord1 = input.positionOS.xy * _SparkFrequency;
                float spark1 = noise(sparkCoord1 + time * 0.5);
                spark1 = pow(saturate(spark1), 2.0);
                
                // Secondary spark layer (different frequency)
                float2 sparkCoord2 = input.positionOS.xz * _SparkFrequency * 1.7;
                float spark2 = noise(sparkCoord2 - time * 0.3);
                spark2 = pow(saturate(spark2), 3.0);
                
                // Traveling wave effect
                float wave = sin(input.positionOS.y * 10.0 - time * 3.0) * 0.5 + 0.5;
                wave = pow(saturate(wave), 2.0);
                
                // Combine sparks with masking
                float sparkHash = hash(floor(sparkCoord1));
                float sparkMask = step(0.6, sparkHash);
                float combinedSpark = lerp(spark1, spark2, 0.5) * sparkMask;
                combinedSpark = max(combinedSpark, wave * 0.3);
                
                // Pulsing effect
                float pulse = sin(time * 2.0) * 0.3 + 0.7;
                
                // Calculate final animation intensity
                float animationIntensity = 1.0 + combinedSpark * _SparkIntensity;
                animationIntensity *= pulse;
                
                // Final color calculation
                half4 finalColor = _OutlineColor;
                finalColor.rgb *= _GlowIntensity * animationIntensity;
                finalColor.rgb *= (rim * 0.7 + 0.3);
                
                // Alpha based on rim and animation
                finalColor.a = (rim * 0.6 + 0.4) * saturate(animationIntensity * 0.5) * _OutlineColor.a;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    // Built-in Render Pipeline SubShader - Will be used if Built-in RP is active
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "Queue" = "Geometry+1"
        }
        
        // Pass 1: Hide the original mesh completely
        Pass
        {
            Name "HideMesh"
            Tags { "LightMode" = "Always" }
            
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(0, 0, 0, 1);
            }
            ENDCG
        }
        
        // Pass 2: Spark animated outline edges
        Pass
        {
            Name "SparkOutlineEdges"
            Tags { "LightMode" = "Always" }
            
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 localPos : TEXCOORD3;
                UNITY_FOG_COORDS(4)
            };
            
            float4 _OutlineColor;
            float _OutlineWidth;
            float _GlowIntensity;
            float _SparkSpeed;
            float _SparkFrequency;
            float _SparkIntensity;
            float _RimPower;
            float _RimIntensity;
            
            // Simple hash function for pseudo-random values
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }
            
            // Noise function for organic sparkling
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                
                // Expand vertex along normal
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                worldPos.xyz += worldNormal * _OutlineWidth;
                
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.worldNormal = worldNormal;
                o.worldPos = worldPos.xyz;
                o.uv = v.uv;
                o.localPos = v.vertex.xyz;
                
                UNITY_TRANSFER_FOG(o, o.pos);
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // View direction for rim lighting
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                
                // Rim lighting calculation
                float rim = 1.0 - saturate(dot(i.worldNormal, viewDir));
                rim = pow(saturate(rim), _RimPower) * _RimIntensity;
                
                // Time-based animation
                float time = _Time.y * _SparkSpeed;
                
                // Primary spark layer
                float2 sparkCoord1 = i.localPos.xy * _SparkFrequency;
                float spark1 = noise(sparkCoord1 + time * 0.5);
                spark1 = pow(saturate(spark1), 2.0);
                
                // Secondary spark layer (different frequency)
                float2 sparkCoord2 = i.localPos.xz * _SparkFrequency * 1.7;
                float spark2 = noise(sparkCoord2 - time * 0.3);
                spark2 = pow(saturate(spark2), 3.0);
                
                // Traveling wave effect
                float wave = sin(i.localPos.y * 10.0 - time * 3.0) * 0.5 + 0.5;
                wave = pow(saturate(wave), 2.0);
                
                // Combine sparks with masking
                float sparkHash = hash(floor(sparkCoord1));
                float sparkMask = step(0.6, sparkHash);
                float combinedSpark = lerp(spark1, spark2, 0.5) * sparkMask;
                combinedSpark = max(combinedSpark, wave * 0.3);
                
                // Pulsing effect
                float pulse = sin(time * 2.0) * 0.3 + 0.7;
                
                // Calculate final animation intensity
                float animationIntensity = 1.0 + combinedSpark * _SparkIntensity;
                animationIntensity *= pulse;
                
                // Final color calculation
                fixed4 finalColor = _OutlineColor;
                finalColor.rgb *= _GlowIntensity * animationIntensity;
                finalColor.rgb *= (rim * 0.7 + 0.3);
                
                // Alpha based on rim and animation
                finalColor.a = (rim * 0.6 + 0.4) * saturate(animationIntensity * 0.5) * _OutlineColor.a;
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogFactor, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
        
        // Pass 3: Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                V2F_SHADOW_CASTER;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i);
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}