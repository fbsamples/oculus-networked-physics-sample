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
using UnityEditor;

/// <summary>
/// Allows Oculus to build apps from the command line.
/// </summary>
partial class OculusBuildApp
{
    static void SetAndroidTarget()
    {
#if UNITY_5
		EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
#else
		EditorUserBuildSettings.androidBuildSubtarget = AndroidBuildSubtarget.ETC2;
#endif
	if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.Android);
        }
    }
}

