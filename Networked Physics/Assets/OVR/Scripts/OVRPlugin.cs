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

#if !UNITY_5
#define OVR_LEGACY
#endif

using System;
using System.Runtime.InteropServices;

// Internal C# wrapper for OVRPlugin.

internal static class OVRPlugin
{
	public static readonly System.Version wrapperVersion = new System.Version(1, 6, 0);

	private static System.Version _version;
	public static System.Version version
	{
		get {
			if (_version == null)
			{
				try
				{
					string pluginVersion = OVRP_0_1_0.ovrp_GetString(Key.Version);

					if (pluginVersion != null)
					{
						// Truncate unsupported trailing version info for System.Version. Original string is returned if not present.
						pluginVersion = pluginVersion.Split('-')[0];
						_version = new System.Version(pluginVersion);
					}
					else
					{
						_version = new System.Version(0, 0, 0);
					}
				}
				catch
				{
					_version = new System.Version(0, 0, 0);
				}

				// Unity 5.1.1f3-p3 have OVRPlugin version "0.5.0", which isn't accurate.
				if (_version == OVRP_0_5_0.version)
					_version = new System.Version(0, 1, 0);
			}

			return _version;
		}
	}

	private static System.Version _nativeSDKVersion;
	public static System.Version nativeSDKVersion
	{
		get {
			if (_nativeSDKVersion == null)
			{
				try
				{
					string sdkVersion = string.Empty;

					if (version >= OVRP_1_1_0.version)
						sdkVersion = OVRP_1_1_0.ovrp_GetNativeSDKVersion();
					else if (version >= OVRP_0_1_2.version)
						sdkVersion = OVRP_0_1_0.ovrp_GetString(Key.SDKVersion); // Key.SDKVersion added in OVRP 0.1.2
					else
						sdkVersion = "0.0.0";
                                    
					if (sdkVersion != null)
					{
						// Truncate unsupported trailing version info for System.Version. Original string is returned if not present.
						sdkVersion = sdkVersion.Split('-')[0];
						_nativeSDKVersion = new System.Version(sdkVersion);
					}
					else
					{
						_nativeSDKVersion = new System.Version(0, 0, 0);
					}
				}
				catch
				{
					_nativeSDKVersion = new System.Version(0, 0, 0);
				}
			}

			return _nativeSDKVersion;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct GUID
	{
		public int a;
		public short b;
		public short c;
		public byte d0;
		public byte d1;
		public byte d2;
		public byte d3;
		public byte d4;
		public byte d5;
		public byte d6;
		public byte d7;
	}

	public enum Bool
	{
		False = 0,
		True
	}

	public enum Eye
	{
		None  = -1,
		Left  = 0,
		Right = 1,
		Count = 2
	}

	public enum Tracker
	{
		None   = -1,
		Zero   = 0,
		One    = 1,
		Count,
	}

	public enum Node
	{
		None           = -1,
		EyeLeft        = 0,
		EyeRight       = 1,
		EyeCenter      = 2,
		HandLeft       = 3,
		HandRight      = 4,
		TrackerZero    = 5,
		TrackerOne     = 6,
		Count,
	}

	public enum Controller
	{
		None           = 0,
		LTouch         = 0x00000001,
		RTouch         = 0x00000002,
		Touch          = LTouch | RTouch,
		Remote         = 0x00000004,
		Gamepad        = 0x00000008,
		Active         = unchecked((int)0x80000000),
		All            = ~None,
	}

	public enum TrackingOrigin
	{
		EyeLevel       = 0,
		FloorLevel     = 1,
		Count,
	}

	public enum RecenterFlags
	{
		Default        = 0,
		IgnoreAll      = unchecked((int)0x80000000),
		Count,
	}

	public enum BatteryStatus
	{
		Charging = 0,
		Discharging,
		Full,
		NotCharging,
		Unknown,
	}

	public enum PlatformUI
	{
		None = -1,
		GlobalMenu = 0,
		ConfirmQuit,
        GlobalMenuTutorial,
	}

	public enum SystemRegion
	{
		Unspecified = 0,
		Japan,
	}

	private enum Key
	{
		Version = 0,
		ProductName,
		Latency,
		EyeDepth,
		EyeHeight,
		BatteryLevel,
		BatteryTemperature,
		CpuLevel,
		GpuLevel,
		SystemVolume,
		QueueAheadFraction,
		IPD,
		NativeTextureScale,
		VirtualTextureScale,
        Frequency,
		SDKVersion,
    }

	private enum OverlayFlag
	{
		None        = unchecked((int)0x00000000),
		OnTop       = unchecked((int)0x00000001),
		HeadLocked  = unchecked((int)0x00000002),
	}

	private enum Caps
	{
		SRGB = 0,
		Chromatic,
		FlipInput,
		Rotation,
		HeadModel,
		Position,
		CollectPerf,
		DebugDisplay,
		Monoscopic,
		ShareTexture,
		OcclusionMesh,
	}

	private enum Status
	{
		Debug = 0,
		HSWVisible,
		PositionSupported,
		PositionTracked,
		PowerSaving,
		Initialized,
		HMDPresent,
		UserPresent,
		HasVrFocus,
		ShouldQuit,
		ShouldRecenter,
		ShouldRecreateDistortionWindow,
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Vector2i
	{
		public int x;
		public int y;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Vector2f
	{
		public float x;
		public float y;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Vector3f
	{
		public float x;
		public float y;
		public float z;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Quatf
	{
		public float x;
		public float y;
		public float z;
		public float w;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Posef
	{
		public Quatf Orientation;
		public Vector3f Position;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct InputState
	{
		public uint ConnectedControllers;
		public uint Buttons;
		public uint Touches;
		public uint NearTouches;
		public float LIndexTrigger;
		public float RIndexTrigger;
		public float LHandTrigger;
		public float RHandTrigger;
		public Vector2f LThumbstick;
		public Vector2f RThumbstick;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ControllerState
	{
		public uint ConnectedControllers;
		public uint Buttons;
		public uint Touches;
		public uint NearTouches;
		public float LIndexTrigger;
		public float RIndexTrigger;
		public float LHandTrigger;
		public float RHandTrigger;
		public Vector2f LThumbstick;
		public Vector2f RThumbstick;

		// maintain backwards compat for OVRP_0_1_2.ovrp_GetInputState()
		internal ControllerState(InputState inputState)
		{
			ConnectedControllers = inputState.ConnectedControllers;
			Buttons              = inputState.Buttons;
			Touches              = inputState.Touches;
			NearTouches          = inputState.NearTouches;
			LIndexTrigger        = inputState.LIndexTrigger;
			RIndexTrigger        = inputState.RIndexTrigger;
			LHandTrigger         = inputState.LHandTrigger;
			RHandTrigger         = inputState.RHandTrigger;
			LThumbstick          = inputState.LThumbstick;
			RThumbstick          = inputState.RThumbstick;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct HapticsBuffer
	{
		public IntPtr Samples;
		public int SamplesCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct HapticsState
	{
		public int SamplesAvailable;
		public int SamplesQueued;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct HapticsDesc
	{
		public int SampleRateHz;
		public int SampleSizeInBytes;
		public int MinimumSafeSamplesQueued;
		public int MinimumBufferSamplesCount;
		public int OptimalBufferSamplesCount;
		public int MaximumBufferSamplesCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Sizei
	{
		public int w;
		public int h;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Frustumf
	{
		public float zNear;
		public float zFar;
		public float fovX;
		public float fovY;
	}

	public static bool initialized
	{
		get {
			if (version >= OVRP_1_1_0.version)
				return OVRP_1_1_0.ovrp_GetInitialized() == OVRPlugin.Bool.True;
			else
				return GetStatus(Status.Initialized);
		}
	}

	public static bool chromatic
	{
		get { return GetCap(Caps.Chromatic); }
		set { SetCap(Caps.Chromatic, value); }
	}

	public static bool collectPerf
	{
		get { return GetCap(Caps.CollectPerf); }
		set { SetCap(Caps.CollectPerf, value); }
	}

	public static bool debugDisplay
	{
		get { return GetCap(Caps.DebugDisplay); }
		set { SetCap(Caps.DebugDisplay, value); }
	}

	public static bool monoscopic
	{
		get {
			if (version >= OVRP_1_1_0.version)
				return OVRP_1_1_0.ovrp_GetAppMonoscopic() == OVRPlugin.Bool.True;
			else
				return GetCap(Caps.Monoscopic);
		}
		set {
			if (version >= OVRP_1_1_0.version)
				OVRP_1_1_0.ovrp_SetAppMonoscopic(ToBool(value));
			else
				SetCap(Caps.Monoscopic, value);
		}
	}

	public static bool hswVisible { get { return GetStatus(Status.HSWVisible); } }

	public static bool rotation
	{
		get { return GetCap(Caps.Rotation); }
		set { SetCap(Caps.Rotation, value); }
	}

	public static bool position
	{
		get { return GetCap(Caps.Position); }
		set { SetCap(Caps.Position, value); }
	}

	public static bool useIPDInPositionTracking
	{
		get {
			if (version >= OVRP_1_6_0.version)
				return OVRP_1_6_0.ovrp_GetTrackingIPDEnabled() == OVRPlugin.Bool.True;

			return true;
		}

		set {
			if (version >= OVRP_1_6_0.version)
				OVRP_1_6_0.ovrp_SetTrackingIPDEnabled(ToBool(value));
		}
	}

	public static bool positionSupported { get { return GetStatus(Status.PositionSupported); } }

	public static bool positionTracked { get { return GetStatus(Status.PositionTracked); } }

	public static bool powerSaving { get { return GetStatus(Status.PowerSaving); } }

	public static bool hmdPresent { get { return GetStatus(Status.HMDPresent); } }

	public static bool userPresent { get { return GetStatus(Status.UserPresent); } }

	public static bool headphonesPresent
	{
		get {
			if (version >= OVRP_1_3_0.version)
				return OVRP_1_3_0.ovrp_GetSystemHeadphonesPresent() == OVRPlugin.Bool.True;
			else if (version >= OVRP_1_1_0.version)
				return OVRP_1_1_0.ovrp_GetHeadphonesPresent() == OVRPlugin.Bool.True;
			return true;
		}
	}

	public static int recommendedMSAALevel
	{
		get {
			if (version >= OVRP_1_6_0.version)
				return OVRP_1_6_0.ovrp_GetSystemRecommendedMSAALevel ();
			else
				return 2;
		}
	}

	public static SystemRegion systemRegion
	{
		get {
			if (version >= OVRP_1_5_0.version)
				return OVRP_1_5_0.ovrp_GetSystemRegion();
			else
				return SystemRegion.Unspecified;
		}
	}

	private static Guid _cachedAudioOutGuid;
	private static string _cachedAudioOutString;
	public static string audioOutId
	{
		get
		{
			if (version >= OVRP_1_1_0.version)
			{
				try
				{
					IntPtr ptr = OVRP_1_1_0.ovrp_GetAudioOutId();
					if (ptr != IntPtr.Zero)
					{
						GUID nativeGuid = (GUID)Marshal.PtrToStructure(ptr, typeof(OVRPlugin.GUID));
						Guid managedGuid = new Guid(
								nativeGuid.a,
								nativeGuid.b,
								nativeGuid.c,
								nativeGuid.d0,
								nativeGuid.d1,
								nativeGuid.d2,
								nativeGuid.d3,
								nativeGuid.d4,
								nativeGuid.d5,
								nativeGuid.d6,
								nativeGuid.d7);

						if (managedGuid != _cachedAudioOutGuid)
						{
							_cachedAudioOutGuid = managedGuid;
							_cachedAudioOutString = _cachedAudioOutGuid.ToString();
						}

						return _cachedAudioOutString;
					}
				}
				catch
				{
					return string.Empty;
				}
			}
			return string.Empty;
		}
	}

	private static Guid _cachedAudioInGuid;
	private static string _cachedAudioInString;
	public static string audioInId
	{
		get
		{
			if (version >= OVRP_1_1_0.version)
			{
				try
				{
					IntPtr ptr = OVRP_1_1_0.ovrp_GetAudioInId();
					if (ptr != IntPtr.Zero)
					{
						GUID nativeGuid = (GUID)Marshal.PtrToStructure(ptr, typeof(OVRPlugin.GUID));
						Guid managedGuid = new Guid(
								nativeGuid.a,
								nativeGuid.b,
								nativeGuid.c,
								nativeGuid.d0,
								nativeGuid.d1,
								nativeGuid.d2,
								nativeGuid.d3,
								nativeGuid.d4,
								nativeGuid.d5,
								nativeGuid.d6,
								nativeGuid.d7);

						if (managedGuid != _cachedAudioInGuid)
						{
							_cachedAudioInGuid = managedGuid;
							_cachedAudioInString = _cachedAudioInGuid.ToString();
						}

						return _cachedAudioInString;
					}
				}
				catch
				{
					return string.Empty;
				}
			}
			return string.Empty;
		}
	}

	public static bool hasVrFocus
	{
		get {
			if (version >= OVRP_1_1_0.version)
				return OVRP_1_1_0.ovrp_GetAppHasVrFocus() == Bool.True;

			return GetStatus(Status.HasVrFocus);
		}
	}

	public static bool shouldQuit { get { return GetStatus(Status.ShouldQuit); } }

	public static bool shouldRecenter { get { return GetStatus(Status.ShouldRecenter); } }

	public static string productName { get { return OVRP_0_1_0.ovrp_GetString(Key.ProductName); } }

	public static string latency { get { return OVRP_0_1_0.ovrp_GetString(Key.Latency); } }

	public static float eyeDepth
	{
		get { return OVRP_0_1_0.ovrp_GetFloat(Key.EyeDepth); }
		set { OVRP_0_1_0.ovrp_SetFloat(Key.EyeDepth, value); }
	}

	public static float eyeHeight
	{
		get { return OVRP_0_1_0.ovrp_GetFloat(Key.EyeHeight); }
		set { OVRP_0_1_0.ovrp_SetFloat(Key.EyeHeight, value); }
	}

	public static float batteryLevel
	{
		get { return OVRP_0_1_0.ovrp_GetFloat(Key.BatteryLevel); }
	}

	public static float batteryTemperature
	{
		get { return OVRP_0_1_0.ovrp_GetFloat(Key.BatteryTemperature); }
	}

	public static int cpuLevel
	{
		get { return (int)OVRP_0_1_0.ovrp_GetFloat(Key.CpuLevel); }
		set { OVRP_0_1_0.ovrp_SetFloat(Key.CpuLevel, (float)value); }
	}

	public static int gpuLevel
	{
		get { return (int)OVRP_0_1_0.ovrp_GetFloat(Key.GpuLevel); }
		set { OVRP_0_1_0.ovrp_SetFloat(Key.GpuLevel, (float)value); }
	}

	public static int vsyncCount
	{
		get {
			if (version >= OVRP_1_1_0.version)
				return OVRP_1_1_0.ovrp_GetSystemVSyncCount();
			return 1;
		}
		set {
			if (version >= OVRP_1_2_0.version)
				OVRP_1_2_0.ovrp_SetSystemVSyncCount(value);
		}
	}

	public static float systemVolume
	{
		get { return OVRP_0_1_0.ovrp_GetFloat(Key.SystemVolume); }
	}

	public static float queueAheadFraction
	{
		get { return OVRP_0_1_0.ovrp_GetFloat(Key.QueueAheadFraction); }
		set { OVRP_0_1_0.ovrp_SetFloat(Key.QueueAheadFraction, value); }
	}

	public static float ipd
	{
		get { return OVRP_0_1_0.ovrp_GetFloat(Key.IPD); }
		set { OVRP_0_1_0.ovrp_SetFloat(Key.IPD, value); }
	}

#if OVR_LEGACY
	public static bool srgb
	{
		get { return GetCap(Caps.SRGB); }
		set { SetCap(Caps.SRGB, value); }
	}

	public static bool flipInput
	{
		get { return GetCap(Caps.FlipInput); }
		set { SetCap(Caps.FlipInput, value); }
	}

	public static bool debug { get { return GetStatus(Status.Debug); } }

	public static bool shareTexture
	{
		get { return GetCap(Caps.ShareTexture); }
		set { SetCap(Caps.ShareTexture, value); }
	}

	public static float nativeTextureScale
	{
		get { return OVRP_0_1_0.ovrp_GetFloat(Key.NativeTextureScale); }
		set { OVRP_0_1_0.ovrp_SetFloat(Key.NativeTextureScale, value); }
	}

	public static float virtualTextureScale
	{
		get { return OVRP_0_1_0.ovrp_GetFloat(Key.VirtualTextureScale); }
		set { OVRP_0_1_0.ovrp_SetFloat(Key.VirtualTextureScale, value); }
	}

	public static bool shouldRecreateDistortionWindow
	{
		get { return GetStatus(Status.ShouldRecreateDistortionWindow); }
	}
#endif

	public static bool occlusionMesh
	{
		get {
			if (version >= OVRP_1_3_0.version)
				return OVRP_1_3_0.ovrp_GetEyeOcclusionMeshEnabled() == Bool.True;
			else
				return GetCap(Caps.OcclusionMesh);
		}
		set {
			if (version >= OVRP_1_3_0.version)
				OVRP_1_3_0.ovrp_SetEyeOcclusionMeshEnabled(ToBool(value));
			else
				SetCap(Caps.OcclusionMesh, value);
		}
	}

	public static BatteryStatus batteryStatus
	{
		get { return OVRP_0_1_0.ovrp_GetBatteryStatus(); }
	}

	public static Posef GetEyeVelocity(Eye eyeId) { return OVRP_0_1_0.ovrp_GetEyeVelocity(eyeId); }
	public static Posef GetEyeAcceleration(Eye eyeId) { return OVRP_0_1_0.ovrp_GetEyeAcceleration(eyeId); }
	public static Frustumf GetEyeFrustum(Eye eyeId) { return OVRP_0_1_0.ovrp_GetEyeFrustum(eyeId); }
	public static Sizei GetEyeTextureSize(Eye eyeId) { return OVRP_0_1_0.ovrp_GetEyeTextureSize(eyeId); }
	public static Posef GetTrackerPose(Tracker trackerId) { return OVRP_0_1_0.ovrp_GetTrackerPose(trackerId); }
	public static Frustumf GetTrackerFrustum(Tracker trackerId) { return OVRP_0_1_0.ovrp_GetTrackerFrustum(trackerId); }
	public static bool DismissHSW() { return OVRP_0_1_0.ovrp_DismissHSW() == Bool.True; }
	public static bool ShowUI(PlatformUI ui) { return OVRP_0_1_0.ovrp_ShowUI(ui) == Bool.True; }
	public static bool SetOverlayQuad(bool onTop, bool headLocked, IntPtr texture, IntPtr device, Posef pose, Vector3f scale, int layerIndex=0)
	{
		if (version >= OVRP_1_6_0.version)
		{
			uint flags = (uint)OverlayFlag.None;
			if (onTop)
				flags |= (uint)OverlayFlag.OnTop;
			if (headLocked)
				flags |= (uint)OverlayFlag.HeadLocked;
			
			return OVRP_1_6_0.ovrp_SetOverlayQuad3(flags, texture, IntPtr.Zero, device, pose, scale, layerIndex) == Bool.True;
		}

		if (layerIndex != 0)
			return false;
		
		if (version >= OVRP_0_1_1.version)
			return OVRP_0_1_1.ovrp_SetOverlayQuad2(ToBool(onTop), ToBool(headLocked), texture, device, pose, scale) == Bool.True;
		else
			return OVRP_0_1_0.ovrp_SetOverlayQuad(ToBool(onTop), texture, device, pose, scale) == Bool.True;
	}

	public static Posef GetNodePose(Node nodeId)
	{
		if (version >= OVRP_0_1_2.version)
			return OVRP_0_1_2.ovrp_GetNodePose(nodeId);
		else
			return new Posef();
	}

	public static Posef GetNodeVelocity(Node nodeId)
	{
		if (version >= OVRP_0_1_3.version)
			return OVRP_0_1_3.ovrp_GetNodeVelocity(nodeId);
		else
			return new Posef();
	}

	public static Posef GetNodeAcceleration(Node nodeId)
	{
		if (version >= OVRP_0_1_3.version)
			return OVRP_0_1_3.ovrp_GetNodeAcceleration(nodeId);
		else
			return new Posef();
	}

	public static bool GetNodePresent(Node nodeId)
	{
		if (version >= OVRP_1_1_0.version)
			return OVRP_1_1_0.ovrp_GetNodePresent(nodeId) == Bool.True;
		else
			return false;
	}

	public static bool GetNodeOrientationTracked(Node nodeId)
	{
		if (version >= OVRP_1_1_0.version)
			return OVRP_1_1_0.ovrp_GetNodeOrientationTracked(nodeId) == Bool.True;
		else
			return false;
	}

	public static bool GetNodePositionTracked(Node nodeId)
	{
		if (version >= OVRP_1_1_0.version)
			return OVRP_1_1_0.ovrp_GetNodePositionTracked(nodeId) == Bool.True;
		else
			return false;
	}

	public static ControllerState GetControllerState(uint controllerMask)
	{
		if (version >= OVRP_1_1_0.version)
			return OVRP_1_1_0.ovrp_GetControllerState(controllerMask);
		else if (version >= OVRP_0_1_2.version)
			return new ControllerState(OVRP_0_1_2.ovrp_GetInputState(controllerMask));
		else
			return new ControllerState();
	}

	public static bool SetControllerVibration(uint controllerMask, float frequency, float amplitude)
	{
		if (version >= OVRP_0_1_2.version)
			return OVRP_0_1_2.ovrp_SetControllerVibration(controllerMask, frequency, amplitude) == Bool.True;
		else
			return false;
	}

	public static HapticsDesc GetControllerHapticsDesc(uint controllerMask)
	{
		if (version >= OVRP_1_6_0.version)
		{
			return OVRP_1_6_0.ovrp_GetControllerHapticsDesc(controllerMask);
		}
		else
		{
			return new HapticsDesc();
		}
	}

	public static HapticsState GetControllerHapticsState(uint controllerMask)
	{
		if (version >= OVRP_1_6_0.version)
		{
			return OVRP_1_6_0.ovrp_GetControllerHapticsState(controllerMask);
		}
		else
		{
			return new HapticsState();
		}
	}

	public static bool SetControllerHaptics(uint controllerMask, HapticsBuffer hapticsBuffer)
	{
		if (version >= OVRP_1_6_0.version)
		{
			return OVRP_1_6_0.ovrp_SetControllerHaptics(controllerMask, hapticsBuffer) == Bool.True;
		}
		else
		{
			return false;
		}
	}

	public static float GetEyeRecommendedResolutionScale()
	{
		if (version >= OVRP_1_6_0.version)
		{
			return OVRP_1_6_0.ovrp_GetEyeRecommendedResolutionScale();
		}
		else
		{
			return 1.0f;
		}
	}

	public static float GetAppCpuStartToGpuEndTime()
	{
		if (version >= OVRP_1_6_0.version)
		{
			return OVRP_1_6_0.ovrp_GetAppCpuStartToGpuEndTime();
		}
		else
		{
			return 0.0f;
		}
	}

#if OVR_LEGACY
	public static bool Update(int frameIndex) { return OVRP_0_1_0.ovrp_Update(frameIndex) == Bool.True; }
	public static IntPtr GetNativePointer() { return OVRP_0_1_0.ovrp_GetNativePointer(); }
	public static Posef GetEyePose(Eye eyeId) { return OVRP_0_1_0.ovrp_GetEyePose(eyeId); }
	public static bool RecenterPose() { return OVRP_0_1_0.ovrp_RecenterPose() == Bool.True; }
#endif

	private static bool GetStatus(Status bit)
	{
		if (version >= OVRP_0_1_2.version)
			return OVRP_0_1_2.ovrp_GetStatus2((uint)(1 << (int)bit)) != 0;
		else
			return (OVRP_0_1_0.ovrp_GetStatus() & (uint)(1 << (int)bit)) != 0;
	}

	private static bool GetCap(Caps cap)
	{
		if (version >= OVRP_0_1_3.version)
			return OVRP_0_1_3.ovrp_GetCaps2((uint)(1 << (int)cap)) != 0;
		else
			return ((int)OVRP_0_1_0.ovrp_GetCaps() & (1 << (int)cap)) != 0;
	}

	private static void SetCap(Caps cap, bool value)
	{
		if (GetCap(cap) == value)
			return;

		int caps = (int)OVRP_0_1_0.ovrp_GetCaps();
		if (value)
			caps |= (1 << (int)cap);
		else
			caps &= ~(1 << (int)cap);

		OVRP_0_1_0.ovrp_SetCaps((Caps)caps);
	}

	private static Bool ToBool(bool b)
	{
		return (b) ? OVRPlugin.Bool.True : OVRPlugin.Bool.False;
	}

	public static TrackingOrigin GetTrackingOriginType()
	{
		if (version >= OVRP_1_0_0.version)
			return OVRP_1_0_0.ovrp_GetTrackingOriginType();
		else
			return TrackingOrigin.EyeLevel;
	}

	public static bool SetTrackingOriginType(TrackingOrigin originType)
	{
		if (version >= OVRP_1_0_0.version)
			return OVRP_1_0_0.ovrp_SetTrackingOriginType(originType) == Bool.True;
		else
			return false;
	}

	public static Posef GetTrackingCalibratedOrigin()
	{
		if (version >= OVRP_1_0_0.version)
			return OVRP_1_0_0.ovrp_GetTrackingCalibratedOrigin();
		else
			return new Posef();
	}

	public static bool SetTrackingCalibratedOrigin()
	{
		if (version >= OVRP_1_2_0.version)
			return OVRP_1_2_0.ovrpi_SetTrackingCalibratedOrigin() == Bool.True;
		else
			return false;
	}

	public static bool RecenterTrackingOrigin(RecenterFlags flags)
	{
		if (version >= OVRP_1_0_0.version)
			return OVRP_1_0_0.ovrp_RecenterTrackingOrigin((uint)flags) == Bool.True;
		else
			return false;
	}
	
	//HACK: This makes Unity think it always has VR focus while OVRPlugin.cs reports the correct value.
	internal static bool ignoreVrFocus
	{
		set {
			if (version >= OVRP_1_2_1.version) {
				OVRP_1_2_1.ovrp_SetAppIgnoreVrFocus(ToBool(value));
			}
		}
	}

	private const string pluginName = "OVRPlugin";

	private static class OVRP_0_1_0
	{
		public static readonly System.Version version = new System.Version(0, 1, 0);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Posef ovrp_GetEyeVelocity(Eye eyeId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Posef ovrp_GetEyeAcceleration(Eye eyeId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Frustumf ovrp_GetEyeFrustum(Eye eyeId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Sizei ovrp_GetEyeTextureSize(Eye eyeId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Posef ovrp_GetTrackerPose(Tracker trackerId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Frustumf ovrp_GetTrackerFrustum(Tracker trackerId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_DismissHSW();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Caps ovrp_GetCaps();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetCaps(Caps caps);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint ovrp_GetStatus();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float ovrp_GetFloat(Key key);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetFloat(Key key, float value);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern BatteryStatus ovrp_GetBatteryStatus();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetOverlayQuad(Bool onTop, IntPtr texture, IntPtr device, Posef pose, Vector3f scale);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_ShowUI(PlatformUI ui);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetString")]
		private static extern IntPtr _ovrp_GetString(Key key);
		public static string ovrp_GetString(Key key) { return Marshal.PtrToStringAnsi(_ovrp_GetString(key)); }

#if OVR_LEGACY
		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_Update(int frameIndex);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ovrp_GetNativePointer();
		
		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Posef ovrp_GetEyePose(Eye eyeId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_RecenterPose();
#endif
	}

	private static class OVRP_0_1_1
	{
		public static readonly System.Version version = new System.Version(0, 1, 1);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetOverlayQuad2(Bool onTop, Bool headLocked, IntPtr texture, IntPtr device, Posef pose, Vector3f scale);
	}

	private static class OVRP_0_1_2
	{
		public static readonly System.Version version = new System.Version(0, 1, 2);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint ovrp_GetStatus2(uint query);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Posef ovrp_GetNodePose(Node nodeId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern InputState ovrp_GetInputState(uint controllerMask);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetControllerVibration(uint controllerMask, float frequency, float amplitude);
	}

	private static class OVRP_0_1_3
	{
		public static readonly System.Version version = new System.Version(0, 1, 3);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint ovrp_GetCaps2(uint query);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Posef ovrp_GetNodeVelocity(Node nodeId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Posef ovrp_GetNodeAcceleration(Node nodeId);
	}

	private static class OVRP_0_5_0
	{
		public static readonly System.Version version = new System.Version(0, 5, 0);
	}

	private static class OVRP_1_0_0
	{
		public static readonly System.Version version = new System.Version(1, 0, 0);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern TrackingOrigin ovrp_GetTrackingOriginType();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetTrackingOriginType(TrackingOrigin originType);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Posef ovrp_GetTrackingCalibratedOrigin();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_RecenterTrackingOrigin(uint flags);
	}

	private static class OVRP_1_1_0
	{
		public static readonly System.Version version = new System.Version(1, 1, 0);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetInitialized();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetVersion")]
		private static extern IntPtr _ovrp_GetVersion();
		public static string ovrp_GetVersion() { return Marshal.PtrToStringAnsi(_ovrp_GetVersion()); }

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetNativeSDKVersion")]
		private static extern IntPtr _ovrp_GetNativeSDKVersion();
		public static string ovrp_GetNativeSDKVersion() { return Marshal.PtrToStringAnsi(_ovrp_GetNativeSDKVersion()); }

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ovrp_GetAudioOutId();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ovrp_GetAudioInId();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float ovrp_GetEyeTextureScale();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetEyeTextureScale(float value);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetTrackingOrientationSupported();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetTrackingOrientationEnabled();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetTrackingOrientationEnabled(Bool value);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetTrackingPositionSupported();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetTrackingPositionEnabled();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetTrackingPositionEnabled(Bool value);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetNodePresent(Node nodeId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetNodeOrientationTracked(Node nodeId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetNodePositionTracked(Node nodeId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Frustumf ovrp_GetNodeFrustum(Node nodeId);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ControllerState ovrp_GetControllerState(uint controllerMask);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int ovrp_GetSystemCpuLevel();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetSystemCpuLevel(int value);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int ovrp_GetSystemGpuLevel();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetSystemGpuLevel(int value);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetSystemPowerSavingMode();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float ovrp_GetSystemDisplayFrequency();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int ovrp_GetSystemVSyncCount();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float ovrp_GetSystemVolume();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetHeadphonesPresent();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern BatteryStatus ovrp_GetSystemBatteryStatus();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float ovrp_GetSystemBatteryLevel();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float ovrp_GetSystemBatteryTemperature();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetSystemProductName")]
		private static extern IntPtr _ovrp_GetSystemProductName();
		public static string ovrp_GetSystemProductName() { return Marshal.PtrToStringAnsi(_ovrp_GetSystemProductName()); }

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_ShowSystemUI(PlatformUI ui);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetAppMonoscopic();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetAppMonoscopic(Bool value);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetAppHasVrFocus();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetAppShouldQuit();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetAppShouldRecenter();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetAppLatencyTimings")]
		private static extern IntPtr _ovrp_GetAppLatencyTimings();
		public static string ovrp_GetAppLatencyTimings() { return Marshal.PtrToStringAnsi(_ovrp_GetAppLatencyTimings()); }

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetUserPresent();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float ovrp_GetUserIPD();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetUserIPD(float value);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float ovrp_GetUserEyeDepth();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetUserEyeDepth(float value);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float ovrp_GetUserEyeHeight();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetUserEyeHeight(float value);
	}

	private static class OVRP_1_2_0
	{
		public static readonly System.Version version = new System.Version(1, 2, 0);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetSystemVSyncCount(int vsyncCount);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrpi_SetTrackingCalibratedOrigin();
	}

	private static class OVRP_1_2_1
	{
		public static readonly System.Version version = new System.Version(1, 2, 1);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetAppIgnoreVrFocus(Bool value);
	}

	private static class OVRP_1_3_0
	{
		public static readonly System.Version version = new System.Version(1, 3, 0);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetEyeOcclusionMeshEnabled();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetEyeOcclusionMeshEnabled(Bool value);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetSystemHeadphonesPresent();
	}

	private static class OVRP_1_5_0
	{
		public static readonly System.Version version = new System.Version(1, 5, 0);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SystemRegion ovrp_GetSystemRegion();
	}

	private static class OVRP_1_6_0
	{
		public static readonly System.Version version = new System.Version(1, 6, 0);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_GetTrackingIPDEnabled();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetTrackingIPDEnabled(Bool value);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern HapticsDesc ovrp_GetControllerHapticsDesc(uint controllerMask);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern HapticsState ovrp_GetControllerHapticsState(uint controllerMask);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetControllerHaptics(uint controllerMask, HapticsBuffer hapticsBuffer);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Bool ovrp_SetOverlayQuad3(uint flags, IntPtr textureLeft, IntPtr textureRight, IntPtr device, Posef pose, Vector3f scale, int layerIndex);

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float ovrp_GetEyeRecommendedResolutionScale();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float ovrp_GetAppCpuStartToGpuEndTime();

		[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int ovrp_GetSystemRecommendedMSAALevel();
	}
}
