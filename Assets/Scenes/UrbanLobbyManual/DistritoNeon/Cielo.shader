Shader "Lobby/Cielo del distrito"
{
    SubShader
    {
        Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            struct In { float4 p:POSITION; };
            struct Out { float4 p:SV_POSITION; float3 direction:TEXCOORD0; };
            Out Vert(In v) { Out o; o.p=TransformObjectToHClip(v.p.xyz); o.direction=v.p.xyz; return o; }
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*14857.731);}
            float cloudNoise(float2 p)
            {
                float2 cell=floor(p), k=frac(p); k=k*k*(3-2*k);
                return lerp(lerp(hash(cell),hash(cell+float2(1,0)),k.x),lerp(hash(cell+float2(0,1)),hash(cell+1),k.x),k.y);
            }
            half4 Frag(Out i):SV_Target
            {
                float3 d=normalize(i.direction);
                float h=saturate(d.y);
                float3 c=lerp(float3(.335,.235,.38),float3(.065,.103,.21),smoothstep(-.04,.6,h));
                float2 sky=d.xz/(.24+h)*float2(2.8,5.2);
                float cloud=.58*cloudNoise(sky)+.28*cloudNoise(sky*2.15)+.14*cloudNoise(sky*4.55);
                c+=(cloud-.48)*float3(.155,.14,.20)*smoothstep(0,.12,h);
                float2 beam=abs(float2(d.x+.47-.24*d.y,d.x-.66-.29*d.y));
                float shafts=exp2(-beam.x*39)+exp2(-beam.y*48);
                c+=shafts*saturate(-d.z)*exp2(-h*4.5)*float3(.12,.20,.28);
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    c=SRGBToLinear(c);
                #endif
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
