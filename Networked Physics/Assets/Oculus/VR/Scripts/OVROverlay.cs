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

using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

/// <summary>
/// Add OVROverlay script to an object with an optional mesh primitive
/// rendered as a TimeWarp overlay instead by drawing it into the eye buffer.
/// This will take full advantage of the display resolution and avoid double
/// resampling of the texture.
/// 
/// We support 3 types of Overlay shapes right now
///		1. Quad : This is most common overlay type , you render a quad in Timewarp space.
///		2. Cylinder: [Mobile Only][Experimental], Display overlay as partial surface of a cylinder
///			* The cylinder's center will be your game object's center
///			* We encoded the cylinder's parameters in transform.scale, 
///				**[scale.z] is the radius of the cylinder
///				**[scale.y] is the height of the cylinder
///				**[scale.x] is the length of the arc of cylinder
///		* Limitations
///				**Only the half of the cylinder can be displayed, which means the arc angle has to be smaller than 180 degree,  [scale.x] / [scale.z] <= PI
///				**Your camera has to be inside of the inscribed sphere of the cylinder, the overlay will be faded out automatically when the camera is close to the inscribed sphere's surface.
///				**Translation only works correctly with vrDriver 1.04 or above
///		3. Cubemap: Display overlay as a cube map
///		4. OffcenterCubemap: [Mobile Only] Display overlay as a cube map with a texture coordinate offset
///			* The actually sampling will looks like [color = texture(cubeLayerSampler, normalize(direction) + offset)] instead of [color = texture( cubeLayerSampler, direction )]
///			* The extra center offset can be feed from transform.position
///			* Note: if transform.position's magnitude is greater than 1, which will cause some cube map pixel always invisible 
///					Which is usually not what people wanted, we don't kill the ability for developer to do so here, but will warn out.
///     5. Equirect: Display overlay as a 360-degree equirectangular skybox.
/// </summary>
public class OVROverlay : MonoBehaviour
{
#region Interface

	/// <summary>
	/// Determines the on-screen appearance of a layer.
	/// </summary>
	public enum OverlayShape
	{
		Quad = OVRPlugin.OverlayShape.Quad,
		Cylinder = OVRPlugin.OverlayShape.Cylinder,
		Cubemap = OVRPlugin.OverlayShape.Cubemap,
		OffcenterCubemap = OVRPlugin.OverlayShape.OffcenterCubemap,
		Equirect = OVRPlugin.OverlayShape.Equirect,
	}

	/// <summary>
	/// Whether the layer appears behind or infront of other content in the scene.
	/// </summary>
	public enum OverlayType
	{
		None,
		Underlay,
		Overlay,
	};

	/// <summary>
	/// Specify overlay's type
	/// </summary>
	public OverlayType currentOverlayType = OverlayType.Overlay;

	/// <summary>
	/// If true, the texture's content is copied to the compositor each frame.
	/// </summary>
	public bool isDynamic = false;

	/// <summary>
	/// If true, the layer would be used to present protected content (e.g. HDCP). The flag is effective only on PC.
	/// </summary>
	public bool isProtectedContent = false;

	/// <summary>
	/// Specify overlay's shape
	/// </summary>
	public OverlayShape currentOverlayShape = OverlayShape.Quad;
	private OverlayShape prevOverlayShape = OverlayShape.Quad;

	/// <summary>
	/// The left- and right-eye Textures to show in the layer.
	/// \note If you need to change the texture on a per-frame basis, please use OverrideOverlayTextureInfo(..) to avoid caching issues.
	/// </summary>
	public Texture[] textures = new Texture[] { null, null };

	protected IntPtr[] texturePtrs = new IntPtr[] { IntPtr.Zero, IntPtr.Zero };

