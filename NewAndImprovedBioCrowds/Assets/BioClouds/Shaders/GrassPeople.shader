Shader "BioClouds/GrassPeople"
{
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_DensityTex("DensityTexture", 2D) = "white"{}
		_NoiseTex("NoiseTexture", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_NoiseStrength("NoiseStrength", Range(0,1)) = 0.0
		_HeatMapScaleTex("HeatMapScale", 2D) = "white" { }
		_CellWidth("CellWidth", Float) = 2.0
		_Rows("Rows", Int) = 500
		_Cols("Cols", Int) = 500

		_TessUniform("Uniform Tessellation", Range(1, 64)) = 1
			//_NoiseWoo("NoiseWooo", Range(0,1000)) = 0.0
		_GrassDim("Grass People Dimensions", Vector) = (0.1, 0.2, 0, 0)
		_PeopleTex("Grass People Texture", 2D) = "white"{}
		_MaxDensity ("MaxDesnity", Float) = 10

		_ImpostorTexture("ImpostorTexture", 2D) = "white"{}
		_ImpostorView("Impostor View Direction", Vector) = (0,0,0,0)
		_ImpostorSlices("Impostor Slice Quantity", Int) = 10
		_ImpostorSpeedMap("Impostor Speed Map", 2D) = "white"{} //bleh Solution?
		_ImpostorScale("Impostor Scale", Float) = 1.0
		_AnimationFrame("Animation Frame", Range(0, 18)) = 0

	}


		SubShader{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 200
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		ZTest Always
		Pass
		{
			Name "Tesselate"


			CGPROGRAM
			#pragma vertex tess_vert
			#pragma fragment frag
			#pragma target 4.6
			#pragma hull hull
			#pragma domain domain


			#pragma geometry geo

			#include "UnityCG.cginc"


			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _DensityTex_ST;

			float _TessUniform;
			sampler2D _DensityTex;
			float2 _GrassDim;
			float _MaxDensity;

			sampler2D _ImpostorTexture;
			float3 _ImpostorView;
			float _ImpostorSlices;
			sampler2D _ImpostorSpeedMap;
			float _AnimationFrame;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct controlpoint
			{
				float4 vertex : TESSPOS;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};


			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			float3 HASH33(float3 vec)
			{
				return float3(frac(vec.x * 247.3874), 0.0 , frac(vec.z * 679.9878336));
			}

			controlpoint tess_vert(appdata v)
			{
				controlpoint o;
				//o.vertex = UnityObjectToClipPos(v.vertex);
				o.vertex = v.vertex;
				o.uv = TRANSFORM_TEX(v.uv, _DensityTex);
				o.normal = v.normal;
				return o;
			}

			v2f vert(appdata v)
			{
				v2f o;

				fixed4 density = tex2Dlod(_DensityTex, float4(v.uv.x, v.uv.y, 0.0, 0.0));

				o.vertex = v.vertex;
				//o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _DensityTex);
				o.normal = v.normal;
				return o;
			}


			[UNITY_domain("tri")]
			[UNITY_outputcontrolpoints(3)]
			[UNITY_outputtopology("triangle_cw")]
			[UNITY_partitioning("integer")]
			[UNITY_patchconstantfunc("patch_function")]
			controlpoint hull(InputPatch<controlpoint, 3> patch, uint id : SV_OUTPUTControlPointID) 
			{
				return patch[id];
			}

			struct tessfactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};


			tessfactors patch_function(InputPatch<controlpoint, 3> patch)
			{
				float4 patchlod = float4 ((patch[0].uv + patch[1].uv + patch[2].uv) / 3.0, 0, 0);

				float p0factor = tex2Dlod(_DensityTex, patchlod).r;
				float p1factor = tex2Dlod(_DensityTex, patchlod).r ;
				float p2factor = tex2Dlod(_DensityTex, patchlod).r ;
				//float p0factor = tex2Dlod(_DensityTex, float4(patch[0].uv.x, patch[0].uv.y, 0, 0)).r;
				//float p1factor = tex2Dlod(_DensityTex, float4(patch[1].uv.x, patch[1].uv.y, 0, 0)).r ;
				//float p2factor = tex2Dlod(_DensityTex, float4(patch[2].uv.x, patch[2].uv.y, 0, 0)).r ;
				float factor = (p0factor + p1factor + p2factor);
				//float factor = 1.0;
				tessfactors f;
				f.edge[0] = factor > 0.0 ? _TessUniform  * 1  : 0.0;
				f.edge[1] = factor > 0.0 ? _TessUniform  *1  : 0.0;
				f.edge[2] = factor > 0.0 ? _TessUniform	 *1  : 0.0;
				f.inside = factor > 0.0 ? _TessUniform   *1 : 0.0;
				return f;
			}


			
			[UNITY_domain("tri")]
			v2f domain(tessfactors factors,
				OutputPatch<controlpoint, 3> patch,
				float3 barycentricCoordinates : SV_DomainLocation)
			{
				appdata data;

				#define DOMAIN_INTERPOLATE(fieldName) data.fieldName = \
				patch[0].fieldName * barycentricCoordinates.x + \
				patch[1].fieldName* barycentricCoordinates.y +\
				patch[2].fieldName * barycentricCoordinates.z;

				DOMAIN_INTERPOLATE(vertex)
				DOMAIN_INTERPOLATE(uv)
				DOMAIN_INTERPOLATE(normal)
					
				return vert(data);
			} 

			

			struct geometry_out {

				float4 pos : SV_POSITION;
				float2 uv  : TEXCOORD0;
			};

			


			[maxvertexcount(4)]
			void geo(triangle float4 IN[3] : SV_POSITION,  inout TriangleStream<geometry_out> stream) {
				geometry_out o;

				int frame = fmod( _Time.w, _ImpostorSlices);
				float slice = 1 / _ImpostorSlices;

				float3 pos = (IN[0] + IN[1] + IN[2])/3.0;

				o.pos = UnityObjectToClipPos(pos + float3(_GrassDim.x, 0, 0));// +HASH33(pos)); //A
				o.uv = float2(slice * (frame), 0);
				stream.Append(o);

				o.pos = UnityObjectToClipPos(pos + float3(-_GrassDim.x, 0, 0));// + HASH33(pos)); //B
				o.uv = float2(slice * (frame + 1), 0);
				stream.Append(o);

				o.pos = UnityObjectToClipPos(pos + float3(_GrassDim.x, _GrassDim.y, 0));// + HASH33(pos)); //C
				o.uv = float2(slice * (frame), 1);
				stream.Append(o);

				o.pos = UnityObjectToClipPos(pos + float3(-_GrassDim.x, _GrassDim.y, 0));// + HASH33(pos)); //D
				o.uv = float2(slice * (frame + 1), 1);
				stream.Append(o);

				//o.pos = UnityObjectToClipPos( pos+ float3(0, 1, 0));
				//stream.Append(o);
				

				//o.pos = float4(pos  + float3(0.5, 0, 0),1);
				//stream.Append(o);

				//o.pos = float4(pos + float3(-0.5, 0, 0),1);
				//stream.Append(o);

				//o.pos = float4(pos + float3(0, 1, 0),1);
				//stream.Append(o);

				/*o.pos = UnityObjectToClipPos( pos + float4(0.5, 0, 0, 1));
				stream.Append(o);

				o.pos = UnityObjectToClipPos(pos + float4(-0.5, 0, 0, 1));
				stream.Append(o);

				o.pos = UnityObjectToClipPos(pos + float4(0, 1, 0, 1));
				stream.Append(o);*/
			}

			float4 frag(v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_ImpostorTexture, i.uv);
				//fixed4 col = tex2D(_DensityTex, i.uv);

				return col;
			}

			ENDCG
		}

		
	}
}
