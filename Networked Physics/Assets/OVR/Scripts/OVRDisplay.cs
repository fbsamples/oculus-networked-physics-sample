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

using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using VR = UnityEngine.VR;

/// <summary>
/// Manages an Oculus Rift head-mounted display (HMD).
/// </summary>
public class OVRDisplay
{
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
		/// </summary>
		public Vector2 fov;
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
        VR.InputTracking.Recenter();

		if (RecenteredPose != null)
		{
			RecenteredPose();
		}
	}

	/// <summary>
	/// Gets the current linear acceleration of the head.
	/// </summary>
	public Vector3 acceleration
	{
		get {			
			if (!OVRManager.isHmdPresent)
				return Vector3.zero;

            OVRPose ret = OVRPlugin.GetEyeAcceleration(OVRPlugin.Eye.None).ToOVRPose();
            return ret.position;
		}
	}

    /// <summary>
    /// Gets the current angular acceleration of the head.
    /// </summary>
    public Quaternion angularAcceleration
    {
        get
        {
            if (!OVRManager.isHmdPresent)
                return Quaternion.identity;

            OVRPose ret = OVRPlugin.GetEyeAcceleration(OVRPlugin.Eye.None).ToOVRPose();
            return ret.orientation;
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

            OVRPose ret = OVRPlugin.GetEyeVelocity(OVRPlugin.Eye.None).ToOVRPose();
            return ret.position;
        }
    }
	
	/// <summary>
	/// Gets the current angular velocity of the head.
	/// </summary>
	public Quaternion angularVelocity
	{
		get {
			if (!OVRManager.isHmdPresent)
				return Quaternion.identity;

			OVRPose ret = OVRPlugin.GetEyeVelocity(OVRPlugin.Eye.None).ToOVRPose();
			return ret.orientation;
		}
	}

	/// <summary>
	/// Gets the resolution and field of view for the given eye.
	/// </summary>
    public EyeRenderDesc GetEyeRenderDesc(VR.VRNode eye)
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
	/// Gets the recommended MSAA level for optimal quality/performance the current device.
	/// </summary>
	public int recommendedMSAALevel
	{
		get { return OVRPlugin.recommendedMSAALevel; }
	}

	private void UpdateTextures()
	{
		ConfigureEyeDesc(VR.VRNode.LeftEye);
        ConfigureEyeDesc(VR.VRNode.RightEye);
	}

    private void ConfigureEyeDesc(VR.VRNode eye)
	{
		if (!OVRManager.isHmdPresent)
			return;

		OVRPlugin.Sizei size = OVRPlugin.GetEyeTextureSize((OVRPlugin.Eye)eye);
		OVRPlugin.Frustumf frust = OVRPlugin.GetEyeFrustum((OVRPlugin.Eye)eye);

		eyeDescs[(int)eye] = new EyeRenderDesc()
		{
			resolution = new Vector2(size.w, size.h),
            fov = Mathf.Rad2Deg * new Vector2(frust.fovX, frust.fovY),
		};
	}
}
