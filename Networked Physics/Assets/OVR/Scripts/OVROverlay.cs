/************************************************************************************

Copyright   :   Copyright 2014-2017 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.4.1 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.4.1/

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using VR = UnityEngine.VR;

/// <summary>
/// Add OVROverlay script to an object with a Quad mesh filter to have the quad
/// rendered as a TimeWarp overlay instead by drawing it into the eye buffer.
/// This will take full advantage of the display resolution and avoid double
/// resampling of the texture.
/// 
/// If the texture is dynamically generated, as for an interactive GUI or
/// animation, it must be explicitly triple buffered to avoid flickering
/// when it is referenced asynchronously by TimeWarp.
/// </summary>
public class OVROverlay : MonoBehaviour
{
	public enum OverlayType
	{
		None,			// Disabled the overlay
		Underlay,		// Eye buffers blend on top
		Overlay,		// Blends on top of the eye buffer
		OverlayShowLod	// (Deprecated) Blends on top and colorizes texture level of detail
	};

#if UNITY_ANDROID && !UNITY_EDITOR
	const int maxInstances = 3;
#else
	const int maxInstances = 15;
#endif

	static OVROverlay[] instances = new OVROverlay[maxInstances];

	OverlayType		currentOverlayType = OverlayType.Overlay;
    Texture         texture;
	IntPtr 			texId = IntPtr.Zero;
	int				layerIndex = -1;
	Renderer		rend;

	private void ApplyTexture()
	{
		if (rend.material.mainTexture == texture)
			return;

		// Getting the NativeTextureID/PTR synchronizes with the multithreaded renderer, which
		// causes a problem on the first frame if this gets called after the OVRDisplay initialization,
		// so do it in Awake() instead of Start().
		texture = rend.material.mainTexture;
		texId = texture.GetNativeTexturePtr();
	}

	void Awake()
	{
		Debug.Log ("Overlay Awake");

		rend = GetComponent<Renderer>();
		ApplyTexture();
    }

	void OnEnable()
    {
        if (!OVRManager.isHmdPresent)
        {
            enabled = false;
            return;
		}

		OnDisable();

		for (int i = 0; i < maxInstances; ++i)
		{
			if (instances[i] == null || instances[i] == this)
			{
				layerIndex = i;
				instances[i] = this;
				break;
			}
		}
	}

	void OnDisable()
	{
		if (layerIndex != -1)
		{
			rend.enabled = true;

			// Turn off the overlay if it was on.
			OVRPlugin.SetOverlayQuad(true, false, IntPtr.Zero, IntPtr.Zero, OVRPose.identity.ToPosef(), Vector3.one.ToVector3f(), layerIndex);

			instances[layerIndex] = null;
		}

		layerIndex = -1;
	}

	void Update()
    {
#if !UNITY_ANDROID || UNITY_EDITOR
		ApplyTexture();
#endif

//		rend.enabled = true;
    }

    void OnRenderObject()
    {
		// The overlay must be specified every eye frame, because it is positioned relative to the
		// current head location.  If frames are dropped, it will be time warped appropriately,
		// just like the eye buffers.

		if (Camera.current.cameraType != CameraType.Game || layerIndex == -1 || currentOverlayType == OverlayType.None)
		{
//			rend.enabled = true;	// use normal renderer
			return;
		}

		bool overlay = (currentOverlayType == OverlayType.Overlay);

        bool headLocked = false;
        for (var t = transform; t != null && !headLocked; t = t.parent)
            headLocked |= (t == Camera.current.transform);


		OVRPose pose = (headLocked) ? transform.ToHeadSpacePose() : transform.ToTrackingSpacePose();

		Vector3 scale = transform.lossyScale;
        for (int i = 0; i < 3; ++i)
            scale[i] /= Camera.current.transform.lossyScale[i];

		// render with the overlay plane instead of the normal renderer
		bool isOverlayVisible = OVRPlugin.SetOverlayQuad(overlay, headLocked, texId, IntPtr.Zero, pose.flipZ().ToPosef(), scale.ToVector3f(), layerIndex);
		rend.enabled = !isOverlayVisible;
	}
	
}
