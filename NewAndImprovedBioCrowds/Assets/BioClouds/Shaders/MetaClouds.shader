Shader "Unlit/MetaClouds"
{
	Properties{
	_Color("Color", Color) = (1,1,1,1)
	_MainTex("Albedo (RGB)", 2D) = "white" {}

	_HeatMapScaleTex("HeatMapScale", 2D) = "white" { }
	_CellWidth("CellWidth", Float) = 2.0
	_Rows("Rows", Int) = 500
	_Cols("Cols", Int) = 500
	_Threshold("Threshold", Float) = 1.0
	_Clouds("Clouds", Int) = 10
		_Obstacle("Obstacle", Vector) = (1.0,1.0,1.0,1.0)
	}
		SubShader{
				Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
				LOD 200



		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0

			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
			};

			float4 _MainTex_ST;
			sampler2D _MainTex;
			sampler2D _DensityTex;
			sampler2D _NoiseTex;
			sampler2D _HeatMapScaleTex;
			half _NoiseStrength;
			int _CellWidth;
			int _Rows;
			int _Cols;
			half _Glossiness;
			half _Metallic;
			fixed4 _Color;
			int _Clouds;
			float4 _Obstacle;

#ifdef SHADER_API_D3D11
			StructuredBuffer<float3> positions_buffer;
			StructuredBuffer<float> radius_buffer;
#endif

			float g(float x, float y, float x0, float y0, float r) {
				float t = pow((x - x0), 2) + pow((y - y0), 2) - pow(r, 2);
				return  exp(t);
			}

			float f(float x, float y, StructuredBuffer<float3> positions, StructuredBuffer<float> radii)
			{
				float L = 0.0;
				float p = 3;
				float v = 0;

				uint length;
				uint stride;
				positions.GetDimensions(length, stride);

				for (uint i = 0; i < length; i++)
				{
					v += pow(g(x, y, positions[i].x, positions[i].y, radii[i]), -p);
				}

				v = pow(v, (-1.0 / p)) - 1.0;
				v = -v;


				return  v - L;

			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{


				float L = _CellWidth;
				float p = 3;
				float v = 0;

				float x = i.worldPos.x;
				float y = i.worldPos.y;


				uint length;
				uint stride;

				positions_buffer.GetDimensions(length, stride);

				for (uint j = 0; j < length; j++)
				{
					float x0 = positions_buffer[j].x;
					float y0 = positions_buffer[j].y;
					float r = radius_buffer[j];

					//v += exp(   pow(x - x0, 2) + pow(y - y0, 2) - pow(r, 2)    );
					v += pow(r, 2) / (pow(x - x0, 2) + pow(y - y0, 2));
				}

				v = pow(v, (-1.0 / p)) - 1.0;
				v = -v;

				float x0 = _Obstacle.x;
				float y0 = _Obstacle.y;
				float R =  _Obstacle.z;
				float d = 0.0;
				float x1 = x;
				float y1 = y;
				x = abs(x - x0);
				y =  abs(y - y0);

				if (x < R) 
				{
					
					if (y < R)
					{
						d = 0.0;
						return 0.0;
					}
					else
						d = y - R;
				}
				else 
				{
					if(y < R)
					{
						d = x - R;
					}
					else 
					{
						d = distance(float2(x, y), float2(R,R));
					}
				}

				//v =  v - (R * R) / (d * d + 1) ;
				//d = distance(float2(x, y), float2(0, 0));
				float vd = exp(-d);
				v -=  exp (-d) ;

				x0 += 15.0;
				x = abs(x1 - x0);
				y = abs(y1 - y0);

				if (x < R)
				{

					if (y < R) 
					{
						d = 0.0;
						return 0.0;
					}
					else
						d = y - R;
				}
				else
				{
					if (y < R)
					{
						d = x - R;
					}
					else
					{
						d = distance(float2(x, y), float2(R, R));
					}
				}

				//v =  v - (R * R) / (d * d + 1) ;
				//d = distance(float2(x, y), float2(0, 0));
				float v1 = v;
				vd += exp(-d);
				v -= exp(-d);



				//float s = smoothstep(L , vd, v);
				float s = step(L ,  v);

				clip(s- 0.0001);

				//fixed4 densityColor = lerp(fixed4(1,1,1,1), _Color, s);
				fixed4 densityColor = s * _Color;


				fixed4 color = densityColor;


				return color;
			}
			ENDCG
		}
	}
}
