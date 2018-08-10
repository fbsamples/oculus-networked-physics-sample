/**
 * Copyright (c) 2017-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the Scripts directory of this source tree. An additional grant 
 * of patent rights can be found in the PATENTS file in the same directory.
 */

using UnityEngine;
using System.Collections;

public static class Constants
{
    public const string Version = "Networked Physics Build 46";
    public const int MaxClients = 4;
    public const int MaxAuthority = MaxClients + 1;         // 0 is default authority (white), 1 is client 0 authority (blue), 2 is client 1 authority (red), 3 is client 2 (green), 4 is client 3 (yellow).
#if DEBUG_AUTHORITY
    public const int NumCubes = 5;
#else // #if DEBUG_AUTHORITY
    public const int NumCubes = 64;
#endif // #if DEBUG_AUTHORITY
    public const int MaxPacketSize = 16 * 1024;
    public const int MaxStateUpdates = 64;
    public const int PhysicsFrameRate = 60;
    public const int RenderFrameRate = 90;
    public const int PositionBits = 20;
    public const int UnitsPerMeter = ( 1 << PositionBits );
    public const int RotationBits = 20;
    public const int RotationMinimum = 0;
    public const int RotationMaximum = ( 1 << RotationBits ) - 1;
    public const int PositionMinimumXZ = -64 * UnitsPerMeter;
    public const int PositionMaximumXZ = ( 64 * UnitsPerMeter ) - 1;
    public const int PositionMinimumY = 0;  
    public const int PositionMaximumY = ( 64 * UnitsPerMeter ) - 1;
    public const int LinearVelocityMinimum = -16 * UnitsPerMeter;
    public const int LinearVelocityMaximum = ( 16 * UnitsPerMeter ) - 1;
    public const int AngularVelocityMinimum = -32 * UnitsPerMeter;
    public const int AngularVelocityMaximum = ( 32 * UnitsPerMeter ) - 1;
    public const int LocalPositionMinimum = -64 * UnitsPerMeter;
    public const int LocalPositionMaximum = ( 64 * UnitsPerMeter ) - 1;
    public const float HighEnergyCollisionThreshold = 5.0f;
    public const float PushOutVelocity = 1.0f;
    public const int RingBufferSize = 16;
    public const int DeltaBufferSize = 256;
    public const int PositionDeltaMax = PositionMaximumXZ - PositionMinimumXZ;
    public const int LinearVelocityDeltaMax = LinearVelocityMaximum - LinearVelocityMinimum;
    public const int AngularVelocityDeltaMax = AngularVelocityMaximum - AngularVelocityMinimum;
    public const int PositionDeltaSmallBits = 5;
    public const int PositionDeltaSmallThreshold = ( 1 << PositionDeltaSmallBits ) - 1;
    public const int PositionDeltaMediumBits = 10;
    public const int PositionDeltaMediumThreshold = ( 1 << PositionDeltaMediumBits ) - 1;
    public const int LinearVelocityDeltaSmallBits = 5;
    public const int LinearVelocityDeltaSmallThreshold = ( 1 << LinearVelocityDeltaSmallBits ) - 1;
    public const int LinearVelocityDeltaMediumBits = 10;
    public const int LinearVelocityDeltaMediumThreshold = ( 1 << LinearVelocityDeltaMediumBits ) - 1;
    public const int AngularVelocityDeltaSmallBits = 5;
    public const int AngularVelocityDeltaSmallThreshold = ( 1 << AngularVelocityDeltaSmallBits ) - 1;
    public const int AngularVelocityDeltaMediumBits = 10;
    public const int AngularVelocityDeltaMediumThreshold = ( 1 << AngularVelocityDeltaMediumBits ) - 1;
    public const int TriggerBits = 8;
    public const int TriggerMinimum = 0;
    public const int TriggerMaximum = ( 1 << TriggerBits ) - 1;
    public const int VoiceBits = 8;
    public const int VoiceMinimum = 0;
    public const int VoiceMaximum = ( 1 << VoiceBits ) - 1;
    public const int BaselineDifferenceBits = 7;
    public const int MaxBaselineDifference = ( 1 << BaselineDifferenceBits ) - 1;
    public const int MaxInteractionsDefault = 16;
    public const int ReturnToDefaultAuthorityFrames = 1 * PhysicsFrameRate;
    public const float SupportHeightThreshold = 0.1f;
    public const int ThrownObjectPriorityBoostNumFrames = 1 * PhysicsFrameRate;
    public const int HighEnergyCollisionPriorityBoostNumFrames = PhysicsFrameRate / 4;
    public const int JitterBufferSize = 256;
    public const int NumJitterBufferFrames = 10;
}