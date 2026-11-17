// 상호작용 구역 빛기둥: 바닥이 가장 밝고 위로 갈수록 사라지며, 노이즈 무늬가 위로 흐른다. 텍스처 없음.
// 단위 큐브(-0.5~0.5) 메시를 전제로 한다. 스펙: design/quick-specs/interaction-zone-glow-2026-11-17.md
// 대상: URP Forward (PC·Mobile 렌더러 공통), Unlit 투명 1패스, 텍스처 샘플 0, 키워드·변형 없음.
Shader "Game/InteractionZoneGlow"
{
    Properties
    {
        [Header(Look)]
        _BaseColor ("Color", Color) = (0.3, 0.8, 1, 1)
        _Intensity ("Intensity", Range(0.1, 2)) = 0.6
        _FadePower ("Fade Power", Range(0.5, 4)) = 2
        [Header(Motion)]
        _RiseSpeed ("Rise Speed", Range(0.05, 1)) = 0.25
        _NoiseScale ("Noise Scale", Range(1, 8)) = 3
        _NoiseContrast ("Noise Contrast", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Blend SrcAlpha One
            ZWrite Off
            // 앞뒤 벽이 모두 보여야 기둥처럼 읽힌다.
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Intensity;
                half _FadePower;
                half _RiseSpeed;
                half _NoiseScale;
                half _NoiseContrast;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float heightOS : TEXCOORD1;
                half sideMask : TEXCOORD2;
            };

            // 흔히 쓰는 sin 기반 해시 상수. 값 자체에 의미는 없고 격자점마다 0~1 난수를 준다.
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.heightOS = input.positionOS.y + 0.5;
                // 윗면·바닥면은 지우고 옆벽만 남긴다.
                output.sideMask = 1.0 - abs(input.normalOS.y);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half fade = pow(saturate(1.0 - input.heightOS), _FadePower);

                // 벽을 따라가는 가로 좌표(x+z)와 세로(y). 시간만큼 아래로 당겨 무늬가 위로 흐르게 한다.
                float2 p = float2(input.positionWS.x + input.positionWS.z,
                                  input.positionWS.y - _Time.y * _RiseSpeed) * _NoiseScale;
                half noise = lerp(1.0 - _NoiseContrast, 1.0, ValueNoise(p));

                half alpha = saturate(fade * noise * input.sideMask);
                return half4(_BaseColor.rgb * _Intensity, alpha);
            }
            ENDHLSL
        }
    }
}
