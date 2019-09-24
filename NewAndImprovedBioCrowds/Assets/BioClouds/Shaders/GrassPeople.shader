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
			//_NoiseWoo("NoiseWooo", Range(0,1000)) = 0.0
	}
		SubShader{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 200



		Pass
		{
			Name "People"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma hull hull

			#pragma domain domain

			// make fog work
			#pragma multi_compile_fog
			#pragma target 4.6

			#include "UnityCG.cginc"


			sampler2D _MainTex;
			float4 _MainTex_ST;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct TessellationFactors {
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			struct TessellationControlPoint {
				float4 vertex : INTERNALTESSPOS;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
			};

			struct VertexData {
				UNITY_VERTEX_INPUT_INSTANCE_ID
					float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
			};
			struct InterpolatorsVertex {
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
				float3 normal : TEXCOORD1;

				float3 tangent : TEXCOORD2;

				float3 worldPos : TEXCOORD3;

				//UNITY_SHADOW_COORDS(5)
			};

			[UNITY_domain("tri")]
			[UNITY_outputcontrolpoints(3)]
			[UNITY_outputtopology("triangle_cw")]
			[UNITY_partitioning("integer")]
			[UNITY_patchconstantfunc("patch_constant")]
			TessellationControlPoint  hull(InputPatch<VertexData,3 > patch, uint id : SV_OutputControlPointID ) {
				return patch[id];
			}

			TessellationFactors patch_constant(InputPatch<VertexData, 3> patch){
				TessellationFactors f;
				f.edge[0] = 1;
				f.edge[1] = 1;
				f.edge[2] = 1;
				f.inside = 1;
				return f;
			}

			InterpolatorsVertex vert_program(VertexData v) {
				InterpolatorsVertex i;
				UNITY_INITIALIZE_OUTPUT(InterpolatorsVertex, i);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, i);
				i.pos = UnityObjectToClipPos(v.vertex);
				i.worldPos.xyz = mul(unity_ObjectToWorld, v.vertex);

				i.normal = UnityObjectToWorldNormal(v.normal);


				i.tangent = UnityObjectToWorldDir(v.tangent.xyz);

				i.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);

				return i;
			}

			[UNITY_domain("tri")]
			InterpolatorsVertex  domain(
				TessellationFactors factors,
				OutputPatch<VertexData, 3> patch,
				float3 barycentricCoordinates : SV_DomainLocation
				) {
					VertexData data;

					#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) data.fieldName = \
					patch[0].fieldName * barycentricCoordinates.x + \
					patch[1].fieldName * barycentricCoordinates.y + \
					patch[2].fieldName * barycentricCoordinates.z;

					MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
						MY_DOMAIN_PROGRAM_INTERPOLATE(normal)
						MY_DOMAIN_PROGRAM_INTERPOLATE(tangent)
						MY_DOMAIN_PROGRAM_INTERPOLATE(uv)
						MY_DOMAIN_PROGRAM_INTERPOLATE(uv1)
						MY_DOMAIN_PROGRAM_INTERPOLATE(uv2)

						return vert_program(data);
			}


			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			
			TessellationControlPoint vert(VertexData v) {
				TessellationControlPoint p;
				p.vertex = v.vertex;
				p.normal = v.normal;
				p.tangent = v.tangent;
				p.uv = v.uv;
				p.uv1 = v.uv1;
				p.uv2 = v.uv2;
				return p;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
