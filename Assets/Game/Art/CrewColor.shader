Shader "Chromacolony/CrewColor"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        _BaseHue ("Hue of the drawing", Float) = 0.0226
        _BaseSaturation ("Saturation of the drawing", Float) = 0.648
        _BaseValue ("Value of the drawing", Float) = 0.847
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _BaseHue;
                float _BaseSaturation;
                float _BaseValue;
            CBUFFER_END

            float3 RgbToHsv(float3 c)
            {
                float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, k.wz), float4(c.gb, k.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HsvToRgb(float3 c)
            {
                float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + k.xyz) * 6.0 - k.www);
                return c.z * lerp(k.xxx, saturate(p - k.xxx), c.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 drawn = RgbToHsv(texel.rgb);
                float3 crew = RgbToHsv(input.color.rgb);
                float hue = frac(crew.x + (drawn.x - _BaseHue) + 1.0);
                float saturation = saturate(drawn.y * crew.y / max(_BaseSaturation, 0.001));
                float value = saturate(drawn.z * crew.z / max(_BaseValue, 0.001));
                float3 turned = HsvToRgb(float3(hue, saturation, value));

                float apart = abs(drawn.x - _BaseHue);
                apart = min(apart, 1.0 - apart);
                float body = saturate(1.0 - (apart - 0.04) / 0.05) * saturate(drawn.y / 0.18);
                float3 kept = texel.rgb * saturate(crew.z / max(_BaseValue, 0.001));
                return float4(lerp(kept, turned, body), texel.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
