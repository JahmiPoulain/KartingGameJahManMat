Shader "Custom/URP/PainterlyScreenFilter"
{
    Properties
    {
        _EffectStrength ("Effect Strength", Range(0, 1)) = 1

        _PixelSize ("Pixel Size", Range(1, 8)) = 1
        _ColorSteps ("Color Steps", Range(2, 32)) = 10
        _DitherStrength ("Dither Strength", Range(0, 0.08)) = 0.015

        _BrushTex ("Brush / Paper Texture", 2D) = "gray" {}
        _BrushScale ("Brush Scale", Range(1, 80)) = 18
        _BrushDetailScale ("Brush Detail Scale", Range(1, 160)) = 55
        _BrushStrength ("Brush Strength", Range(0, 0.5)) = 0.12
        _BrushContrast ("Brush Contrast", Range(0.1, 4)) = 1.4

        _ShadowTint ("Shadow Tint", Color) = (0.55, 0.47, 0.72, 1)
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.30

        _LightTint ("Light Tint", Color) = (1.18, 1.08, 0.82, 1)
        _LightStrength ("Light Strength", Range(0, 1)) = 0.18

        _Saturation ("Saturation", Range(0, 2)) = 1.12
        _Contrast ("Contrast", Range(0, 2)) = 1.08

        _EdgeColor ("Edge Color", Color) = (0.055, 0.035, 0.025, 1)
        _EdgeStrength ("Edge Strength", Range(0, 1)) = 0.55
        _EdgeThreshold ("Edge Threshold", Range(0.005, 0.5)) = 0.085
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.3)) = 0.055
        _EdgeWidth ("Edge Width", Range(0.5, 4)) = 1.25
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "PainterlyScreenFilter"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_BrushTex);
            SAMPLER(sampler_BrushTex);

            CBUFFER_START(UnityPerMaterial)
                half _EffectStrength;

                half _PixelSize;
                half _ColorSteps;
                half _DitherStrength;

                half _BrushScale;
                half _BrushDetailScale;
                half _BrushStrength;
                half _BrushContrast;

                half4 _ShadowTint;
                half _ShadowStrength;

                half4 _LightTint;
                half _LightStrength;

                half _Saturation;
                half _Contrast;

                half4 _EdgeColor;
                half _EdgeStrength;
                half _EdgeThreshold;
                half _EdgeSoftness;
                half _EdgeWidth;
            CBUFFER_END

            half Luma(half3 c)
            {
                return dot(c, half3(0.2126h, 0.7152h, 0.0722h));
            }

            half Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float2 PixelateUV(float2 uv, half pixelSize)
            {
                pixelSize = max(pixelSize, 1.0h);

                float2 screenSize = _ScreenParams.xy;
                float2 pixelCoord = floor(uv * screenSize / pixelSize) * pixelSize + pixelSize * 0.5;
                return pixelCoord / screenSize;
            }

            half SampleBrush(float2 uv)
            {
                half b = SAMPLE_TEXTURE2D(_BrushTex, sampler_BrushTex, uv).r;
                b = saturate((b - 0.5h) * _BrushContrast + 0.5h);
                return b;
            }

            half GetBrush(float2 uv)
            {
                // Deux couches avec UV un peu tournés pour éviter le motif trop évident.
                float2 uv1 = uv * _BrushScale;

                float2 centered = uv - 0.5;
                float2 uvRot;
                uvRot.x = centered.x * 0.74 - centered.y * 0.67;
                uvRot.y = centered.x * 0.67 + centered.y * 0.74;
                uvRot += 0.5;

                float2 uv2 = uvRot * _BrushDetailScale + float2(0.173, 0.619);

                half b1 = SampleBrush(uv1);
                half b2 = SampleBrush(uv2);

                return saturate(b1 * 0.65h + b2 * 0.35h);
            }

            half EdgeFromColor(float2 uv, float2 texel)
            {
                half3 cL = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texel.x, 0)).rgb;
                half3 cR = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texel.x, 0)).rgb;
                half3 cD = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(0, texel.y)).rgb;
                half3 cU = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, texel.y)).rgb;

                half lumX = abs(Luma(cR) - Luma(cL));
                half lumY = abs(Luma(cU) - Luma(cD));

                half colX = length(cR - cL);
                half colY = length(cU - cD);

                half edgeRaw = max(lumX + lumY, (colX + colY) * 0.33h);
                return smoothstep(_EdgeThreshold, _EdgeThreshold + _EdgeSoftness, edgeRaw);
            }

            half3 AdjustSaturation(half3 color, half saturation)
            {
                half l = Luma(color);
                return lerp(half3(l, l, l), color, saturation);
            }

            half3 AdjustContrast(half3 color, half contrast)
            {
                return saturate((color - 0.5h) * contrast + 0.5h);
            }

            half3 Posterize(half3 color, half steps)
            {
                steps = max(steps, 2.0h);
                return floor(color * steps + 0.5h) / steps;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float2 uvPix = PixelateUV(uv, _PixelSize);

                half4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvPix);
                half3 original = src.rgb;
                half3 color = original;

                // Petit dither avant posterize : casse les aplats numériques trop moches.
                half noise = Hash12(floor(uv * _ScreenParams.xy));
                color += (noise - 0.5h) * _DitherStrength;

                color = saturate(color);
                color = AdjustContrast(color, _Contrast);
                color = AdjustSaturation(color, _Saturation);

                // Teinte painterly selon la luminance.
                half lum = Luma(color);
                half shadowMask = 1.0h - smoothstep(0.22h, 0.68h, lum);
                half lightMask = smoothstep(0.52h, 0.96h, lum);

                color = lerp(color, color * _ShadowTint.rgb, shadowMask * _ShadowStrength);
                color = lerp(color, color * _LightTint.rgb, lightMask * _LightStrength);

                // Réduction de couleurs.
                color = Posterize(color, _ColorSteps);

                // Texture de peinture/papier en screen-space.
                half brush = GetBrush(uv);
                half brushMul = lerp(1.0h - _BrushStrength, 1.0h + _BrushStrength, brush);
                color *= brushMul;

                // Contours basés sur les différences de couleur.
                float2 texel = (_ScreenParams.zw - 1.0) * _EdgeWidth;
                half edge = EdgeFromColor(uvPix, texel);
                color = lerp(color, _EdgeColor.rgb, saturate(edge * _EdgeStrength));

                // Blend global pour pouvoir doser l'effet dans le material.
                color = lerp(original, color, _EffectStrength);

                return half4(saturate(color), src.a);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
