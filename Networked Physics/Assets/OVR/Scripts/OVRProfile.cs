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
using System.Collections;
using System.Threading;
using VR = UnityEngine.VR;

/// <summary>
/// Contains information about the user's preferences and body dimensions.
/// </summary>
/// <description>
/// ** Accessing the cloud profile data **
/// 
/// > Create a new profile object:
/// 	
/// 	OVRProfile profile = new OVRProfile();
/// 
/// 
/// > Call Java async load (connects to Horizon service for local data):
/// 	
/// 	profile.TriggerAndroidLoad()
/// 
/// 
/// 	For most cases, you can probably just create the profile object in Awake() and 
/// 	immediately call the load.
/// 		
/// 		
/// > Check the status of the load:
/// 		
/// 	profile.GetState();
/// 
/// Possible returns, are an enum OVRProfile.State:
/// 
/// 	NOT_TRIGGERED	// Haven't called TriggerAndroidLoad() yet
/// 	LOADING			// Java hasn't returned a result yet
/// 	READY			// Success, we have profile data from Horizon
/// 	ERROR			// Something failed, maybe no Horizon, no user, etc
/// 		
/// 		
/// > Check to see if the load failed for some reason, maybe no Horizon installed or no
///   logged in user:
/// 				
/// 	profile.IsLoadFailure();
/// 
///  	For the failure case in OVRProfile.ProfileCompleteListener.onFailure(), 
/// 	it might make sense to set defaults using OVRProfile.SetFallbackDefaults(), as
/// 	it's unlikely any retry would succeed where the original request failed.
/// 
/// > If everything went well, we can now the the logged in user ID, name and locale using:
/// 
/// 	profile.GetID();		//ex: 9dasfl454
/// 	profile.GetName();		//ex: brian.dewolff
/// 	profile.GetLocale();	//ex: en_US
/// 
/// </description>
public class OVRProfile : Object
{
	public enum State
	{
		NOT_TRIGGERED,
		LOADING,
		READY,
		ERROR
	};
	
#if UNITY_ANDROID && !UNITY_EDITOR
	private class ProfileCompleteListener : OVROnCompleteListener
	{
		private OVRProfile mProfile;
		
		public ProfileCompleteListener(OVRProfile profile)
		{
			mProfile = profile;
		}
		
		public override void onSuccess()
		{
			mProfile.SetFromSystem();
		}
		
		public override void onFailure()
		{
			mProfile.state = State.ERROR;
		}
	}
#endif

	public string id { get; private set; }
	public string userName { get; private set; }
	public string locale { get; private set; }
	public float ipd { get; private set; }
	public float eyeHeight { get; private set; }
	public float eyeDepth { get; private set; }
	public float neckHeight { get; private set; }

	public State state { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
	private AndroidJavaClass jniOvr;
#endif

	public OVRProfile()
	{
		SetFallbackDefaults();
	}

	public void TriggerLoad()
	{
		state = State.LOADING;

#if UNITY_ANDROID && !UNITY_EDITOR
		try
		{
			AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
			AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
			
			jniOvr = new AndroidJavaClass("com.oculus.svclib.OVR");
			jniOvr.CallStatic("loadProfileAsync", activity, new ProfileCompleteListener(this));
		}
		catch
		{
			Debug.LogWarning("Failed to create Profile Listener");
			state = State.ERROR;
		}
#else
		SetFromSystem();
#endif
	}

	private void SetFromSystem()
	{
		SetFallbackDefaults();

#if UNITY_ANDROID && !UNITY_EDITOR
		id = jniOvr.CallStatic<string>("getProfileID");
		userName = jniOvr.CallStatic<string>("getProfileName");
		locale = jniOvr.CallStatic<string>("getProfileLocale");
#endif

		if (OVRManager.isHmdPresent)
		{
			ipd = OVRPlugin.ipd;
			eyeHeight = OVRPlugin.eyeHeight;
			eyeDepth = OVRPlugin.eyeDepth;
			neckHeight = eyeHeight - 0.075f;
		}

		state = State.READY;
	}

	private void SetFallbackDefaults()
	{
		id = "000abc123def";
		userName = "Oculus User";
		locale = "en_US";

		ipd = 0.06f;
		eyeHeight = 1.675f;
		eyeDepth = 0.0805f;
		neckHeight = eyeHeight - 0.075f;

		state = State.NOT_TRIGGERED;
	}
}
