Shader "UrbanLobby/Tower Signal"
{
    Properties
    {
        [HDR] _SignalColor ("Signal Color", Color) = (1, 0.01, 0.03, 1)
        [Toggle(_TOWER_EYES)] _AnimateEyes ("Animate crown face", Float) = 0
        [Toggle(_TOWER_STONE)] _FacadeStone ("Faceted exterior stone", Float) = 0
        [Toggle(_TOWER_CONDUIT)] _Conduit ("Corrupted energy conduit", Float) = 0
        [Toggle(_TOWER_CRYSTAL)] _Crystal ("Faceted crown crystal", Float) = 0
        _IdleSeconds ("Rest between expressions (seconds)", Range(0.25, 8)) = 2.2
        _ExpressionSeconds ("Expression duration (seconds)", Range(0.3, 4)) = 1.3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma shader_feature_local _TOWER_EYES
            #pragma shader_feature_local_vertex _ _TOWER_STONE _TOWER_CONDUIT _TOWER_CRYSTAL
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #if defined(_TOWER_STONE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                half4 _SignalColor;
                float _AnimateEyes;
                float _FacadeStone;
                float _Conduit;
                float _Crystal;
                float _IdleSeconds;
                float _ExpressionSeconds;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fog : TEXCOORD0;
                half3 signalGain : TEXCOORD1;
                #if defined(_TOWER_EYES)
                    float2 panelUV : TEXCOORD2;
                    half3 mouthPose : TEXCOORD3;
                #endif
            };

            Varyings Vertex(float4 positionOS : POSITION, float3 normalOS : NORMAL, float2 uv : TEXCOORD0)
            {
                Varyings output;
                output.signalGain = 1.0h;
                #if defined(_TOWER_STONE)
                    // Broad planar highlights keep the setbacks readable in the plaza's night fog.
                    float3 facetLight = normalize(float3(-0.55, 0.70, -0.45));
                    half relief = 0.44h + 0.56h * saturate(dot(normalOS, facetLight));
                    half3 worldNormal = TransformObjectToWorldNormal(normalOS);
                    Light key = GetMainLight();
                    half3 sceneLight = SampleSH(worldNormal) + key.color * saturate(dot(worldNormal, key.direction));
                    output.signalGain = relief * (0.72h + 0.45h * saturate(sceneLight));
                #elif defined(_TOWER_CRYSTAL)
                    // A cool edge light and a brighter street-facing facet reveal the cut of each shard.
                    half3 normal = normalize(normalOS);
                    half street = saturate(dot(normal, normalize(float3(-0.55, -0.72, -0.80))));
                    half edge = saturate(dot(normal, normalize(float3(0.72, 0.30, -0.64))));
                    edge *= edge * edge;
                    output.signalGain = half3(0.23h + 0.88h * street + 0.60h * edge,
                                              0.16h + 2.0h * street + 4.8h * edge,
                                              0.42h + 1.5h * street + 3.1h * edge);
                #elif defined(_TOWER_CONDUIT)
                    // Facets stay legible below the cornices. Energy travels up the pipe in long bands.
                    float rim = saturate(dot(normalOS, normalize(float3(-0.5, 0.2, -0.8))));
                    float band = 0.5 + 0.5 * cos(positionOS.y * 0.18 - _Time.y * 1.8);
                    float pulse = band * band * band;
                    output.signalGain = half3(0.32 + 0.75 * rim + 0.18 * pulse,
                                              0.3 + 3.0 * rim + 4.0 * pulse,
                                              0.32 + 1.6 * rim + 1.8 * pulse);
                #endif
                #if defined(_TOWER_EYES)
                    // UVs 2..3 identify the front panel. Its mouth and the original
                    // eye mesh share this material, clock and Inspector settings.
                    bool panel = uv.x >= 2.0;
                    output.panelUV = float2(uv.x - 2.0, uv.y);
                    output.mouthPose = 0;
                    bool originalFace = positionOS.z < -12.4 && positionOS.y > 84.9 && positionOS.y < 95.0;
                    if (panel || originalFace)
                    {
                        float idle = max(_IdleSeconds, 0.25);
                        float duration = max(_ExpressionSeconds, 0.3);
                        float cycle = idle + duration;
                        float slot = floor(fmod(_Time.y / cycle, 6.0));
                        float phase = saturate((fmod(_Time.y, cycle) - idle) / duration);
                        float weight = smoothstep(0.0, 0.16, phase) * (1.0 - smoothstep(0.76, 1.0, phase));
                        float glance = (slot == 1.0 ? 1.0 : slot == 3.0 ? -1.0 : 0.0) * weight;
                        float blink = slot == 0.0 ? 1.0 - smoothstep(0.035, 0.18, abs(phase - 0.45)) : 0.0;
                        float grin = slot == 2.0 ? weight : 0.0;
                        float glitch = slot == 4.0 ? weight * step(0.36, frac(phase * 3.0)) : 0.0;
                        float fury = slot == 5.0 ? weight : 0.0;
                        // Displace opposite halves as units so every pixel keeps a hard edge.

                        output.signalGain = 1.0h + 0.9h * fury - 0.5h * glitch;
                        if (panel)
                        {
                            output.mouthPose = half3(grin, fury, glitch);
                        }
                        else if (positionOS.y > 87.5)
                        {
                            float restY = positionOS.y - 91.0;
                            positionOS.y -= restY * blink * 0.94;
                            positionOS.y += (positionOS.x > 0.0 ? 0.9 : -0.3) * grin;
                            positionOS.y += (abs(positionOS.x) * 0.22 - 1.3) * fury;
                            positionOS.x += 1.25 * glance + sign(positionOS.x) * 0.85 * glitch;
                        }
                        else
                        {
                            // The original three mouth pixels share a mesh with the eyes.
                            // Collapse their triangles while the panel draws the larger mouth.
                            positionOS.xyz = float3(0.0, 86.0, -12.6);
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
                half3 signal = _SignalColor.rgb * input.signalGain;
                #if defined(_TOWER_EYES)
                    if (input.panelUV.x >= 0.0)
                    {
                        float2 pixel = floor(input.panelUV * float2(31.0, 23.0));
                        float column = pixel.x - 15.0 - round(input.mouthPose.z * 2.0);
                        float distance = abs(column);
                        float grin = input.mouthPose.x;
                        float fury = input.mouthPose.y;
                        float width = 4.0 + round(3.0 * max(grin, fury));
                        float restRow = 5.0 - floor(distance * 0.5);
                        float smileRow = 3.0 + floor(distance * 0.65);
                        float snarlRow = 6.0 - floor(distance * 0.38);
                        float row = round(lerp(lerp(restRow, smileRow, grin), snarlRow, fury));
                        bool tooth = fury > 0.5 && fmod(distance, 3.0) < 0.5 && pixel.y == row - 1.0;
                        bool lit = distance <= width && (pixel.y == row || tooth);
                        signal = lit ? signal : half3(0.014, 0.011, 0.025);
                    }
                #endif
                half3 fogged = MixFog(signal, input.fog);
                return half4(lerp(signal, fogged, 0.22h), 1);
            }
            ENDHLSL
        }
    }
}