	/// <summary>
	/// Use this function to set texture and texNativePtr when app is running 
	/// GetNativeTexturePtr is a slow behavior, the value should be pre-cached 
	/// </summary>
#if UNITY_2017_2_OR_NEWER
	public void OverrideOverlayTextureInfo(Texture srcTexture, IntPtr nativePtr, UnityEngine.XR.XRNode node)
#else
	public void OverrideOverlayTextureInfo(Texture srcTexture, IntPtr nativePtr, UnityEngine.VR.VRNode node)
#endif
	{
#if UNITY_2017_2_OR_NEWER
		int index = (node == UnityEngine.XR.XRNode.RightEye) ? 1 : 0;
#else
		int index = (node == UnityEngine.VR.VRNode.RightEye) ? 1 : 0;
#endif

		if (textures.Length <= index)
			return;
		
		textures[index] = srcTexture;
		texturePtrs[index] = nativePtr;

		isOverridePending = true;
	}

	protected bool isOverridePending;

	internal const int maxInstances = 15;
	internal static OVROverlay[] instances = new OVROverlay[maxInstances];

#endregion

	private static Material tex2DMaterial;
	private static Material cubeMaterial;

	private OVRPlugin.LayerLayout layout {
		get {
#if UNITY_ANDROID && !UNITY_EDITOR
			if (textures.Length == 2 && textures[1] != null)
				return OVRPlugin.LayerLayout.Stereo;
#endif
			return OVRPlugin.LayerLayout.Mono;
		}
	}

	private struct LayerTexture {
		public Texture appTexture;
		public IntPtr appTexturePtr;
		public Texture[] swapChain;
		public IntPtr[] swapChainPtr;
	};
	private LayerTexture[] layerTextures;

	private OVRPlugin.LayerDesc layerDesc;
	private int stageCount = -1;

	private int layerIndex = -1; // Controls the composition order based on wake-up time.

	private int layerId = 0; // The layer's internal handle in the compositor.
	private GCHandle layerIdHandle;
	private IntPtr layerIdPtr = IntPtr.Zero;

	private int frameIndex = 0;
	private int prevFrameIndex = -1;

	private Renderer rend;

	private int texturesPerStage { get { return (layout == OVRPlugin.LayerLayout.Stereo) ? 2 : 1; } }

