Shader "Custom/ClipBoxShader" {
    Properties{
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _EmissionMap("Emission Texture", 2D) = "black" {}
        _Emission("Emission", float) = 0
        [HDR] _EmissionColor("Emission color", Color) = (1,1,1,1)
    }

        SubShader{
            Tags { "RenderType" = "Transparent" }
            LOD 200

            CGPROGRAM
            #pragma surface surf Standard fullforwardshadows addshadow
            #pragma target 3.0

            sampler2D _MainTex;
            half _Glossiness;
            half _Metallic;
            float _Emission;
            fixed4 _EmissionColor;
            float4x4 _WorldToBox;
            uniform sampler2D _EmissionMap;

            struct Input {
                float2 uv_MainTex;
                float3 worldPos;
            };

            void surf(Input IN, inout SurfaceOutputStandard o) {
                float3 boxPosition = mul(_WorldToBox, float4(IN.worldPos, 1));

                if (all(boxPosition > -0.5 && boxPosition < 0.5))
                    discard;                

                fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
                o.Albedo = c.rgb;
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = c.a;
                o.Emission = _EmissionColor.rgb * tex2D(_EmissionMap, IN.uv_MainTex).a * _Emission;
            }
            ENDCG
        }
            FallBack "Diffuse"
}