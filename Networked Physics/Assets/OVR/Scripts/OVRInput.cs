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

#if !UNITY_5
#define OVR_LEGACY
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Provides a unified input system for Oculus controllers and gamepads.
/// </summary>
public static class OVRInput
{
	[Flags]
	/// Virtual button mappings that allow the same input bindings to work across different controllers.
	public enum Button
	{
		None                      = 0,          ///< Maps to RawButton: [Gamepad, Touch, LTouch, RTouch: None]
		One                       = 0x00000001, ///< Maps to RawButton: [Gamepad, Touch, RTouch: A], [LTouch: X]
		Two                       = 0x00000002, ///< Maps to RawButton: [Gamepad, Touch, RTouch: B], [LTouch: Y]
		Three                     = 0x00000004, ///< Maps to RawButton: [Gamepad, Touch: X], [LTouch, RTouch: None]
		Four                      = 0x00000008, ///< Maps to RawButton: [Gamepad, Touch: Y], [LTouch, RTouch: None]
		Start                     = 0x00000100, ///< Maps to RawButton: [Gamepad: Start], [Touch, LTouch, RTouch: None]
		Back                      = 0x00000200, ///< Maps to RawButton: [Gamepad: Back], [Touch, LTouch, RTouch: None]
		PrimaryShoulder           = 0x00001000, ///< Maps to RawButton: [Gamepad: LShoulder], [Touch, LTouch, RTouch: None]
		PrimaryIndexTrigger       = 0x00002000, ///< Maps to RawButton: [Gamepad, Touch, LTouch: LIndexTrigger], [RTouch: RIndexTrigger]
		PrimaryHandTrigger        = 0x00004000, ///< Maps to RawButton: [Gamepad: None], [Touch, LTouch: LHandTrigger], [RTouch: RHandTrigger]
		PrimaryThumbstick         = 0x00008000, ///< Maps to RawButton: [Gamepad, Touch, LTouch: LThumbstick], [RTouch: RThumbstick]
		PrimaryThumbstickUp       = 0x00010000, ///< Maps to RawButton: [Gamepad, Touch, LTouch: LThumbstickUp], [RTouch: RThumbstickUp]
		PrimaryThumbstickDown     = 0x00020000, ///< Maps to RawButton: [Gamepad, Touch, LTouch: LThumbstickDown], [RTouch: RThumbstickDown]
		PrimaryThumbstickLeft     = 0x00040000, ///< Maps to RawButton: [Gamepad, Touch, LTouch: LThumbstickLeft], [RTouch: RThumbstickLeft]
		PrimaryThumbstickRight    = 0x00080000, ///< Maps to RawButton: [Gamepad, Touch, LTouch: LThumbstickRight], [RTouch: RThumbstickRight]
		SecondaryShoulder         = 0x00100000, ///< Maps to RawButton: [Gamepad: RShoulder], [Touch, LTouch, RTouch: None]
		SecondaryIndexTrigger     = 0x00200000, ///< Maps to RawButton: [Gamepad, Touch: RIndexTrigger], [LTouch, RTouch: None]
		SecondaryHandTrigger      = 0x00400000, ///< Maps to RawButton: [Gamepad: None], [Touch: RHandTrigger], [LTouch, RTouch: None]
		SecondaryThumbstick       = 0x00800000, ///< Maps to RawButton: [Gamepad, Touch: RThumbstick], [LTouch, RTouch: None]
		SecondaryThumbstickUp     = 0x01000000, ///< Maps to RawButton: [Gamepad, Touch: RThumbstickUp], [LTouch, RTouch: None]
		SecondaryThumbstickDown   = 0x02000000, ///< Maps to RawButton: [Gamepad, Touch: RThumbstickDown], [LTouch, RTouch: None]
		SecondaryThumbstickLeft   = 0x04000000, ///< Maps to RawButton: [Gamepad, Touch: RThumbstickLeft], [LTouch, RTouch: None]
		SecondaryThumbstickRight  = 0x08000000, ///< Maps to RawButton: [Gamepad, Touch: RThumbstickRight], [LTouch, RTouch: None]
		DpadUp                    = 0x00000010, ///< Maps to RawButton: [Gamepad: DpadUp], [Touch, LTouch, RTouch: None]
		DpadDown                  = 0x00000020, ///< Maps to RawButton: [Gamepad: DpadDown], [Touch, LTouch, RTouch: None]
		DpadLeft                  = 0x00000040, ///< Maps to RawButton: [Gamepad: DpadLeft], [Touch, LTouch, RTouch: None]
		DpadRight                 = 0x00000080, ///< Maps to RawButton: [Gamepad: DpadRight], [Touch, LTouch, RTouch: None]
		Up                        = 0x10000000, ///< Maps to RawButton: [Gamepad, Touch, LTouch: LThumbstickUp], [RTouch: RThumbstickUp]
		Down                      = 0x20000000, ///< Maps to RawButton: [Gamepad, Touch, LTouch: LThumbstickDown], [RTouch: RThumbstickDown]
		Left                      = 0x40000000, ///< Maps to RawButton: [Gamepad, Touch, LTouch: LThumbstickLeft], [RTouch: RThumbstickLeft]
		Right     = unchecked((int)0x80000000), ///< Maps to RawButton: [Gamepad, Touch, LTouch: LThumbstickRight], [RTouch: RThumbstickRight]
		Any                       = ~None,      ///< Maps to RawButton: [Gamepad, Touch, LTouch, RTouch: Any]
	}

