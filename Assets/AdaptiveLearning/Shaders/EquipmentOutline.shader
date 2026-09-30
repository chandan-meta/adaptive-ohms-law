Shader "AdaptiveLearning/EquipmentOutline"
{
    Properties
    {
        _Color ("Outline colour", Color) = (1, .35, .025, 1)
        _Width ("Width in screen pixels", Float) = 3
        _StencilComp ("Stencil comparison", Float) = 6
        _StencilPass ("Stencil operation", Float) = 0
        _ColorMask ("Colour write mask", Float) = 15
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask [_ColorMask]
            Stencil { Ref 64 ReadMask 64 WriteMask 64 Comp [_StencilComp] Pass [_StencilPass] }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Width;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(input.normalOS));
                float2 direction = mul((float3x3)UNITY_MATRIX_P, normalVS).xy;
                direction /= max(length(direction), .001);
                output.positionCS.xy += direction * (2.0 * _Width / _ScreenParams.xy) * output.positionCS.w;
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
