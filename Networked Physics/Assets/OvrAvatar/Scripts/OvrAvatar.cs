using UnityEngine;
using System.Collections;
using System;
using System.Linq;

public class OvrAvatar : MonoBehaviour {

    public OvrAvatarDriver Driver;
    public Transform HeadRoot;
    public OvrAvatarHead Head;
    public Transform HandLeftRoot;
    public Transform HandRightRoot;
    public OvrAvatarTouchController ControllerLeft;
    public OvrAvatarTouchController ControllerRight;
    public OvrAvatarHand HandLeft;
    public OvrAvatarHand HandRight;
    public bool RecordPackets;
    public bool StartWithControllers;

    public bool TrackPositions = true;
    public bool TrackRotations = true;
    
    const float PacketDurationSeconds = 1 / 30.0f;
    OvrAvatarPacket currentPacket;
    GameObject head;

    public class PacketEventArgs : EventArgs
    {
        public readonly OvrAvatarPacket Packet;
        public PacketEventArgs(OvrAvatarPacket packet)
        {
            Packet = packet;
        }
    }

    public EventHandler<PacketEventArgs> PacketRecorded;

    // Use this for initialization
    void Start() {
        ShowControllers(StartWithControllers);
    }

	// Update is called once per frame
	void Update ()
    {
        if (Driver != null)
        {
            // Get the current pose from the driver
            OvrAvatarDriver.PoseFrame pose;
            if (Driver.GetCurrentPose(out pose))
            {
                // If we're recording, record the pose
                if (RecordPackets)
                {
                    RecordFrame(Time.deltaTime, pose);
                }

                // Update the various avatar components with this pose
                UpdateTransform(HeadRoot, pose.headPosition, pose.headRotation);
                UpdateTransform(HandLeftRoot, pose.handLeftPosition, pose.handLeftRotation);
                UpdateTransform(HandRightRoot, pose.handRightPosition, pose.handRightRotation);
                if (ControllerLeft != null)
                {
                    ControllerLeft.UpdatePose(pose.controllerLeftPose);
                }
                if (ControllerRight != null)
                {
                    ControllerRight.UpdatePose(pose.controllerRightPose);
                }
                if (HandLeft != null)
                {
                    HandLeft.UpdatePose(pose.handLeftPose);
                }
                if (HandRight != null)
                {
                    HandRight.UpdatePose(pose.handRightPose);
                }
                if (Head != null)
                {
                    Head.UpdatePose(pose.voiceAmplitude);
                }
                TempFixupTransforms();
            }
        }

    }

    void TempFixupTransforms()
    {
        // If we're showing controllers, fix up the hand transforms to center the grip around the controller
        if (ControllerLeft != null && HandLeft != null)
        {
            Vector3 localPosition = Vector3.zero;
            Quaternion localRotation = Quaternion.identity;
            if (ControllerLeft.gameObject.activeSelf)
            {
                localPosition = new Vector3(0.0088477f, -0.013713f, -0.006552f);
                localRotation = Quaternion.Euler(1.44281f, 2.976443f, 349.8051f);
            }
            HandLeft.transform.localPosition = localPosition;
            HandLeft.transform.localRotation = localRotation;
        }
        if (ControllerRight != null && HandRight != null)
        {
            Vector3 localPosition = Vector3.zero;
            Quaternion localRotation = Quaternion.identity;
            if (ControllerRight.gameObject.activeSelf)
            {
                localPosition = new Vector3(-0.0088477f, -0.013713f, -0.006552f);
                localRotation = Quaternion.Euler(-1.44281f, 2.976443f, -349.8051f);
            }
            HandRight.transform.localPosition = localPosition;
            HandRight.transform.localRotation = localRotation;
        }
    }

    public void ShowControllers(bool show)
    {
        if (HandLeft != null)
        {
            HandLeft.HoldController(show);
        }
        if (HandRight != null)
        {
            HandRight.HoldController(show);
        }
        if (ControllerLeft != null)
        {
            ControllerLeft.gameObject.SetActive(show);
        }
        if (ControllerRight != null)
        {
            ControllerRight.gameObject.SetActive(show);
        }
    }

    void UpdateTransform(Transform transform, Vector3 position, Quaternion rotation)
    {
        if (transform == null)
        {
            return;
        }
        if (TrackPositions)
        {
            transform.localPosition = position;
        }
        if (TrackRotations)
        {
            transform.localRotation = rotation;
        }
    }

    void RecordFrame(float deltaSeconds, OvrAvatarDriver.PoseFrame frame)
    {
        // If this is our first packet, store the pose as the initial frame
        if (currentPacket == null)
        {
            currentPacket = new OvrAvatarPacket(frame);
            deltaSeconds = 0;
        }

        float recordedSeconds = 0;
        while (recordedSeconds < deltaSeconds)
        {
            float remainingSeconds = deltaSeconds - recordedSeconds;
            float remainingPacketSeconds = PacketDurationSeconds - currentPacket.Duration;

            // If we're not going to fill the packet, just add the frame
            if (remainingSeconds < remainingPacketSeconds)
            {
                currentPacket.AddFrame(frame, remainingSeconds);
                recordedSeconds += remainingSeconds;
            }

            // If we're going to fill the packet, interpolate the pose, send the packet,
            // and open a new one
            else
            {
                // Interpolate between the packet's last frame and our target pose
                // to compute a pose at the end of the packet time.
                OvrAvatarDriver.PoseFrame a = currentPacket.FinalFrame;
                OvrAvatarDriver.PoseFrame b = frame;
                float t = remainingPacketSeconds / remainingSeconds;
                OvrAvatarDriver.PoseFrame intermediatePose = OvrAvatarDriver.PoseFrame.Interpolate(a, b, t);
                currentPacket.AddFrame(intermediatePose, remainingPacketSeconds);
                recordedSeconds += remainingPacketSeconds;

                // Broadcast the recorded packet
                if (PacketRecorded != null)
                {
                    PacketRecorded(this, new PacketEventArgs(currentPacket));
                }

                // Open a new packet
                currentPacket = new OvrAvatarPacket(intermediatePose);
            }
        }
    }
}     