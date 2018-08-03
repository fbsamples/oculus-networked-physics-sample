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
using UnityEngine;

/// <summary>
/// Manages an Oculus Rift head-mounted display (HMD).
/// </summary>
public class OVRDisplay
{
	/// <summary>
	/// Contains full fov information per eye
	/// Under Symmetric Fov mode, UpFov == DownFov and LeftFov == RightFov.
	/// </summary>
	public struct EyeFov
	{
		public float UpFov;
		public float DownFov;
		public float LeftFov;
		public float RightFov;
	}

	/// <summary>
	/// Specifies the size and field-of-view for one eye texture.
	/// </summary>
	public struct EyeRenderDesc
	{
		/// <summary>
		/// The horizontal and vertical size of the texture.
		/// </summary>
		public Vector2 resolution;

		/// <summary>
		/// The angle of the horizontal and vertical field of view in degrees.
		/// For Symmetric FOV interface compatibility
		/// Note this includes the fov angle from both sides
		/// </summary>
		public Vector2 fov;

		/// <summary>
		/// The full information of field of view in degrees.
		/// When Asymmetric FOV isn't enabled, this returns the maximum fov angle
		/// </summary>
		public EyeFov fullFov;
	}

	/// <summary>
	/// Contains latency measurements for a single frame of rendering.
	/// </summary>
	public struct LatencyData
	{
		/// <summary>
		/// The time it took to render both eyes in seconds.
		/// </summary>
		public float render;

		/// <summary>
		/// The time it took to perform TimeWarp in seconds.
		/// </summary>
		public float timeWarp;

		/// <summary>
		/// The time between the end of TimeWarp and scan-out in seconds.
		/// </summary>
		public float postPresent;
		public float renderError;
		public float timeWarpError;
	}

	private bool needsConfigureTexture;
	private EyeRenderDesc[] eyeDescs = new EyeRenderDesc[2];
	private bool recenterRequested = false;
	private int recenterRequestedFrameCount = int.MaxValue;

	/// <summary>
	/// Creates an instance of OVRDisplay. Called by OVRManager.
	/// </summary>
	public OVRDisplay()
	{
		UpdateTextures();
	}

	/// <summary>
	/// Updates the internal state of the OVRDisplay. Called by OVRManager.
	/// </summary>
	public void Update()
	{
		UpdateTextures();

		if (recenterRequested && Time.frameCount > recenterRequestedFrameCount)
		{
			if (RecenteredPose != null)
			{
				RecenteredPose();
			}
			recenterRequested = false;
			recenterRequestedFrameCount = int.MaxValue;
		}
	}

	/// <summary>
	/// Occurs when the head pose is reset.
	/// </summary>
	public event System.Action RecenteredPose;

	/// <summary>
	/// Recenters the head pose.
	/// </summary>
	public void RecenterPose()
	{
#if UNITY_2017_2_OR_NEWER
        UnityEngine.XR.InputTracking.Recenter();
#else
		UnityEngine.VR.InputTracking.Recenter();
#endif

		// The current poses are cached for the current frame and won't be updated immediately 
		// after UnityEngine.VR.InputTracking.Recenter(). So we need to wait until next frame 
		// to trigger the RecenteredPose delegate. The application could expect the correct pose 
		// when the RecenteredPose delegate get called.
		recenterRequested = true;
		recenterRequestedFrameCount = Time.frameCount;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		OVRMixedReality.RecenterPose();
#endif
	}

	/// <summary>
	/// Gets the current linear acceleration of the head.
	/// </summary>
	public Vector3 acceleration
	{
		get {			
			if (!OVRManager.isHmdPresent)
				return Vector3.zero;

			return OVRPlugin.GetNodeAcceleration(OVRPlugin.Node.Head, OVRPlugin.Step.Render).FromFlippedZVector3f();
		}
	}

    /// <summary>
    /// Gets the current angular acceleration of the head.
    /// </summary>
    public Vector3 angularAcceleration
    {
        get
        {
            if (!OVRManager.isHmdPresent)
				return Vector3.zero;

			return OVRPlugin.GetNodeAngularAcceleration(OVRPlugin.Node.Head, OVRPlugin.Step.Render).FromFlippedZVector3f() * Mathf.Rad2Deg;
        }
    }

    /// <summary>
    /// Gets the current linear velocity of the head.
    /// </summary>
    public Vector3 velocity
    {
        get
        {
            if (!OVRManager.isHmdPresent)
                return Vector3.zero;

			return OVRPlugin.GetNodeVelocity(OVRPlugin.Node.Head, OVRPlugin.Step.Render).FromFlippedZVector3f();
        }
    }
	
	/// <summary>
	/// Gets the current angular velocity of the head.
	/// </summary>
	public Vector3 angularVelocity
	{
		get {
			if (!OVRManager.isHmdPresent)
				return Vector3.zero;

			return OVRPlugin.GetNodeAngularVelocity(OVRPlugin.Node.Head, OVRPlugin.Step.Render).FromFlippedZVector3f() * Mathf.Rad2Deg;
		}
	}

	/// <summary>
	/// Gets the resolution and field of view for the given eye.
	/// </summary>
#if UNITY_2017_2_OR_NEWER
    public EyeRenderDesc GetEyeRenderDesc(UnityEngine.XR.XRNode eye)
#else
	public EyeRenderDesc GetEyeRenderDesc(UnityEngine.VR.VRNode eye)
#endif
	{
		return eyeDescs[(int)eye];
	}

