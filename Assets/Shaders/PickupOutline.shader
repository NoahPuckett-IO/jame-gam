// a thin white line round things Henry can pick up: the mesh's back faces pushed out a fixed number of pixels
Shader "JameGam/Pickup Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Width ("Width (pixels at 1080p)", Float) = 1.6
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry+10" }
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Width;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float4 clip = TransformObjectToHClip(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                float2 dir = mul((float3x3)UNITY_MATRIX_VP, normalWS).xy;
                float len = length(dir);
                dir = len > 1e-5 ? dir / len : float2(0, 0);
                float2 px = _Width * (_ScreenParams.y / 1080.0) * 2.0 / _ScreenParams.xy;
                clip.xy += dir * px * clip.w;
                o.positionCS = clip;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
