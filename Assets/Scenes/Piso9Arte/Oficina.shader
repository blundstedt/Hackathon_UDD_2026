Shader "Piso9/Acabados de oficina"
{
    Properties { _LightAtlas("Iluminación de la planta",2D)="gray"{} }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            TEXTURE2D(_LightAtlas); SAMPLER(sampler_LightAtlas);
            struct V {float4 pos:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;half4 c:COLOR;};
            struct A {float4 p:POSITION;float3 n:NORMAL;half4 c:COLOR;};
            V vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.pos=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;return o;}
            float hash(float2 v){return frac(sin(dot(v,float2(37.1,91.7)))*43758.545);}
            half4 frag(V i):SV_Target
            {
                float3 rgb=i.c.rgb;float style=i.c.a;float3 w=i.w;
                float2 uv=(w.xz+6.4)/float2(115.2,102.4);
                float3 atlas=SAMPLE_TEXTURE2D(_LightAtlas,sampler_LightAtlas,saturate(uv)).rgb;
                if(style>.37 && style<.43){
                    float u=abs(i.n.x)>.7?w.z:w.x;
                    float2 g=float2(u/1.12,w.y/2.5), f=frac(g);
                    float glow=hash(floor(g)+floor(w.xz*.05));
                    float pane=step(.13,f.x)*step(f.x,.83)*step(.14,f.y)*step(f.y,.78)*step(abs(i.n.y),.4);
                    rgb=lerp(rgb*(.68+.28*hash(floor(g))),lerp(float3(.28,.39,.54),float3(.96,.65,.39),glow),pane*smoothstep(.51,.90,glow));
                    return half4(SRGBToLinear(rgb),1);
                }
                if(style>.95){
                    // Only a few overhead tubes drift in brightness in the archive wing.
                    float drift=1;
                    if(w.y>6.3 && w.x>52 && w.z>45 && hash(floor(w.xz/6.4))>.68)
                        drift-=.14*pow(.5+.5*sin(_Time.y*2.1+w.x*.08),6);
                    return half4(SRGBToLinear(rgb)*1.55*drift,1);
                }
                if(style>.085 && style<.115){
                    float2 tile=w.xz/1.28;float2 f=frac(tile);float stripe=fmod(floor(tile.x)+floor(tile.y),2)==0?w.x:w.z;
                    float seam=smoothstep(.004,.016,min(min(f.x,1-f.x),min(f.y,1-f.y)));
                    rgb*= (.92+.08*seam)*(.92+.09*hash(floor(tile))) * (1+.017*sin(stripe*160)/(1+length(fwidth(w.xz))*100));
                }
                if(style>.28 && style<.32)rgb*=.97+.045*sin(w.x*12+w.z*9+sin(w.z*3+w.x)*1.4);
                if(style>.48 && style<.52){
                    float2 f=abs(frac(w.xz/1.28)-.5);
                    rgb*=1-.23*smoothstep(.485,.5,max(f.x,f.y));
                }
                float facing=.85+.18*dot(normalize(i.n),normalize(float3(-.55,.86,.45)));
                float height=saturate((w.y+.2)/3.8);
                float3 illum=lerp(atlas,float3(.57,.60,.69),height*.22);
                float3 shaded=rgb*illum*facing;
                if(style>.16 && style<.20){
                    float along=abs(i.n.x)>.7?w.z:w.x;
                    float axis=abs(frac(along/12.8+.5)-.5)*12.8;
                    float spill=exp(-.38*axis*axis-.57*(w.y-3.74)*(w.y-3.74));
                    shaded+=float3(.32,.18,.055)*spill;
                    shaded*=.77+.23*smoothstep(0,1.15,w.y);
                }
                return half4(SRGBToLinear(saturate(shaded)),1);
            }
            ENDHLSL
        }
    }
}
