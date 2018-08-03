using UnityEngine;
using System.Collections;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

public class OVRExternalComposition : OVRComposition
{
	private GameObject foregroundCameraGameObject;
	private Camera foregroundCamera;
	private GameObject backgroundCameraGameObject;
	private Camera backgroundCamera;
	private GameObject cameraProxyPlane;

	public override OVRManager.CompositionMethod CompositionMethod() { return OVRManager.CompositionMethod.External; }

	public OVRExternalComposition(GameObject parentObject, Camera mainCamera)
	{
		Debug.Assert(backgroundCameraGameObject == null);
		backgroundCameraGameObject = new GameObject();
		backgroundCameraGameObject.name = "MRBackgroundCamera";
		backgroundCameraGameObject.transform.parent = parentObject.transform;
		backgroundCamera = backgroundCameraGameObject.AddComponent<Camera>();
		backgroundCamera.stereoTargetEye = StereoTargetEyeMask.None;
		backgroundCamera.depth = float.MaxValue;
		backgroundCamera.rect = new Rect(0.0f, 0.0f, 0.5f, 1.0f);
		backgroundCamera.clearFlags = mainCamera.clearFlags;
		backgroundCamera.backgroundColor = mainCamera.backgroundColor;
		backgroundCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);
		backgroundCamera.nearClipPlane = mainCamera.nearClipPlane;
		backgroundCamera.farClipPlane = mainCamera.farClipPlane;

		Debug.Assert(foregroundCameraGameObject == null);
		foregroundCameraGameObject = new GameObject();
		foregroundCameraGameObject.name = "MRForgroundCamera";
		foregroundCameraGameObject.transform.parent = parentObject.transform;
		foregroundCamera = foregroundCameraGameObject.AddComponent<Camera>();
		foregroundCamera.stereoTargetEye = StereoTargetEyeMask.None;
		foregroundCamera.depth = float.MaxValue;
		foregroundCamera.rect = new Rect(0.5f, 0.0f, 0.5f, 1.0f);
		foregroundCamera.clearFlags = CameraClearFlags.Color;
		foregroundCamera.backgroundColor = OVRMixedReality.chromaKeyColor;
		foregroundCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);
		foregroundCamera.nearClipPlane = mainCamera.nearClipPlane;
		foregroundCamera.farClipPlane = mainCamera.farClipPlane;

		// Create cameraProxyPlane for clipping
		Debug.Assert(cameraProxyPlane == null);
		cameraProxyPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
		cameraProxyPlane.name = "MRProxyClipPlane";
		cameraProxyPlane.transform.parent = parentObject.transform;
		cameraProxyPlane.GetComponent<Collider>().enabled = false;
		cameraProxyPlane.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		Material clipMaterial = new Material(Shader.Find("Oculus/OVRMRClipPlane"));
		cameraProxyPlane.GetComponent<MeshRenderer>().material = clipMaterial;
		clipMaterial.SetColor("_Color", OVRMixedReality.chromaKeyColor);
		clipMaterial.SetFloat("_Visible", 0.0f);
		cameraProxyPlane.transform.localScale = new Vector3(1000, 1000, 1000);
		cameraProxyPlane.SetActive(true);
		OVRMRForegroundCameraManager foregroundCameraManager = foregroundCameraGameObject.AddComponent<OVRMRForegroundCameraManager>();
		foregroundCameraManager.clipPlaneGameObj = cameraProxyPlane;
	}

	public override void Update(Camera mainCamera)
	{
		OVRPlugin.SetHandNodePoseStateLatency(0.0);		// the HandNodePoseStateLatency doesn't apply to the external composition. Always enforce it to 0.0

		backgroundCamera.clearFlags = mainCamera.clearFlags;
		backgroundCamera.backgroundColor = mainCamera.backgroundColor;
		backgroundCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);
		backgroundCamera.nearClipPlane = mainCamera.nearClipPlane;
		backgroundCamera.farClipPlane = mainCamera.farClipPlane;

		foregroundCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);
		foregroundCamera.nearClipPlane = mainCamera.nearClipPlane;
		foregroundCamera.farClipPlane = mainCamera.farClipPlane;

		if (OVRMixedReality.useFakeExternalCamera || OVRPlugin.GetExternalCameraCount() == 0)
		{
			OVRPose worldSpacePose = new OVRPose();
			OVRPose trackingSpacePose = new OVRPose();
			trackingSpacePose.position = OVRMixedReality.fakeCameraPositon;
			trackingSpacePose.orientation = OVRMixedReality.fakeCameraRotation;
			worldSpacePose = OVRExtensions.ToWorldSpacePose(trackingSpacePose);

			backgroundCamera.fieldOfView = OVRMixedReality.fakeCameraFov;
			backgroundCamera.aspect = OVRMixedReality.fakeCameraAspect;
			backgroundCamera.transform.FromOVRPose(worldSpacePose);

			foregroundCamera.fieldOfView = OVRMixedReality.fakeCameraFov;
			foregroundCamera.aspect = OVRMixedReality.fakeCameraAspect;
			foregroundCamera.transform.FromOVRPose(worldSpacePose);
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
				backgroundCamera.fieldOfView = fovY;
				backgroundCamera.aspect = aspect;
				backgroundCamera.transform.FromOVRPose(worldSpacePose);
				foregroundCamera.fieldOfView = fovY;
				foregroundCamera.aspect = intrinsics.FOVPort.LeftTan / intrinsics.FOVPort.UpTan;
				foregroundCamera.transform.FromOVRPose(worldSpacePose);
			}
			else
			{
				Debug.LogError("Failed to get external camera information");
				return;
			}
		}

		// Assume player always standing straightly
		Vector3 externalCameraToHeadXZ = mainCamera.transform.position - foregroundCamera.transform.position;
		externalCameraToHeadXZ.y = 0;
		cameraProxyPlane.transform.position = mainCamera.transform.position;
		cameraProxyPlane.transform.LookAt(cameraProxyPlane.transform.position + externalCameraToHeadXZ);
	}

	public override void Cleanup()
	{
		OVRCompositionUtil.SafeDestroy(ref backgroundCameraGameObject);
		backgroundCamera = null;
		OVRCompositionUtil.SafeDestroy(ref foregroundCameraGameObject);
		foregroundCamera = null;
		OVRCompositionUtil.SafeDestroy(ref cameraProxyPlane);
		Debug.Log("ExternalComposition deactivated");
	}

}

/// <summary>
/// Helper internal class for foregroundCamera, don't call it outside
/// </summary>
internal class OVRMRForegroundCameraManager : MonoBehaviour
{
	public GameObject clipPlaneGameObj;
	private Material clipPlaneMaterial;
	void OnPreRender()
	{
		// the clipPlaneGameObj should be only visible to foreground camera
		if (clipPlaneGameObj)
		{
			if (clipPlaneMaterial == null)
				clipPlaneMaterial = clipPlaneGameObj.GetComponent<MeshRenderer>().material;
			clipPlaneGameObj.GetComponent<MeshRenderer>().material.SetFloat("_Visible", 1.0f);
		}
	}
	void OnPostRender()
	{
		if (clipPlaneGameObj)
		{
			Debug.Assert(clipPlaneMaterial);
			clipPlaneGameObj.GetComponent<MeshRenderer>().material.SetFloat("_Visible", 0.0f);
		}
	}
}

#endif
