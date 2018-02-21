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
using System.Collections;

/// <summary>
/// Shows the Oculus plaform UI.
/// </summary>
public class OVRPlatformMenu : MonoBehaviour
{
	/// <summary>
	/// A timer that appears at the gaze cursor before a platform UI transition.
	/// </summary>
	public GameObject cursorTimer;

	/// <summary>
	/// The current color of the cursor timer.
	/// </summary>
	public Color cursorTimerColor = new Color(0.0f, 0.643f, 1.0f, 1.0f);	// set default color to same as native cursor timer

	/// <summary>
	/// The distance at which the cursor timer appears.
	/// </summary>
	public float fixedDepth = 3.0f;

	/// <summary>
	/// The key code.
	/// </summary>
	public KeyCode keyCode = KeyCode.Escape;

	public enum eHandler
	{
		ResetCursor,
		ShowGlobalMenu,
		ShowConfirmQuit,
	};

	public eHandler doubleTapHandler = eHandler.ResetCursor;
	public eHandler shortPressHandler = eHandler.ShowConfirmQuit;
	public eHandler longPressHandler = eHandler.ShowGlobalMenu;

	private GameObject instantiatedCursorTimer = null;
	private Material cursorTimerMaterial = null;
	private float doubleTapDelay = 0.25f;
	private float shortPressDelay = 0.25f;
	private float longPressDelay = 0.75f;

	enum eBackButtonAction
	{
		NONE,
		DOUBLE_TAP,
		SHORT_PRESS,
		LONG_PRESS
	};

	private int downCount = 0;
	private int upCount = 0;
	private float initialDownTime = -1.0f;
	private bool waitForUp = false;

	eBackButtonAction ResetAndSendAction( eBackButtonAction action )
	{
		print( "ResetAndSendAction( " + action + " );" );
		downCount = 0;
		upCount = 0;
		initialDownTime = -1.0f;
		waitForUp = false;
		ResetCursor();
		if ( action == eBackButtonAction.LONG_PRESS )
		{
			// since a long press triggers off of time and not an up,
			// wait for an up to happen before handling any more key state.
			waitForUp = true;
		}
		return action;
	}

	eBackButtonAction HandleBackButtonState() 
	{
		if ( waitForUp )
		{
			if ( !Input.GetKeyDown( keyCode ) && !Input.GetKey( keyCode ) )
			{
				waitForUp = false;
			}
			else
			{
				return eBackButtonAction.NONE;
			}
		}

		if ( Input.GetKeyDown( keyCode ) )
		{
			// just came down
			downCount++;
			if ( downCount == 1 )
			{
				initialDownTime = Time.realtimeSinceStartup;
			}
		}
		else if ( downCount > 0 )
		{
			if ( Input.GetKey( keyCode ) )
			{
				if ( downCount <= upCount )
				{
					// just went down
					downCount++;
				}

				float timeSinceFirstDown = Time.realtimeSinceStartup - initialDownTime;
				if ( timeSinceFirstDown > shortPressDelay )
				{
					// The gaze cursor timer should start unfilled once short-press time is exceeded
					// then fill up completely, so offset the times by the short-press delay.
					float t = ( timeSinceFirstDown - shortPressDelay ) / ( longPressDelay - shortPressDelay );
					UpdateCursor( t );
				}

				if ( timeSinceFirstDown > longPressDelay )
				{
					return ResetAndSendAction( eBackButtonAction.LONG_PRESS );
				}
			}
			else
			{
				bool started = initialDownTime >= 0.0f;
				if ( started )
				{
					if ( upCount < downCount )
					{
						// just came up
						upCount++;
					}

					float timeSinceFirstDown = Time.realtimeSinceStartup - initialDownTime;
					if ( timeSinceFirstDown < doubleTapDelay )
					{
						if ( downCount == 2 && upCount == 2 )
						{
							return ResetAndSendAction( eBackButtonAction.DOUBLE_TAP );
						}
					}
					else if ( timeSinceFirstDown > shortPressDelay )
					{
						if ( downCount == 1 && upCount == 1 )
						{
							return ResetAndSendAction( eBackButtonAction.SHORT_PRESS );
						}
					}
					else if ( timeSinceFirstDown < longPressDelay )
					{
						// this is an abort of a long press after short-press delay has passed
						return ResetAndSendAction( eBackButtonAction.NONE );
					}
				}
			}
		}

		// down reset, but perform no action
		return eBackButtonAction.NONE;
	}

