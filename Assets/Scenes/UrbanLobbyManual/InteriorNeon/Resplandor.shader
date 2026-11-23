Shader "Lobby/Resplandor de superficie"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct In { float4 p:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Out { float4 p:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Out Vert(In i) { Out o; o.p=TransformObjectToHClip(i.p.xyz); o.uv=i.uv; o.color=i.color; return o; }
            half4 Frag(Out i):SV_Target
            {
                float2 d=abs(i.uv*2-1);
                float intensity=pow(saturate(1-d.x*d.x),2)*pow(saturate(1-d.y),3);
                return half4(i.color.rgb*i.color.rgb*intensity*i.color.a,0);
            }
            ENDHLSL
        }
    }
}
