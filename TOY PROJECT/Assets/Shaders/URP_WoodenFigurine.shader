Shader "Custom/URP_WoodenFigurine"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.8, 0.6, 0.4, 1)
        _BaseMap("Base Map", 2D) = "white" {}
        
        _ShadowSteps("Shadow Steps", Range(2, 10)) = 3
        _ShadowSoftness("Shadow Softness", Range(0, 0.5)) = 0.05
        
        _RimColor("Rim Color", Color) = (0.9, 0.7, 0.5, 1)
        _RimPower("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity("Rim Intensity", Range(0, 1)) = 0.5
        
        _Smoothness("Smoothness", Range(0, 1)) = 0.2
        
        _FlashColor("Flash Color", Color) = (1, 1, 1, 1)
        _FlashAmount("Flash Amount", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _ShadowSteps;
                float _ShadowSoftness;
                float _Smoothness;
                float4 _FlashColor;
                float _FlashAmount;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 albedo = baseMap.rgb * _BaseColor.rgb;
                
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 normalWS = normalize(input.normalWS);
                
                float NdotL = dot(normalWS, mainLight.direction);
                float toonDiff = floor(NdotL * _ShadowSteps) / _ShadowSteps;
                toonDiff = smoothstep(toonDiff - _ShadowSoftness, toonDiff + _ShadowSoftness, NdotL);
                toonDiff = max(0.2, toonDiff);
                
                float3 lighting = albedo * mainLight.color * toonDiff * mainLight.shadowAttenuation;
                
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float rimDot = 1.0 - saturate(dot(normalWS, viewDirWS));
                float3 rim = _RimColor.rgb * pow(rimDot, _RimPower) * _RimIntensity;
                
                float3 finalColor = lighting + rim + albedo * 0.1;
                
                finalColor = lerp(finalColor, _FlashColor.rgb, _FlashAmount);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}