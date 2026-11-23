Shader "Piso9/Turno de noche" {
 Properties { _Tint("Tinte",Color)=(1,1,1,1) _Expression("Estado del monitor",Float)=0 _Kind("Modelo",Float)=0 _Pulse("Interferencia",Float)=0 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
 float4 _Tint;
 float _Expression,_Kind,_Pulse;
 struct A {float4 p:POSITION; float3 n:NORMAL; float4 c:COLOR;};
 struct V {float4 pos:SV_POSITION; float3 n:TEXCOORD0; float4 c:COLOR; float3 w:TEXCOORD1;};
 V vert(A v){V o;o.pos=TransformObjectToHClip(v.p.xyz);o.n=TransformObjectToWorldNormal(v.n);o.c=v.c;o.w=TransformObjectToWorld(v.p.xyz);return o;}
 half4 frag(V i):SV_Target {
   if(i.c.a<.05){
     float2 uv=floor(i.c.rg*float2(48,32))/float2(47,31);
     float row=floor(uv.y*32);
     float glitch=step(.92,frac(sin(row+floor(_Time.y*13))*4375.24));
     uv.x+=glitch*(_Pulse*.08+(_Expression==6?.09:0));
     float x=abs(uv.x-.5), y=uv.y;
     float e=_Expression;
     if(e==8) return half4(.008,.004,.01,1);
     float eyes,mouth;
     if(_Kind>1.5){
       float2 eye=float2((x-.239)*1.5,y-.66);
       eyes=abs(length(eye)-.089)<.033;
       if(e==2||e==5) eyes=(abs(y-(.55+x*.58))<.048 && x>.09 && x<.36);
       if(e==4) eyes=max(eyes,step(abs(y-.64),.018)*step(x,.39));
       float curve=.23+1.35*x*x;
       float teeth=frac(x*(e==3?20:23));
       float depth=(e==3?.30:e==5?.24:.17)*(1-abs(teeth*2-1));
       mouth=(x<.38 && y<curve+.045 && y>curve-.035-depth);
       if(e==1 || e==3 || e==5) mouth=max(mouth,step(abs(x-.22),.018)*step(.16,y)*step(y,.35));
       if(e==1) eyes=max(eyes,step(length(eye),.045));
     } else {
       float center=.64+(_Kind>.5?(x-.22)*.85:.065*cos((x-.22)*21));
       eyes=(abs(y-center)<.045 && x>.105 && x<.33);
       mouth=(abs(y-(.28+x*x*1.95))<.037 && x<.31);
       if(e==3) mouth=(x<.12 && abs(y-.32)<.095);
       if(e==6||e==7) eyes=(abs(abs(x-.22)-abs(y-.65))<.034 && x>.12 && x<.32);
     }
     float pixels=saturate(eyes+mouth);
     if(e==7) pixels*=step(.35,frac(sin(row*2+floor(_Time.y*9))*515.9));
     float vignette=saturate(1-length((uv-.5)*1.5));
     return half4(float3(.019,.003,.017)*vignette+pixels*float3(2.9,.075,.57)*(1-.16*fmod(row,2)),1);
   }
   float light=.62+.4*saturate(dot(normalize(i.n),normalize(float3(-.45,.82,.4))));
   float glow=i.c.a>.9?2.15:light;
   float rim=pow(1-saturate(dot(normalize(i.n),normalize(_WorldSpaceCameraPos-i.w))),3);
   float3 base=SRGBToLinear(i.c.rgb)*glow*_Tint.rgb;
   return half4(base+rim*float3(.056,.013,.036)*step(.05,i.c.a),1);
 }
 ENDHLSL
 } }
}
