/********************************************************************************//**
\file      HeadGlow.shader
\brief     Adds a glow around the mesh using the hand stencil value as a mask.
\copyright Copyright 2015 Oculus VR, LLC All Rights reserved.
************************************************************************************/

Shader "OvrTouch/HeadGlow" {

	Properties {
		_ColorPrimary ("Color Primary", Color) = (0, 0.513725, 1)
		_ColorSecondary ("Color Secondary", Color) = (0, 0.78824, 1)
	}

	SubShader {

		Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" }
		LOD 200

		//==============================================================================
		// Render Pass
		//==============================================================================

		ZWrite Off
		Cull Off
		Offset -1, -1
		Blend OneMinusDstColor One

		Stencil {
            Ref 127 // Note: Written by Head.shader
            Comp NotEqual
            Pass Keep
        }

		CGPROGRAM

		#pragma surface surf NoLighting vertex:vert
		#pragma target 3.0

		struct Input {
			float3 worldPos;
			float3 worldNormal;
			float4 vertexColor : COLOR;
		};

		fixed3 _ColorPrimary;
		fixed3 _ColorSecondary;

		//==============================================================================
		// Main
		//==============================================================================

		//==============================================================================
		half4 LightingNoLighting (SurfaceOutput output, half3 lightDir, half atten) {
			return 0;
		}

		//==============================================================================
		void vert (inout appdata_full v) {
			// Extrude
			v.vertex.xyz += v.normal * 0.007;
		}

		//==============================================================================
		void surf (Input input, inout SurfaceOutput output) {			
			// Compute emissive term
			half4 vertexColor = input.vertexColor;
			half3 emission = lerp(_ColorPrimary, _ColorSecondary, vertexColor.r);

			// Compute alpha term
			float3 vertexToCam = normalize(_WorldSpaceCameraPos - input.worldPos);
			float invFalloff = saturate(abs(dot(vertexToCam, input.worldNormal)));
			half alpha = invFalloff * invFalloff;

			// Output
			output.Albedo = 0;
			output.Emission = emission * alpha;
			output.Alpha = 0;
		}

		ENDCG

	}

	FallBack "Diffuse"

}
