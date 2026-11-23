Shader "Piso9/Cristal arquitectónico"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct V{float4 pos:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;};
            V vert(float4 p:POSITION,float3 n:NORMAL){V o;o.w=TransformObjectToWorld(p.xyz);o.pos=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(n);return o;}
            half4 frag(V i):SV_Target{
                float fres=pow(1-abs(dot(normalize(_WorldSpaceCameraPos-i.w),normalize(i.n))),2);
                float stripMask=1-smoothstep(.015,.045,abs(i.w.y-1.48));
                return half4(.18+.14*stripMask,.34+.16*stripMask,.42+.18*stripMask,.075+.20*fres+.27*stripMask);
            }
            ENDHLSL
        }
    }
}
