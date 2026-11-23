Shader "Piso1/Paisaje facetado"
{
    Properties { [Enum(UnityEngine.Rendering.CullMode)] _Cull("Caras", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Input { float4 position:POSITION; float3 normal:NORMAL; half4 color:COLOR; };
            struct Vary { float4 position:SV_POSITION; float3 world:TEXCOORD0; half3 normal:TEXCOORD1; half4 color:COLOR; half fog:TEXCOORD2; };
            Vary Vert(Input v)
            {
                Vary o;
                o.world=TransformObjectToWorld(v.position.xyz);
                o.position=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(v.normal);
                o.color=v.color;
                o.fog=ComputeFogFactor(o.position.z);
                return o;
            }
            half4 Frag(Vary i):SV_Target
            {
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                half diffuse=saturate(dot(normalize(i.normal),sun.direction));
                half3 base=SRGBToLinear(i.color.rgb);
                // Dappled light softens the transition from the sunny meadow to the near forest floor.
                float edge=smoothstep(.50,1.03,length(i.world.xz/float2(34,28)));
                float pattern=sin(.52*i.world.x+.31*i.world.z)+.52*sin(.68*i.world.z-.37*i.world.x);
                half shade=edge*(.26+.21*smoothstep(-.7,.7,pattern))*(1-smoothstep(1,5,i.world.y));
                half3 lit=base*(1-shade)*(half3(.47,.46,.57)+half3(1.03,.82,.56)*diffuse*sun.shadowAttenuation);
                return half4(MixFog(lerp(lit,base*1.32,i.color.a),i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
