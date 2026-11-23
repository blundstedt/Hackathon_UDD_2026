Shader "Piso1/Bruma baja"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            struct V { float4 pos:SV_POSITION; float3 w:TEXCOORD0; };
            V vert(float4 p:POSITION){V o;o.w=TransformObjectToWorld(p.xyz);o.pos=TransformWorldToHClip(o.w);return o;}
            half4 frag(V i):SV_Target
            {
                float t=_Time.y*.12;
                float wave=sin(i.w.x*.19+t)+.6*sin(i.w.z*.37-t)+.38*sin(i.w.x*.73+i.w.z*.28);
                float h=(i.w.y-2.65-.56*wave)/1.65;
                return half4(SRGBToLinear(half3(.65,.49,.76)),exp(-h*h)*(.18+.075*wave));
            }
            ENDHLSL
        }
    }
}
