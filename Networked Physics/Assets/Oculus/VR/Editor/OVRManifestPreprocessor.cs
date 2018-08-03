using UnityEngine;
using UnityEditor;
using System.IO;

class OVRManifestPreprocessor
{
	[MenuItem("Tools/Oculus/Create store-compatible AndroidManifest.xml", false, 100000)]
	public static void GenerateManifestForSubmission()
	{
		var so = ScriptableObject.CreateInstance(typeof(OVRPluginUpdaterStub));
		var script = MonoScript.FromScriptableObject(so);
		string assetPath = AssetDatabase.GetAssetPath(script);
		string editorDir = Directory.GetParent(assetPath).FullName;
		string srcFile = editorDir + "/AndroidManifest.OVRSubmission.xml";

		if (!File.Exists(srcFile))
		{
			Debug.LogError("Cannot find Android manifest template for submission." +
				" Please delete the OVR folder and reimport the Oculus Utilities.");
			return;
		}

		string manifestFolder = Application.dataPath + "/Plugins/Android";

		if (!Directory.Exists(manifestFolder))
			Directory.CreateDirectory(manifestFolder);

		string dstFile = manifestFolder + "/AndroidManifest.xml";

		if (File.Exists(dstFile))
		{
			Debug.LogWarning("Cannot create Oculus store-compatible manifest due to conflicting file: \""
				+ dstFile + "\". Please remove it and try again.");
			return;
		}

		File.Copy(srcFile, dstFile);
		AssetDatabase.Refresh();
	}

	[MenuItem("Tools/Oculus/Remove AndroidManifest.xml")]
	public static void RemoveAndroidManifest()
	{
		AssetDatabase.DeleteAsset("Assets/Plugins/Android/AndroidManifest.xml");
		AssetDatabase.Refresh();
	}
}
