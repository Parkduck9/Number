// Elite duel focus overlay (DECISIONS 49): a full-screen quad on the DuelBlur sorting layer that shows the 2D Renderer's
// _CameraSortingLayerTexture (everything up to the World layer, Renderer2D "Foremost Sorting Layer") lightly blurred, desaturated
// and darkened, blended in by _Amount. 13 taps; the radius is in pixels of a 1080p screen, so it scales with the resolution.
// In Resources so Shader.Find works in builds. URP HLSL, WebGL2-safe (no compute, plain texture samples).
Shader "Number/DuelFocusBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (unused)", 2D) = "white" {}
        _Amount ("Amount", Range(0,1)) = 0
        _RadiusPx ("Blur Radius (px at 1080p)", Float) = 4.0
        _Brightness ("Brightness", Range(0,1)) = 0.55
        _Desaturate ("Desaturate", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CameraSortingLayerTexture);
            SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                half _Amount;
                float _RadiusPx;
                half _Brightness;
                half _Desaturate;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 screenPos : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos = ComputeScreenPos(o.positionCS); // same screen UV as Shader Graph's Scene Color in the 2D Renderer
                return o;
            }

            half3 Tap(float2 uv) { return SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, uv).rgb; }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float2 r = (_RadiusPx / 1080.0) * float2(_ScaledScreenParams.y / _ScaledScreenParams.x, 1.0); // radius in UV
                float2 h = r * 0.5;
                float2 d = r * 0.7;
                half3 c = Tap(uv) * 0.2h;
                c += (Tap(uv + float2(h.x, 0)) + Tap(uv - float2(h.x, 0)) + Tap(uv + float2(0, h.y)) + Tap(uv - float2(0, h.y))) * 0.1h;
                c += (Tap(uv + float2(r.x, 0)) + Tap(uv - float2(r.x, 0)) + Tap(uv + float2(0, r.y)) + Tap(uv - float2(0, r.y))) * 0.06h;
                c += (Tap(uv + d) + Tap(uv - d) + Tap(uv + float2(d.x, -d.y)) + Tap(uv + float2(-d.x, d.y))) * 0.04h;
                half l = dot(c, half3(0.299h, 0.587h, 0.114h));
                c = lerp(c, half3(l, l, l), _Desaturate) * _Brightness;
                return half4(c, _Amount);
            }
            ENDHLSL
        }
    }
}
