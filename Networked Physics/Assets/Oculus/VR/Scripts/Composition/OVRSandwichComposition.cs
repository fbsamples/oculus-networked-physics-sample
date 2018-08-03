using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

public class OVRSandwichComposition : OVRCameraComposition
{
	public float frameRealtime;

	public Camera fgCamera;
	public Camera bgCamera;

	public class HistoryRecord
	{
		public float timestamp = float.MinValue;
		public RenderTexture fgRenderTexture;
		public RenderTexture bgRenderTexture;
		public RenderTexture boundaryMeshMaskTexture;
	}

	public readonly int historyRecordCount = 8;        // enough to compensate 88ms latency @ 90 fps
	public readonly HistoryRecord[] historyRecordArray;
	public int historyRecordCursorIndex = 0;

	public GameObject cameraProxyPlane;

	public Camera compositionCamera;
	public OVRSandwichCompositionManager compositionManager;

	private int _cameraFramePlaneLayer = -1;

	// find an unnamed layer between 24..29
	public int cameraFramePlaneLayer
	{
		get
		{
			if (_cameraFramePlaneLayer < 0)
			{
				for (int i=24; i<=29; ++i)
				{
					string layerName = LayerMask.LayerToName(i);
					if (layerName == null || layerName.Length == 0)
					{
						_cameraFramePlaneLayer = i;
						break;
					}
				}
				if (_cameraFramePlaneLayer == -1)
				{
					Debug.LogWarning("Unable to find an unnamed layer between 24 and 29.");
					_cameraFramePlaneLayer = 25;
				}
				Debug.LogFormat("Set the CameraFramePlaneLayer in SandwichComposition to {0}. Please do NOT put any other gameobject in this layer.", _cameraFramePlaneLayer);
			}
			return _cameraFramePlaneLayer;
		}
	}

	public override OVRManager.CompositionMethod CompositionMethod() { return OVRManager.CompositionMethod.Sandwich; }

	public OVRSandwichComposition(GameObject parentObject, Camera mainCamera, OVRManager.CameraDevice cameraDevice, bool useDynamicLighting, OVRManager.DepthQuality depthQuality)
		: base(cameraDevice, useDynamicLighting, depthQuality)
	{
		frameRealtime = Time.realtimeSinceStartup;

		historyRecordCount = OVRManager.instance.sandwichCompositionBufferedFrames;
		if (historyRecordCount < 1)
		{
			Debug.LogWarning("Invalid sandwichCompositionBufferedFrames in OVRManager. It should be at least 1");
			historyRecordCount = 1;
		}
		if (historyRecordCount > 16)
		{
			Debug.LogWarning("The value of sandwichCompositionBufferedFrames in OVRManager is too big. It would consume a lot of memory. It has been override to 16");
			historyRecordCount = 16;
		}
		historyRecordArray = new HistoryRecord[historyRecordCount];
		for (int i=0; i<historyRecordCount; ++i)
		{
			historyRecordArray[i] = new HistoryRecord();
		}
		historyRecordCursorIndex = 0;

		GameObject fgObject = new GameObject("MRSandwichForegroundCamera");
		fgObject.transform.parent = parentObject.transform;
		fgCamera = fgObject.AddComponent<Camera>();
		fgCamera.depth = 200;
		fgCamera.clearFlags = CameraClearFlags.SolidColor;
		fgCamera.backgroundColor = Color.clear;
		fgCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);
		fgCamera.nearClipPlane = mainCamera.nearClipPlane;
		fgCamera.farClipPlane = mainCamera.farClipPlane;

