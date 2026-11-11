Shader "EchoesOfTheFog/Forest Ground"
{
    Properties
    {
        _Moss ("Forest floor", 2D) = "white" {}
        _Dirt ("Wet earth", 2D) = "white" {}
        _MossNormal ("Forest floor normal", 2D) = "bump" {}
        _DirtNormal ("Earth normal", 2D) = "bump" {}
        _Color ("Tint", Color) = (0.8,0.82,0.75,1)
        _TileSize ("Texture size in metres", Float) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _Moss, _Dirt, _MossNormal, _DirtNormal;
        fixed4 _Color;
        float _TileSize;
        struct Input { float3 worldPos; fixed4 color : COLOR; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.worldPos.xz / _TileSize;
            float noise = tex2D(_Dirt, uv * 0.17).g;
            float blend = saturate(IN.color.r + (noise - 0.5) * 0.2);
            fixed3 moss = tex2D(_Moss, uv).rgb;
            fixed3 dirt = tex2D(_Dirt, uv * 0.8).rgb;
            float variation = lerp(0.8, 1.08, tex2D(_Moss, uv * 0.073).r);
            o.Albedo = lerp(moss, dirt, blend) * _Color.rgb * variation;
            o.Normal = normalize(lerp(UnpackNormal(tex2D(_MossNormal, uv)), UnpackNormal(tex2D(_DirtNormal, uv * 0.8)), blend));
            o.Metallic = 0;
            o.Smoothness = lerp(0.10, 0.32, blend);
            o.Occlusion = 0.95;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
