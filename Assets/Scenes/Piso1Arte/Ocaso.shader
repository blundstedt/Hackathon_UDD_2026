Shader "Piso1/Ocaso"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            struct V { float4 pos:SV_POSITION; float3 ray:TEXCOORD0; };
            V vert(float4 p:POSITION){ V o;o.pos=TransformObjectToHClip(p.xyz);o.ray=p.xyz;return o; }
            float hash(float2 p){return frac(sin(dot(p,float2(121.3,47.7)))*43758.54);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            half4 frag(V i):SV_Target
            {
                float3 d=normalize(i.ray),sun=normalize(float3(-.43,.205,1));
                float h=max(d.y,0),sunDist=length(d-sun);
                float3 sky=lerp(float3(.98,.65,.40),float3(.85,.43,.50),smoothstep(.015,.34,h));
                sky=lerp(sky,float3(.33,.30,.52),smoothstep(.30,.9,h));
                sky+=float3(.24,.12,-.08)*exp(-sunDist*3.8);
                float2 p=d.xz/max(.065,h+.06)*float2(1.24,6.9);
                float cloud=noise(p)*.57+noise(p*2.5)*.28+noise(p*5.4)*.15;
                float mask=smoothstep(.50,.69,cloud)*smoothstep(.015,.16,h);
                sky=lerp(sky,lerp(float3(.95,.56,.46),float3(.56,.40,.55),smoothstep(.54,.73,cloud)),mask*.68);
                sky=lerp(sky,float3(1,.88,.56),1-smoothstep(.035,.038,sunDist));
                return half4(SRGBToLinear(saturate(sky)),1);
            }
            ENDHLSL
        }
    }
}
