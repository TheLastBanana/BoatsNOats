Shader "Custom/Portal Effect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Displacement("Displacement", 2D) = "black" {}
        _Wobbliness("Wobbliness", Float) = 1.0
        _Amplitude("Amplitude", Float) = 0.1
        _Speed("Speed", Vector) = (100, 90, 0, 0)
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
            sampler2D _Displacement;
            float _Amplitude;
            float _Wobbliness;
            float2 _Speed;
            float4 _MainTex_TexelSize;

            fixed4 frag (v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
            {
                float2 timeOffset = _Time.xx * _Speed.xy;

                // Use color in displacement map as a displacement value
                fixed4 dispColX = tex2D(_Displacement, screenPos.xy * _Wobbliness / 1000 + timeOffset.x);
                fixed4 dispColY = tex2D(_Displacement, screenPos.xy * _Wobbliness / 1000 + timeOffset.y);

                fixed2 disp = fixed2(
                    (dispColX.x - 0.5) * _Amplitude,
                    (dispColY.x - 0.5) * _Amplitude
                ) * _MainTex_TexelSize.xy;

                fixed4 col = tex2D(_MainTex, i.uv + disp);

                return col;
            }
            ENDCG
        }
    }
}
