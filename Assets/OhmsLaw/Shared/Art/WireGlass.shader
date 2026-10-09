Shader "OhmsLaw/WireGlass"
{
    Properties
    {
        _BaseColor ("Glass tint", Color) = (0.2,0.65,0.9,0.08)
        _EmissionColor ("Edge tint", Color) = (0.1,0.45,0.65,1)
        _Smoothness ("Highlight smoothness", Range(0,1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; half fog : TEXCOORD2; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half rim = pow(1 - saturate(abs(dot(n, view))), 3);
                Light light = GetMainLight();
                half highlight = pow(saturate(dot(n, normalize(light.direction + view))), lerp(32,128,_Smoothness));
                half alpha = saturate(_BaseColor.a + rim * 0.45 + highlight * 0.16);
                half3 reflection = GlossyEnvironmentReflection(reflect(-view, n), 0.16h, 1.0h);
                half3 colour = _BaseColor.rgb * (0.4 + rim * 1.4) + _EmissionColor.rgb * rim + reflection * rim * 0.7 + light.color * highlight * 0.7;
                return half4(MixFog(colour, i.fog) * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
