// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

/********************************************************************************//**
\file      Head.shader
\brief     Basic head shader impementation.
\copyright Copyright 2015 Oculus VR, LLC All Rights reserved.
************************************************************************************/

Shader "OvrAvatar/Head" {

	Properties {
		_ColorPrimary ("Color Primary", Color) = (0.396078, 0.725490, 1)
		_ColorSecondary ("Color Secondary", Color) = (0, 1, 0.94902)
		_MainTex ("Wireframe (RGB)", 2D) = "white" {}
		_WireTex ("Wire (RGB)", 2D) = "white" {}
		_VoiceAmplitude ("VoiceAmplitude", Float) = 0
	}

	SubShader {

		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		LOD 200

		//==============================================================================
		// Depth Pass
		//==============================================================================

		Pass {

	       ZWrite On
	       ColorMask 0

		   Stencil {
				Ref 127 // Note: Read by HeadGlow.shader
				Comp Always
				Pass Replace
			}

	   }
		
		//==============================================================================
		// Render Pass
		//==============================================================================

		ZWrite Off
		Offset -1, -1

		CGPROGRAM

		#pragma surface surf Standard fullforwardshadows vertex:vert alpha:blend
		#pragma target 3.0

		#define ColorBlack half3(0, 0, 0)
		#define EmissionFactor (0.9)
		#define RimFactor (0.1)

		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
			float3 worldPos;
			float4 vertexColor : COLOR;
		};

		fixed3 _ColorPrimary;
		fixed3 _ColorSecondary;
		sampler2D _MainTex;
		sampler2D _WireTex;
		float _VoiceAmplitude;

		//==============================================================================
		// Functions
		//==============================================================================

		//==============================================================================
		half2 WireTexCoord (float3 worldPos, float4 vertexColor) {
			half2 wireTexCoord;
			wireTexCoord.x = (vertexColor.r + _Time.y) * 0.1;
			wireTexCoord.y = abs(worldPos.z);
			return wireTexCoord;
		}

		//==============================================================================
		float3 SafeNormal (float3 normal) {
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
		void vert (inout appdata_full v) {
			// Wire texture lookup
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			half2 wireTexCoord = WireTexCoord(worldPos, v.color);
			half3 wireTex = tex2Dlod(_WireTex, half4(wireTexCoord, 0, 0));

			// Extrude
			v.vertex.xyz += v.normal * wireTex.g * 0.003;
		}

		//==============================================================================
		void surf (Input input, inout SurfaceOutputStandard output) {
			// Compute rim term
			half viewDotNormal = dot(input.viewDir, SafeNormal(output.Normal));
			half rim = pow(1 - saturate(viewDotNormal), 0.5) * (1 - RimFactor) + RimFactor;

			// Compute wire texcoord
			half4 vertexColor = input.vertexColor;
			half2 wireTexCoord = WireTexCoord(input.worldPos, vertexColor);

			// Texture lookups
			half3 wireTex = tex2D(_WireTex, wireTexCoord);
			half3 wireframeTex = tex2D(_MainTex, input.uv_MainTex);

			// Compute alpha term
			half aAlpha = 8 * (wireframeTex.r * wireTex.g + 100 * _VoiceAmplitude * wireframeTex.g * wireframeTex.r);
			half bAlpha = rim * vertexColor.r * lerp(1, 0.25, wireframeTex.b);
			half alpha = (aAlpha + bAlpha) * 3;

			// Compute emissive term
			half3 emission = lerp(ColorBlack, _ColorPrimary, rim);
			emission = lerp(emission, _ColorSecondary, aAlpha);
			emission += rim * 0.5;

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