	[Flags]
	/// Raw button mappings that can be used to directly query the state of a controller.
	public enum RawButton
	{
		None                      = 0,          ///< Maps to Physical Button: [Gamepad, Touch, LTouch, RTouch: None]
		A                         = 0x00000001, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: A], [LTouch: None]
		B                         = 0x00000002, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: B], [LTouch: None]
		X                         = 0x00000100, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: X], [RTouch: None]
		Y                         = 0x00000200, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: Y], [RTouch: None]
		Start                     = 0x00100000, ///< Maps to Physical Button: [Gamepad: Start], [Touch, LTouch, RTouch: None]
		Back                      = 0x00200000, ///< Maps to Physical Button: [Gamepad: Back], [Touch, LTouch, RTouch: None]
		LShoulder                 = 0x00000800, ///< Maps to Physical Button: [Gamepad: LShoulder], [Touch, LTouch, RTouch: None]
		LIndexTrigger             = 0x10000000, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: LIndexTrigger], [RTouch: None]
		LHandTrigger              = 0x20000000, ///< Maps to Physical Button: [Gamepad: None], [Touch, LTouch: LHandTrigger], [RTouch: None]
		LThumbstick               = 0x00000400, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: LThumbstick], [RTouch: None]
		LThumbstickUp             = 0x00000010, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: LThumbstickUp], [RTouch: None]
		LThumbstickDown           = 0x00000020, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: LThumbstickDown], [RTouch: None]
		LThumbstickLeft           = 0x00000040, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: LThumbstickLeft], [RTouch: None]
		LThumbstickRight          = 0x00000080, ///< Maps to Physical Button: [Gamepad, Touch, LTouch: LThumbstickRight], [RTouch: None]
		RShoulder                 = 0x00000008, ///< Maps to Physical Button: [Gamepad: RShoulder], [Touch, LTouch, RTouch: None]
		RIndexTrigger             = 0x04000000, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: RIndexTrigger], [LTouch: None]
		RHandTrigger              = 0x08000000, ///< Maps to Physical Button: [Gamepad: None], [Touch, RTouch: RHandTrigger], [LTouch: None]
		RThumbstick               = 0x00000004, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: RThumbstick], [LTouch: None]
		RThumbstickUp             = 0x00001000, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: RThumbstickUp], [LTouch: None]
		RThumbstickDown           = 0x00002000, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: RThumbstickDown], [LTouch: None]
		RThumbstickLeft           = 0x00004000, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: RThumbstickLeft], [LTouch: None]
		RThumbstickRight          = 0x00008000, ///< Maps to Physical Button: [Gamepad, Touch, RTouch: RThumbstickRight], [LTouch: None]
		DpadUp                    = 0x00010000, ///< Maps to Physical Button: [Gamepad: DpadUp], [Touch, LTouch, RTouch: None]
		DpadDown                  = 0x00020000, ///< Maps to Physical Button: [Gamepad: DpadDown], [Touch, LTouch, RTouch: None]
		DpadLeft                  = 0x00040000, ///< Maps to Physical Button: [Gamepad: DpadLeft], [Touch, LTouch, RTouch: None]
		DpadRight                 = 0x00080000, ///< Maps to Physical Button: [Gamepad: DpadRight], [Touch, LTouch, RTouch: None]
		Any                       = ~None,      ///< Maps to Physical Button: [Gamepad, Touch, LTouch, RTouch: Any]
	}

    [Flags]
	/// Virtual capacitive touch mappings that allow the same input bindings to work across different controllers with capacitive touch support.
	public enum Touch
	{
		None                      = 0,                            ///< Maps to RawTouch: [Gamepad, Touch, LTouch, RTouch: None]
		One                       = Button.One,                   ///< Maps to RawTouch: [Gamepad: None], [Touch, RTouch: A], [LTouch: X]
		Two                       = Button.Two,                   ///< Maps to RawTouch: [Gamepad: None], [Touch, RTouch: B], [LTouch: Y]
		Three                     = Button.Three,                 ///< Maps to RawTouch: [Gamepad: None], [Touch: X], [LTouch, RTouch: None]
		Four                      = Button.Four,                  ///< Maps to RawTouch: [Gamepad: None], [Touch: Y], [LTouch, RTouch: None]
		PrimaryIndexTrigger       = Button.PrimaryIndexTrigger,   ///< Maps to RawTouch: [Gamepad: None], [Touch, LTouch: LIndexTrigger], [RTouch: RIndexTrigger]
		PrimaryThumbstick         = Button.PrimaryThumbstick,     ///< Maps to RawTouch: [Gamepad: None], [Touch, LTouch: LThumbstick], [RTouch: RThumbstick]
		PrimaryThumbRest          = 0x00001000,                   ///< Maps to RawTouch: [Gamepad: None], [Touch, LTouch: LThumbRest], [RTouch: RThumbRest]
		SecondaryIndexTrigger     = Button.SecondaryIndexTrigger, ///< Maps to RawTouch: [Gamepad: None], [Touch: RIndexTrigger], [LTouch, RTouch: None]
		SecondaryThumbstick       = Button.SecondaryThumbstick,   ///< Maps to RawTouch: [Gamepad: None], [Touch: RThumbstick], [LTouch, RTouch: None]
		SecondaryThumbRest        = 0x00100000,                   ///< Maps to RawTouch: [Gamepad: None], [Touch: RThumbRest], [LTouch, RTouch: None]
		Any                       = ~None,                        ///< Maps to RawTouch: [Gamepad: None], [Touch, LTouch, RTouch: Any]
	}

    [Flags]
	/// Raw capacitive touch mappings that can be used to directly query the state of a controller.
	public enum RawTouch
	{
		None                      = 0,                            ///< Maps to Physical Touch: [Gamepad, Touch, LTouch, RTouch: None]
		A                         = RawButton.A,                  ///< Maps to Physical Touch: [Gamepad: None], [Touch, RTouch: A], [LTouch: None]
		B                         = RawButton.B,                  ///< Maps to Physical Touch: [Gamepad: None], [Touch, RTouch: B], [LTouch: None]
		X                         = RawButton.X,                  ///< Maps to Physical Touch: [Gamepad: None], [Touch, LTouch: X], [RTouch: None]
		Y                         = RawButton.Y,                  ///< Maps to Physical Touch: [Gamepad: None], [Touch, LTouch: Y], [RTouch: None]
		LIndexTrigger             = 0x00001000,                   ///< Maps to Physical Touch: [Gamepad: None], [Touch, LTouch: LIndexTrigger], [RTouch: None]
		LThumbstick               = RawButton.LThumbstick,        ///< Maps to Physical Touch: [Gamepad: None], [Touch, LTouch: LThumbstick], [RTouch: None]
		LThumbRest                = 0x00000800,                   ///< Maps to Physical Touch: [Gamepad: None], [Touch, LTouch: LThumbRest], [RTouch: None]
		RIndexTrigger             = 0x00000010,                   ///< Maps to Physical Touch: [Gamepad: None], [Touch, RTouch: RIndexTrigger], [LTouch: None]
		RThumbstick               = RawButton.RThumbstick,        ///< Maps to Physical Touch: [Gamepad: None], [Touch, RTouch: RThumbstick], [LTouch: None]
		RThumbRest                = 0x00000008,                   ///< Maps to Physical Touch: [Gamepad: None], [Touch, RTouch: RThumbRest], [LTouch: None]
		Any                       = ~None,                        ///< Maps to Physical Touch: [Gamepad: None], [Touch, LTouch, RTouch: Any]
	}

    [Flags]
	/// Virtual near touch mappings that allow the same input bindings to work across different controllers with near touch support.
	/// A near touch uses the capacitive touch sensors of a controller to detect approximate finger proximity prior to a full touch being reported.
	public enum NearTouch
	{
		None                      = 0,          ///< Maps to RawNearTouch: [Gamepad, Touch, LTouch, RTouch: None]
		PrimaryIndexTrigger       = 0x00000001, ///< Maps to RawNearTouch: [Gamepad: None], [Touch, LTouch: LIndexTrigger], [RTouch: RIndexTrigger]
		PrimaryThumbButtons       = 0x00000002, ///< Maps to RawNearTouch: [Gamepad: None], [Touch, LTouch: LThumbButtons], [RTouch: RThumbButtons]
		SecondaryIndexTrigger     = 0x00000004, ///< Maps to RawNearTouch: [Gamepad: None], [Touch: RIndexTrigger], [LTouch, RTouch: None]
		SecondaryThumbButtons     = 0x00000008, ///< Maps to RawNearTouch: [Gamepad: None], [Touch: RThumbButtons], [LTouch, RTouch: None]
		Any                       = ~None,      ///< Maps to RawNearTouch: [Gamepad: None], [Touch, LTouch, RTouch: Any]
	}

    [Flags]
	/// Raw near touch mappings that can be used to directly query the state of a controller.
	public enum RawNearTouch
	{
		None                      = 0,          ///< Maps to Physical NearTouch: [Gamepad, Touch, LTouch, RTouch: None]
		LIndexTrigger             = 0x00000001, ///< Maps to Physical NearTouch: [Gamepad: None], Implies finger is in close proximity to LIndexTrigger.
		LThumbButtons             = 0x00000002, ///< Maps to Physical NearTouch: [Gamepad: None], Implies thumb is in close proximity to LThumbstick OR X/Y buttons.
		RIndexTrigger             = 0x00000004, ///< Maps to Physical NearTouch: [Gamepad: None], Implies finger is in close proximity to RIndexTrigger.
		RThumbButtons             = 0x00000008, ///< Maps to Physical NearTouch: [Gamepad: None], Implies thumb is in close proximity to RThumbstick OR A/B buttons.
		Any                       = ~None,      ///< Maps to Physical NearTouch: [Gamepad: None], [Touch, LTouch, RTouch: Any]
	}

    [Flags]
	/// Virtual 1-dimensional axis (float) mappings that allow the same input bindings to work across different controllers.
	public enum Axis1D
	{
		None                      = 0,     ///< Maps to RawAxis1D: [Gamepad, Touch, LTouch, RTouch: None]
		PrimaryIndexTrigger       = 0x01,  ///< Maps to RawAxis1D: [Gamepad, Touch, LTouch: LIndexTrigger], [RTouch: RIndexTrigger]
		PrimaryHandTrigger        = 0x04,  ///< Maps to RawAxis1D: [Gamepad: None], [Touch, LTouch: LHandTrigger], [RTouch: RHandTrigger]
		SecondaryIndexTrigger     = 0x02,  ///< Maps to RawAxis1D: [Gamepad, Touch: RIndexTrigger], [LTouch, RTouch: None]
		SecondaryHandTrigger      = 0x08,  ///< Maps to RawAxis1D: [Gamepad: None], [Touch: RHandTrigger], [LTouch, RTouch: None]
		Any                       = ~None, ///< Maps to RawAxis1D: [Gamepad, Touch, LTouch, RTouch: Any]
	}

    [Flags]
	/// Raw 1-dimensional axis (float) mappings that can be used to directly query the state of a controller.
	public enum RawAxis1D
	{
		None                      = 0,     ///< Maps to Physical Axis1D: [Gamepad, Touch, LTouch, RTouch: None]
		LIndexTrigger             = 0x01,  ///< Maps to Physical Axis1D: [Gamepad, Touch, LTouch: LIndexTrigger], [RTouch: None]
		LHandTrigger              = 0x04,  ///< Maps to Physical Axis1D: [Gamepad: None], [Touch, LTouch: LHandTrigger], [RTouch: None]
		RIndexTrigger             = 0x02,  ///< Maps to Physical Axis1D: [Gamepad, Touch, RTouch: RIndexTrigger], [LTouch: None]
		RHandTrigger              = 0x08,  ///< Maps to Physical Axis1D: [Gamepad: None], [Touch, RTouch: RHandTrigger], [LTouch: None]
		Any                       = ~None, ///< Maps to Physical Axis1D: [Gamepad, Touch, LTouch, RTouch: Any]
	}

    [Flags]
	/// Virtual 2-dimensional axis (Vector2) mappings that allow the same input bindings to work across different controllers.
	public enum Axis2D
	{
		None                      = 0,     ///< Maps to RawAxis2D: [Gamepad, Touch, LTouch, RTouch: None]
		PrimaryThumbstick         = 0x01,  ///< Maps to RawAxis2D: [Gamepad, Touch, LTouch: LThumbstick], [RTouch: RThumbstick]
		SecondaryThumbstick       = 0x02,  ///< Maps to RawAxis2D: [Gamepad, Touch: RThumbstick], [LTouch, RTouch: None]
		Any                       = ~None, ///< Maps to RawAxis2D: [Gamepad, Touch, LTouch, RTouch: Any]
	}

    [Flags]
	/// Raw 2-dimensional axis (Vector2) mappings that can be used to directly query the state of a controller.
	public enum RawAxis2D
	{
		None                      = 0,     ///< Maps to Physical Axis2D: [Gamepad, Touch, LTouch, RTouch: None]
		LThumbstick               = 0x01,  ///< Maps to Physical Axis2D: [Gamepad, Touch, LTouch: LThumbstick], [RTouch: None]
		RThumbstick               = 0x02,  ///< Maps to Physical Axis2D: [Gamepad, Touch, RTouch: RThumbstick], [LTouch: None]
		Any                       = ~None, ///< Maps to Physical Axis2D: [Gamepad, Touch, LTouch, RTouch: Any]
	}

	[Flags]
	/// Identifies a controller which can be used to query the virtual or raw input state.
	public enum Controller
	{
		None                      = OVRPlugin.Controller.None,    ///< Null controller.
		LTouch                    = OVRPlugin.Controller.LTouch,  ///< Left Oculus Touch controller. Virtual input mapping differs from the combined L/R Touch mapping.
		RTouch                    = OVRPlugin.Controller.RTouch,  ///< Right Oculus Touch controller. Virtual input mapping differs from the combined L/R Touch mapping.
		Touch                     = OVRPlugin.Controller.Touch,   ///< Combined Left/Right pair of Oculus Touch controllers.
		Remote                    = OVRPlugin.Controller.Remote,  ///< Oculus Remote controller.
		Gamepad                   = OVRPlugin.Controller.Gamepad, ///< Xbox 360 or Xbox One gamepad on PC. Generic gamepad on Android.
		Active                    = OVRPlugin.Controller.Active,  ///< Default controller. Represents the controller that most recently registered a button press from the user.
		All                       = OVRPlugin.Controller.All,     ///< Represents the logical OR of all controllers.
	}

	private static readonly float AXIS_AS_BUTTON_THRESHOLD = 0.5f;
	private static readonly float AXIS_DEADZONE_THRESHOLD = 0.2f;
	private static List<OVRControllerBase> controllers;
	private static Controller activeControllerType = Controller.None;
	private static Controller connectedControllerTypes = Controller.None;

	/// <summary>
	/// Creates an instance of OVRInput.
	/// </summary>
	static OVRInput()
	{
		controllers = new List<OVRControllerBase>
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			new OVRControllerGamepadAndroid(),
#else
			new OVRControllerGamepadDesktop(),
			new OVRControllerTouch(),
			new OVRControllerLTouch(),
			new OVRControllerRTouch(),
			new OVRControllerRemote(),
#endif
		};
	}

	/// <summary>
	/// Updates the internal state of the OVRInput. Must be called manually if used independently from OVRManager.
	/// </summary>
	public static void Update()
	{
		connectedControllerTypes = Controller.None;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			connectedControllerTypes |= controller.Update();

			if ((connectedControllerTypes & controller.controllerType) != 0)
			{
				if (Get(RawButton.Any, controller.controllerType)
					|| Get(RawTouch.Any, controller.controllerType))
				{
					activeControllerType = controller.controllerType;
				}
			}
		}

		if ((activeControllerType == Controller.LTouch) || (activeControllerType == Controller.RTouch))
		{
			// If either Touch controller is Active, set both to Active.
			activeControllerType = Controller.Touch;
		}

		if ((connectedControllerTypes & activeControllerType) == 0)
		{
			activeControllerType = Controller.None;
		}
	}

	/// <summary>
	/// Returns true if the given Controller's orientation is currently tracked.
	/// Only supported for Oculus LTouch and RTouch controllers. Non-tracked controllers will return false.
	/// </summary>
	public static bool GetControllerOrientationTracked(OVRInput.Controller controllerType)
	{
		switch (controllerType)
		{
			case Controller.LTouch:
                return OVRPlugin.GetNodeOrientationTracked(OVRPlugin.Node.HandLeft);
            case Controller.RTouch:
                return OVRPlugin.GetNodeOrientationTracked(OVRPlugin.Node.HandRight);
            default:
				return false;
		}
	}

	/// <summary>
	/// Returns true if the given Controller's position is currently tracked.
	/// Only supported for Oculus LTouch and RTouch controllers. Non-tracked controllers will return false.
	/// </summary>
	public static bool GetControllerPositionTracked(OVRInput.Controller controllerType)
	{
		switch (controllerType)
		{
			case Controller.LTouch:
                return OVRPlugin.GetNodePositionTracked(OVRPlugin.Node.HandLeft);
            case Controller.RTouch:
                return OVRPlugin.GetNodePositionTracked(OVRPlugin.Node.HandRight);
            default:
				return false;
		}
	}

	/// <summary>
	/// Gets the position of the given Controller local to its tracking space.
	/// Only supported for Oculus LTouch and RTouch controllers. Non-tracked controllers will return Vector3.zero.
	/// </summary>
	public static Vector3 GetLocalControllerPosition(OVRInput.Controller controllerType)
	{
		switch (controllerType)
		{
			case Controller.LTouch:
                return OVRPlugin.GetNodePose(OVRPlugin.Node.HandLeft).ToOVRPose().position;
            case Controller.RTouch:
                return OVRPlugin.GetNodePose(OVRPlugin.Node.HandRight).ToOVRPose().position;
            default:
				return Vector3.zero;
		}
	}

	/// <summary>
    /// Gets the linear velocity of the given Controller local to its tracking space.
    /// Only supported for Oculus LTouch and RTouch controllers. Non-tracked controllers will return Vector3.zero.
    /// </summary>
    public static Vector3 GetLocalControllerVelocity(OVRInput.Controller controllerType)
    {
        switch (controllerType)
        {
            case Controller.LTouch:
                return OVRPlugin.GetNodeVelocity(OVRPlugin.Node.HandLeft).ToOVRPose().position;
            case Controller.RTouch:
                return OVRPlugin.GetNodeVelocity(OVRPlugin.Node.HandRight).ToOVRPose().position;
            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    /// Gets the linear acceleration of the given Controller local to its tracking space.
    /// Only supported for Oculus LTouch and RTouch controllers. Non-tracked controllers will return Vector3.zero.
    /// </summary>
    public static Vector3 GetLocalControllerAcceleration(OVRInput.Controller controllerType)
    {
        switch (controllerType)
        {
            case Controller.LTouch:
                return OVRPlugin.GetNodeAcceleration(OVRPlugin.Node.HandLeft).ToOVRPose().position;
            case Controller.RTouch:
                return OVRPlugin.GetNodeAcceleration(OVRPlugin.Node.HandRight).ToOVRPose().position;
            default:
                return Vector3.zero;
        }
    }

	/// <summary>
	/// Gets the rotation of the given Controller local to its tracking space.
	/// Only supported for Oculus LTouch and RTouch controllers. Non-tracked controllers will return Quaternion.identity.
	/// </summary>
	public static Quaternion GetLocalControllerRotation(OVRInput.Controller controllerType)
	{
		switch (controllerType)
		{
			case Controller.LTouch:
                return OVRPlugin.GetNodePose(OVRPlugin.Node.HandLeft).ToOVRPose().orientation;
            case Controller.RTouch:
                return OVRPlugin.GetNodePose(OVRPlugin.Node.HandRight).ToOVRPose().orientation;
            default:
				return Quaternion.identity;
		}
	}

    /// <summary>
    /// Gets the angular velocity of the given Controller local to its tracking space.
    /// Only supported for Oculus LTouch and RTouch controllers. Non-tracked controllers will return Quaternion.identity.
    /// </summary>
    public static Quaternion GetLocalControllerAngularVelocity(OVRInput.Controller controllerType)
    {
        switch (controllerType)
        {
            case Controller.LTouch:
                return OVRPlugin.GetNodeVelocity(OVRPlugin.Node.HandLeft).ToOVRPose().orientation;
            case Controller.RTouch:
                return OVRPlugin.GetNodeVelocity(OVRPlugin.Node.HandRight).ToOVRPose().orientation;
            default:
                return Quaternion.identity;
        }
    }

    /// <summary>
    /// Gets the angular acceleration of the given Controller local to its tracking space.
    /// Only supported for Oculus LTouch and RTouch controllers. Non-tracked controllers will return Quaternion.identity.
    /// </summary>
    public static Quaternion GetLocalControllerAngularAcceleration(OVRInput.Controller controllerType)
    {
        switch (controllerType)
        {
            case Controller.LTouch:
                return OVRPlugin.GetNodeAcceleration(OVRPlugin.Node.HandLeft).ToOVRPose().orientation;
            case Controller.RTouch:
                return OVRPlugin.GetNodeAcceleration(OVRPlugin.Node.HandRight).ToOVRPose().orientation;
            default:
                return Quaternion.identity;
        }
    }

	/// <summary>
	/// Gets the current state of the given virtual button mask with the given controller mask.
	/// Returns true if any masked button is down on any masked controller.
	/// </summary>
	public static bool Get(Button virtualMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedButton(virtualMask, RawButton.None, controllerMask);
	}

	/// <summary>
	/// Gets the current state of the given raw button mask with the given controller mask.
	/// Returns true if any masked button is down on any masked controllers.
	/// </summary>
	public static bool Get(RawButton rawMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedButton(Button.None, rawMask, controllerMask);
	}

	private static bool GetResolvedButton(Button virtualMask, RawButton rawMask, Controller controllerMask)
	{
		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				RawButton resolvedMask = rawMask | controller.ResolveToRawMask(virtualMask);

				if (((RawButton)controller.currentState.Buttons & resolvedMask) != 0)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Gets the current down state of the given virtual button mask with the given controller mask.
	/// Returns true if any masked button was pressed this frame on any masked controller and no masked button was previously down last frame.
	/// </summary>
	public static bool GetDown(Button virtualMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedButtonDown(virtualMask, RawButton.None, controllerMask);
	}

	/// <summary>
	/// Gets the current down state of the given raw button mask with the given controller mask.
	/// Returns true if any masked button was pressed this frame on any masked controller and no masked button was previously down last frame.
	/// </summary>
	public static bool GetDown(RawButton rawMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedButtonDown(Button.None, rawMask, controllerMask);
	}

	private static bool GetResolvedButtonDown(Button virtualMask, RawButton rawMask, Controller controllerMask)
	{
		bool down = false;

		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				RawButton resolvedMask = rawMask | controller.ResolveToRawMask(virtualMask);

				if (((RawButton)controller.previousState.Buttons & resolvedMask) != 0)
				{
					return false;
				}

				if ((((RawButton)controller.currentState.Buttons & resolvedMask) != 0)
					&& (((RawButton)controller.previousState.Buttons & resolvedMask) == 0))
				{
					down = true;
				}
			}
		}

		return down;
	}

	/// <summary>
	/// Gets the current up state of the given virtual button mask with the given controller mask.
	/// Returns true if any masked button was released this frame on any masked controller and no other masked button is still down this frame.
	/// </summary>
	public static bool GetUp(Button virtualMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedButtonUp(virtualMask, RawButton.None, controllerMask);
	}

	/// <summary>
	/// Gets the current up state of the given raw button mask with the given controller mask.
	/// Returns true if any masked button was released this frame on any masked controller and no other masked button is still down this frame.
	/// </summary>
	public static bool GetUp(RawButton rawMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedButtonUp(Button.None, rawMask, controllerMask);
	}

	private static bool GetResolvedButtonUp(Button virtualMask, RawButton rawMask, Controller controllerMask)
	{
		bool up = false;

		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				RawButton resolvedMask = rawMask | controller.ResolveToRawMask(virtualMask);

				if (((RawButton)controller.currentState.Buttons & resolvedMask) != 0)
				{
					return false;
				}

				if ((((RawButton)controller.currentState.Buttons & resolvedMask) == 0)
					&& (((RawButton)controller.previousState.Buttons & resolvedMask) != 0))
				{
					up = true;
				}
			}
		}

		return up;
	}

	/// <summary>
	/// Gets the current state of the given virtual touch mask with the given controller mask.
	/// Returns true if any masked touch is down on any masked controller.
	/// </summary>
	public static bool Get(Touch virtualMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedTouch(virtualMask, RawTouch.None, controllerMask);
	}

	/// <summary>
	/// Gets the current state of the given raw touch mask with the given controller mask.
	/// Returns true if any masked touch is down on any masked controllers.
	/// </summary>
	public static bool Get(RawTouch rawMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedTouch(Touch.None, rawMask, controllerMask);
	}

	private static bool GetResolvedTouch(Touch virtualMask, RawTouch rawMask, Controller controllerMask)
	{
		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				RawTouch resolvedMask = rawMask | controller.ResolveToRawMask(virtualMask);

				if (((RawTouch)controller.currentState.Touches & resolvedMask) != 0)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Gets the current down state of the given virtual touch mask with the given controller mask.
	/// Returns true if any masked touch was pressed this frame on any masked controller and no masked touch was previously down last frame.
	/// </summary>
	public static bool GetDown(Touch virtualMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedTouchDown(virtualMask, RawTouch.None, controllerMask);
	}

	/// <summary>
	/// Gets the current down state of the given raw touch mask with the given controller mask.
	/// Returns true if any masked touch was pressed this frame on any masked controller and no masked touch was previously down last frame.
	/// </summary>
	public static bool GetDown(RawTouch rawMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedTouchDown(Touch.None, rawMask, controllerMask);
	}

	private static bool GetResolvedTouchDown(Touch virtualMask, RawTouch rawMask, Controller controllerMask)
	{
		bool down = false;

		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				RawTouch resolvedMask = rawMask | controller.ResolveToRawMask(virtualMask);

				if (((RawTouch)controller.previousState.Touches & resolvedMask) != 0)
				{
					return false;
				}

				if ((((RawTouch)controller.currentState.Touches & resolvedMask) != 0)
					&& (((RawTouch)controller.previousState.Touches & resolvedMask) == 0))
				{
					down = true;
				}
			}
		}

		return down;
	}

	/// <summary>
	/// Gets the current up state of the given virtual touch mask with the given controller mask.
	/// Returns true if any masked touch was released this frame on any masked controller and no other masked touch is still down this frame.
	/// </summary>
	public static bool GetUp(Touch virtualMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedTouchUp(virtualMask, RawTouch.None, controllerMask);
	}

	/// <summary>
	/// Gets the current up state of the given raw touch mask with the given controller mask.
	/// Returns true if any masked touch was released this frame on any masked controller and no other masked touch is still down this frame.
	/// </summary>
	public static bool GetUp(RawTouch rawMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedTouchUp(Touch.None, rawMask, controllerMask);
	}

	private static bool GetResolvedTouchUp(Touch virtualMask, RawTouch rawMask, Controller controllerMask)
	{
		bool up = false;

		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				RawTouch resolvedMask = rawMask | controller.ResolveToRawMask(virtualMask);

				if (((RawTouch)controller.currentState.Touches & resolvedMask) != 0)
				{
					return false;
				}

				if ((((RawTouch)controller.currentState.Touches & resolvedMask) == 0)
					&& (((RawTouch)controller.previousState.Touches & resolvedMask) != 0))
				{
					up = true;
				}
			}
		}

		return up;
	}

	/// <summary>
	/// Gets the current state of the given virtual near touch mask with the given controller mask.
	/// Returns true if any masked near touch is down on any masked controller.
	/// </summary>
	public static bool Get(NearTouch virtualMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedNearTouch(virtualMask, RawNearTouch.None, controllerMask);
	}

	/// <summary>
	/// Gets the current state of the given raw near touch mask with the given controller mask.
	/// Returns true if any masked near touch is down on any masked controllers.
	/// </summary>
	public static bool Get(RawNearTouch rawMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedNearTouch(NearTouch.None, rawMask, controllerMask);
	}

	private static bool GetResolvedNearTouch(NearTouch virtualMask, RawNearTouch rawMask, Controller controllerMask)
	{
		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				RawNearTouch resolvedMask = rawMask | controller.ResolveToRawMask(virtualMask);

				if (((RawNearTouch)controller.currentState.NearTouches & resolvedMask) != 0)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Gets the current down state of the given virtual near touch mask with the given controller mask.
	/// Returns true if any masked near touch was pressed this frame on any masked controller and no masked near touch was previously down last frame.
	/// </summary>
	public static bool GetDown(NearTouch virtualMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedNearTouchDown(virtualMask, RawNearTouch.None, controllerMask);
	}

	/// <summary>
	/// Gets the current down state of the given raw near touch mask with the given controller mask.
	/// Returns true if any masked near touch was pressed this frame on any masked controller and no masked near touch was previously down last frame.
	/// </summary>
	public static bool GetDown(RawNearTouch rawMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedNearTouchDown(NearTouch.None, rawMask, controllerMask);
	}

	private static bool GetResolvedNearTouchDown(NearTouch virtualMask, RawNearTouch rawMask, Controller controllerMask)
	{
		bool down = false;

		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				RawNearTouch resolvedMask = rawMask | controller.ResolveToRawMask(virtualMask);

				if (((RawNearTouch)controller.previousState.NearTouches & resolvedMask) != 0)
				{
					return false;
				}

				if ((((RawNearTouch)controller.currentState.NearTouches & resolvedMask) != 0)
					&& (((RawNearTouch)controller.previousState.NearTouches & resolvedMask) == 0))
				{
					down = true;
				}
			}
		}

		return down;
	}

	/// <summary>
	/// Gets the current up state of the given virtual near touch mask with the given controller mask.
	/// Returns true if any masked near touch was released this frame on any masked controller and no other masked near touch is still down this frame.
	/// </summary>
	public static bool GetUp(NearTouch virtualMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedNearTouchUp(virtualMask, RawNearTouch.None, controllerMask);
	}

	/// <summary>
	/// Gets the current up state of the given raw near touch mask with the given controller mask.
	/// Returns true if any masked near touch was released this frame on any masked controller and no other masked near touch is still down this frame.
	/// </summary>
	public static bool GetUp(RawNearTouch rawMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedNearTouchUp(NearTouch.None, rawMask, controllerMask);
	}

	private static bool GetResolvedNearTouchUp(NearTouch virtualMask, RawNearTouch rawMask, Controller controllerMask)
	{
		bool up = false;

		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				RawNearTouch resolvedMask = rawMask | controller.ResolveToRawMask(virtualMask);

				if (((RawNearTouch)controller.currentState.NearTouches & resolvedMask) != 0)
				{
					return false;
				}

				if ((((RawNearTouch)controller.currentState.NearTouches & resolvedMask) == 0)
					&& (((RawNearTouch)controller.previousState.NearTouches & resolvedMask) != 0))
				{
					up = true;
				}
			}
		}

		return up;
	}

	/// <summary>
	/// Gets the current state of the given virtual 1-dimensional axis mask on the given controller mask.
	/// Returns the value of the largest masked axis across all masked controllers. Values range from 0 to 1.
	/// </summary>
	public static float Get(Axis1D virtualMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedAxis1D(virtualMask, RawAxis1D.None, controllerMask);
	}

	/// <summary>
	/// Gets the current state of the given raw 1-dimensional axis mask on the given controller mask.
	/// Returns the value of the largest masked axis across all masked controllers. Values range from 0 to 1.
	/// </summary>
	public static float Get(RawAxis1D rawMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedAxis1D(Axis1D.None, rawMask, controllerMask);
	}

	private static float GetResolvedAxis1D(Axis1D virtualMask, RawAxis1D rawMask, Controller controllerMask)
	{
		float maxAxis = 0.0f;

		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				RawAxis1D resolvedMask = rawMask | controller.ResolveToRawMask(virtualMask);

				if ((RawAxis1D.LIndexTrigger & resolvedMask) != 0)
				{
					maxAxis = CalculateAbsMax(maxAxis, controller.currentState.LIndexTrigger);
				}
				if ((RawAxis1D.RIndexTrigger & resolvedMask) != 0)
				{
					maxAxis = CalculateAbsMax(maxAxis, controller.currentState.RIndexTrigger);
				}
				if ((RawAxis1D.LHandTrigger & resolvedMask) != 0)
				{
					maxAxis = CalculateAbsMax(maxAxis, controller.currentState.LHandTrigger);
				}
				if ((RawAxis1D.RHandTrigger & resolvedMask) != 0)
				{
					maxAxis = CalculateAbsMax(maxAxis, controller.currentState.RHandTrigger);
				}
			}
		}

		maxAxis = CalculateDeadzone(maxAxis, AXIS_DEADZONE_THRESHOLD);

		return maxAxis;
	}

	/// <summary>
	/// Gets the current state of the given virtual 2-dimensional axis mask on the given controller mask.
	/// Returns the vector of the largest masked axis across all masked controllers. Values range from -1 to 1.
	/// </summary>
	public static Vector2 Get(Axis2D virtualMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedAxis2D(virtualMask, RawAxis2D.None, controllerMask);
	}

	/// <summary>
	/// Gets the current state of the given raw 2-dimensional axis mask on the given controller mask.
	/// Returns the vector of the largest masked axis across all masked controllers. Values range from -1 to 1.
	/// </summary>
	public static Vector2 Get(RawAxis2D rawMask, Controller controllerMask = Controller.Active)
	{
		return GetResolvedAxis2D(Axis2D.None, rawMask, controllerMask);
	}

	private static Vector2 GetResolvedAxis2D(Axis2D virtualMask, RawAxis2D rawMask, Controller controllerMask)
	{
		Vector2 maxAxis = Vector2.zero;

		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				RawAxis2D resolvedMask = rawMask | controller.ResolveToRawMask(virtualMask);

				if ((RawAxis2D.LThumbstick & resolvedMask) != 0)
				{
					Vector2 axis = new Vector2(
						controller.currentState.LThumbstick.x,
						controller.currentState.LThumbstick.y);

					maxAxis = CalculateAbsMax(maxAxis, axis);
				}
				if ((RawAxis2D.RThumbstick & resolvedMask) != 0)
				{
					Vector2 axis = new Vector2(
						controller.currentState.RThumbstick.x,
						controller.currentState.RThumbstick.y);

					maxAxis = CalculateAbsMax(maxAxis, axis);
				}
			}
		}

		maxAxis = CalculateDeadzone(maxAxis, AXIS_DEADZONE_THRESHOLD);

		return maxAxis;
	}

	/// <summary>
	/// Returns a mask of all currently connected controller types.
	/// </summary>
	public static Controller GetConnectedControllers()
	{
		return connectedControllerTypes;
	}

	/// <summary>
	/// Returns the current active controller type.
	/// </summary>
	public static Controller GetActiveController()
	{
		return activeControllerType;
	}

	/// <summary>
	/// Activates vibration with the given frequency and amplitude with the given controller mask.
	/// Ignored on controllers that do not support vibration. Expected values range from 0 to 1.
	/// </summary>
	public static void SetControllerVibration(float frequency, float amplitude, Controller controllerMask = Controller.Active)
	{
		if ((controllerMask & Controller.Active) != 0)
			controllerMask |= activeControllerType;

		for (int i = 0; i < controllers.Count; i++)
		{
			OVRControllerBase controller = controllers[i];

			if (ShouldResolveController(controller.controllerType, controllerMask))
			{
				controller.SetControllerVibration(frequency, amplitude);
			}
		}
	}

	private static Vector2 CalculateAbsMax(Vector2 a, Vector2 b)
	{
		float absA = a.sqrMagnitude;
		float absB = b.sqrMagnitude;

		if (absA >= absB)
			return a;
		return b;
	}

	private static float CalculateAbsMax(float a, float b)
	{
		float absA = (a >= 0) ? a : -a;
		float absB = (b >= 0) ? b : -b;

		if (absA >= absB)
			return a;
		return b;
	}

	private static Vector2 CalculateDeadzone(Vector2 a, float deadzone)
	{
		if (a.sqrMagnitude <= (deadzone * deadzone))
			return Vector2.zero;

		a *= ((a.magnitude - deadzone) / (1.0f - deadzone));

		if (a.sqrMagnitude > 1.0f)
			return a.normalized;
		return a;
	}

	private static float CalculateDeadzone(float a, float deadzone)
	{
		float mag = (a >= 0) ? a : -a;

		if (mag <= deadzone)
			return 0.0f;

		a *= (mag - deadzone) / (1.0f - deadzone);

		if ((a * a) > 1.0f)
			return (a >= 0) ? 1.0f : -1.0f;
		return a;
	}

	private static bool ShouldResolveController(Controller controllerType, Controller controllerMask)
	{
		bool isValid = false;

		if ((controllerType & controllerMask) == controllerType)
		{
			isValid = true;
		}

		// If the mask requests both Touch controllers, reject the individual touch controllers.
		if (((controllerMask & Controller.Touch) == Controller.Touch)
			&& ((controllerType & Controller.Touch) != 0)
			&& ((controllerType & Controller.Touch) != Controller.Touch))
		{
			isValid = false;
		}

		return isValid;
	}

	private abstract class OVRControllerBase
	{
		public class VirtualButtonMap
		{
			public RawButton None                     = RawButton.None;
			public RawButton One                      = RawButton.None;
			public RawButton Two                      = RawButton.None;
			public RawButton Three                    = RawButton.None;
			public RawButton Four                     = RawButton.None;
			public RawButton Start                    = RawButton.None;
			public RawButton Back                     = RawButton.None;
			public RawButton PrimaryShoulder          = RawButton.None;
			public RawButton PrimaryIndexTrigger      = RawButton.None;
			public RawButton PrimaryHandTrigger       = RawButton.None;
			public RawButton PrimaryThumbstick        = RawButton.None;
			public RawButton PrimaryThumbstickUp      = RawButton.None;
			public RawButton PrimaryThumbstickDown    = RawButton.None;
			public RawButton PrimaryThumbstickLeft    = RawButton.None;
			public RawButton PrimaryThumbstickRight   = RawButton.None;
			public RawButton SecondaryShoulder        = RawButton.None;
			public RawButton SecondaryIndexTrigger    = RawButton.None;
			public RawButton SecondaryHandTrigger     = RawButton.None;
			public RawButton SecondaryThumbstick      = RawButton.None;
			public RawButton SecondaryThumbstickUp    = RawButton.None;
			public RawButton SecondaryThumbstickDown  = RawButton.None;
			public RawButton SecondaryThumbstickLeft  = RawButton.None;
			public RawButton SecondaryThumbstickRight = RawButton.None;
			public RawButton DpadUp                   = RawButton.None;
			public RawButton DpadDown                 = RawButton.None;
			public RawButton DpadLeft                 = RawButton.None;
			public RawButton DpadRight                = RawButton.None;
			public RawButton Up                       = RawButton.None;
			public RawButton Down                     = RawButton.None;
			public RawButton Left                     = RawButton.None;
			public RawButton Right                    = RawButton.None;

			public RawButton ToRawMask(Button virtualMask)
			{
				RawButton rawMask = 0;

				if (virtualMask == Button.None)
					return RawButton.None;

				if ((virtualMask & Button.One) != 0)
					rawMask |= One;
				if ((virtualMask & Button.Two) != 0)
					rawMask |= Two;
				if ((virtualMask & Button.Three) != 0)
					rawMask |= Three;
				if ((virtualMask & Button.Four) != 0)
					rawMask |= Four;
				if ((virtualMask & Button.Start) != 0)
					rawMask |= Start;
				if ((virtualMask & Button.Back) != 0)
					rawMask |= Back;
				if ((virtualMask & Button.PrimaryShoulder) != 0)
					rawMask |= PrimaryShoulder;
				if ((virtualMask & Button.PrimaryIndexTrigger) != 0)
					rawMask |= PrimaryIndexTrigger;
				if ((virtualMask & Button.PrimaryHandTrigger) != 0)
					rawMask |= PrimaryHandTrigger;
				if ((virtualMask & Button.PrimaryThumbstick) != 0)
					rawMask |= PrimaryThumbstick;
				if ((virtualMask & Button.PrimaryThumbstickUp) != 0)
					rawMask |= PrimaryThumbstickUp;
				if ((virtualMask & Button.PrimaryThumbstickDown) != 0)
					rawMask |= PrimaryThumbstickDown;
				if ((virtualMask & Button.PrimaryThumbstickLeft) != 0)
					rawMask |= PrimaryThumbstickLeft;
				if ((virtualMask & Button.PrimaryThumbstickRight) != 0)
					rawMask |= PrimaryThumbstickRight;
				if ((virtualMask & Button.SecondaryShoulder) != 0)
					rawMask |= SecondaryShoulder;
				if ((virtualMask & Button.SecondaryIndexTrigger) != 0)
					rawMask |= SecondaryIndexTrigger;
				if ((virtualMask & Button.SecondaryHandTrigger) != 0)
					rawMask |= SecondaryHandTrigger;
				if ((virtualMask & Button.SecondaryThumbstick) != 0)
					rawMask |= SecondaryThumbstick;
				if ((virtualMask & Button.SecondaryThumbstickUp) != 0)
					rawMask |= SecondaryThumbstickUp;
				if ((virtualMask & Button.SecondaryThumbstickDown) != 0)
					rawMask |= SecondaryThumbstickDown;
				if ((virtualMask & Button.SecondaryThumbstickLeft) != 0)
					rawMask |= SecondaryThumbstickLeft;
				if ((virtualMask & Button.SecondaryThumbstickRight) != 0)
					rawMask |= SecondaryThumbstickRight;
				if ((virtualMask & Button.DpadUp) != 0)
					rawMask |= DpadUp;
				if ((virtualMask & Button.DpadDown) != 0)
					rawMask |= DpadDown;
				if ((virtualMask & Button.DpadLeft) != 0)
					rawMask |= DpadLeft;
				if ((virtualMask & Button.DpadRight) != 0)
					rawMask |= DpadRight;
				if ((virtualMask & Button.Up) != 0)
					rawMask |= Up;
				if ((virtualMask & Button.Down) != 0)
					rawMask |= Down;
				if ((virtualMask & Button.Left) != 0)
					rawMask |= Left;
				if ((virtualMask & Button.Right) != 0)
					rawMask |= Right;

				return rawMask;
			}
		}

		public class VirtualTouchMap
		{
			public RawTouch None                      = RawTouch.None;
			public RawTouch One                       = RawTouch.None;
			public RawTouch Two                       = RawTouch.None;
			public RawTouch Three                     = RawTouch.None;
			public RawTouch Four                      = RawTouch.None;
			public RawTouch PrimaryIndexTrigger       = RawTouch.None;
			public RawTouch PrimaryThumbstick         = RawTouch.None;
			public RawTouch PrimaryThumbRest          = RawTouch.None;
			public RawTouch SecondaryIndexTrigger     = RawTouch.None;
			public RawTouch SecondaryThumbstick       = RawTouch.None;
			public RawTouch SecondaryThumbRest        = RawTouch.None;

			public RawTouch ToRawMask(Touch virtualMask)
			{
				RawTouch rawMask = 0;

				if (virtualMask == Touch.None)
					return RawTouch.None;

				if ((virtualMask & Touch.One) != 0)
					rawMask |= One;
				if ((virtualMask & Touch.Two) != 0)
					rawMask |= Two;
				if ((virtualMask & Touch.Three) != 0)
					rawMask |= Three;
				if ((virtualMask & Touch.Four) != 0)
					rawMask |= Four;
				if ((virtualMask & Touch.PrimaryIndexTrigger) != 0)
					rawMask |= PrimaryIndexTrigger;
				if ((virtualMask & Touch.PrimaryThumbstick) != 0)
					rawMask |= PrimaryThumbstick;
				if ((virtualMask & Touch.PrimaryThumbRest) != 0)
					rawMask |= PrimaryThumbRest;
				if ((virtualMask & Touch.SecondaryIndexTrigger) != 0)
					rawMask |= SecondaryIndexTrigger;
				if ((virtualMask & Touch.SecondaryThumbstick) != 0)
					rawMask |= SecondaryThumbstick;
				if ((virtualMask & Touch.SecondaryThumbRest) != 0)
					rawMask |= SecondaryThumbRest;

				return rawMask;
			}
		}

		public class VirtualNearTouchMap
		{
			public RawNearTouch None                      = RawNearTouch.None;
			public RawNearTouch PrimaryIndexTrigger       = RawNearTouch.None;
			public RawNearTouch PrimaryThumbButtons       = RawNearTouch.None;
			public RawNearTouch SecondaryIndexTrigger     = RawNearTouch.None;
			public RawNearTouch SecondaryThumbButtons     = RawNearTouch.None;

			public RawNearTouch ToRawMask(NearTouch virtualMask)
			{
				RawNearTouch rawMask = 0;

				if (virtualMask == NearTouch.None)
					return RawNearTouch.None;

				if ((virtualMask & NearTouch.PrimaryIndexTrigger) != 0)
					rawMask |= PrimaryIndexTrigger;
				if ((virtualMask & NearTouch.PrimaryThumbButtons) != 0)
					rawMask |= PrimaryThumbButtons;
				if ((virtualMask & NearTouch.SecondaryIndexTrigger) != 0)
					rawMask |= SecondaryIndexTrigger;
				if ((virtualMask & NearTouch.SecondaryThumbButtons) != 0)
					rawMask |= SecondaryThumbButtons;

				return rawMask;
			}
		}

		public class VirtualAxis1DMap
		{
			public RawAxis1D None                      = RawAxis1D.None;
			public RawAxis1D PrimaryIndexTrigger       = RawAxis1D.None;
			public RawAxis1D PrimaryHandTrigger        = RawAxis1D.None;
			public RawAxis1D SecondaryIndexTrigger     = RawAxis1D.None;
			public RawAxis1D SecondaryHandTrigger      = RawAxis1D.None;

			public RawAxis1D ToRawMask(Axis1D virtualMask)
			{
				RawAxis1D rawMask = 0;

				if (virtualMask == Axis1D.None)
					return RawAxis1D.None;

				if ((virtualMask & Axis1D.PrimaryIndexTrigger) != 0)
					rawMask |= PrimaryIndexTrigger;
				if ((virtualMask & Axis1D.PrimaryHandTrigger) != 0)
					rawMask |= PrimaryHandTrigger;
				if ((virtualMask & Axis1D.SecondaryIndexTrigger) != 0)
					rawMask |= SecondaryIndexTrigger;
				if ((virtualMask & Axis1D.SecondaryHandTrigger) != 0)
					rawMask |= SecondaryHandTrigger;

				return rawMask;
			}
		}

		public class VirtualAxis2DMap
		{
			public RawAxis2D None                      = RawAxis2D.None;
			public RawAxis2D PrimaryThumbstick         = RawAxis2D.None;
			public RawAxis2D SecondaryThumbstick       = RawAxis2D.None;

			public RawAxis2D ToRawMask(Axis2D virtualMask)
			{
				RawAxis2D rawMask = 0;

				if (virtualMask == Axis2D.None)
					return RawAxis2D.None;

				if ((virtualMask & Axis2D.PrimaryThumbstick) != 0)
					rawMask |= PrimaryThumbstick;
				if ((virtualMask & Axis2D.SecondaryThumbstick) != 0)
					rawMask |= SecondaryThumbstick;

				return rawMask;
			}
		}

		public Controller controllerType = Controller.None;
		public VirtualButtonMap buttonMap = new VirtualButtonMap();
		public VirtualTouchMap touchMap = new VirtualTouchMap();
		public VirtualNearTouchMap nearTouchMap = new VirtualNearTouchMap();
		public VirtualAxis1DMap axis1DMap = new VirtualAxis1DMap();
		public VirtualAxis2DMap axis2DMap = new VirtualAxis2DMap();
		public OVRPlugin.ControllerState previousState = new OVRPlugin.ControllerState();
		public OVRPlugin.ControllerState currentState = new OVRPlugin.ControllerState();

		public OVRControllerBase()
		{
			ConfigureButtonMap();
			ConfigureTouchMap();
			ConfigureNearTouchMap();
			ConfigureAxis1DMap();
			ConfigureAxis2DMap();
		}

		public virtual Controller Update()
		{
			OVRPlugin.ControllerState state = OVRPlugin.GetControllerState((uint)controllerType);

			if (state.LIndexTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LIndexTrigger;
			if (state.LHandTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LHandTrigger;
			if (state.LThumbstick.y >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickUp;
			if (state.LThumbstick.y <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickDown;
			if (state.LThumbstick.x <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickLeft;
			if (state.LThumbstick.x >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickRight;

			if (state.RIndexTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RIndexTrigger;
			if (state.RHandTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RHandTrigger;
			if (state.RThumbstick.y >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickUp;
			if (state.RThumbstick.y <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickDown;
			if (state.RThumbstick.x <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickLeft;
			if (state.RThumbstick.x >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickRight;

			previousState = currentState;
			currentState = state;

			return ((Controller)currentState.ConnectedControllers & controllerType);
		}

		public virtual void SetControllerVibration(float frequency, float amplitude)
		{
			OVRPlugin.SetControllerVibration((uint)controllerType, frequency, amplitude);
		}

		public abstract void ConfigureButtonMap();
		public abstract void ConfigureTouchMap();
		public abstract void ConfigureNearTouchMap();
		public abstract void ConfigureAxis1DMap();
		public abstract void ConfigureAxis2DMap();

		public RawButton ResolveToRawMask(Button virtualMask)
		{
			return buttonMap.ToRawMask(virtualMask);
		}

		public RawTouch ResolveToRawMask(Touch virtualMask)
		{
			return touchMap.ToRawMask(virtualMask);
		}

		public RawNearTouch ResolveToRawMask(NearTouch virtualMask)
		{
			return nearTouchMap.ToRawMask(virtualMask);
		}

		public RawAxis1D ResolveToRawMask(Axis1D virtualMask)
		{
			return axis1DMap.ToRawMask(virtualMask);
		}

		public RawAxis2D ResolveToRawMask(Axis2D virtualMask)
		{
			return axis2DMap.ToRawMask(virtualMask);
		}
	}

	private class OVRControllerTouch : OVRControllerBase
	{
		public OVRControllerTouch()
		{
			controllerType = Controller.Touch;
		}

		public override void ConfigureButtonMap()
		{
			buttonMap.None                     = RawButton.None;
			buttonMap.One                      = RawButton.A;
			buttonMap.Two                      = RawButton.B;
			buttonMap.Three                    = RawButton.X;
			buttonMap.Four                     = RawButton.Y;
			buttonMap.Start                    = RawButton.None;
			buttonMap.Back                     = RawButton.None;
			buttonMap.PrimaryShoulder          = RawButton.None;
			buttonMap.PrimaryIndexTrigger      = RawButton.LIndexTrigger;
			buttonMap.PrimaryHandTrigger       = RawButton.LHandTrigger;
			buttonMap.PrimaryThumbstick        = RawButton.LThumbstick;
			buttonMap.PrimaryThumbstickUp      = RawButton.LThumbstickUp;
			buttonMap.PrimaryThumbstickDown    = RawButton.LThumbstickDown;
			buttonMap.PrimaryThumbstickLeft    = RawButton.LThumbstickLeft;
			buttonMap.PrimaryThumbstickRight   = RawButton.LThumbstickRight;
			buttonMap.SecondaryShoulder        = RawButton.None;
			buttonMap.SecondaryIndexTrigger    = RawButton.RIndexTrigger;
			buttonMap.SecondaryHandTrigger     = RawButton.RHandTrigger;
			buttonMap.SecondaryThumbstick      = RawButton.RThumbstick;
			buttonMap.SecondaryThumbstickUp    = RawButton.RThumbstickUp;
			buttonMap.SecondaryThumbstickDown  = RawButton.RThumbstickDown;
			buttonMap.SecondaryThumbstickLeft  = RawButton.RThumbstickLeft;
			buttonMap.SecondaryThumbstickRight = RawButton.RThumbstickRight;
			buttonMap.DpadUp                   = RawButton.None;
			buttonMap.DpadDown                 = RawButton.None;
			buttonMap.DpadLeft                 = RawButton.None;
			buttonMap.DpadRight                = RawButton.None;
			buttonMap.Up                       = RawButton.LThumbstickUp;
			buttonMap.Down                     = RawButton.LThumbstickDown;
			buttonMap.Left                     = RawButton.LThumbstickLeft;
			buttonMap.Right                    = RawButton.LThumbstickRight;
		}

		public override void ConfigureTouchMap()
		{
			touchMap.None                      = RawTouch.None;
			touchMap.One                       = RawTouch.A;
			touchMap.Two                       = RawTouch.B;
			touchMap.Three                     = RawTouch.X;
			touchMap.Four                      = RawTouch.Y;
			touchMap.PrimaryIndexTrigger       = RawTouch.LIndexTrigger;
			touchMap.PrimaryThumbstick         = RawTouch.LThumbstick;
			touchMap.PrimaryThumbRest          = RawTouch.LThumbRest;
			touchMap.SecondaryIndexTrigger     = RawTouch.RIndexTrigger;
			touchMap.SecondaryThumbstick       = RawTouch.RThumbstick;
			touchMap.SecondaryThumbRest        = RawTouch.RThumbRest;
		}

		public override void ConfigureNearTouchMap()
		{
			nearTouchMap.None                      = RawNearTouch.None;
			nearTouchMap.PrimaryIndexTrigger       = RawNearTouch.LIndexTrigger;
			nearTouchMap.PrimaryThumbButtons       = RawNearTouch.LThumbButtons;
			nearTouchMap.SecondaryIndexTrigger     = RawNearTouch.RIndexTrigger;
			nearTouchMap.SecondaryThumbButtons     = RawNearTouch.RThumbButtons;
		}

		public override void ConfigureAxis1DMap()
		{
			axis1DMap.None                      = RawAxis1D.None;
			axis1DMap.PrimaryIndexTrigger       = RawAxis1D.LIndexTrigger;
			axis1DMap.PrimaryHandTrigger        = RawAxis1D.LHandTrigger;
			axis1DMap.SecondaryIndexTrigger     = RawAxis1D.RIndexTrigger;
			axis1DMap.SecondaryHandTrigger      = RawAxis1D.RHandTrigger;
		}

		public override void ConfigureAxis2DMap()
		{
			axis2DMap.None                      = RawAxis2D.None;
			axis2DMap.PrimaryThumbstick         = RawAxis2D.LThumbstick;
			axis2DMap.SecondaryThumbstick       = RawAxis2D.RThumbstick;
		}
	}

	private class OVRControllerLTouch : OVRControllerBase
	{
		public OVRControllerLTouch()
		{
			controllerType = Controller.LTouch;
		}

		public override void ConfigureButtonMap()
		{
			buttonMap.None                     = RawButton.None;
			buttonMap.One                      = RawButton.X;
			buttonMap.Two                      = RawButton.Y;
			buttonMap.Three                    = RawButton.None;
			buttonMap.Four                     = RawButton.None;
			buttonMap.Start                    = RawButton.None;
			buttonMap.Back                     = RawButton.None;
			buttonMap.PrimaryShoulder          = RawButton.None;
			buttonMap.PrimaryIndexTrigger      = RawButton.LIndexTrigger;
			buttonMap.PrimaryHandTrigger       = RawButton.LHandTrigger;
			buttonMap.PrimaryThumbstick        = RawButton.LThumbstick;
			buttonMap.PrimaryThumbstickUp      = RawButton.LThumbstickUp;
			buttonMap.PrimaryThumbstickDown    = RawButton.LThumbstickDown;
			buttonMap.PrimaryThumbstickLeft    = RawButton.LThumbstickLeft;
			buttonMap.PrimaryThumbstickRight   = RawButton.LThumbstickRight;
			buttonMap.SecondaryShoulder        = RawButton.None;
			buttonMap.SecondaryIndexTrigger    = RawButton.None;
			buttonMap.SecondaryHandTrigger     = RawButton.None;
			buttonMap.SecondaryThumbstick      = RawButton.None;
			buttonMap.SecondaryThumbstickUp    = RawButton.None;
			buttonMap.SecondaryThumbstickDown  = RawButton.None;
			buttonMap.SecondaryThumbstickLeft  = RawButton.None;
			buttonMap.SecondaryThumbstickRight = RawButton.None;
			buttonMap.DpadUp                   = RawButton.None;
			buttonMap.DpadDown                 = RawButton.None;
			buttonMap.DpadLeft                 = RawButton.None;
			buttonMap.DpadRight                = RawButton.None;
			buttonMap.Up                       = RawButton.LThumbstickUp;
			buttonMap.Down                     = RawButton.LThumbstickDown;
			buttonMap.Left                     = RawButton.LThumbstickLeft;
			buttonMap.Right                    = RawButton.LThumbstickRight;
		}

		public override void ConfigureTouchMap()
		{
			touchMap.None                      = RawTouch.None;
			touchMap.One                       = RawTouch.X;
			touchMap.Two                       = RawTouch.Y;
			touchMap.Three                     = RawTouch.None;
			touchMap.Four                      = RawTouch.None;
			touchMap.PrimaryIndexTrigger       = RawTouch.LIndexTrigger;
			touchMap.PrimaryThumbstick         = RawTouch.LThumbstick;
			touchMap.PrimaryThumbRest          = RawTouch.LThumbRest;
			touchMap.SecondaryIndexTrigger     = RawTouch.None;
			touchMap.SecondaryThumbstick       = RawTouch.None;
			touchMap.SecondaryThumbRest        = RawTouch.None;
		}

		public override void ConfigureNearTouchMap()
		{
			nearTouchMap.None                      = RawNearTouch.None;
			nearTouchMap.PrimaryIndexTrigger       = RawNearTouch.LIndexTrigger;
			nearTouchMap.PrimaryThumbButtons       = RawNearTouch.LThumbButtons;
			nearTouchMap.SecondaryIndexTrigger     = RawNearTouch.None;
			nearTouchMap.SecondaryThumbButtons     = RawNearTouch.None;
		}

		public override void ConfigureAxis1DMap()
		{
			axis1DMap.None                      = RawAxis1D.None;
			axis1DMap.PrimaryIndexTrigger       = RawAxis1D.LIndexTrigger;
			axis1DMap.PrimaryHandTrigger        = RawAxis1D.LHandTrigger;
			axis1DMap.SecondaryIndexTrigger     = RawAxis1D.None;
			axis1DMap.SecondaryHandTrigger      = RawAxis1D.None;
		}

		public override void ConfigureAxis2DMap()
		{
			axis2DMap.None                      = RawAxis2D.None;
			axis2DMap.PrimaryThumbstick         = RawAxis2D.LThumbstick;
			axis2DMap.SecondaryThumbstick       = RawAxis2D.None;
		}
	}

	private class OVRControllerRTouch : OVRControllerBase
	{
		public OVRControllerRTouch()
		{
			controllerType = Controller.RTouch;
		}

		public override void ConfigureButtonMap()
		{
			buttonMap.None                     = RawButton.None;
			buttonMap.One                      = RawButton.A;
			buttonMap.Two                      = RawButton.B;
			buttonMap.Three                    = RawButton.None;
			buttonMap.Four                     = RawButton.None;
			buttonMap.Start                    = RawButton.None;
			buttonMap.Back                     = RawButton.None;
			buttonMap.PrimaryShoulder          = RawButton.None;
			buttonMap.PrimaryIndexTrigger      = RawButton.RIndexTrigger;
			buttonMap.PrimaryHandTrigger       = RawButton.RHandTrigger;
			buttonMap.PrimaryThumbstick        = RawButton.RThumbstick;
			buttonMap.PrimaryThumbstickUp      = RawButton.RThumbstickUp;
			buttonMap.PrimaryThumbstickDown    = RawButton.RThumbstickDown;
			buttonMap.PrimaryThumbstickLeft    = RawButton.RThumbstickLeft;
			buttonMap.PrimaryThumbstickRight   = RawButton.RThumbstickRight;
			buttonMap.SecondaryShoulder        = RawButton.None;
			buttonMap.SecondaryIndexTrigger    = RawButton.None;
			buttonMap.SecondaryHandTrigger     = RawButton.None;
			buttonMap.SecondaryThumbstick      = RawButton.None;
			buttonMap.SecondaryThumbstickUp    = RawButton.None;
			buttonMap.SecondaryThumbstickDown  = RawButton.None;
			buttonMap.SecondaryThumbstickLeft  = RawButton.None;
			buttonMap.SecondaryThumbstickRight = RawButton.None;
			buttonMap.DpadUp                   = RawButton.None;
			buttonMap.DpadDown                 = RawButton.None;
			buttonMap.DpadLeft                 = RawButton.None;
			buttonMap.DpadRight                = RawButton.None;
			buttonMap.Up                       = RawButton.RThumbstickUp;
			buttonMap.Down                     = RawButton.RThumbstickDown;
			buttonMap.Left                     = RawButton.RThumbstickLeft;
			buttonMap.Right                    = RawButton.RThumbstickRight;
		}

		public override void ConfigureTouchMap()
		{
			touchMap.None                      = RawTouch.None;
			touchMap.One                       = RawTouch.A;
			touchMap.Two                       = RawTouch.B;
			touchMap.Three                     = RawTouch.None;
			touchMap.Four                      = RawTouch.None;
			touchMap.PrimaryIndexTrigger       = RawTouch.RIndexTrigger;
			touchMap.PrimaryThumbstick         = RawTouch.RThumbstick;
			touchMap.PrimaryThumbRest          = RawTouch.RThumbRest;
			touchMap.SecondaryIndexTrigger     = RawTouch.None;
			touchMap.SecondaryThumbstick       = RawTouch.None;
			touchMap.SecondaryThumbRest        = RawTouch.None;
		}

		public override void ConfigureNearTouchMap()
		{
			nearTouchMap.None                      = RawNearTouch.None;
			nearTouchMap.PrimaryIndexTrigger       = RawNearTouch.RIndexTrigger;
			nearTouchMap.PrimaryThumbButtons       = RawNearTouch.RThumbButtons;
			nearTouchMap.SecondaryIndexTrigger     = RawNearTouch.None;
			nearTouchMap.SecondaryThumbButtons     = RawNearTouch.None;
		}

		public override void ConfigureAxis1DMap()
		{
			axis1DMap.None                      = RawAxis1D.None;
			axis1DMap.PrimaryIndexTrigger       = RawAxis1D.RIndexTrigger;
			axis1DMap.PrimaryHandTrigger        = RawAxis1D.RHandTrigger;
			axis1DMap.SecondaryIndexTrigger     = RawAxis1D.None;
			axis1DMap.SecondaryHandTrigger      = RawAxis1D.None;
		}

		public override void ConfigureAxis2DMap()
		{
			axis2DMap.None                      = RawAxis2D.None;
			axis2DMap.PrimaryThumbstick         = RawAxis2D.RThumbstick;
			axis2DMap.SecondaryThumbstick       = RawAxis2D.None;
		}
	}

	private class OVRControllerRemote : OVRControllerBase
	{
		public OVRControllerRemote()
		{
			controllerType = Controller.Remote;
		}

		public override void ConfigureButtonMap()
		{
			buttonMap.None                     = RawButton.None;
			buttonMap.One                      = RawButton.Start;
			buttonMap.Two                      = RawButton.Back;
			buttonMap.Three                    = RawButton.None;
			buttonMap.Four                     = RawButton.None;
			buttonMap.Start                    = RawButton.Start;
			buttonMap.Back                     = RawButton.Back;
			buttonMap.PrimaryShoulder          = RawButton.None;
			buttonMap.PrimaryIndexTrigger      = RawButton.None;
			buttonMap.PrimaryHandTrigger       = RawButton.None;
			buttonMap.PrimaryThumbstick        = RawButton.None;
			buttonMap.PrimaryThumbstickUp      = RawButton.None;
			buttonMap.PrimaryThumbstickDown    = RawButton.None;
			buttonMap.PrimaryThumbstickLeft    = RawButton.None;
			buttonMap.PrimaryThumbstickRight   = RawButton.None;
			buttonMap.SecondaryShoulder        = RawButton.None;
			buttonMap.SecondaryIndexTrigger    = RawButton.None;
			buttonMap.SecondaryHandTrigger     = RawButton.None;
			buttonMap.SecondaryThumbstick      = RawButton.None;
			buttonMap.SecondaryThumbstickUp    = RawButton.None;
			buttonMap.SecondaryThumbstickDown  = RawButton.None;
			buttonMap.SecondaryThumbstickLeft  = RawButton.None;
			buttonMap.SecondaryThumbstickRight = RawButton.None;
			buttonMap.DpadUp                   = RawButton.DpadUp;
			buttonMap.DpadDown                 = RawButton.DpadDown;
			buttonMap.DpadLeft                 = RawButton.DpadLeft;
			buttonMap.DpadRight                = RawButton.DpadRight;
			buttonMap.Up                       = RawButton.DpadUp;
			buttonMap.Down                     = RawButton.DpadDown;
			buttonMap.Left                     = RawButton.DpadLeft;
			buttonMap.Right                    = RawButton.DpadRight;
		}

		public override void ConfigureTouchMap()
		{
			touchMap.None                      = RawTouch.None;
			touchMap.One                       = RawTouch.None;
			touchMap.Two                       = RawTouch.None;
			touchMap.Three                     = RawTouch.None;
			touchMap.Four                      = RawTouch.None;
			touchMap.PrimaryIndexTrigger       = RawTouch.None;
			touchMap.PrimaryThumbstick         = RawTouch.None;
			touchMap.PrimaryThumbRest          = RawTouch.None;
			touchMap.SecondaryIndexTrigger     = RawTouch.None;
			touchMap.SecondaryThumbstick       = RawTouch.None;
			touchMap.SecondaryThumbRest        = RawTouch.None;
		}

		public override void ConfigureNearTouchMap()
		{
			nearTouchMap.None                  = RawNearTouch.None;
			nearTouchMap.PrimaryIndexTrigger   = RawNearTouch.None;
			nearTouchMap.PrimaryThumbButtons   = RawNearTouch.None;
			nearTouchMap.SecondaryIndexTrigger = RawNearTouch.None;
			nearTouchMap.SecondaryThumbButtons = RawNearTouch.None;
		}

		public override void ConfigureAxis1DMap()
		{
			axis1DMap.None                     = RawAxis1D.None;
			axis1DMap.PrimaryIndexTrigger      = RawAxis1D.None;
			axis1DMap.PrimaryHandTrigger       = RawAxis1D.None;
			axis1DMap.SecondaryIndexTrigger    = RawAxis1D.None;
			axis1DMap.SecondaryHandTrigger     = RawAxis1D.None;
		}

		public override void ConfigureAxis2DMap()
		{
			axis2DMap.None                     = RawAxis2D.None;
			axis2DMap.PrimaryThumbstick        = RawAxis2D.None;
			axis2DMap.SecondaryThumbstick      = RawAxis2D.None;
		}
	}

	private class OVRControllerGamepadDesktop : OVRControllerBase
	{
		/// <summary> An axis on the gamepad. </summary>
		private enum AxisGPC
		{
			None = -1,
			LeftXAxis = 0,
			LeftYAxis,
			RightXAxis,
			RightYAxis,
			LeftTrigger,
			RightTrigger,
			DPad_X_Axis,
			DPad_Y_Axis,
			Max,
		};

		/// <summary> A button on the gamepad. </summary>
		public enum ButtonGPC
		{
			None = -1,
			A = 0,
			B,
			X,
			Y,
			Up,
			Down,
			Left,
			Right,
			Start,
			Back,
			LStick,
			RStick,
			LeftShoulder,
			RightShoulder,
			Max
		};

		private bool initialized = false;
		private bool joystickDetected = false;
		private float joystickCheckInterval = 1.0f;
		private float joystickCheckTime = 0.0f;

		public OVRControllerGamepadDesktop()
		{
			controllerType = Controller.Gamepad;

			initialized = OVR_GamepadController_Initialize();
		}

		~OVRControllerGamepadDesktop()
		{
			if (!initialized)
				return;

			OVR_GamepadController_Destroy();
		}

		private bool ShouldUpdate()
		{
			// XInput is notoriously slow to update if no Xbox controllers are present. (up to ~0.5 ms)
			// Use Unity's joystick detection as a quick way to short-circuit the need to query XInput.
			if ((Time.realtimeSinceStartup - joystickCheckTime) > joystickCheckInterval)
			{
				joystickCheckTime = Time.realtimeSinceStartup;
				joystickDetected = false;
				var joystickNames = UnityEngine.Input.GetJoystickNames();

				for (int i = 0; i < joystickNames.Length; i++)
				{
					if (joystickNames[i] != String.Empty)
					{
						joystickDetected = true;
						break;
					}
				}
			}

			return joystickDetected;
		}

		public override Controller Update()
		{
			if (!initialized || !ShouldUpdate())
			{
				return Controller.None;
			}

			OVRPlugin.ControllerState state = new OVRPlugin.ControllerState();

			bool result = OVR_GamepadController_Update();

			if (result)
				state.ConnectedControllers = (uint)Controller.Gamepad;

			if (OVR_GamepadController_GetButton((int)ButtonGPC.A))
				state.Buttons |= (uint)RawButton.A;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.B))
				state.Buttons |= (uint)RawButton.B;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.X))
				state.Buttons |= (uint)RawButton.X;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.Y))
				state.Buttons |= (uint)RawButton.Y;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.Up))
				state.Buttons |= (uint)RawButton.DpadUp;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.Down))
				state.Buttons |= (uint)RawButton.DpadDown;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.Left))
				state.Buttons |= (uint)RawButton.DpadLeft;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.Right))
				state.Buttons |= (uint)RawButton.DpadRight;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.Start))
				state.Buttons |= (uint)RawButton.Start;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.Back))
				state.Buttons |= (uint)RawButton.Back;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.LStick))
				state.Buttons |= (uint)RawButton.LThumbstick;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.RStick))
				state.Buttons |= (uint)RawButton.RThumbstick;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.LeftShoulder))
				state.Buttons |= (uint)RawButton.LShoulder;
			if (OVR_GamepadController_GetButton((int)ButtonGPC.RightShoulder))
				state.Buttons |= (uint)RawButton.RShoulder;

			state.LThumbstick.x = OVR_GamepadController_GetAxis((int)AxisGPC.LeftXAxis);
			state.LThumbstick.y = OVR_GamepadController_GetAxis((int)AxisGPC.LeftYAxis);
			state.RThumbstick.x = OVR_GamepadController_GetAxis((int)AxisGPC.RightXAxis);
			state.RThumbstick.y = OVR_GamepadController_GetAxis((int)AxisGPC.RightYAxis);
			state.LIndexTrigger = OVR_GamepadController_GetAxis((int)AxisGPC.LeftTrigger);
			state.RIndexTrigger = OVR_GamepadController_GetAxis((int)AxisGPC.RightTrigger);

			if (state.LIndexTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LIndexTrigger;
			if (state.LHandTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LHandTrigger;
			if (state.LThumbstick.y >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickUp;
			if (state.LThumbstick.y <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickDown;
			if (state.LThumbstick.x <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickLeft;
			if (state.LThumbstick.x >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickRight;

			if (state.RIndexTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RIndexTrigger;
			if (state.RHandTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RHandTrigger;
			if (state.RThumbstick.y >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickUp;
			if (state.RThumbstick.y <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickDown;
			if (state.RThumbstick.x <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickLeft;
			if (state.RThumbstick.x >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickRight;

			previousState = currentState;
			currentState = state;

			return ((Controller)currentState.ConnectedControllers & controllerType);
		}

		public override void ConfigureButtonMap()
		{
			buttonMap.None                     = RawButton.None;
			buttonMap.One                      = RawButton.A;
			buttonMap.Two                      = RawButton.B;
			buttonMap.Three                    = RawButton.X;
			buttonMap.Four                     = RawButton.Y;
			buttonMap.Start                    = RawButton.Start;
			buttonMap.Back                     = RawButton.Back;
			buttonMap.PrimaryShoulder          = RawButton.LShoulder;
			buttonMap.PrimaryIndexTrigger      = RawButton.LIndexTrigger;
			buttonMap.PrimaryHandTrigger       = RawButton.None;
			buttonMap.PrimaryThumbstick        = RawButton.LThumbstick;
			buttonMap.PrimaryThumbstickUp      = RawButton.LThumbstickUp;
			buttonMap.PrimaryThumbstickDown    = RawButton.LThumbstickDown;
			buttonMap.PrimaryThumbstickLeft    = RawButton.LThumbstickLeft;
			buttonMap.PrimaryThumbstickRight   = RawButton.LThumbstickRight;
			buttonMap.SecondaryShoulder        = RawButton.RShoulder;
			buttonMap.SecondaryIndexTrigger    = RawButton.RIndexTrigger;
			buttonMap.SecondaryHandTrigger     = RawButton.None;
			buttonMap.SecondaryThumbstick      = RawButton.RThumbstick;
			buttonMap.SecondaryThumbstickUp    = RawButton.RThumbstickUp;
			buttonMap.SecondaryThumbstickDown  = RawButton.RThumbstickDown;
			buttonMap.SecondaryThumbstickLeft  = RawButton.RThumbstickLeft;
			buttonMap.SecondaryThumbstickRight = RawButton.RThumbstickRight;
			buttonMap.DpadUp                   = RawButton.DpadUp;
			buttonMap.DpadDown                 = RawButton.DpadDown;
			buttonMap.DpadLeft                 = RawButton.DpadLeft;
			buttonMap.DpadRight                = RawButton.DpadRight;
			buttonMap.Up                       = RawButton.LThumbstickUp;
			buttonMap.Down                     = RawButton.LThumbstickDown;
			buttonMap.Left                     = RawButton.LThumbstickLeft;
			buttonMap.Right                    = RawButton.LThumbstickRight;
		}

		public override void ConfigureTouchMap()
		{
			touchMap.None                      = RawTouch.None;
			touchMap.One                       = RawTouch.None;
			touchMap.Two                       = RawTouch.None;
			touchMap.Three                     = RawTouch.None;
			touchMap.Four                      = RawTouch.None;
			touchMap.PrimaryIndexTrigger       = RawTouch.None;
			touchMap.PrimaryThumbstick         = RawTouch.None;
			touchMap.PrimaryThumbRest          = RawTouch.None;
			touchMap.SecondaryIndexTrigger     = RawTouch.None;
			touchMap.SecondaryThumbstick       = RawTouch.None;
			touchMap.SecondaryThumbRest        = RawTouch.None;
		}

		public override void ConfigureNearTouchMap()
		{
			nearTouchMap.None                      = RawNearTouch.None;
			nearTouchMap.PrimaryIndexTrigger       = RawNearTouch.None;
			nearTouchMap.PrimaryThumbButtons       = RawNearTouch.None;
			nearTouchMap.SecondaryIndexTrigger     = RawNearTouch.None;
			nearTouchMap.SecondaryThumbButtons     = RawNearTouch.None;
		}

		public override void ConfigureAxis1DMap()
		{
			axis1DMap.None                      = RawAxis1D.None;
			axis1DMap.PrimaryIndexTrigger       = RawAxis1D.LIndexTrigger;
			axis1DMap.PrimaryHandTrigger        = RawAxis1D.None;
			axis1DMap.SecondaryIndexTrigger     = RawAxis1D.RIndexTrigger;
			axis1DMap.SecondaryHandTrigger      = RawAxis1D.None;
		}

		public override void ConfigureAxis2DMap()
		{
			axis2DMap.None                      = RawAxis2D.None;
			axis2DMap.PrimaryThumbstick         = RawAxis2D.LThumbstick;
			axis2DMap.SecondaryThumbstick       = RawAxis2D.RThumbstick;
		}

		public override void SetControllerVibration(float frequency, float amplitude)
		{
			int gpcNode = 0;
			float gpcFrequency = frequency * 200.0f; //Map frequency from 0-1 CAPI range to 0-200 GPC range
			float gpcStrength = amplitude;

			OVR_GamepadController_SetVibration(gpcNode, gpcStrength, gpcFrequency);
		}

		private const string DllName = "OVRGamepad";

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool OVR_GamepadController_Initialize();
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool OVR_GamepadController_Destroy();
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool OVR_GamepadController_Update();
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		private static extern float OVR_GamepadController_GetAxis(int axis);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool OVR_GamepadController_GetButton(int button);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool OVR_GamepadController_SetVibration(int node, float strength, float frequency);
	}

	private class OVRControllerGamepadAndroid : OVRControllerBase
	{
		private static class AndroidButtonNames
		{
			public static readonly KeyCode A = KeyCode.JoystickButton0;
			public static readonly KeyCode B = KeyCode.JoystickButton1;
			public static readonly KeyCode X = KeyCode.JoystickButton2;
			public static readonly KeyCode Y = KeyCode.JoystickButton3;
			public static readonly KeyCode Start = KeyCode.JoystickButton10;
			public static readonly KeyCode Back = KeyCode.JoystickButton11;
			public static readonly KeyCode LThumbstick = KeyCode.JoystickButton8;
			public static readonly KeyCode RThumbstick = KeyCode.JoystickButton9;
			public static readonly KeyCode LShoulder = KeyCode.JoystickButton4;
			public static readonly KeyCode RShoulder = KeyCode.JoystickButton5;
		}

		private static class AndroidAxisNames
		{
			public static readonly string LThumbstickX = "Oculus_GearVR_LThumbstickX";
			public static readonly string LThumbstickY = "Oculus_GearVR_LThumbstickY";
			public static readonly string RThumbstickX = "Oculus_GearVR_RThumbstickX";
			public static readonly string RThumbstickY = "Oculus_GearVR_RThumbstickY";
			public static readonly string LIndexTrigger = "Oculus_GearVR_LIndexTrigger";
			public static readonly string RIndexTrigger = "Oculus_GearVR_RIndexTrigger";
			public static readonly string DpadX = "Oculus_GearVR_DpadX";
			public static readonly string DpadY = "Oculus_GearVR_DpadY";
		}

		private bool joystickDetected = false;
		private float joystickCheckInterval = 1.0f;
		private float joystickCheckTime = 0.0f;

		public OVRControllerGamepadAndroid()
		{
			controllerType = Controller.Gamepad;
		}

		private bool ShouldUpdate()
		{
			// Use Unity's joystick detection as a quick way to determine joystick availability.
			if ((Time.realtimeSinceStartup - joystickCheckTime) > joystickCheckInterval)
			{
				joystickCheckTime = Time.realtimeSinceStartup;
				joystickDetected = false;
				var joystickNames = UnityEngine.Input.GetJoystickNames();

				for (int i = 0; i < joystickNames.Length; i++)
				{
					if (joystickNames[i] != String.Empty)
					{
						joystickDetected = true;
						break;
					}
				}
			}

			return joystickDetected;
		}

		public override Controller Update()
		{
			if (!ShouldUpdate())
			{
				return Controller.None;
			}

			OVRPlugin.ControllerState state = new OVRPlugin.ControllerState();

			state.ConnectedControllers = (uint)Controller.Gamepad;

			if (Input.GetKey(AndroidButtonNames.A))
				state.Buttons |= (uint)RawButton.A;
			if (Input.GetKey(AndroidButtonNames.B))
				state.Buttons |= (uint)RawButton.B;
			if (Input.GetKey(AndroidButtonNames.X))
				state.Buttons |= (uint)RawButton.X;
			if (Input.GetKey(AndroidButtonNames.Y))
				state.Buttons |= (uint)RawButton.Y;
			if (Input.GetKey(AndroidButtonNames.Start))
				state.Buttons |= (uint)RawButton.Start;
			if (Input.GetKey(AndroidButtonNames.Back) || Input.GetKey(KeyCode.Escape))
				state.Buttons |= (uint)RawButton.Back;
			if (Input.GetKey(AndroidButtonNames.LThumbstick))
				state.Buttons |= (uint)RawButton.LThumbstick;
			if (Input.GetKey(AndroidButtonNames.RThumbstick))
				state.Buttons |= (uint)RawButton.RThumbstick;
			if (Input.GetKey(AndroidButtonNames.LShoulder))
				state.Buttons |= (uint)RawButton.LShoulder;
			if (Input.GetKey(AndroidButtonNames.RShoulder))
				state.Buttons |= (uint)RawButton.RShoulder;

			state.LThumbstick.x = Input.GetAxisRaw(AndroidAxisNames.LThumbstickX);
			state.LThumbstick.y = Input.GetAxisRaw(AndroidAxisNames.LThumbstickY);
			state.RThumbstick.x = Input.GetAxisRaw(AndroidAxisNames.RThumbstickX);
			state.RThumbstick.y = Input.GetAxisRaw(AndroidAxisNames.RThumbstickY);
			state.LIndexTrigger = Input.GetAxisRaw(AndroidAxisNames.LIndexTrigger);
			state.RIndexTrigger = Input.GetAxisRaw(AndroidAxisNames.RIndexTrigger);

			if (state.LIndexTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LIndexTrigger;
			if (state.LHandTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LHandTrigger;
			if (state.LThumbstick.y >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickUp;
			if (state.LThumbstick.y <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickDown;
			if (state.LThumbstick.x <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickLeft;
			if (state.LThumbstick.x >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.LThumbstickRight;

			if (state.RIndexTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RIndexTrigger;
			if (state.RHandTrigger >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RHandTrigger;
			if (state.RThumbstick.y >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickUp;
			if (state.RThumbstick.y <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickDown;
			if (state.RThumbstick.x <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickLeft;
			if (state.RThumbstick.x >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.RThumbstickRight;

			float dpadX = Input.GetAxisRaw(AndroidAxisNames.DpadX);
			float dpadY = Input.GetAxisRaw(AndroidAxisNames.DpadY);

			if (dpadX <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.DpadLeft;
			if (dpadX >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.DpadRight;
			if (dpadY <= -AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.DpadDown;
			if (dpadY >= AXIS_AS_BUTTON_THRESHOLD)
				state.Buttons |= (uint)RawButton.DpadUp;

			previousState = currentState;
			currentState = state;

			return ((Controller)currentState.ConnectedControllers & controllerType);
		}

		public override void ConfigureButtonMap()
		{
			buttonMap.None                     = RawButton.None;
			buttonMap.One                      = RawButton.A;
			buttonMap.Two                      = RawButton.B;
			buttonMap.Three                    = RawButton.X;
			buttonMap.Four                     = RawButton.Y;
			buttonMap.Start                    = RawButton.Start;
			buttonMap.Back                     = RawButton.Back;
			buttonMap.PrimaryShoulder          = RawButton.LShoulder;
			buttonMap.PrimaryIndexTrigger      = RawButton.LIndexTrigger;
			buttonMap.PrimaryHandTrigger       = RawButton.None;
			buttonMap.PrimaryThumbstick        = RawButton.LThumbstick;
			buttonMap.PrimaryThumbstickUp      = RawButton.LThumbstickUp;
			buttonMap.PrimaryThumbstickDown    = RawButton.LThumbstickDown;
			buttonMap.PrimaryThumbstickLeft    = RawButton.LThumbstickLeft;
			buttonMap.PrimaryThumbstickRight   = RawButton.LThumbstickRight;
			buttonMap.SecondaryShoulder        = RawButton.RShoulder;
			buttonMap.SecondaryIndexTrigger    = RawButton.RIndexTrigger;
			buttonMap.SecondaryHandTrigger     = RawButton.None;
			buttonMap.SecondaryThumbstick      = RawButton.RThumbstick;
			buttonMap.SecondaryThumbstickUp    = RawButton.RThumbstickUp;
			buttonMap.SecondaryThumbstickDown  = RawButton.RThumbstickDown;
			buttonMap.SecondaryThumbstickLeft  = RawButton.RThumbstickLeft;
			buttonMap.SecondaryThumbstickRight = RawButton.RThumbstickRight;
			buttonMap.DpadUp                   = RawButton.DpadUp;
			buttonMap.DpadDown                 = RawButton.DpadDown;
			buttonMap.DpadLeft                 = RawButton.DpadLeft;
			buttonMap.DpadRight                = RawButton.DpadRight;
			buttonMap.Up                       = RawButton.LThumbstickUp;
			buttonMap.Down                     = RawButton.LThumbstickDown;
			buttonMap.Left                     = RawButton.LThumbstickLeft;
			buttonMap.Right                    = RawButton.LThumbstickRight;
		}

		public override void ConfigureTouchMap()
		{
			touchMap.None                      = RawTouch.None;
			touchMap.One                       = RawTouch.None;
			touchMap.Two                       = RawTouch.None;
			touchMap.Three                     = RawTouch.None;
			touchMap.Four                      = RawTouch.None;
			touchMap.PrimaryIndexTrigger       = RawTouch.None;
			touchMap.PrimaryThumbstick         = RawTouch.None;
			touchMap.PrimaryThumbRest          = RawTouch.None;
			touchMap.SecondaryIndexTrigger     = RawTouch.None;
			touchMap.SecondaryThumbstick       = RawTouch.None;
			touchMap.SecondaryThumbRest        = RawTouch.None;
		}

		public override void ConfigureNearTouchMap()
		{
			nearTouchMap.None                      = RawNearTouch.None;
			nearTouchMap.PrimaryIndexTrigger       = RawNearTouch.None;
			nearTouchMap.PrimaryThumbButtons       = RawNearTouch.None;
			nearTouchMap.SecondaryIndexTrigger     = RawNearTouch.None;
			nearTouchMap.SecondaryThumbButtons     = RawNearTouch.None;
		}

		public override void ConfigureAxis1DMap()
		{
			axis1DMap.None                      = RawAxis1D.None;
			axis1DMap.PrimaryIndexTrigger       = RawAxis1D.LIndexTrigger;
			axis1DMap.PrimaryHandTrigger        = RawAxis1D.None;
			axis1DMap.SecondaryIndexTrigger     = RawAxis1D.RIndexTrigger;
			axis1DMap.SecondaryHandTrigger      = RawAxis1D.None;
		}

		public override void ConfigureAxis2DMap()
		{
			axis2DMap.None                      = RawAxis2D.None;
			axis2DMap.PrimaryThumbstick         = RawAxis2D.LThumbstick;
			axis2DMap.SecondaryThumbstick       = RawAxis2D.RThumbstick;
		}

		public override void SetControllerVibration(float frequency, float amplitude)
		{

		}
	}
}

