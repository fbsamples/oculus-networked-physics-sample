using UnityEngine;
using System.Collections;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

public class OVRDirectComposition : OVRCameraComposition
{
	public GameObject directCompositionCameraGameObject;
	public Camera directCompositionCamera;
	public RenderTexture boundaryMeshMaskTexture = null;

	public override OVRManager.CompositionMethod CompositionMethod() { return OVRManager.CompositionMethod.Direct; }

	public OVRDirectComposition(GameObject parentObject, Camera mainCamera, OVRManager.CameraDevice cameraDevice, bool useDynamicLighting, OVRManager.DepthQuality depthQuality)
		: base(cameraDevice, useDynamicLighting, depthQuality)
	{
		Debug.Assert(directCompositionCameraGameObject == null);
		directCompositionCameraGameObject = new GameObject();
		directCompositionCameraGameObject.name = "MRDirectCompositionCamera";
		directCompositionCameraGameObject.transform.parent = parentObject.transform;
		directCompositionCamera = directCompositionCameraGameObject.AddComponent<Camera>();
		directCompositionCamera.stereoTargetEye = StereoTargetEyeMask.None;
		directCompositionCamera.depth = float.MaxValue;
		directCompositionCamera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
		directCompositionCamera.clearFlags = mainCamera.clearFlags;
		directCompositionCamera.backgroundColor = mainCamera.backgroundColor;
		directCompositionCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);
		directCompositionCamera.nearClipPlane = mainCamera.nearClipPlane;
		directCompositionCamera.farClipPlane = mainCamera.farClipPlane;


		if (!hasCameraDeviceOpened)
		{
			Debug.LogError("Unable to open camera device " + cameraDevice);
		}
		else
		{
			Debug.Log("DirectComposition activated : useDynamicLighting " + (useDynamicLighting ? "ON" : "OFF"));
			CreateCameraFramePlaneObject(parentObject, directCompositionCamera, useDynamicLighting);
		}
	}

	public override void Update(Camera mainCamera)
	{
		if (!hasCameraDeviceOpened)
		{
			return;
		}

		if (!OVRPlugin.SetHandNodePoseStateLatency(OVRManager.instance.handPoseStateLatency))
		{
			Debug.LogWarning("HandPoseStateLatency is invalid. Expect a value between 0.0 to 0.5, get " + OVRManager.instance.handPoseStateLatency);
		}

		directCompositionCamera.clearFlags = mainCamera.clearFlags;
		directCompositionCamera.backgroundColor = mainCamera.backgroundColor;
		directCompositionCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);
		directCompositionCamera.nearClipPlane = mainCamera.nearClipPlane;
		directCompositionCamera.farClipPlane = mainCamera.farClipPlane;

		if (OVRMixedReality.useFakeExternalCamera || OVRPlugin.GetExternalCameraCount() == 0)
		{
			OVRPose worldSpacePose = new OVRPose();
			OVRPose trackingSpacePose = new OVRPose();
			trackingSpacePose.position = OVRMixedReality.fakeCameraPositon;
			trackingSpacePose.orientation = OVRMixedReality.fakeCameraRotation;
			worldSpacePose = OVRExtensions.ToWorldSpacePose(trackingSpacePose);

			directCompositionCamera.fieldOfView = OVRMixedReality.fakeCameraFov;
			directCompositionCamera.aspect = OVRMixedReality.fakeCameraAspect;
			directCompositionCamera.transform.FromOVRPose(worldSpacePose);
		}
		else
		{
			OVRPlugin.CameraExtrinsics extrinsics;
			OVRPlugin.CameraIntrinsics intrinsics;

			// So far, only support 1 camera for MR and always use camera index 0
			if (OVRPlugin.GetMixedRealityCameraInfo(0, out extrinsics, out intrinsics))
			{
				OVRPose worldSpacePose = ComputeCameraWorldSpacePose(extrinsics);

				float fovY = Mathf.Atan(intrinsics.FOVPort.UpTan) * Mathf.Rad2Deg * 2;
				float aspect = intrinsics.FOVPort.LeftTan / intrinsics.FOVPort.UpTan;
				directCompositionCamera.fieldOfView = fovY;
				directCompositionCamera.aspect = aspect;
				directCompositionCamera.transform.FromOVRPose(worldSpacePose);
			}
			else
			{
				Debug.LogWarning("Failed to get external camera information");
			}
		}

		if (hasCameraDeviceOpened)
		{
			if (boundaryMeshMaskTexture == null || boundaryMeshMaskTexture.width != Screen.width || boundaryMeshMaskTexture.height != Screen.height)
			{
				boundaryMeshMaskTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8);
				boundaryMeshMaskTexture.Create();
			}
			UpdateCameraFramePlaneObject(mainCamera, directCompositionCamera, boundaryMeshMaskTexture);
			directCompositionCamera.GetComponent<OVRCameraFrameCompositionManager>().boundaryMeshMaskTexture = boundaryMeshMaskTexture;
		}
	}

	public override void Cleanup()
	{
		base.Cleanup();

		OVRCompositionUtil.SafeDestroy(ref directCompositionCameraGameObject);
		directCompositionCamera = null;

		Debug.Log("DirectComposition deactivated");
	}
}

#endif