	private bool CreateLayer(int mipLevels, int sampleCount, OVRPlugin.EyeTextureFormat etFormat, int flags, OVRPlugin.Sizei size, OVRPlugin.OverlayShape shape)
	{
		if (!layerIdHandle.IsAllocated || layerIdPtr == IntPtr.Zero)
		{
			layerIdHandle = GCHandle.Alloc(layerId, GCHandleType.Pinned);
			layerIdPtr = layerIdHandle.AddrOfPinnedObject();
		}

		if (layerIndex == -1)
		{
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

		bool needsSetup = (
			isOverridePending ||
			layerDesc.MipLevels != mipLevels ||
			layerDesc.SampleCount != sampleCount ||
			layerDesc.Format != etFormat ||
			layerDesc.Layout != layout ||
			layerDesc.LayerFlags != flags ||
			!layerDesc.TextureSize.Equals(size) ||
			layerDesc.Shape != shape);

		if (!needsSetup)
			return false;

		OVRPlugin.LayerDesc desc = OVRPlugin.CalculateLayerDesc(shape, layout, size, mipLevels, sampleCount, etFormat, flags);
		OVRPlugin.EnqueueSetupLayer(desc, layerIdPtr);
		layerId = (int)layerIdHandle.Target;

		if (layerId > 0)
		{
			layerDesc = desc;
			stageCount = OVRPlugin.GetLayerTextureStageCount(layerId);
		}

		isOverridePending = false;

		return true;
	}

	private bool CreateLayerTextures(bool useMipmaps, OVRPlugin.Sizei size, bool isHdr)
	{
		bool needsCopy = false;

		if (stageCount <= 0)
			return false;

		// For newer SDKs, blit directly to the surface that will be used in compositing.

		if (layerTextures == null)
			layerTextures = new LayerTexture[texturesPerStage];

		for (int eyeId = 0; eyeId < texturesPerStage; ++eyeId)
		{
			if (layerTextures[eyeId].swapChain == null)
				layerTextures[eyeId].swapChain = new Texture[stageCount];
			
			if (layerTextures[eyeId].swapChainPtr == null)
				layerTextures[eyeId].swapChainPtr = new IntPtr[stageCount];

			for (int stage = 0; stage < stageCount; ++stage)
			{
				Texture sc = layerTextures[eyeId].swapChain[stage];
				IntPtr scPtr = layerTextures[eyeId].swapChainPtr[stage];

				if (sc != null && scPtr != IntPtr.Zero)
					continue;

				if (scPtr == IntPtr.Zero)
					scPtr = OVRPlugin.GetLayerTexture(layerId, stage, (OVRPlugin.Eye)eyeId);

				if (scPtr == IntPtr.Zero)
					continue;

				var txFormat = (isHdr) ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;

				if (currentOverlayShape != OverlayShape.Cubemap && currentOverlayShape != OverlayShape.OffcenterCubemap)
					sc = Texture2D.CreateExternalTexture(size.w, size.h, txFormat, useMipmaps, true, scPtr);
#if UNITY_2017_1_OR_NEWER
				else
					sc = Cubemap.CreateExternalTexture(size.w, txFormat, useMipmaps, scPtr);
#endif

				layerTextures[eyeId].swapChain[stage] = sc;
				layerTextures[eyeId].swapChainPtr[stage] = scPtr;

				needsCopy = true;
			}
		}

		return needsCopy;
	}

	private void DestroyLayerTextures()
	{
		for (int eyeId = 0; layerTextures != null && eyeId < texturesPerStage; ++eyeId)
		{
			if (layerTextures[eyeId].swapChain != null)
			{
				for (int stage = 0; stage < stageCount; ++stage)
					DestroyImmediate(layerTextures[eyeId].swapChain[stage]);
			}
		}

		layerTextures = null;
	}

	private void DestroyLayer()
	{
		if (layerIndex != -1)
		{
			// Turn off the overlay if it was on.
			OVRPlugin.EnqueueSubmitLayer(true, false, IntPtr.Zero, IntPtr.Zero, -1, 0, OVRPose.identity.ToPosef(), Vector3.one.ToVector3f(), layerIndex, (OVRPlugin.OverlayShape)prevOverlayShape);
			instances[layerIndex] = null;
			layerIndex = -1;
		}

		if (layerIdPtr != IntPtr.Zero)
		{
			OVRPlugin.EnqueueDestroyLayer(layerIdPtr);
			layerIdPtr = IntPtr.Zero;
			layerIdHandle.Free();
			layerId = 0;
		}

		layerDesc = new OVRPlugin.LayerDesc();

		frameIndex = 0;
		prevFrameIndex = -1;
	}

	private bool LatchLayerTextures()
	{
		for (int i = 0; i < texturesPerStage; ++i)
		{
			if (textures[i] != layerTextures[i].appTexture || layerTextures[i].appTexturePtr == IntPtr.Zero)
			{
				if (textures[i] != null)
				{
#if UNITY_EDITOR
					var assetPath = UnityEditor.AssetDatabase.GetAssetPath(textures[i]);
					var importer = (UnityEditor.TextureImporter)UnityEditor.TextureImporter.GetAtPath(assetPath);
					if (importer && importer.textureType != UnityEditor.TextureImporterType.Default)
					{
						Debug.LogError("Need Default Texture Type for overlay");
						return false;
					}
#endif
					var rt = textures[i] as RenderTexture;
					if (rt && !rt.IsCreated())
						rt.Create();
					
					layerTextures[i].appTexturePtr = (texturePtrs[i] != IntPtr.Zero) ? texturePtrs[i] : textures[i].GetNativeTexturePtr();

					if (layerTextures[i].appTexturePtr != IntPtr.Zero)
						layerTextures[i].appTexture = textures[i];
				}
			}

			if (currentOverlayShape == OverlayShape.Cubemap)
			{
				if (textures[i] as Cubemap == null)
				{
					Debug.LogError("Need Cubemap texture for cube map overlay");
					return false;
				}
			}
		}

#if !UNITY_ANDROID || UNITY_EDITOR
		if (currentOverlayShape == OverlayShape.OffcenterCubemap)
		{
			Debug.LogWarning("Overlay shape " + currentOverlayShape + " is not supported on current platform");
			return false;
		}
#endif

		if (layerTextures[0].appTexture == null || layerTextures[0].appTexturePtr == IntPtr.Zero)
			return false;

		return true;
	}

	private OVRPlugin.LayerDesc GetCurrentLayerDesc()
	{
		OVRPlugin.LayerDesc newDesc = new OVRPlugin.LayerDesc() {
			Format = OVRPlugin.EyeTextureFormat.R8G8B8A8_sRGB,
			LayerFlags = (int)OVRPlugin.LayerFlags.TextureOriginAtBottomLeft,
			Layout = layout,
			MipLevels = 1,
			SampleCount = 1,
			Shape = (OVRPlugin.OverlayShape)currentOverlayShape,
			TextureSize = new OVRPlugin.Sizei() { w = textures[0].width, h = textures[0].height }
		};

		var tex2D = textures[0] as Texture2D;
		if (tex2D != null)
		{
			if (tex2D.format == TextureFormat.RGBAHalf || tex2D.format == TextureFormat.RGBAFloat)
				newDesc.Format = OVRPlugin.EyeTextureFormat.R16G16B16A16_FP;
			
			newDesc.MipLevels = tex2D.mipmapCount;
		}

		var texCube = textures[0] as Cubemap;
		if (texCube != null)
		{
			if (texCube.format == TextureFormat.RGBAHalf || texCube.format == TextureFormat.RGBAFloat)
				newDesc.Format = OVRPlugin.EyeTextureFormat.R16G16B16A16_FP;
			
			newDesc.MipLevels = texCube.mipmapCount;
		}

		var rt = textures[0] as RenderTexture;
		if (rt != null)
		{
			newDesc.SampleCount = rt.antiAliasing;

			if (rt.format == RenderTextureFormat.ARGBHalf || rt.format == RenderTextureFormat.ARGBFloat || rt.format == RenderTextureFormat.RGB111110Float)
				newDesc.Format = OVRPlugin.EyeTextureFormat.R16G16B16A16_FP;
		}

		if (isProtectedContent)
		{
			newDesc.LayerFlags |= (int)OVRPlugin.LayerFlags.ProtectedContent;
		}

		return newDesc;
	}

	private bool PopulateLayer(int mipLevels, bool isHdr, OVRPlugin.Sizei size, int sampleCount, int stage)
	{
		bool ret = false;

		RenderTextureFormat rtFormat = (isHdr) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;

		for (int eyeId = 0; eyeId < texturesPerStage; ++eyeId)
		{
			Texture et = layerTextures[eyeId].swapChain[stage];
			if (et == null)
				continue;

			for (int mip = 0; mip < mipLevels; ++mip)
			{
				int width = size.w >> mip;
				if (width < 1) width = 1;
				int height = size.h >> mip;
				if (height < 1) height = 1;
#if UNITY_2017_1_1 || UNITY_2017_2_OR_NEWER
				RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height, rtFormat, 0);
				descriptor.msaaSamples = sampleCount;
				descriptor.useMipMap = true;
				descriptor.autoGenerateMips = false;
				descriptor.sRGB = false;

				var tempRTDst = RenderTexture.GetTemporary(descriptor);
#else
				var tempRTDst = RenderTexture.GetTemporary(width, height, 0, rtFormat, RenderTextureReadWrite.Linear, sampleCount);
#endif

				if (!tempRTDst.IsCreated())
					tempRTDst.Create();

				tempRTDst.DiscardContents();

				bool dataIsLinear = isHdr || (QualitySettings.activeColorSpace == ColorSpace.Linear);

#if !UNITY_2017_1_OR_NEWER
				var rt = textures[eyeId] as RenderTexture;
				dataIsLinear |= rt != null && rt.sRGB; //HACK: Unity 5.6 and earlier convert to linear on read from sRGB RenderTexture.
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
				dataIsLinear = true; //HACK: Graphics.CopyTexture causes linear->srgb conversion on target write with D3D but not GLES.
#endif

				if (currentOverlayShape != OverlayShape.Cubemap && currentOverlayShape != OverlayShape.OffcenterCubemap)
				{
					tex2DMaterial.SetInt("_linearToSrgb", (!isHdr && dataIsLinear) ? 1 : 0);
					
					//Resolve, decompress, swizzle, etc not handled by simple CopyTexture.
#if !UNITY_ANDROID || UNITY_EDITOR
					// The PC compositor uses premultiplied alpha, so multiply it here.
					tex2DMaterial.SetInt("_premultiply", 1);
#endif
					Graphics.Blit(textures[eyeId], tempRTDst, tex2DMaterial);
					Graphics.CopyTexture(tempRTDst, 0, 0, et, 0, mip);
				}
#if UNITY_2017_1_OR_NEWER
				else // Cubemap
				{
					for (int face = 0; face < 6; ++face)
					{
						cubeMaterial.SetInt("_linearToSrgb", (!isHdr && dataIsLinear) ? 1 : 0);
						
#if !UNITY_ANDROID || UNITY_EDITOR
						// The PC compositor uses premultiplied alpha, so multiply it here.
						cubeMaterial.SetInt("_premultiply", 1);
#endif
						cubeMaterial.SetInt("_face", face);
						//Resolve, decompress, swizzle, etc not handled by simple CopyTexture.
						Graphics.Blit(textures[eyeId], tempRTDst, cubeMaterial);
						Graphics.CopyTexture(tempRTDst, 0, 0, et, face, mip);
					}
				}
#endif
				RenderTexture.ReleaseTemporary(tempRTDst);

				ret = true;
			}
		}

		return ret;
	}

