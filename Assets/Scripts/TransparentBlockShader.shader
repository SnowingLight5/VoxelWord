Shader "Minecraft/Transparent Blocks" {
    Properties {
        _MainTex ("Block Texture Atlas", 2D) = "white" {}
    }

    SubShader {
        Tags { 
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "TRUE"
            "RenderType" = "Transparent" }
        LOD 100
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vertFunction
            #pragma fragment fragFunction
            #pragma target 2.0
            // make fog work
            //#pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                //UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
            float globalLightLevel;
            float minGlobalLightLevel;
            float maxGlobalLightLevel;
            //float4 _MainTex_ST;

            v2f vertFunction (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv = v.uv;
                o.color = v.color;
                //UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 fragFunction (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

                float shade = (maxGlobalLightLevel - minGlobalLightLevel) * globalLightLevel + minGlobalLightLevel; 
                shade *= i.color.a;
                shade = clamp(1 - shade, minGlobalLightLevel, maxGlobalLightLevel);
                
                clip(col.a - 1);
                col = lerp(col, float4(0,0,0,1), shade);
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