	/// <summary>
	/// Gets the current measured latency values.
	/// </summary>
	public LatencyData latency
	{
		get {
			if (!OVRManager.isHmdPresent)
				return new LatencyData();

            string latency = OVRPlugin.latency;

            var r = new Regex("Render: ([0-9]+[.][0-9]+)ms, TimeWarp: ([0-9]+[.][0-9]+)ms, PostPresent: ([0-9]+[.][0-9]+)ms", RegexOptions.None);

            var ret = new LatencyData();

            Match match = r.Match(latency);
            if (match.Success)
            {
                ret.render = float.Parse(match.Groups[1].Value);
                ret.timeWarp = float.Parse(match.Groups[2].Value);
                ret.postPresent = float.Parse(match.Groups[3].Value);     
            }

            return ret;
		}
	}

	/// <summary>
	/// Gets application's frame rate reported by oculus plugin
	/// </summary>
	public float appFramerate
	{
		get
		{
			if (!OVRManager.isHmdPresent)
				return 0;

			return OVRPlugin.GetAppFramerate();
		}
	}

	/// <summary>
	/// Gets the recommended MSAA level for optimal quality/performance the current device.
	/// </summary>
	public int recommendedMSAALevel
	{
		get
		{
			int result = OVRPlugin.recommendedMSAALevel;

			if (result == 1)
				result = 0;
			
			return result;
		}
	}

	/// <summary>
	/// Gets the list of available display frequencies supported by this hardware.
	/// </summary>
	public float[] displayFrequenciesAvailable
	{
		get { return OVRPlugin.systemDisplayFrequenciesAvailable; }
	}

	/// <summary>
	/// Gets and sets the current display frequency.
	/// </summary>
	public float displayFrequency
	{
		get
		{
			return OVRPlugin.systemDisplayFrequency;
		}
		set
		{
			OVRPlugin.systemDisplayFrequency = value;
		}
	}

	private void UpdateTextures()
	{
#if UNITY_2017_2_OR_NEWER
		ConfigureEyeDesc(UnityEngine.XR.XRNode.LeftEye);
        ConfigureEyeDesc(UnityEngine.XR.XRNode.RightEye);
#else
		ConfigureEyeDesc(UnityEngine.VR.VRNode.LeftEye);
		ConfigureEyeDesc(UnityEngine.VR.VRNode.RightEye);
#endif
	}

#if UNITY_2017_2_OR_NEWER
    private void ConfigureEyeDesc(UnityEngine.XR.XRNode eye)
#else
	private void ConfigureEyeDesc(UnityEngine.VR.VRNode eye)
#endif
	{
		if (!OVRManager.isHmdPresent)
			return;

		OVRPlugin.Sizei size = OVRPlugin.GetEyeTextureSize((OVRPlugin.Eye)eye);

		eyeDescs[(int)eye] = new EyeRenderDesc();
		eyeDescs[(int)eye].resolution = new Vector2(size.w, size.h);

		OVRPlugin.Frustumf2 frust;
		if (OVRPlugin.GetNodeFrustum2((OVRPlugin.Node)eye, out frust))
		{
			eyeDescs[(int)eye].fullFov.LeftFov = Mathf.Rad2Deg * Mathf.Atan(frust.Fov.LeftTan);
			eyeDescs[(int)eye].fullFov.RightFov = Mathf.Rad2Deg * Mathf.Atan(frust.Fov.RightTan);
			eyeDescs[(int)eye].fullFov.UpFov = Mathf.Rad2Deg * Mathf.Atan(frust.Fov.UpTan);
			eyeDescs[(int)eye].fullFov.DownFov = Mathf.Rad2Deg * Mathf.Atan(frust.Fov.DownTan);
		}
		else
		{
			OVRPlugin.Frustumf frustOld = OVRPlugin.GetEyeFrustum((OVRPlugin.Eye)eye);
			eyeDescs[(int)eye].fullFov.LeftFov = Mathf.Rad2Deg * frustOld.fovX * 0.5f;
			eyeDescs[(int)eye].fullFov.RightFov = Mathf.Rad2Deg * frustOld.fovX * 0.5f;
			eyeDescs[(int)eye].fullFov.UpFov = Mathf.Rad2Deg * frustOld.fovY * 0.5f;
			eyeDescs[(int)eye].fullFov.DownFov = Mathf.Rad2Deg * frustOld.fovY * 0.5f;
		}

		// Symmetric Fov uses the maximum fov angle
		float maxFovX = Mathf.Max(eyeDescs[(int)eye].fullFov.LeftFov, eyeDescs[(int)eye].fullFov.RightFov);
		float maxFovY = Mathf.Max(eyeDescs[(int)eye].fullFov.UpFov, eyeDescs[(int)eye].fullFov.DownFov);
		eyeDescs[(int)eye].fov.x = maxFovX * 2.0f;
		eyeDescs[(int)eye].fov.y = maxFovY * 2.0f;

		if (!OVRPlugin.AsymmetricFovEnabled)
		{
			eyeDescs[(int)eye].fullFov.LeftFov = maxFovX;
			eyeDescs[(int)eye].fullFov.RightFov = maxFovX;

			eyeDescs[(int)eye].fullFov.UpFov = maxFovY;
			eyeDescs[(int)eye].fullFov.DownFov = maxFovY;
		}


	}
}
