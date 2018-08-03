// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

/********************************************************************************//**
\file      Hand.shader
\brief     Basic hand shader impementation.
\copyright Copyright 2015 Oculus VR, LLC All Rights reserved.
************************************************************************************/

Shader "OvrAvatar/Hand" {

	Properties {
		_ColorPrimary ("Color Primary", Color) = (0.396078, 0.725490, 1)
		_ColorSecondary ("Color Secondary", Color) = (0, 1, 0.94902)
		_DepthFade ("Depth Fade", float) = 0.05
		_MainTex ("Wireframe (RGB)", 2D) = "white" {}
		_WireTex ("Wire (RGB)", 2D) = "white" {}
	}

	SubShader {

		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		LOD 200

		//==============================================================================
		// Depth Pass
		//==============================================================================

		Pass {

			ZWrite On
			Cull Off
			ColorMask 0

			Stencil {
				Ref 127 // Note: Read by HandGlow.shader
				Comp Always
				Pass Replace
			}

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "Assets/OvrAvatar/Content/Materials/UtilCg.cginc"

			struct v2f {
				float4 position : SV_POSITION;
			};

			sampler2D _WireTex;

			//==============================================================================
			// Main
			//==============================================================================

			//==============================================================================
			v2f vert (appdata_full v) {
				// Wire texture lookup
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				half2 wireTexCoord = UtilWireTexCoord(worldPos, v.color);
				half3 wireTex = tex2Dlod(_WireTex, half4(wireTexCoord, 0, 0));

				// Extrude
				half intensity = step(0.2, v.color.r);
				v.vertex.xyz += UtilWireExtrudeAmount(v.normal, wireTex.g) * intensity;

				// Output
				v2f output;
				output.position = UnityObjectToClipPos(v.vertex);
				return output;
			}

			//==============================================================================
			float4 frag (v2f input) : COLOR {
				return 0;
			}

			ENDCG

		}

		//==============================================================================
		// Render Pass
		//==============================================================================

		ZWrite Off
		Offset -0.1, -1

		CGPROGRAM

		#pragma surface surf Standard fullforwardshadows vertex:vert alpha:blend
		#pragma target 3.0
		#include "Assets/OvrAvatar/Content/Materials/UtilCg.cginc"

		#define ColorBlack half3(0, 0, 0)
		#define EmissionFactor (0.9)
		#define RimFactor (0.19)

		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
			float3 worldPos;
			float4 projPos : TEXCOORD2;
			float4 vertexColor : COLOR;
		};

		fixed3 _ColorPrimary;
		fixed3 _ColorSecondary;
		float _DepthFade;
		sampler2D _MainTex;
		sampler2D _WireTex;
		sampler2D_float _CameraDepthTexture;

		//==============================================================================
		// Functions
		//==============================================================================

		//==============================================================================
		float3 SafeNormalize (float3 normal) {
			float magSq = dot(normal, normal);
			if (magSq == 0) {
				return 0;
			}
			return normalize(normal);
		}

		//==============================================================================
		// Main
		//==============================================================================

		//==============================================================================
		void vert (inout appdata_full v, out Input output) {
			UNITY_INITIALIZE_OUTPUT(Input, output);

			// Wire texture lookup
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			half2 wireTexCoord = UtilWireTexCoord(worldPos, v.color);
			half3 wireTex = tex2Dlod(_WireTex, half4(wireTexCoord, 0, 0));

			// Extrude
			v.vertex.xyz += UtilWireExtrudeAmount(v.normal, wireTex.g);

			// Projected pixel position
			output.projPos = UtilProjectedPosition(v.vertex);
		}

		//==============================================================================
		void surf (Input input, inout SurfaceOutputStandard output) {
			// Compute rim term
			half viewDotNormal = dot(input.viewDir, SafeNormalize(output.Normal));
			half rim = pow(1 - saturate(viewDotNormal), 0.5) * (1 - RimFactor) + RimFactor;

			// Compute wire texcoord
			half4 vertexColor = input.vertexColor;
			half2 wireTexCoord = UtilWireTexCoord(input.worldPos, vertexColor);

			// Texture lookups
			half3 wireTex = tex2D(_WireTex, wireTexCoord);
			half3 wireframeTex = tex2D(_MainTex, input.uv_MainTex);

			// Compute emissive term
			half3 emission = lerp(ColorBlack, _ColorPrimary, rim);
			emission = lerp(emission, _ColorSecondary, 2 * wireframeTex.b * wireTex.g);
			emission += rim * 0.5 * vertexColor.r;

			// Compute alpha term
			half aAlpha = 8 * wireframeTex.r * wireTex.g * vertexColor.r;
			half bAlpha = rim * vertexColor.r;
			half alpha = (aAlpha + bAlpha) * 3;

			// Perform depth fade
			// CBD: disabling because of mirror rendering incompatibility
			//alpha *= UtilDepthFade(_CameraDepthTexture, input.projPos, -0.005, _DepthFade);
			
			// Output
			output.Albedo = 0;
			output.Metallic = 0;
			output.Smoothness = 0;
			output.Emission = emission * EmissionFactor;
			output.Alpha = alpha;
		}

		ENDCG

	}

	FallBack "Diffuse"

}
