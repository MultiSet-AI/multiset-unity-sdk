Shader "Custom/OutlineOnly"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 1, 1, 1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.03)) = 0.005
        _GlowIntensity ("Glow Intensity", Range(1, 8)) = 3
        
        [Header(Animation Type)]
        [KeywordEnum(Flow, Pulse, Breathe, Spark)] _AnimationType ("Animation Type", Float) = 0
        
        [Header(Flow Animation)]
        _FlowSpeed ("Flow Speed", Range(0.5, 5.0)) = 2.0
        _FlowTiling ("Flow Tiling", Range(1, 20)) = 5
        _FlowIntensityMin ("Flow Min Intensity", Range(0.1, 1.0)) = 0.3
        _FlowIntensityMax ("Flow Max Intensity", Range(1.0, 3.0)) = 2.5
        
        [Header(Pulse Animation)]
        _PulseSpeed ("Pulse Speed", Range(0.3, 4.0)) = 1.8
        _PulseMin ("Pulse Min", Range(0.2, 1.0)) = 0.4
        _PulseMax ("Pulse Max", Range(1.0, 3.0)) = 2.2
        
        [Header(Breathe Animation)]
        _BreatheSpeed ("Breathe Speed", Range(0.2, 2.0)) = 0.8
        _BreatheIntensity ("Breathe Intensity", Range(0.3, 2.0)) = 1.5
        
        [Header(Spark Animation)]
        _SparkSpeed ("Spark Speed", Range(1.0, 8.0)) = 4.0
        _SparkFrequency ("Spark Frequency", Range(2, 15)) = 8
        _SparkIntensity ("Spark Intensity", Range(1.0, 5.0)) = 3.0
    }
    
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
        
        // Pass 2: Animated outline edges
        Pass
        {
            Name "AnimatedOutlineEdges"
            Tags { "LightMode" = "Always" }
            
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ANIMATIONTYPE_FLOW _ANIMATIONTYPE_PULSE _ANIMATIONTYPE_BREATHE _ANIMATIONTYPE_SPARK
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
            };
            
            float4 _OutlineColor;
            float _OutlineWidth;
            float _GlowIntensity;
            
            // Animation parameters
            float _FlowSpeed;
            float _FlowTiling;
            float _FlowIntensityMin;
            float _FlowIntensityMax;
            
            float _PulseSpeed;
            float _PulseMin;
            float _PulseMax;
            
            float _BreatheSpeed;
            float _BreatheIntensity;
            
            float _SparkSpeed;
            float _SparkFrequency;
            float _SparkIntensity;
            
            // Simple hash function for pseudo-random values
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                
                float animationMultiplier = 1.0;
                
                #ifdef _ANIMATIONTYPE_BREATHE
                    // Smooth breathing animation
                    float breathe = sin(_Time.y * _BreatheSpeed) * 0.5 + 0.5;
                    breathe = smoothstep(0.0, 1.0, breathe);
                    animationMultiplier = lerp(0.7, _BreatheIntensity, breathe);
                #endif
                
                #ifdef _ANIMATIONTYPE_PULSE
                    // Sharp pulse animation
                    float pulse = sin(_Time.y * _PulseSpeed);
                    pulse = pulse * pulse; // Square for sharper pulses
                    animationMultiplier = lerp(_PulseMin, _PulseMax, pulse);
                #endif
                
                // Expand vertex along normal
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                worldPos.xyz += worldNormal * _OutlineWidth * animationMultiplier;
                
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.worldNormal = worldNormal;
                o.worldPos = worldPos.xyz;
                o.uv = v.uv;
                o.localPos = v.vertex.xyz;
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float animationIntensity = 1.0;
                
                #ifdef _ANIMATIONTYPE_FLOW
                    // Flowing energy waves
                    float flowPattern = sin((i.localPos.y + i.localPos.x) * _FlowTiling + _Time.y * _FlowSpeed);
                    flowPattern = flowPattern * 0.5 + 0.5; // Normalize to 0-1
                    flowPattern = smoothstep(0.2, 0.8, flowPattern); // Create more defined waves
                    animationIntensity = lerp(_FlowIntensityMin, _FlowIntensityMax, flowPattern);
                #endif
                
                #ifdef _ANIMATIONTYPE_PULSE
                    // Intense pulsing
                    float pulse = sin(_Time.y * _PulseSpeed);
                    pulse = pow(abs(pulse), 0.5); // More dramatic curve
                    animationIntensity = lerp(_PulseMin, _PulseMax, pulse);
                #endif
                
                #ifdef _ANIMATIONTYPE_BREATHE
                    // Gentle breathing
                    float breathe = sin(_Time.y * _BreatheSpeed) * 0.5 + 0.5;
                    breathe = smoothstep(0.1, 0.9, breathe);
                    animationIntensity = lerp(0.6, _BreatheIntensity, breathe);
                #endif
                
                #ifdef _ANIMATIONTYPE_SPARK
                    // Sparkling/twinkling effect
                    float2 sparkCoord = floor(i.localPos.xy * _SparkFrequency);
                    float sparkHash = hash(sparkCoord);
                    float sparkTime = _Time.y * _SparkSpeed + sparkHash * 6.28;
                    float spark = sin(sparkTime);
                    spark = pow(abs(spark), 3.0); // Sharp spikes
                    
                    // Only some areas spark based on hash
                    float sparkMask = step(0.7, sparkHash);
                    animationIntensity = 1.0 + spark * sparkMask * _SparkIntensity;
                #endif
                
                // Base rim lighting
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float rim = 1.0 - saturate(dot(i.worldNormal, viewDir));
                rim = pow(rim, 1.5);
                
                // Combine effects
                float4 finalColor = _OutlineColor;
                finalColor.rgb *= _GlowIntensity * animationIntensity * (rim * 0.7 + 0.3);
                finalColor.a = (rim * 0.6 + 0.4) * saturate(animationIntensity * 0.5);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Diffuse"
}