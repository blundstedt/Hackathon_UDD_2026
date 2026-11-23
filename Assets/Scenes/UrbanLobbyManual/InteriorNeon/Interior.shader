Shader "Lobby/Interior de la torre"
{
    Properties
    {
        _Ink ("Tinte", Vector) = (1,1,1,1)
        _VertexInk ("Color de la malla", Float) = 1
        _Emission ("Luminancia", Float) = 0
        _Floor ("Baldosas pulidas", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Ink;
            float _VertexInk, _Emission, _Floor;
            CBUFFER_END
            struct Attributes { float4 p:POSITION; float3 n:NORMAL; half4 color:COLOR; };
            struct Varyings { float4 p:SV_POSITION; float3 ws:TEXCOORD0; half3 n:TEXCOORD1; half4 ink:COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.ws=TransformObjectToWorld(v.p.xyz);
                o.p=TransformWorldToHClip(o.ws);
                o.n=TransformObjectToWorldNormal(v.n);
                o.ink=lerp(float4(_Ink.rgb,_Emission),v.color,_VertexInk);
                return o;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(27.31,61.17)))*1537.54); }
            float spill(float2 delta,float width) { return exp2(-dot(delta,delta)/width); }
            half4 Frag(Varyings i):SV_Target
            {
                float3 n=normalize(i.n);
                float3 v=SafeNormalize(_WorldSpaceCameraPos.xyz-i.ws);
                float2 p=i.ws.xz-float2(.92,-70.70);
                float pink=spill(p-float2(-8,-4),54)+.6*spill(p-float2(16,10),50);
                float gold=spill(p-float2(17,-9),55)+spill(p-float2(0,-15),22);
                float blue=spill(p-float2(-23,9),72)+spill(p-float2(23,-3),60);
                float3 tone=float3(.74,.77,.94)+pink*float3(.51,-.08,.34)+gold*float3(.43,.16,-.05)+blue*float3(-.06,.24,.45);
                float light=.66+.35*saturate(dot(n,normalize(float3(-.42,.76,.50))))+.18*abs(n.x);
                float3 c=i.ink.rgb*tone*light;
                c+=float3(.08,.045,.10)*pow(1-abs(dot(v,n)),3);
                float ceilingLight = spill(abs(frac(p/float2(11,8)+.5)-.5)*float2(11,8),7);
                c *= .82 + .30*ceilingLight + .10*saturate(n.y);
                float height=i.ws.y;
                float receptionLight=exp2(-.9*abs(p.x+8.18)-.34*abs(height-1.2)-.7*max(abs(p.y+3.5)-6.9,0));
                float barLight=exp2(-1.35*abs(p.y+7.03)-.7*abs(height-1.45)-.6*max(abs(p.x-17)-4.8,0));
                float columns=exp2(-.55*abs(abs(p.x)-24)-.35*abs(height-4.6)-.8*abs(frac((p.y+4)/10+.5)-.5)*10);
                c+=receptionLight*float3(.22,.032,.14)+barLight*float3(.35,.11,.035)+columns*float3(.22,.06,.15);
                if(_Floor>.5){
                    float2 tile=p/2.45;
                    float2 edge=(.5-abs(frac(tile)-.5))/max(fwidth(tile),.0001);
                    c*=lerp(.48,1,saturate(min(edge.x,edge.y))) * (.92+.1*hash(floor(tile)));
                    // Soft reflection of the repeating rectangular ceiling luminaires.
                    float2 ceiling=p-v.xz*8.1/max(v.y,.06);
                    float2 lamp=abs(frac(ceiling/float2(11,8)+.5)-.5)*float2(11,8);
                    float blur=.55+.035*length(i.ws-_WorldSpaceCameraPos.xyz);
                    float2 d=max(lamp-float2(1.18,.34),0);
                    float reflection=spill(d,blur)*saturate(1-abs(ceiling.x)/38)*saturate(1-abs(ceiling.y)/38);
                    float2 signHit=p-v.xz*1.35/max(v.y,.06);
                    float barGlint=exp2(-2.5*abs(signHit.y+7.06)-1.4*max(abs(signHit.x-17)-4.8,0));
                    float deskGlint=exp2(-2.5*abs(signHit.x+8.2)-1.4*max(abs(signHit.y+3.5)-7,0));
                    c+=reflection*float3(.29,.11,.22)+pink*float3(.13,.018,.09)+gold*float3(.12,.055,.01)+blue*float3(.015,.05,.10);
                    c+=barGlint*float3(.52,.21,.035)+deskGlint*float3(.47,.035,.31);
                }
                c=lerp(c,i.ink.rgb,i.ink.a);
                #if !defined(UNITY_COLORSPACE_GAMMA)
                c=SRGBToLinear(c);
                #endif
                return half4(c*(1+i.ink.a*.8),1);
            }
            ENDHLSL
        }
    }
}
