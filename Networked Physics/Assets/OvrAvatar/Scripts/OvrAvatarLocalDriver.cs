using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class OvrAvatarLocalDriver : OvrAvatarDriver {

    float voiceAmplitude = 0.0f;
    const int VoiceDefaultFrequency = 48000;
    const float VoiceEmaAlpha = 0.0005f;

    float emaAlpha = VoiceEmaAlpha;

    ControllerPose GetControllerPose(OVRInput.Controller controller)
    {
        return new ControllerPose
        {
            button1IsDown = OVRInput.Get(OVRInput.Button.One, controller),
            button2IsDown = OVRInput.Get(OVRInput.Button.Two, controller),
            joystickPosition = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller),
            indexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller),
            gripTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller),
        };
    }

    HandPose GetHandPose(OVRInput.Controller controller)
    {
        return new HandPose
        {
            indexFlex = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller),
            gripFlex = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller),
            isPointing = !OVRInput.Get(OVRInput.NearTouch.PrimaryIndexTrigger, controller),
            isThumbUp = !OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, controller),
        };
    }

    void Start()
    {
        AudioSource source = GetComponent<AudioSource>();
        if (source != null)
        {
            string selectedDeviceName = null;
            int recordFrequency = VoiceDefaultFrequency;
            foreach (string deviceName in Microphone.devices)
            {
                if (deviceName == "Microphone (Rift Audio)")
                {
                    selectedDeviceName = deviceName;
                    int minFrequency;
                    int maxFrequency;
                    Microphone.GetDeviceCaps(deviceName, out minFrequency, out maxFrequency);
                    recordFrequency = maxFrequency != 0 ? maxFrequency : VoiceDefaultFrequency;
                    emaAlpha *= VoiceDefaultFrequency / (float)recordFrequency;
                    break;
                }
            }
            source.clip = Microphone.Start(selectedDeviceName, true, 1, recordFrequency);
            source.loop = true;
            source.Play();
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            voiceAmplitude = Math.Abs(data[i]) * emaAlpha + voiceAmplitude * (1 - emaAlpha);
            data[i] = 0;
            data[i + 1] = 0;
        }
    }
   
    public override bool GetCurrentPose(out PoseFrame pose)
    {
        pose = new PoseFrame
        {
            voiceAmplitude = voiceAmplitude,
            headPosition = UnityEngine.VR.InputTracking.GetLocalPosition(UnityEngine.VR.VRNode.CenterEye),
            headRotation = UnityEngine.VR.InputTracking.GetLocalRotation(UnityEngine.VR.VRNode.CenterEye),
            handLeftPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch),
            handLeftRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch),
            handRightPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch),
            handRightRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch),
            controllerLeftPose = GetControllerPose(OVRInput.Controller.LTouch),
            handLeftPose = GetHandPose(OVRInput.Controller.LTouch),
            controllerRightPose = GetControllerPose(OVRInput.Controller.RTouch),
            handRightPose = GetHandPose(OVRInput.Controller.RTouch),
        };
        return true;
    }

}
