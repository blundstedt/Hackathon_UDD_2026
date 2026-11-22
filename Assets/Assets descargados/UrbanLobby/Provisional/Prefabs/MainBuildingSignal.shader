Shader "UrbanLobby/Tower Signal"
{
    Properties
    {
        [HDR] _SignalColor ("Signal Color", Color) = (1, 0.01, 0.03, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SignalColor;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fog : TEXCOORD0;
            };

            Varyings Vertex(float4 positionOS : POSITION)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(positionOS.xyz);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                // Keep the tower beacon legible across the foggy plaza.
                half3 fogged = MixFog(_SignalColor.rgb, input.fog);
                return half4(lerp(_SignalColor.rgb, fogged, 0.22h), 1);
            }
            ENDHLSL
        }
    }
}
