Shader "Custom/Portal Border"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Color("Main Color", Color) = (1, 1, 1, 1)
        _Resolution ("Resolution", Float) = 1000
        _Speed ("Speed", Float) = 100
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

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
            float _Resolution;
            float _Speed;
            float4 _Color;

			fixed4 frag (v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
			{
                float2 timeOffset = _Time.xx * _Speed;
				fixed4 col = tex2D(_MainTex, screenPos.xy / _Resolution + timeOffset);
                col *= _Color;

				return col;
			}
			ENDCG
		}
	}
}
