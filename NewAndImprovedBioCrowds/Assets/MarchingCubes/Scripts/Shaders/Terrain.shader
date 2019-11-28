Shader "Custom/Terrain"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
			//_Noise("Noise",2D) = "white"{}
			//_Noise2("Noise2",2D) = "white"{}



			_CausticsTex("Caustics (RGB)", 2D) = "white" {}

			_Caustics1_ST("Caustics 1 ST", Vector) = (1,1,0,0)
			_Caustics2_ST("Caustics 1 ST", Vector) = (1,1,0,0)

			_Caustics1_Speed("Caustics 1 Speed", Vector) = (1, 1, 0 ,0)
			_Caustics2_Speed("Caustics 2 Speed", Vector) = (1, 1, 0 ,0)
	}
		SubShader
			{
				Tags{"Queue" = "Transparent" "RenderType" = "Transparent" }
				LOD 200

				CGPROGRAM
					#pragma surface surf Standard fullforwardshadows alpha : fade

				//#pragma surface surf Standard fullforwardshadows

				#pragma target 3.0

				sampler2D _MainTex;

				struct Input
				{
					float2 uv_MainTex;
					float3 worldPos;
					float3 worldNormal;
				};

				float boundsY;
				float normalOffsetWeight;

				half _Glossiness;
				half _Metallic;
				fixed4 _Color;
				sampler2D ramp;
				//sampler2D _Noise;
				//sampler2D _Noise2;

				sampler2D _CausticsTex;
				float4 _Caustics1_ST;
				float4 _Caustics2_ST;
				float2 _Caustics1_Speed;
				float2 _Caustics2_Speed;



				void surf(Input IN, inout SurfaceOutputStandard o)

				{
					//float2  uv = IN.uv_MainTex;
					//float valor_noise1 = tex2D(_Noise, float2(uv.y * _SinTime.y, uv.v));
					//float valor_noise2 = text2D(_Noise, uv.v);

					//float total = valor_noise1 + valor_noise2;

					//float reflexao = step(total, threshold);

					//float3 tex = tex2D(ramp, float2(h,.5));
					//o.Albedo = tex;



					float h = smoothstep(-boundsY / 2, boundsY / 2, IN.worldPos.y + IN.worldNormal.y * normalOffsetWeight);


					fixed4 c = tex2D(_MainTex, IN.worldPos.xy) * _Color;
					o.Albedo = c.rgb;


					fixed2 uv = (IN.worldPos.xz + float2(0,1)) * _Caustics1_ST.xy + _Caustics1_ST.zw;
					uv += -_Caustics1_Speed * _Time.y / 25;
					fixed3 caustics1 = tex2D(_CausticsTex, uv).rgb;

					fixed2 uv2 = IN.worldPos.xz * _Caustics2_ST.xy + _Caustics2_ST.zw;
					uv2 += _Caustics1_Speed * _Time.y / 25;
					fixed3 caustics2 = tex2D(_CausticsTex, uv2).rgb;



					o.Albedo.rgb += min(caustics1, caustics2);

					o.Metallic = _Metallic;
					o.Smoothness = _Glossiness;
					o.Alpha = c.a;
				}
				ENDCG
			}
				FallBack "Diffuse"
}