	private bool SubmitLayer(bool overlay, bool headLocked, OVRPose pose, Vector3 scale, int frameIndex)
	{
		int rightEyeIndex = (texturesPerStage >= 2) ? 1 : 0;
		bool isOverlayVisible = OVRPlugin.EnqueueSubmitLayer(overlay, headLocked, layerTextures[0].appTexturePtr, layerTextures[rightEyeIndex].appTexturePtr, layerId, frameIndex, pose.flipZ().ToPosef(), scale.ToVector3f(), layerIndex, (OVRPlugin.OverlayShape)currentOverlayShape);

		prevOverlayShape = currentOverlayShape;

		return isOverlayVisible;
	}

#region Unity Messages

	void Awake()
	{
		Debug.Log("Overlay Awake");

		if (tex2DMaterial == null)
			tex2DMaterial = new Material(Shader.Find("Oculus/Texture2D Blit"));

		if (cubeMaterial == null)
			cubeMaterial = new Material(Shader.Find("Oculus/Cubemap Blit"));

		rend = GetComponent<Renderer>();

		if (textures.Length == 0)
			textures = new Texture[] { null };

		// Backward compatibility
		if (rend != null && textures[0] == null)
			textures[0] = rend.material.mainTexture;
	}

	void OnEnable()
	{
		if (!OVRManager.isHmdPresent)
		{
			enabled = false;
			return;
		}
	}

