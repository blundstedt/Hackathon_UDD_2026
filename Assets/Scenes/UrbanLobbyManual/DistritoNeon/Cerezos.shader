Shader "Lobby/Cerezos del distrito"
{
    Properties { _BaseMap ("Atlas de hojas", 2D) = "white" {} _Cutoff ("Recorte", Range(0,1)) = .5 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST; float _Cutoff;
            CBUFFER_END
            struct Attributes { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.world=TransformObjectToWorld(v.position.xyz);
                o.position=TransformWorldToHClip(o.world); o.uv=TRANSFORM_TEX(v.uv,_BaseMap); return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half4 leaf=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                clip(leaf.a-_Cutoff);
                float3 p=i.world;
                float clusters=.5+.5*sin(p.x*1.4+p.z*.65+sin(p.z*1.8)+p.y*.9);
                float light=saturate(.105*p.y+leaf.g*.67+clusters*.36-.34);
                float3 petal=lerp(float3(.235,.08,.20),float3(1,.48,.74),light);
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    petal=SRGBToLinear(petal);
                #endif
                return half4(petal,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
