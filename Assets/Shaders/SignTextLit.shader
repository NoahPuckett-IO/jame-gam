// TextMesh text printed on things (cereal boxes, matchboxes, signs that aren't backlit). Same font-atlas alpha as
// Sign Text, but shaded by the scene's lights and ambient like the surface it's printed on, so it doesn't glow
// in the dark.
Shader "JameGam/Sign Text Lit"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fog : TEXCOORD3;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                // a TextMesh faces its local -Z
                o.normalWS = TransformObjectToWorldNormal(float3(0, 0, -1));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                half3 n = normalize(front ? i.normalWS : -i.normalWS);
                half3 light = SampleSH(n);
                Light main = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                light += main.color * (main.distanceAttenuation * main.shadowAttenuation * saturate(dot(n, main.direction)));
            #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                LIGHT_LOOP_BEGIN(count)
                    Light l = GetAdditionalLight(lightIndex, i.positionWS);
                    light += l.color * (l.distanceAttenuation * saturate(dot(n, l.direction)));
                LIGHT_LOOP_END
            #endif
                half4 c = i.color;
                c.rgb *= light;
                c.a *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                c.rgb = MixFog(c.rgb, i.fog);
                return c;
            }
            ENDHLSL
        }
    }
}
