// Based on URP 17.3.0 Sprite-Lit-Default and its shared 2D includes.
Shader "NUMBER/SpriteFlash"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _FlashColor("Flash Color", Color) = (1,1,1,1)
        _FlashAmount("Flash Amount", Range(0,1)) = 0
        _GlitchAmount("Glitch Amount", Range(0,1)) = 0
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

        // Identical layout in both passes; never conditional on shader keywords.
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half4 _FlashColor;
            float4 _MainTex_TexelSize;
            half _FlashAmount;
            half _GlitchAmount;
        CBUFFER_END

        float2 GlitchUV(float2 uv)
        {
            // Four source-texel rows, updated 12 times/second; also stable in the normals pass.
            float seed = floor(uv.y * _MainTex_TexelSize.w * 0.25) + floor(_Time.y * 12.0) * 17.0;
            float hash = frac(seed * 0.1031);
            hash = frac(hash * (hash + 33.33) * (hash + hash));
            float shift = (floor(hash * 5.0) - 2.0) * step(0.65, frac(hash * 13.0));
            uv.x += shift * _MainTex_TexelSize.x * _GlitchAmount;
            return uv;
        }
        ENDHLSL

        Pass
        {
            Name "SpriteLit"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex LitVertex
            #pragma fragment LitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            Varyings LitVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 litColor;
                UNITY_BRANCH
                if (_GlitchAmount > 0.0h)
                {
                    input.uv = GlitchUV(input.uv);
                    half4 main = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                    float2 split = float2(_MainTex_TexelSize.x * _GlitchAmount, 0.0);
                    half red = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + split).r;
                    half2 cyan = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - split).gb;
                    main.rgb = lerp(main.rgb, half3(red, cyan), 0.35h * _GlitchAmount);
                    main *= input.color;

                    half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                    half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                    SurfaceData2D surfaceData;
                    InputData2D inputData;
                    InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                    InitializeInputData(input.uv, input.lightingUV, inputData);
                    #if defined(DEBUG_DISPLAY)
                    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                    surfaceData.normalWS = input.normalWS;
                    #endif
                    litColor = CombinedShapeLightShared(surfaceData, inputData);
                }
                else
                {
                    // Exact stock shading path, with no glitch texture reads or row arithmetic.
                    litColor = CommonLitFragment(input, input.color);
                }

                // After all lighting and tint: full flash stays white even with black lighting.
                litColor.rgb = lerp(litColor.rgb, _FlashColor.rgb, _FlashAmount);
                return litColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "SpriteNormals"
            Tags { "LightMode"="NormalsRendering" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex NormalsVertex
            #pragma fragment NormalsFragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color : COLOR;
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            Varyings NormalsVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 NormalsFragment(Varyings input) : SV_Target
            {
                UNITY_BRANCH
                if (_GlitchAmount > 0.0h)
                    input.uv = GlitchUV(input.uv);
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
