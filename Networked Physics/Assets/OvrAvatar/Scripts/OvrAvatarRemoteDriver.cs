using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class OvrAvatarRemoteDriver : OvrAvatarDriver
{
    bool isStreaming = false;
    Queue<OvrAvatarPacket> packetQueue = new Queue<OvrAvatarPacket>();
    OvrAvatarPacket currentPacket = null;
    OvrAvatarDriver.PoseFrame currentPose = null;
    float currentPacketTime = 0.0f;

    int currentSequence = -1;

    const int MinPacketQueue = 1;
    const int MaxPacketQueue = 4;

    void Update()
    {
        // If we're not currently streaming, check to see if we've buffered enough
        if (!isStreaming && packetQueue.Count > MinPacketQueue)
        {
            currentPacket = packetQueue.Dequeue();
            isStreaming = true;
        }

        // If we are streaming, update our pose
        if (isStreaming)
        {
            currentPacketTime += Time.deltaTime;

            // If we've elapsed past our current packet, advance
            while (currentPacketTime > currentPacket.Duration)
            {

                // If we're out of packets, stop streaming and
                // lock to the final frame
                if (packetQueue.Count == 0)
                {
                    currentPose = currentPacket.FinalFrame;
                    currentPacketTime = 0.0f;
                    currentPacket = null;
                    isStreaming = false;
                    return;
                }

                while (packetQueue.Count > MaxPacketQueue)
                {
                    packetQueue.Dequeue();
                }

                // Otherwise, dequeue the next packet
                currentPacketTime -= currentPacket.Duration;
                currentPacket = packetQueue.Dequeue();
            }

            // Compute the pose based on our current time offset in the packet
            currentPose = currentPacket.GetPoseFrame(currentPacketTime);
        }
    }

    public void QueuePacket(int sequence, OvrAvatarPacket packet)
    {
        if (sequence - currentSequence < 0)
        {
            return;
        }
        currentSequence = sequence;
        packetQueue.Enqueue(packet);
    }

    public override bool GetCurrentPose(out PoseFrame pose)
    {
        if (currentPose != null)
        {
            pose = currentPose;
            return true;
        }
        pose = null;
        return false;
    }
}