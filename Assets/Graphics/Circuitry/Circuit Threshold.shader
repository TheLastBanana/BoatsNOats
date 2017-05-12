Shader "Custom/Circuit Threshold"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
            };

            v2f vert(
                float4 vertex : POSITION, // vertex position input
                float2 uv : TEXCOORD0, // texture coordinate input
                out float4 outpos : SV_POSITION // clip space position output
            )
            {
                v2f o;
                o.uv = uv;
                outpos = UnityObjectToClipPos(vertex);
                return o;
            }
            
            sampler2D _MainTex;

            fixed4 frag (v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                if (col.r != 0 || col.g != 0 || col.b != 0 || col.a != 0)
                    col = fixed4(1, 1, 1, 1);
                else
                    col = fixed4(0, 0, 0, 1);

                return col;
            }
            ENDCG
        }
    }
}
