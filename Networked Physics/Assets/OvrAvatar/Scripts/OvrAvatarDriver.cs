using UnityEngine;
using System.Collections;

public abstract class OvrAvatarDriver : MonoBehaviour {

    public struct ControllerPose
    {
        public bool button1IsDown;
        public bool button2IsDown;
        public Vector2 joystickPosition;
        public float indexTrigger;
        public float gripTrigger;

        public static ControllerPose Interpolate(ControllerPose a, ControllerPose b, float t)
        {
            return new ControllerPose
            {
                button1IsDown = t < 0.5f ? a.button1IsDown : b.button1IsDown,
                button2IsDown = t < 0.5f ? a.button2IsDown : b.button2IsDown,
                joystickPosition = Vector2.Lerp(a.joystickPosition, b.joystickPosition, t),
                indexTrigger = Mathf.Lerp(a.indexTrigger, b.indexTrigger, t),
                gripTrigger = Mathf.Lerp(a.gripTrigger, b.gripTrigger, t),
            };
        }
    }

    public struct HandPose
    {
        public float indexFlex;
        public float gripFlex;
        public bool isPointing;
        public bool isThumbUp;

        public static HandPose Interpolate(HandPose a, HandPose b, float t)
        {
            return new HandPose
            {
                indexFlex = Mathf.Lerp(a.indexFlex, b.indexFlex, t),
                gripFlex = Mathf.Lerp(a.gripFlex, b.gripFlex, t),
                isPointing = t < 0.5f ? a.isPointing : b.isPointing,
                isThumbUp = t < 0.5f ? a.isThumbUp : b.isThumbUp,
            };
        }
    }

    public class PoseFrame
    {
        public Vector3 headPosition;
        public Quaternion headRotation;
        public Vector3 handLeftPosition;
        public Quaternion handLeftRotation;
        public Vector3 handRightPosition;
        public Quaternion handRightRotation;
        public float voiceAmplitude;

        public ControllerPose controllerLeftPose;
        public ControllerPose controllerRightPose;
        public HandPose handLeftPose;
        public HandPose handRightPose;

        public static PoseFrame Interpolate(PoseFrame a, PoseFrame b, float t)
        {
            return new PoseFrame
            {
                headPosition = Vector3.Lerp(a.headPosition, b.headPosition, t),
                headRotation = Quaternion.Slerp(a.headRotation, b.headRotation, t),
                handLeftPosition = Vector3.Lerp(a.handLeftPosition, b.handLeftPosition, t),
                handLeftRotation = Quaternion.Slerp(a.handLeftRotation, b.handLeftRotation, t),
                handRightPosition = Vector3.Lerp(a.handRightPosition, b.handRightPosition, t),
                handRightRotation = Quaternion.Slerp(a.handRightRotation, b.handRightRotation, t),
                voiceAmplitude = Mathf.Lerp(a.voiceAmplitude, b.voiceAmplitude, t),
                controllerLeftPose = ControllerPose.Interpolate(a.controllerLeftPose, b.controllerLeftPose, t),
                controllerRightPose = ControllerPose.Interpolate(a.controllerRightPose, b.controllerRightPose, t),
                handLeftPose = HandPose.Interpolate(a.handLeftPose, b.handLeftPose, t),
                handRightPose = HandPose.Interpolate(a.handRightPose, b.handRightPose, t),
            };
        }
    };

    public abstract bool GetCurrentPose(out PoseFrame pose);
}