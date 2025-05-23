Shader "Custom/TerrainClipShader" {
    Properties{
        [HideInInspector] _MainTex("BaseMap (RGB)", 2D) = "white" {}
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _SplatTex3("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] _SplatTex2("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _SplatTex1("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _SplatTex0("Layer 0 (R)", 2D) = "white" {}

        // These params are used for our shader
        _TileTexArr("Tile Texture Array", 2DArray) = "" {}
        _TileNormalMapTexArr("Tileset NormalMap Texture Array (RGBA)", 2DArray) = "" {}
        _TileMetallicGlossMapTexArr("Tileset MetallicGlossMap Texture Array (RGBA)", 2DArray) = "" {}
        _TilemapTex("Tilemap (R)", 2D) = "red" {}
        _TilemapDim("Tilemap Dimension (in tiles)", Int) = 128
        _MaxIndex("Max Tileset Index", Int) = 255
    }
        SubShader{
            Tags { "RenderType" = "Transparent" }
            LOD 200

        CGPROGRAM

        #pragma target 3.5
        #pragma surface surf Standard
        #pragma glsl
        #pragma enable_d3d11_debug_symbols

        //#pragma shader_feature _NORMALMAP
        #pragma multi_compile __ _NORMALMAP

        UNITY_DECLARE_TEX2DARRAY(_TileTexArr);

        #ifdef _NORMALMAP
            UNITY_DECLARE_TEX2DARRAY(_TileNormalMapTexArr);
        #endif

        sampler2D _TilemapTex;
        float4 _TileTexArr_TexelSize;
        int _MaxIndex;
        int _TilemapDim;
        float4x4 _WorldToTerrainBox;
        float4x4 _TerrainMatrices[8];

        struct Input
        {
            float2 uv_MainTex : TEXCOORD0;
            float3 worldPos;
            //float2 uv_BumpMap : TEXCOORD1;
        };

        // compute all 4 posible configurations of terrain tiles (normal, rotated, flipped, rotated and flipped)
        // rotations and translations not conflated together, because OpenGL ES 2.0 only supports square matrices
        static float2x2 rotations[4] = {
            float2x2(1.0, 0.0, 0.0, 1.0),
            float2x2(0.0, 1.0, -1.0, 0.0),
            float2x2(-1.0, 0.0, 0.0, -1.0),
            float2x2(0.0, -1.0, 1.0, 0.0)
        };
        static float2 translations[4] = {
            float2(0.0, 0.0),
            float2(0.0, 1.0),
            float2(1.0, 1.0),
            float2(1.0, 0.0)
        };

        #define MIPMAP_BIAS (-0.5)

        inline float GetMipLevel(float2 iUV, float4 iTextureSize)
        {
            float2 dx = ddx(iUV * iTextureSize.z);
            float2 dy = ddy(iUV * iTextureSize.w);
            float d = max(dot(dx, dx), dot(dy,dy));
            return 0.5 * log2(d) + MIPMAP_BIAS;
        }

        #pragma enable_d3d11_debug_symbols
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // float3 boxPosition = mul(_WorldToTerrainBox, float4(IN.worldPos, 1));

            // if (all(boxPosition > -0.5 && boxPosition < 0.5))
            //     discard;

            // Apply transformation using each matrix in the array
            for (int i = 0; i < 8; i++) // Use only first 2 matrices
            {
                float3 boxPosition = mul(_TerrainMatrices[i], float4(IN.worldPos, 1));

                if (all(boxPosition > -0.5 && boxPosition < 0.5))
                    discard;
            }

            // Get offset to tile in atlas
            uint index = tex2D(_TilemapTex, IN.uv_MainTex).a * _MaxIndex + 0.5;

            // Offset to fragment position inside tile
            float2 unwrappedUV = IN.uv_MainTex * _TilemapDim;
            float2 uv = mul(rotations[index % 4], frac(unwrappedUV)) + translations[index % 4];

            // Sample based on gradient and set output
            float3 uv3 = float3(uv, index / 4); // compute correct texture array index from index

            float mipMapLevel = GetMipLevel(unwrappedUV, _TileTexArr_TexelSize);
            half4 c = UNITY_SAMPLE_TEX2DARRAY_SAMPLER_LOD(_TileTexArr, _TileTexArr, uv3, mipMapLevel);

            o.Albedo = c.rgb;
            o.Alpha = c.a;

            #ifdef _NORMALMAP
                o.Normal = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY_SAMPLER_LOD(_TileNormalMapTexArr, _TileTexArr, uv3, mipMapLevel));
            #endif
        }
        ENDCG
    }
    FallBack "Standard"
}