		GameObject bgObject = new GameObject("MRSandwichBackgroundCamera");
		bgObject.transform.parent = parentObject.transform;
		bgCamera = bgObject.AddComponent<Camera>();
		bgCamera.depth = 100;
		bgCamera.clearFlags = mainCamera.clearFlags;
		bgCamera.backgroundColor = mainCamera.backgroundColor;
		bgCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);
		bgCamera.nearClipPlane = mainCamera.nearClipPlane;
		bgCamera.farClipPlane = mainCamera.farClipPlane;

		// Create cameraProxyPlane for clipping
		Debug.Assert(cameraProxyPlane == null);
		cameraProxyPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
		cameraProxyPlane.name = "MRProxyClipPlane";
		cameraProxyPlane.transform.parent = parentObject.transform;
		cameraProxyPlane.GetComponent<Collider>().enabled = false;
		cameraProxyPlane.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		Material clipMaterial = new Material(Shader.Find("Oculus/OVRMRClipPlane"));
		cameraProxyPlane.GetComponent<MeshRenderer>().material = clipMaterial;
		clipMaterial.SetColor("_Color", Color.clear);
		clipMaterial.SetFloat("_Visible", 0.0f);
		cameraProxyPlane.transform.localScale = new Vector3(1000, 1000, 1000);
		cameraProxyPlane.SetActive(true);
		OVRMRForegroundCameraManager foregroundCameraManager = fgCamera.gameObject.AddComponent<OVRMRForegroundCameraManager>();
		foregroundCameraManager.clipPlaneGameObj = cameraProxyPlane;

		GameObject compositionCameraObject = new GameObject("MRSandwichCaptureCamera");
		compositionCameraObject.transform.parent = parentObject.transform;
		compositionCamera = compositionCameraObject.AddComponent<Camera>();
		compositionCamera.stereoTargetEye = StereoTargetEyeMask.None;
		compositionCamera.depth = float.MaxValue;
		compositionCamera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
		compositionCamera.clearFlags = CameraClearFlags.Depth;
		compositionCamera.backgroundColor = mainCamera.backgroundColor;
		compositionCamera.cullingMask = 1 << cameraFramePlaneLayer;
		compositionCamera.nearClipPlane = mainCamera.nearClipPlane;
		compositionCamera.farClipPlane = mainCamera.farClipPlane;

		if (!hasCameraDeviceOpened)
		{
			Debug.LogError("Unable to open camera device " + cameraDevice);
		}
		else
		{
			Debug.Log("SandwichComposition activated : useDynamicLighting " + (useDynamicLighting ? "ON" : "OFF"));
			CreateCameraFramePlaneObject(parentObject, compositionCamera, useDynamicLighting);
			cameraFramePlaneObject.layer = cameraFramePlaneLayer;
			RefreshRenderTextures(mainCamera);
			compositionManager = compositionCamera.gameObject.AddComponent<OVRSandwichCompositionManager>();
			compositionManager.fgTexture = historyRecordArray[historyRecordCursorIndex].fgRenderTexture;
			compositionManager.bgTexture = historyRecordArray[historyRecordCursorIndex].bgRenderTexture;
		}
	}

	public override void Update(Camera mainCamera)
	{
		if (!hasCameraDeviceOpened)
		{
			return;
		}

		frameRealtime = Time.realtimeSinceStartup;

		++historyRecordCursorIndex;
		if (historyRecordCursorIndex >= historyRecordCount)
		{
			historyRecordCursorIndex = 0;
		}

		if (!OVRPlugin.SetHandNodePoseStateLatency(OVRManager.instance.handPoseStateLatency))
		{
			Debug.LogWarning("HandPoseStateLatency is invalid. Expect a value between 0.0 to 0.5, get " + OVRManager.instance.handPoseStateLatency);
		}

		RefreshRenderTextures(mainCamera);

		bgCamera.clearFlags = mainCamera.clearFlags;
		bgCamera.backgroundColor = mainCamera.backgroundColor;
		bgCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);

		fgCamera.cullingMask = mainCamera.cullingMask & (~OVRManager.instance.extraHiddenLayers);

		if (OVRMixedReality.useFakeExternalCamera || OVRPlugin.GetExternalCameraCount() == 0)
		{
			OVRPose worldSpacePose = new OVRPose();
			OVRPose trackingSpacePose = new OVRPose();
			trackingSpacePose.position = OVRMixedReality.fakeCameraPositon;
			trackingSpacePose.orientation = OVRMixedReality.fakeCameraRotation;
			worldSpacePose = OVRExtensions.ToWorldSpacePose(trackingSpacePose);

			RefreshCameraPoses(OVRMixedReality.fakeCameraFov, OVRMixedReality.fakeCameraAspect, worldSpacePose);
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

				RefreshCameraPoses(fovY, aspect, worldSpacePose);
			}
			else
			{
				Debug.LogWarning("Failed to get external camera information");
			}
		}

		compositionCamera.GetComponent<OVRCameraFrameCompositionManager>().boundaryMeshMaskTexture = historyRecordArray[historyRecordCursorIndex].boundaryMeshMaskTexture;
		HistoryRecord record = GetHistoryRecordForComposition();
		UpdateCameraFramePlaneObject(mainCamera, compositionCamera, record.boundaryMeshMaskTexture);
		OVRSandwichCompositionManager compositionManager = compositionCamera.gameObject.GetComponent<OVRSandwichCompositionManager>();
		compositionManager.fgTexture = record.fgRenderTexture;
		compositionManager.bgTexture = record.bgRenderTexture;

		cameraProxyPlane.transform.position = fgCamera.transform.position + fgCamera.transform.forward * cameraFramePlaneDistance;
		cameraProxyPlane.transform.LookAt(cameraProxyPlane.transform.position + fgCamera.transform.forward);
	}

	public override void Cleanup()
	{
		base.Cleanup();

		Camera[] cameras = { fgCamera, bgCamera, compositionCamera };

		foreach (Camera c in cameras)
		{
			OVRCompositionUtil.SafeDestroy(c.gameObject);
		}

		fgCamera = null;
		bgCamera = null;
		compositionCamera = null;

		Debug.Log("SandwichComposition deactivated");
	}

	private RenderTextureFormat DesiredRenderTextureFormat(RenderTextureFormat originalFormat)
	{
		if (originalFormat == RenderTextureFormat.RGB565)
		{
			return RenderTextureFormat.ARGB1555;
		}
		else if (originalFormat == RenderTextureFormat.RGB111110Float)
		{
			return RenderTextureFormat.ARGBHalf;
		}
		else
		{
			return originalFormat;
		}
	}

	protected void RefreshRenderTextures(Camera mainCamera)
	{
		int width = Screen.width;
		int height = Screen.height;
		RenderTextureFormat format = mainCamera.targetTexture ? DesiredRenderTextureFormat(mainCamera.targetTexture.format) : RenderTextureFormat.ARGB32;
		int depth = mainCamera.targetTexture ? mainCamera.targetTexture.depth : 24;

		Debug.Assert(fgCamera != null && bgCamera != null);

		HistoryRecord record = historyRecordArray[historyRecordCursorIndex];

		record.timestamp = frameRealtime;

		if (record.fgRenderTexture == null || record.fgRenderTexture.width != width || record.fgRenderTexture.height != height || record.fgRenderTexture.format != format || record.fgRenderTexture.depth != depth)
		{
			record.fgRenderTexture = new RenderTexture(width, height, depth, format);
			record.fgRenderTexture.name = "Sandwich FG " + historyRecordCursorIndex.ToString();
		}
		fgCamera.targetTexture = record.fgRenderTexture;

		if (record.bgRenderTexture == null || record.bgRenderTexture.width != width || record.bgRenderTexture.height != height || record.bgRenderTexture.format != format || record.bgRenderTexture.depth != depth)
		{
			record.bgRenderTexture = new RenderTexture(width, height, depth, format);
			record.bgRenderTexture.name = "Sandwich BG " + historyRecordCursorIndex.ToString();
		}
		bgCamera.targetTexture = record.bgRenderTexture;

		if (OVRManager.instance.virtualGreenScreenType != OVRManager.VirtualGreenScreenType.Off)
		{
			if (record.boundaryMeshMaskTexture == null || record.boundaryMeshMaskTexture.width != width || record.boundaryMeshMaskTexture.height != height)
			{
				record.boundaryMeshMaskTexture = new RenderTexture(width, height, 0, RenderTextureFormat.R8);
				record.boundaryMeshMaskTexture.name = "Boundary Mask " + historyRecordCursorIndex.ToString();
				record.boundaryMeshMaskTexture.Create();
			}
		}
		else
		{
			record.boundaryMeshMaskTexture = null;
		}

		Debug.Assert(fgCamera.targetTexture != null && bgCamera.targetTexture != null && (OVRManager.instance.virtualGreenScreenType == OVRManager.VirtualGreenScreenType.Off || record.boundaryMeshMaskTexture != null));
	}

	protected HistoryRecord GetHistoryRecordForComposition()
	{
		float expectedTime = frameRealtime - OVRManager.instance.sandwichCompositionRenderLatency;
		int currIndex = historyRecordCursorIndex;
		int prevIndex = currIndex - 1;
		if (prevIndex < 0)
		{
			prevIndex = historyRecordCount - 1;
		}
		while (prevIndex != historyRecordCursorIndex)
		{
			if (historyRecordArray[prevIndex].timestamp <= expectedTime)
			{
				float timeToCurrIndex = historyRecordArray[currIndex].timestamp - expectedTime;
				float timeToPrevIndex = expectedTime - historyRecordArray[prevIndex].timestamp;
				return timeToCurrIndex <= timeToPrevIndex ? historyRecordArray[currIndex] : historyRecordArray[prevIndex];
			}
			currIndex = prevIndex;
			prevIndex = currIndex - 1;
			if (prevIndex < 0) prevIndex = historyRecordCount - 1;
		}
		// return the earliest frame
		return historyRecordArray[currIndex];
	}

	protected void RefreshCameraPoses(float fovY, float aspect, OVRPose pose)
	{
		Camera[] cameras = { fgCamera, bgCamera, compositionCamera };
		foreach (Camera c in cameras)
		{
			c.fieldOfView = fovY;
			c.aspect = aspect;
			c.transform.FromOVRPose(pose);
		}
	}

	public class OVRSandwichCompositionManager : MonoBehaviour
	{
		public RenderTexture fgTexture;
		public RenderTexture bgTexture;
		public Material alphaBlendMaterial;

		void Start()
		{
			Shader alphaBlendShader = Shader.Find("Oculus/UnlitTransparent");
			if (alphaBlendShader == null)
			{
				Debug.LogError("Unable to create transparent shader");
				return;
			}
			alphaBlendMaterial = new Material(alphaBlendShader);
		}

		private void OnPreRender()
		{
			if (fgTexture == null || bgTexture == null || alphaBlendMaterial == null)
			{
				Debug.LogError("OVRSandwichCompositionManager has not setup properly");
				return;
			}

			Graphics.Blit(bgTexture, RenderTexture.active);
		}

		void OnPostRender()
		{
			if (fgTexture == null || bgTexture == null || alphaBlendMaterial == null)
			{
				Debug.LogError("OVRSandwichCompositionManager has not setup properly");
				return;
			}

			Graphics.Blit(fgTexture, RenderTexture.active, alphaBlendMaterial);
		}
	}
}

#endif
