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
using VR = UnityEngine.VR;

/// <summary>
/// Toggles an on-screen debug graph with VR rendering and performance statistics.
/// </summary>
public class OVRDebugGraph : MonoBehaviour
{
	public enum DebugPerfMode
	{
		DEBUG_PERF_OFF,         // data still being collected, just not displayed
		DEBUG_PERF_RUNNING,     // display continuously changing graph
		DEBUG_PERF_FROZEN,      // no new data collection, but displayed
		DEBUG_PERF_MAX,
	}

	/// <summary>
	/// The current display mode.
	/// </summary>
	public DebugPerfMode debugMode = DebugPerfMode.DEBUG_PERF_OFF;

	/// <summary>
	/// The gamepad button that will toggle the display mode.
	/// </summary>
	public OVRInput.RawButton toggleButton = OVRInput.RawButton.Start;

	/// <summary>
	/// Initialize the debug mode
	/// </summary>
	void Start()
	{
		if (!OVRManager.isHmdPresent)
		{
			enabled = false;
			return;
		}

		OVRPlugin.debugDisplay = (debugMode != DebugPerfMode.DEBUG_PERF_OFF);
		OVRPlugin.collectPerf = (debugMode == DebugPerfMode.DEBUG_PERF_RUNNING);
	}

	/// <summary>
	/// Check input and toggle the debug graph.
	/// See the input mapping setup in the Unity Integration guide.
	/// </summary>
	void Update()
	{
		// NOTE: some of the buttons defined in OVRInput.RawButton are not available on the Android game pad controller
		if (OVRInput.GetDown( toggleButton ))
		{
			Debug.Log(" TOGGLE GRAPH ");

			//*************************
			// toggle the debug graph .. off -> running -> paused
			//*************************
			switch (debugMode)
			{
				case DebugPerfMode.DEBUG_PERF_OFF:
					debugMode = DebugPerfMode.DEBUG_PERF_RUNNING;
					break;
				case DebugPerfMode.DEBUG_PERF_RUNNING:
					debugMode = DebugPerfMode.DEBUG_PERF_FROZEN;
					break;
				case DebugPerfMode.DEBUG_PERF_FROZEN:
					debugMode = DebugPerfMode.DEBUG_PERF_OFF;
					break;
			}
			
			// Turn on/off debug graph
			OVRPlugin.debugDisplay = (debugMode != DebugPerfMode.DEBUG_PERF_OFF);
			OVRPlugin.collectPerf = (debugMode == DebugPerfMode.DEBUG_PERF_FROZEN);
		}
	}
}
