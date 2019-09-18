Shader "Custom/Density" {
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

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
	#pragma surface surf Standard fullforwardshadows alpha
	//#pragma alpha : fade
		// Use shader model 3.0 target, to get nicer looking lighting
	#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _DensityTex;
		sampler2D _NoiseTex;
		sampler2D _HeatMapScaleTex;
		half _NoiseStrength;

		static const float PI = 3.14159265f;

		static const float3x3 rX = { 1.0f, 0.0f, 0.0f,
			0.0f, cos(PI / 4.0f), -sin(PI / 4.0f),
			0.0f, sin(PI / 4.0f), cos(PI / 4.0f) };

		static const float3x3 rY = { cos(PI / 4.0f), 0.0f, sin(PI / 4.0f),
			0.0f, 1.0f, 0.0f,
			-sin(PI / 4.0f), 0.0f, cos(PI / 4.0f) };

		static const float3x3 rZ = { cos(PI / 4.0f), -sin(PI / 4.0f), 0.0f,
			sin(PI / 4.0f), cos(PI / 4.0f), 0.0f,
			0.0f, 0.0f, 1.0f };


		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};


		int _CellWidth;
		int _Rows;
		int _Cols;
		//float _NoiseWoo;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
			UNITY_INSTANCING_BUFFER_END(Props)

			void surf(Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color

			//float2 currentCell = IN.worldPos.rg / _CellWidth;
			float2 currentCell = IN.uv_MainTex ;
			//Limit the world
			//clip((IN.worldPos.rg / _CellWidth).r);
			//clip(-(currentCell.r - _Rows-10));
			//clip((IN.worldPos.rg / _CellWidth).g);
			//clip(-(currentCell.g - _Cols-10));
			//clip(abs(sin(IN.worldPos.rg  * PI / _CellWidth)) - 0.3);
			//Limit the world Draw the grid
			

			half densityValue = tex2D(_DensityTex, currentCell.rg).r;
			clip(densityValue-0.000001);

			fixed4 densityColor = tex2D(_HeatMapScaleTex, fixed2(densityValue - 0.01, 0.0));

			fixed3 correctedUV = mul(IN.worldPos,(mul(rZ, mul(rX, rY))));
			//correctedUV.rg = correctedUV.rg + _SinTime * _NoiseWoo;

			fixed4 noise1 = tex2D(_NoiseTex, correctedUV.rg);

			half noise = noise1.r;

			//clip(noise - _NoiseStrength);
			fixed4 color = densityColor; //tex2D(_MainTex, correctedUV.rg) * densityColor;



										 //fixed4 c = lerp(color, noise, _NoiseStrength);
			fixed4 c = color * noise;

			o.Albedo.rgb = c;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 0.8f;



	}
	ENDCG
	}
		FallBack "Diffuse"
}
