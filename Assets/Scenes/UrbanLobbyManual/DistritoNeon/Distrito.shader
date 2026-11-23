Shader "Lobby/Distrito nocturno"
{
    Properties
    {
        [Enum(Metal,0,Pavimento,1,Ventanas,2)] _Surface ("Superficie", Float) = 0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Surface;
            CBUFFER_END

            struct Attributes { float4 position : POSITION; half4 color : COLOR; float3 normal : NORMAL; };
            struct Varyings { float4 position : SV_POSITION; float3 world : TEXCOORD0; half3 normal : TEXCOORD1; half4 color : COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.world = TransformObjectToWorld(v.position.xyz);
                o.position = TransformWorldToHClip(o.world);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.color = v.color;
                return o;
            }
            float noiseCell(float2 cell)
            {
                float3 h = frac(float3(cell.xyx)*float3(.1031,.11369,.13787));
                h += dot(h,h.yzx+19.19);
                return frac((h.x+h.y)*h.z);
            }
            // Elliptical light reflected on the wet plaza beneath each row of luminaires.
            float footprint(float2 delta, float2 radius)
            {
                float2 d = delta / radius;
                return exp2(-dot(d,d));
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float3 v = SafeNormalize(_WorldSpaceCameraPos.xyz - i.world);
                float2 p = i.world.xz-float2(.92,13.30);
                float sun = saturate(Luminance(GetMainLight().color));
                float3 c = i.color.rgb;
                float shade = .64+.27*abs(n.x)+.34*saturate(n.y)+.13*max(0,n.z);
                if (_Surface > 1.5 && abs(n.y) < .45 && i.color.a < .5)
                {
                    float wall = abs(n.x) > abs(n.z) ? i.world.z : i.world.x;
                    float salt = floor(dot(abs(n.xz),i.world.xz)*.8);
                    float2 uv = float2(wall/1.68,i.world.y/2.35);
                    float2 cell = floor(uv);
                    float2 s = abs(frac(uv)-.5);
                    float2 aa = min(fwidth(uv),.32);
                    float2 glass = 1-smoothstep(float2(.30,.24)-aa,float2(.31,.25)+aa,s);
                    float seed = noiseCell(cell+salt);
                    float row = noiseCell(float2(salt,cell.y));
                    float lit = smoothstep(.42,.62,seed + (row-.5)*.54);
                    float3 pane = lerp(float3(.34,.72,.96),float3(1,.69,.38),smoothstep(.51,.74,seed));
                    c = c*shade + glass.x*glass.y*pane*lit*(.4+.55*row);
                }
                else if (_Surface > .5 && _Surface < 1.5)
                {
                    float2 uv = p/float2(1.55,1.55);
                    uv.x += .5*fmod(abs(floor(uv.y)),2);
                    float2 groutDistance = (.5-abs(frac(uv)-.5))/max(fwidth(uv),.004);
                    float grain = noiseCell(floor(uv));
                    float tile = lerp(.70,1,saturate(1.65*min(groutDistance.x,groutDistance.y)))*(.84+.22*grain);
                    float2 world = i.world.xz;
                    float2 flecks = world * float2(8.5, 19.0);
                    float2 grainFootprint = fwidth(flecks);
                    float grit = lerp(.5, noiseCell(floor(flecks)), saturate(1-max(grainFootprint.x,grainFootprint.y)));
                    float roughness = .6 + .29 * grain + .11 * grit;
                    float2 warp = float2(sin(world.y*18.5 + world.x*1.4),sin(world.x*25.1 + world.y*2.2));
                    float2 ray = -v.xz / max(.05, v.y);
                    float2 neonHit = world + ray*4.03 + warp*.21;
                    float2 nearestLamp = float2(abs(neonHit.x-.92) - 8.0, frac((neonHit.y+13.0)/17.0+.5)*17.0-8.5);
                    float lanterns = footprint(nearestLamp,float2(.65,1.45)) + .23*footprint(nearestLamp,float2(2.0,3.3));
                    float2 gateHit = world + ray*4.5 + warp*.32;
                    float portal = footprint(float2(abs(gateHit.x-.92)-3.57,gateHit.y+38.2),float2(.56,3.0));
                    float2 shopHit = world + ray*4.65 + warp*.27;
                    float shop = footprint(float2(shopHit.x-16.4,max(abs(shopHit.y+20)-6,0)),float2(1.02,1.0));
                    float2 rangeHit = world + ray*8.4 + warp*.32;
                    float rangeSign = footprint(float2(rangeHit.x+16.6,max(abs(rangeHit.y+15)-6.4,0)),float2(1.26,1.3));
                    float2 symbolHit = world + ray*19.3 + warp*.35;
                    float signal = footprint(symbolHit-float2(.92,-38.5),float2(3.4,1.4));
                    float tone = .88 + .10*grain + .07*(grit-.5);
                    float2 tc = abs(frac(uv)-.5);
                    float edgeFlash = smoothstep(.47,.497,max(tc.x,tc.y)) * (.3+.7*grain);
                    float redPool = footprint(world-float2(.92,-38),float2(6.9,5.9))+ .52*footprint(world-float2(.92,-2.5),float2(5.5,5));
                    float cyanPool = footprint(world-float2(-16,-15),float2(7,8));
                    float edgePool = exp2(-.3*abs(abs(world.x-.92)-15));
                    c=(float3(.16,.16,.225)+sun*float3(.16,.15,.14)+redPool*float3(.24,.038,.12)
                      + cyanPool*float3(.01,.12,.16)+edgePool*float3(.12,.055,.10))*tile*tone;
                    c += roughness*(lanterns*float3(.93,.47,.18)+portal*float3(.91,.38,.12)
                       + shop*float3(.85,.08,.46)+rangeSign*float3(.85,.43,.12)+signal*float3(.95,.035,.20));
                    c += edgeFlash*float3(.037,.033,.055);
                }
                else
                {
                    float2 az=i.world.xz-float2(.92,0);
                    float amberWash=footprint(az-float2(-19,-15),float2(7.3,10)) * exp2(-.42*abs(i.world.y-5.3));
                    float shopWash=exp2(-.47*abs(az.x-17) - .29*abs(i.world.y-2.7));
                    float portalWash=footprint(az-float2(0,-39.3),float2(8,6))*exp2(-.32*abs(i.world.y-4.3));
                    float3 ambient=float3(.89,.90,1.04);
                    c *= shade*ambient*(.88+.26*sun);
                    c += amberWash*float3(.215,.095,.02)+shopWash*float3(.15,.046,.088)+portalWash*float3(.19,.046,.055);
                    c += float3(.047,.034,.072)*pow(1-abs(dot(v,n)),3);
                }
                c = lerp(c,i.color.rgb,i.color.a);
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    c = SRGBToLinear(c);
                #endif
                return half4(c*(1+i.color.a*1.35),1);
            }
            ENDHLSL
        }
    }
}