	void OnDisable()
	{
		if ((gameObject.hideFlags & HideFlags.DontSaveInBuild) != 0)
			return;
	
		DestroyLayerTextures();
		DestroyLayer();
	}

	void OnDestroy()
	{
		DestroyLayerTextures();
		DestroyLayer();
	}

	bool ComputeSubmit(ref OVRPose pose, ref Vector3 scale, ref bool overlay, ref bool headLocked)
	{
		Camera headCamera = Camera.main;

		overlay = (currentOverlayType == OverlayType.Overlay);
		headLocked = false;
		for (var t = transform; t != null && !headLocked; t = t.parent)
			headLocked |= (t == headCamera.transform);

		pose = (headLocked) ? transform.ToHeadSpacePose(headCamera) : transform.ToTrackingSpacePose(headCamera);
		scale = transform.lossyScale;
		for (int i = 0; i < 3; ++i)
			scale[i] /= headCamera.transform.lossyScale[i];

		if (currentOverlayShape == OverlayShape.Cubemap)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			//HACK: VRAPI cubemaps assume are yawed 180 degrees relative to LibOVR.
			pose.orientation = pose.orientation * Quaternion.AngleAxis(180, Vector3.up);
#endif
			pose.position = headCamera.transform.position;
		}

		// Pack the offsetCenter directly into pose.position for offcenterCubemap
		if (currentOverlayShape == OverlayShape.OffcenterCubemap)
		{
			pose.position = transform.position;
			if (pose.position.magnitude > 1.0f)
			{
				Debug.LogWarning("Your cube map center offset's magnitude is greater than 1, which will cause some cube map pixel always invisible .");
				return false;
			}
		}

