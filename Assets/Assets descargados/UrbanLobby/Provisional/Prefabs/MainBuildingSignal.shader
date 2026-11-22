Shader "UrbanLobby/Tower Signal"
{
    Properties
    {
        [HDR] _SignalColor ("Signal Color", Color) = (1, 0.01, 0.03, 1)
        [Toggle(_TOWER_EYES)] _AnimateEyes ("Animate crown face", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma shader_feature_local_vertex _TOWER_EYES
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SignalColor;
                float _AnimateEyes;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fog : TEXCOORD0;
            };

            Varyings Vertex(float4 positionOS : POSITION)
            {
                Varyings output;
                #if defined(_TOWER_EYES)
                    // The crown shares a mesh with the antenna. Animate only the
                    // eye and mouth rows on the front display, in model space.
                    if (positionOS.z < -12.4 && positionOS.y > 84.9 && positionOS.y < 95.0)
                    {
                        float t = fmod(_Time.y, 9.0);
                        float glance = smoothstep(0.6, 1.0, t) - smoothstep(2.1, 2.6, t)
                                     - smoothstep(4.0, 4.5, t) + smoothstep(6.3, 6.8, t);
                        float blink = 1.0 - smoothstep(0.035, 0.18, abs(t - 3.1));
                        float brow = smoothstep(4.4, 4.9, t) - smoothstep(5.9, 6.4, t);
                        float surprise = smoothstep(6.55, 6.9, t) - smoothstep(7.8, 8.3, t);
                        if (positionOS.y > 87.5)
                        {
                            float restY = positionOS.y - 91.0;
                            positionOS.y -= restY * blink * 0.94;
                            positionOS.y += (positionOS.x > 0.0 ? 0.9 : -0.3) * brow;
                            positionOS.y += (0.35 + 0.12 * restY) * surprise;
                            positionOS.x += 1.25 * glance;
                        }
                        else
                        {
                            float corner = smoothstep(0.30, 0.55, abs(positionOS.x));
                            // The raised brow pulls the mouth into a crooked smile.
                            float smile = lerp(-0.66, 0.48 + 0.34 * sign(positionOS.x), corner);
                            positionOS.y += smile * brow;
                            positionOS.y -= surprise * (0.40 + 0.62 * corner);
                            positionOS.x *= 1.0 + 0.42 * brow - 0.37 * surprise;
                            positionOS.x += 0.42 * glance;
                        }
                    }
                #endif
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