	/// <summary>
	/// Instantiate the cursor timer
	/// </summary>
	void Awake()
	{
		if (!OVRManager.isHmdPresent)
		{
			enabled = false;
			return;
		}
		if ((cursorTimer != null) && (instantiatedCursorTimer == null)) 
		{
			//Debug.Log("Instantiating CursorTimer");
			instantiatedCursorTimer = Instantiate(cursorTimer) as GameObject;
			if (instantiatedCursorTimer != null)
			{
				cursorTimerMaterial = instantiatedCursorTimer.GetComponent<Renderer>().material;
				cursorTimerMaterial.SetColor ( "_Color", cursorTimerColor ); 
				instantiatedCursorTimer.GetComponent<Renderer>().enabled = false;
			}
		}
	}

	/// <summary>
	/// Destroy the cloned material
	/// </summary>
	void OnDestroy()
	{
		if (cursorTimerMaterial != null)
		{
			Destroy(cursorTimerMaterial);
		}
	}

	/// <summary>
	/// Reset when resuming
	/// </summary>
	void OnApplicationFocus( bool focusState )
	{
		//Input.ResetInputAxes();
		//ResetAndSendAction( eBackButtonAction.LONG_PRESS );
	}

	/// <summary>
	/// Reset when resuming
	/// </summary>
	void OnApplicationPause( bool pauseStatus ) 
	{
		if ( !pauseStatus )
		{
			Input.ResetInputAxes();
		}
		//ResetAndSendAction( eBackButtonAction.LONG_PRESS );
	}

	/// <summary>
	/// Show the confirm quit menu
	/// </summary>
	void ShowConfirmQuitMenu()
	{
		ResetCursor();

#if UNITY_ANDROID && !UNITY_EDITOR
		Debug.Log("[PlatformUI-ConfirmQuit] Showing @ " + Time.time);
		OVRManager.PlatformUIConfirmQuit();
#endif
	}

	/// <summary>
	/// Show the platform UI global menu
	/// </summary>
	void ShowGlobalMenu()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		Debug.Log("[PlatformUI-Global] Showing @ " + Time.time);
		OVRManager.PlatformUIGlobalMenu();
#endif
	}

	void DoHandler(eHandler handler)
	{
		if (handler == eHandler.ResetCursor)
			ResetCursor ();
		if (handler == eHandler.ShowConfirmQuit)
			ShowConfirmQuitMenu ();
		if (handler == eHandler.ShowGlobalMenu)
			ShowGlobalMenu ();
	}

	/// <summary>
	/// Tests for long-press and activates global platform menu when detected.
	/// as per the Unity integration doc, the back button responds to "mouse 1" button down/up/etc
	/// </summary>
	void Update()
	{
#if UNITY_ANDROID
		eBackButtonAction action = HandleBackButtonState();
		if ( action == eBackButtonAction.DOUBLE_TAP )
			DoHandler(doubleTapHandler);
		else if ( action == eBackButtonAction.SHORT_PRESS )
			DoHandler(shortPressHandler);
		else if ( action == eBackButtonAction.LONG_PRESS )
			DoHandler(longPressHandler);
#endif
	}

	/// <summary>
	/// Update the cursor based on how long the back button is pressed
	/// </summary>
	void UpdateCursor(float timerRotateRatio)
	{
		timerRotateRatio = Mathf.Clamp( timerRotateRatio, 0.0f, 1.0f );
		if (instantiatedCursorTimer != null)
		{
			instantiatedCursorTimer.GetComponent<Renderer>().enabled = true;

			// Clamp the rotation ratio to avoid rendering artifacts
			float rampOffset = Mathf.Clamp(1.0f - timerRotateRatio, 0.0f, 1.0f);
			cursorTimerMaterial.SetFloat ( "_ColorRampOffset", rampOffset );
			//print( "alphaAmount = " + alphaAmount );

			// Draw timer at fixed distance in front of camera
			// cursor positions itself based on camera forward and draws at a fixed depth
			Vector3 cameraForward = Camera.main.transform.forward;
			Vector3 cameraPos = Camera.main.transform.position;
			instantiatedCursorTimer.transform.position = cameraPos + (cameraForward * fixedDepth);
			instantiatedCursorTimer.transform.forward = cameraForward;
		}
	}

	void ResetCursor()
	{
		if (instantiatedCursorTimer != null)
		{
			cursorTimerMaterial.SetFloat("_ColorRampOffset", 1.0f);
			instantiatedCursorTimer.GetComponent<Renderer>().enabled = false;
			//print( "ResetCursor" );
		}
	}
}
