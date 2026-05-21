Shader "Custom/URP/PainterlyToon"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _BrushTex ("Brush Texture", 2D) = "gray" {}
        _BrushScale ("Brush Scale", Range(0.1, 40)) = 8
        _BrushStrength ("Brush Strength", Range(0, 1)) = 0.35

        _ShadowColor ("Shadow Color", Color) = (0.35, 0.28, 0.45, 1)
        _LightTint ("Light Tint", Color) = (1.25, 1.1, 0.85, 1)
        _ShadowBreakup ("Shadow Breakup", Range(0, 0.6)) = 0.18
        _Bands ("Light Bands", Range(2, 6)) = 3
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.25

        _RimColor ("Rim Color", Color) = (1, 0.85, 0.45, 1)
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.25
        _RimPower ("Rim Power", Range(0.5, 8)) = 3

        _OutlineColor ("Outline Color", Color) = (0.04, 0.025, 0.02, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.008
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        TEXTURE2D(_BrushTex);
        SAMPLER(sampler_BrushTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;

            half4 _BaseColor;
            half4 _ShadowColor;
            half4 _LightTint;

            half _BrushScale;
            half _BrushStrength;
            half _ShadowBreakup;
            half _Bands;
            half _AmbientStrength;

            half4 _RimColor;
            half _RimStrength;
            half _RimPower;

            half4 _OutlineColor;
            half _OutlineWidth;
        CBUFFER_END

        ENDHLSL

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite On

            HLSLPROGRAM

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                positionWS += normalWS * _OutlineWidth;

                output.positionHCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }

        Pass
        {
            Name "PainterlyForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);

                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = baseTex.rgb * _BaseColor.rgb;

                // Brush en world-space : les coups de pinceau restent stables dans le monde.
                float2 brushUV = input.positionWS.xz * _BrushScale;
                half brush = SAMPLE_TEXTURE2D(_BrushTex, sampler_BrushTex, brushUV).r;

                // Variation sale type peinture.
                half paintVariation = lerp(1.0h - _BrushStrength, 1.0h + _BrushStrength, brush);
                albedo *= paintVariation;

                Light mainLight = GetMainLight(input.shadowCoord);

                // Éclairage stylisé.
                half ndotl = dot(normalWS, mainLight.direction) * 0.5h + 0.5h;

                // La brush casse la frontière lumière/ombre.
                half breakup = (brush - 0.5h) * _ShadowBreakup;
                half rawLight = saturate(ndotl + breakup);

                // Toon bands.
                half bandCount = max(2.0h, _Bands);
                half bandedLight = floor(rawLight * bandCount) / (bandCount - 1.0h);
                bandedLight = saturate(bandedLight);

                // Ombres URP.
                bandedLight *= mainLight.shadowAttenuation;

                half3 shadowCol = albedo * _ShadowColor.rgb;
                half3 lightCol = albedo * _LightTint.rgb * mainLight.color;

                half3 color = lerp(shadowCol, lightCol, bandedLight);

                // Ambiance simple pour éviter les noirs morts.
                half3 ambient = SampleSH(normalWS) * albedo * _AmbientStrength;
                color += ambient;

                // Lumières additionnelles, très atténuées pour garder le style.
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();

                for (uint i = 0u; i < lightCount; i++)
                {
                    Light light = GetAdditionalLight(i, input.positionWS);
                    half addNdotL = saturate(dot(normalWS, light.direction));
                    half addBand = step(0.55h, addNdotL);

                    color += albedo * light.color * addBand * light.distanceAttenuation * light.shadowAttenuation * 0.25h;
                }
                #endif

                // Rim light illustration.
                half rim = pow(1.0h - saturate(dot(normalWS, viewDirWS)), _RimPower);
                color += _RimColor.rgb * rim * _RimStrength;

                return half4(color, baseTex.a * _BaseColor.a);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}