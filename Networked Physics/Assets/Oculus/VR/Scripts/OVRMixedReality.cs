/************************************************************************************

Copyright   :   Copyright 2017 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.4.1 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.4.1

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

/// <summary>
/// Manages mix-reality elements
/// </summary>
internal static class OVRMixedReality
{
	/// <summary>
	/// Configurable parameters
	/// </summary>
	public static Color chromaKeyColor = Color.green;

	/// <summary>
	/// For Debugging purpose, we can use preset parameters to fake a camera when external camera is not available
	/// </summary>
	public static bool useFakeExternalCamera = false;
	public static Vector3 fakeCameraPositon = new Vector3(3.0f, 0.0f, 3.0f);
	public static Quaternion fakeCameraRotation = Quaternion.LookRotation((new Vector3(0.0f, 1.0f, 0.0f) - fakeCameraPositon).normalized, Vector3.up);
	public static float fakeCameraFov = 60.0f;
	public static float fakeCameraAspect = 16.0f / 9.0f;

	/// <summary>
	/// Composition object
	/// </summary>
	public static OVRComposition currentComposition = null;

	/// <summary>
	/// Updates the internal state of the Mixed Reality Camera. Called by OVRManager.
	/// </summary>

	public static void Update(GameObject parentObject, Camera mainCamera, OVRManager.CompositionMethod compositionMethod, bool useDynamicLighting, OVRManager.CameraDevice cameraDevice, OVRManager.DepthQuality depthQuality)
	{
		if (!OVRPlugin.initialized)
		{
			Debug.LogError("OVRPlugin not initialized");
			return;
		}

		if (!OVRPlugin.IsMixedRealityInitialized())
			OVRPlugin.InitializeMixedReality();

		if (!OVRPlugin.IsMixedRealityInitialized())
		{
			Debug.LogError("Unable to initialize MixedReality");
			return;
		}

		OVRPlugin.UpdateExternalCamera();
		OVRPlugin.UpdateCameraDevices();

		if (currentComposition != null && currentComposition.CompositionMethod() != compositionMethod)
		{
			currentComposition.Cleanup();
			currentComposition = null;
		}

		if (compositionMethod == OVRManager.CompositionMethod.External)
		{
			if (currentComposition == null)
			{
				currentComposition = new OVRExternalComposition(parentObject, mainCamera);
			}
		}
		else if (compositionMethod == OVRManager.CompositionMethod.Direct)
		{
			if (currentComposition == null)
			{
				currentComposition = new OVRDirectComposition(parentObject, mainCamera, cameraDevice, useDynamicLighting, depthQuality);
			}
		}
		else if (compositionMethod == OVRManager.CompositionMethod.Sandwich)
		{
			if (currentComposition == null)
			{
				currentComposition = new OVRSandwichComposition(parentObject, mainCamera, cameraDevice, useDynamicLighting, depthQuality);
			}
		}
		else
		{
			Debug.LogError("Unknown CompositionMethod : " + compositionMethod);
			return;
		}
		currentComposition.Update(mainCamera);
	}

	public static void Cleanup()
	{
		if (currentComposition != null)
		{
			currentComposition.Cleanup();
			currentComposition = null;
		}
		if (OVRPlugin.IsMixedRealityInitialized())
		{
			OVRPlugin.ShutdownMixedReality();
		}
	}

	public static void RecenterPose()
	{
		if (currentComposition != null)
		{
			currentComposition.RecenterPose();
		}
	}
}

#endif