		// Cylinder overlay sanity checking
		if (currentOverlayShape == OverlayShape.Cylinder)
		{
			float arcAngle = scale.x / scale.z / (float)Math.PI * 180.0f;
			if (arcAngle > 180.0f)
			{
				Debug.LogWarning("Cylinder overlay's arc angle has to be below 180 degree, current arc angle is " + arcAngle + " degree." );
				return false;
			}
		}

		return true;
	}

	void LateUpdate()
	{
		// The overlay must be specified every eye frame, because it is positioned relative to the
		// current head location.  If frames are dropped, it will be time warped appropriately,
		// just like the eye buffers.
		if (currentOverlayType == OverlayType.None || textures.Length < texturesPerStage || textures[0] == null)
			return;

		OVRPose pose = OVRPose.identity;
		Vector3 scale = Vector3.one;
		bool overlay = false;
		bool headLocked = false;
		if (!ComputeSubmit(ref pose, ref scale, ref overlay, ref headLocked))
			return;

		OVRPlugin.LayerDesc newDesc = GetCurrentLayerDesc();
		bool isHdr = (newDesc.Format == OVRPlugin.EyeTextureFormat.R16G16B16A16_FP);

		bool createdLayer = CreateLayer(newDesc.MipLevels, newDesc.SampleCount, newDesc.Format, newDesc.LayerFlags, newDesc.TextureSize, newDesc.Shape);

		if (layerIndex == -1 || layerId <= 0)
			return;
	
		bool useMipmaps = (newDesc.MipLevels > 1);

		createdLayer |= CreateLayerTextures(useMipmaps, newDesc.TextureSize, isHdr);

		if (layerTextures[0].appTexture as RenderTexture != null)
			isDynamic = true;

		if (!LatchLayerTextures())
			return;

		// Don't populate the same frame image twice.
		if (frameIndex > prevFrameIndex)
		{
			int stage = frameIndex % stageCount;
			if (!PopulateLayer (newDesc.MipLevels, isHdr, newDesc.TextureSize, newDesc.SampleCount, stage))
				return;
		}

		bool isOverlayVisible = SubmitLayer(overlay, headLocked, pose, scale, frameIndex);

		prevFrameIndex = frameIndex;
		if (isDynamic)
			++frameIndex;
		
		// Backward compatibility: show regular renderer if overlay isn't visible.
		if (rend)
			rend.enabled = !isOverlayVisible;
	}

#endregion
}
