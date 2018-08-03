using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;

[CustomEditor(typeof(OVRManager))]
public class OVRManagerEditor : Editor
{
	override public void OnInspectorGUI()
	{
		DrawDefaultInspector();

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		OVRManager manager = (OVRManager)target;
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Mixed Reality Capture", EditorStyles.boldLabel);
		SetupBoolField("Show Properties", ref manager.expandMixedRealityCapturePropertySheet);
		if (manager.expandMixedRealityCapturePropertySheet)
		{
			string[] layerMaskOptions = new string[32];
			for (int i=0; i<32; ++i)
			{
				layerMaskOptions[i] = LayerMask.LayerToName(i);
				if (layerMaskOptions[i].Length == 0)
				{
					layerMaskOptions[i] = "<Layer " + i.ToString() + ">";
				}
			}

			EditorGUI.indentLevel++;

			EditorGUILayout.Space();
			SetupBoolField("enableMixedReality", ref manager.enableMixedReality);
			SetupCompositoinMethodField("compositionMethod", ref manager.compositionMethod);
			SetupLayerMaskField("extraHiddenLayers", ref manager.extraHiddenLayers, layerMaskOptions);

			if (manager.compositionMethod == OVRManager.CompositionMethod.Direct || manager.compositionMethod == OVRManager.CompositionMethod.Sandwich)
			{
				EditorGUILayout.Space();
				if (manager.compositionMethod == OVRManager.CompositionMethod.Direct)
				{
					EditorGUILayout.LabelField("Direct Composition", EditorStyles.boldLabel);
				}
				else
				{
					EditorGUILayout.LabelField("Sandwich Composition", EditorStyles.boldLabel);
				}
				EditorGUI.indentLevel++;

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
				SetupCameraDeviceField("capturingCameraDevice", ref manager.capturingCameraDevice);
				SetupBoolField("flipCameraFrameHorizontally", ref manager.flipCameraFrameHorizontally);
				SetupBoolField("flipCameraFrameVertically", ref manager.flipCameraFrameVertically);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Chroma Key", EditorStyles.boldLabel);
				SetupColorField("chromaKeyColor", ref manager.chromaKeyColor);
				SetupFloatField("chromaKeySimilarity", ref manager.chromaKeySimilarity);
				SetupFloatField("chromaKeySmoothRange", ref manager.chromaKeySmoothRange);
				SetupFloatField("chromaKeySpillRange", ref manager.chromaKeySpillRange);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Dynamic Lighting", EditorStyles.boldLabel);
				SetupBoolField("useDynamicLighting", ref manager.useDynamicLighting);
				SetupDepthQualityField("depthQuality", ref manager.depthQuality);
				SetupFloatField("dynamicLightingSmoothFactor", ref manager.dynamicLightingSmoothFactor);
				SetupFloatField("dynamicLightingDepthVariationClampingValue", ref manager.dynamicLightingDepthVariationClampingValue);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Virtual Green Screen", EditorStyles.boldLabel);
				SetupVirtualGreenTypeField("virtualGreenScreenType", ref manager.virtualGreenScreenType);
				SetupFloatField("virtualGreenScreenTopY", ref manager.virtualGreenScreenTopY);
				SetupFloatField("virtualGreenScreenBottomY", ref manager.virtualGreenScreenBottomY);
				SetupBoolField("virtualGreenScreenApplyDepthCulling", ref manager.virtualGreenScreenApplyDepthCulling);
				SetupFloatField("virtualGreenScreenDepthTolerance", ref manager.virtualGreenScreenDepthTolerance);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Latency Control", EditorStyles.boldLabel);
				SetupFloatField("handPoseStateLatency", ref manager.handPoseStateLatency);
				if  (manager.compositionMethod == OVRManager.CompositionMethod.Sandwich)
				{
					SetupFloatField("sandwichCompositionRenderLatency", ref manager.sandwichCompositionRenderLatency);
					SetupIntField("sandwichCompositionBufferedFrames", ref manager.sandwichCompositionBufferedFrames);
				}
				EditorGUI.indentLevel--;
			}

			EditorGUI.indentLevel--;
		}
#endif
	}

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
	void SetupBoolField(string name, ref bool member)
	{
		EditorGUI.BeginChangeCheck();
		bool value = EditorGUILayout.Toggle(name, member);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			member = value;
		}
	}

	void SetupIntField(string name, ref int member)
	{
		EditorGUI.BeginChangeCheck();
		int value = EditorGUILayout.IntField(name, member);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			member = value;
		}
	}

	void SetupFloatField(string name, ref float member)
	{
		EditorGUI.BeginChangeCheck();
		float value = EditorGUILayout.FloatField(name, member);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			member = value;
		}
	}

	void SetupDoubleField(string name, ref double member)
	{
		EditorGUI.BeginChangeCheck();
		double value = EditorGUILayout.DoubleField(name, member);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			member = value;
		}
	}
	void SetupColorField(string name, ref Color member)
	{
		EditorGUI.BeginChangeCheck();
		Color value = EditorGUILayout.ColorField(name, member);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			member = value;
		}
	}

	void SetupLayerMaskField(string name, ref LayerMask layerMask, string[] layerMaskOptions)
	{
		EditorGUI.BeginChangeCheck();
		int value = EditorGUILayout.MaskField(name, layerMask, layerMaskOptions);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			layerMask = value;
		}
	}

	void SetupCompositoinMethodField(string name, ref OVRManager.CompositionMethod method)
	{
		EditorGUI.BeginChangeCheck();
		OVRManager.CompositionMethod value = (OVRManager.CompositionMethod)EditorGUILayout.EnumPopup(name, method);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			method = value;
		}
	}

	void SetupCameraDeviceField(string name, ref OVRManager.CameraDevice device)
	{
		EditorGUI.BeginChangeCheck();
		OVRManager.CameraDevice value = (OVRManager.CameraDevice)EditorGUILayout.EnumPopup(name, device);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			device = value;
		}
	}

	void SetupDepthQualityField(string name, ref OVRManager.DepthQuality depthQuality)
	{
		EditorGUI.BeginChangeCheck();
		OVRManager.DepthQuality value = (OVRManager.DepthQuality)EditorGUILayout.EnumPopup(name, depthQuality);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			depthQuality = value;
		}
	}

	void SetupVirtualGreenTypeField(string name, ref OVRManager.VirtualGreenScreenType virtualGreenScreenType)
	{
		EditorGUI.BeginChangeCheck();
		OVRManager.VirtualGreenScreenType value = (OVRManager.VirtualGreenScreenType)EditorGUILayout.EnumPopup(name, virtualGreenScreenType);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Changed " + name);
			virtualGreenScreenType = value;
		}
	}
#endif
}
