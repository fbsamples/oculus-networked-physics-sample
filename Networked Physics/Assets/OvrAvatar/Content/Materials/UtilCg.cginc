// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/********************************************************************************//**
\file      UtilCg.cginc
\brief     Utility functions for OvrTouch.
		   
		   ProjectedPosition/DepthFade based on soft particle implementation:
		   builtin_shaders-5.1.2f1\DefaultResourcesExtra\Particle Add.shader

\copyright Copyright 2015 Oculus VR, LLC All Rights reserved.
************************************************************************************/

#ifndef UTIL_CG_INCLUDED
#define UTIL_CG_INCLUDED

#include "UnityCG.cginc"

//==============================================================================
// Functions
//==============================================================================

//==============================================================================
float4 UtilProjectedPosition (float4 vertex) {
	float4 projectedPosition = ComputeScreenPos(UnityObjectToClipPos(vertex));
	projectedPosition.z = -mul(UNITY_MATRIX_MV, vertex).z;
	return projectedPosition;
}

//==============================================================================
float UtilDepthFade (
	sampler2D_float depthTex,
	float4 projectedPosition,
	float pixelDepthOffset,
	float depthFade
) {
	float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(depthTex, UNITY_PROJ_COORD(projectedPosition)));
	float pixelDepth = projectedPosition.z;
	float alpha = saturate(sceneDepth - (pixelDepth + pixelDepthOffset));
	return saturate(alpha * (sceneDepth - pixelDepth) / (max(depthFade, 0.01) * 0.01));
}

//==============================================================================
float3 UtilWireExtrudeAmount (float3 normal, half3 wireTexColor) {
	return normal * wireTexColor.g * 0.003;
}

//==============================================================================
half2 UtilWireTexCoord (float3 worldPos, float4 vertexColor) {
	half2 wireTexCoord;
	wireTexCoord.x = (vertexColor.r + _Time.y) * 0.1;
	wireTexCoord.y = worldPos.z;
	return wireTexCoord;
}

#endif
