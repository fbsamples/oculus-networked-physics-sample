/**
 * Copyright (c) 2017-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the Scripts directory of this source tree. An additional grant 
 * of patent rights can be found in the PATENTS file in the same directory.
 */

using System;
using UnityEngine;
using UnityEngine.Assertions;

public struct AvatarStateQuantized
{
    public int client_index;

    public int head_position_x;
    public int head_position_y;
    public int head_position_z;

    public uint head_rotation_largest;
    public uint head_rotation_a;
    public uint head_rotation_b;
    public uint head_rotation_c;

    public int left_hand_position_x;
    public int left_hand_position_y;
    public int left_hand_position_z;

    public uint left_hand_rotation_largest;
    public uint left_hand_rotation_a;
    public uint left_hand_rotation_b;
    public uint left_hand_rotation_c;

    public int left_hand_grip_trigger;
    public int left_hand_index_trigger;
    public bool left_hand_pointing;
    public bool left_hand_thumbs_up;

    public bool left_hand_holding_cube;
    public int left_hand_cube_id;
    public ushort left_hand_authority_sequence;
    public ushort left_hand_ownership_sequence;
    public int left_hand_cube_local_position_x;
    public int left_hand_cube_local_position_y;
    public int left_hand_cube_local_position_z;
    public uint left_hand_cube_local_rotation_largest;
    public uint left_hand_cube_local_rotation_a;
    public uint left_hand_cube_local_rotation_b;
    public uint left_hand_cube_local_rotation_c;

    public int right_hand_position_x;
    public int right_hand_position_y;
    public int right_hand_position_z;

    public uint right_hand_rotation_largest;
    public uint right_hand_rotation_a;
    public uint right_hand_rotation_b;
    public uint right_hand_rotation_c;

    public int right_hand_grip_trigger;
    public int right_hand_index_trigger;
    public bool right_hand_pointing;
    public bool right_hand_thumbs_up;

    public bool right_hand_holding_cube;
    public int right_hand_cube_id;
    public ushort right_hand_authority_sequence;
    public ushort right_hand_ownership_sequence;
    public int right_hand_cube_local_position_x;
    public int right_hand_cube_local_position_y;
    public int right_hand_cube_local_position_z;
    public uint right_hand_cube_local_rotation_largest;
    public uint right_hand_cube_local_rotation_a;
    public uint right_hand_cube_local_rotation_b;
    public uint right_hand_cube_local_rotation_c;

    public int voice_amplitude;

    public static AvatarStateQuantized defaults;
}

public struct AvatarState
{
    public int client_index;

    public Vector3 head_position;
    public Quaternion head_rotation;
    
    public Vector3 left_hand_position;
    public Quaternion left_hand_rotation;
    public float left_hand_grip_trigger;
    public float left_hand_index_trigger;
    public bool left_hand_pointing;
    public bool left_hand_thumbs_up;
    public bool left_hand_holding_cube;
    public int left_hand_cube_id;
    public ushort left_hand_authority_sequence;
    public ushort left_hand_ownership_sequence;
    public Vector3 left_hand_cube_local_position;
    public Quaternion left_hand_cube_local_rotation;

    public Vector3 right_hand_position;
    public Quaternion right_hand_rotation;
    public float right_hand_grip_trigger;
    public float right_hand_index_trigger;
    public bool right_hand_pointing;
    public bool right_hand_thumbs_up;
    public bool right_hand_holding_cube;
    public int right_hand_cube_id;
    public ushort right_hand_authority_sequence;
    public ushort right_hand_ownership_sequence;
    public Vector3 right_hand_cube_local_position;
    public Quaternion right_hand_cube_local_rotation;

    public float voice_amplitude;

    public static AvatarState defaults;

    public static void Initialize( out AvatarState state, int clientIndex, OvrAvatarDriver.PoseFrame frame, GameObject leftHandHeldObject, GameObject rightHandHeldObject )
    {
        state.client_index = clientIndex;

        state.head_position = frame.headPosition;
        state.head_rotation = frame.headRotation;

        state.left_hand_position = frame.handLeftPosition;
        state.left_hand_rotation = frame.handLeftRotation;
        state.left_hand_grip_trigger = frame.handLeftPose.gripFlex;
        state.left_hand_index_trigger = frame.handLeftPose.indexFlex;
        state.left_hand_pointing = frame.handLeftPose.isPointing;
        state.left_hand_thumbs_up = frame.handLeftPose.isThumbUp;

        if ( leftHandHeldObject )
        {
            state.left_hand_holding_cube = true;

            NetworkInfo networkInfo = leftHandHeldObject.GetComponent<NetworkInfo>();

            state.left_hand_cube_id = networkInfo.GetCubeId();
            state.left_hand_authority_sequence = networkInfo.GetAuthoritySequence();
            state.left_hand_ownership_sequence = networkInfo.GetOwnershipSequence();
            state.left_hand_cube_local_position = leftHandHeldObject.transform.localPosition;
            state.left_hand_cube_local_rotation = leftHandHeldObject.transform.localRotation;
        }
        else
        {
            state.left_hand_holding_cube = false;
            state.left_hand_cube_id = -1;
            state.left_hand_authority_sequence = 0;
            state.left_hand_ownership_sequence = 0;
            state.left_hand_cube_local_position = Vector3.zero;
            state.left_hand_cube_local_rotation = Quaternion.identity;
        }

        state.right_hand_position = frame.handRightPosition;
        state.right_hand_rotation = frame.handRightRotation;
        state.right_hand_grip_trigger = frame.handRightPose.gripFlex;
        state.right_hand_index_trigger = frame.handRightPose.indexFlex;
        state.right_hand_pointing = frame.handRightPose.isPointing;
        state.right_hand_thumbs_up = frame.handRightPose.isThumbUp;

        if ( rightHandHeldObject )
        {
            state.right_hand_holding_cube = true;

            NetworkInfo networkInfo = rightHandHeldObject.GetComponent<NetworkInfo>();

            state.right_hand_cube_id = networkInfo.GetCubeId();
            state.right_hand_authority_sequence = networkInfo.GetAuthoritySequence();
            state.right_hand_ownership_sequence = networkInfo.GetOwnershipSequence();
            state.right_hand_cube_local_position = rightHandHeldObject.transform.localPosition;
            state.right_hand_cube_local_rotation = rightHandHeldObject.transform.localRotation;
        }
        else
        {
            state.right_hand_holding_cube = false;
            state.right_hand_cube_id = -1;
            state.right_hand_authority_sequence = 0;
            state.right_hand_ownership_sequence = 0;
            state.right_hand_cube_local_position = Vector3.zero;
            state.right_hand_cube_local_rotation = Quaternion.identity;
        }

        state.voice_amplitude = frame.voiceAmplitude;
    }

    public static void ApplyPose( ref AvatarState state, int clientIndex, OvrAvatarDriver.PoseFrame frame, Context context )
    {
        frame.headPosition = state.head_position;
        frame.headRotation = state.head_rotation;

        frame.handLeftPosition = state.left_hand_position;
        frame.handLeftRotation = state.left_hand_rotation;
        frame.handLeftPose.gripFlex = state.left_hand_grip_trigger;
        frame.handLeftPose.indexFlex = state.left_hand_index_trigger;
        frame.handLeftPose.isPointing = state.left_hand_pointing;
        frame.handLeftPose.isThumbUp = state.left_hand_thumbs_up;

        frame.handRightPosition = state.right_hand_position;
        frame.handRightRotation = state.right_hand_rotation;
        frame.handRightPose.gripFlex = state.right_hand_grip_trigger;
        frame.handRightPose.indexFlex = state.right_hand_index_trigger;
        frame.handRightPose.isPointing = state.right_hand_pointing;
        frame.handRightPose.isThumbUp = state.right_hand_thumbs_up;

        frame.voiceAmplitude = state.voice_amplitude;
    }

    public static void UpdateLeftHandSequenceNumbers( ref AvatarState state, Context context )
    {
        if ( state.left_hand_holding_cube )
        {
            var cube = context.GetCube( state.left_hand_cube_id );
            var networkInfo = cube.GetComponent<NetworkInfo>();
            if ( Network.Util.SequenceGreaterThan( state.left_hand_ownership_sequence, networkInfo.GetOwnershipSequence() ) )
            {
#if DEBUG_AUTHORITY
                Debug.Log( "server -> client: update left hand sequence numbers - ownership sequence " + networkInfo.GetOwnershipSequence() + "->" + state.left_hand_ownership_sequence + ", authority sequence " + networkInfo.GetOwnershipSequence() + "->" + state.left_hand_authority_sequence );
#endif // #if DEBUG_AUTHORITY
                networkInfo.SetOwnershipSequence( state.left_hand_ownership_sequence );
                networkInfo.SetAuthoritySequence( state.left_hand_authority_sequence );
            }
        }
    }

    public static void UpdateRightHandSequenceNumbers( ref AvatarState state, Context context )
    {
        if ( state.right_hand_holding_cube )
        {
            var cube = context.GetCube( state.right_hand_cube_id );
            var networkInfo = cube.GetComponent<NetworkInfo>();
            if ( Network.Util.SequenceGreaterThan( state.right_hand_ownership_sequence, networkInfo.GetOwnershipSequence() ) )
            {
#if DEBUG_AUTHORITY
                Debug.Log( "server -> client: update right hand sequence numbers - ownership sequence " + networkInfo.GetOwnershipSequence() + "->" + state.right_hand_ownership_sequence + ", authority sequence " + networkInfo.GetOwnershipSequence() + "->" + state.right_hand_authority_sequence );
#endif // #if DEBUG_AUTHORITY
                networkInfo.SetOwnershipSequence( state.right_hand_ownership_sequence );
                networkInfo.SetAuthoritySequence( state.right_hand_authority_sequence );
            }
        }
    }

    public static void ApplyLeftHandUpdate( ref AvatarState state, int clientIndex, Context context, RemoteAvatar remoteAvatar )
    {
        Assert.IsTrue( clientIndex == state.client_index );

        if ( state.left_hand_holding_cube )
        {
            var cube = context.GetCube( state.left_hand_cube_id );

            var networkInfo = cube.GetComponent<NetworkInfo>();

            if ( !networkInfo.IsHeldByRemotePlayer( remoteAvatar, remoteAvatar.GetLeftHand() ) )
            {
                networkInfo.AttachCubeToRemotePlayer( remoteAvatar, remoteAvatar.GetLeftHand(), state.client_index );
            }

            networkInfo.SetAuthoritySequence( state.left_hand_authority_sequence );

            networkInfo.SetOwnershipSequence( state.left_hand_ownership_sequence );

            networkInfo.MoveWithSmoothingLocal( state.left_hand_cube_local_position, state.left_hand_cube_local_rotation );
        }
    }

    public static void ApplyRightHandUpdate( ref AvatarState state, int clientIndex, Context context, RemoteAvatar remoteAvatar )
    {
        Assert.IsTrue( clientIndex == state.client_index );

        if ( state.right_hand_holding_cube )
        {
            GameObject cube = context.GetCube( state.right_hand_cube_id );

            var networkInfo = cube.GetComponent<NetworkInfo>();

            if ( !networkInfo.IsHeldByRemotePlayer( remoteAvatar, remoteAvatar.GetRightHand() ) )
            {
                networkInfo.AttachCubeToRemotePlayer( remoteAvatar, remoteAvatar.GetRightHand(), state.client_index );
            }

            networkInfo.SetAuthoritySequence( state.right_hand_authority_sequence );
            networkInfo.SetOwnershipSequence( state.right_hand_ownership_sequence );

            networkInfo.MoveWithSmoothingLocal( state.right_hand_cube_local_position, state.right_hand_cube_local_rotation );
        }
    }

    public static void Quantize( ref AvatarState state, out AvatarStateQuantized quantized )
    {
        quantized.client_index = state.client_index;

        quantized.head_position_x = (int) Math.Floor( state.head_position.x * Constants.UnitsPerMeter + 0.5f );
        quantized.head_position_y = (int) Math.Floor( state.head_position.y * Constants.UnitsPerMeter + 0.5f );
        quantized.head_position_z = (int) Math.Floor( state.head_position.z * Constants.UnitsPerMeter + 0.5f );

        Snapshot.QuaternionToSmallestThree( state.head_rotation,
                                            out quantized.head_rotation_largest,
                                            out quantized.head_rotation_a,
                                            out quantized.head_rotation_b,
                                            out quantized.head_rotation_c );

        quantized.left_hand_position_x = (int) Math.Floor( state.left_hand_position.x * Constants.UnitsPerMeter + 0.5f );
        quantized.left_hand_position_y = (int) Math.Floor( state.left_hand_position.y * Constants.UnitsPerMeter + 0.5f );
        quantized.left_hand_position_z = (int) Math.Floor( state.left_hand_position.z * Constants.UnitsPerMeter + 0.5f );

        Snapshot.QuaternionToSmallestThree( state.left_hand_rotation,
                                            out quantized.left_hand_rotation_largest,
                                            out quantized.left_hand_rotation_a,
                                            out quantized.left_hand_rotation_b,
                                            out quantized.left_hand_rotation_c );

        quantized.left_hand_grip_trigger = (int) Math.Floor( state.left_hand_grip_trigger * Constants.TriggerMaximum + 0.5f );
        quantized.left_hand_index_trigger = (int) Math.Floor( state.left_hand_index_trigger * Constants.TriggerMaximum + 0.5f );
        quantized.left_hand_pointing = state.left_hand_pointing;
        quantized.left_hand_thumbs_up = state.left_hand_thumbs_up;

        if ( state.left_hand_holding_cube )
        {
            quantized.left_hand_holding_cube = true;

            quantized.left_hand_cube_id = state.left_hand_cube_id;
            quantized.left_hand_authority_sequence = state.left_hand_authority_sequence;
            quantized.left_hand_ownership_sequence = state.left_hand_ownership_sequence;

            quantized.left_hand_cube_local_position_x = (int) Math.Floor( state.left_hand_cube_local_position.x * Constants.UnitsPerMeter + 0.5f );
            quantized.left_hand_cube_local_position_y = (int) Math.Floor( state.left_hand_cube_local_position.y * Constants.UnitsPerMeter + 0.5f );
            quantized.left_hand_cube_local_position_z = (int) Math.Floor( state.left_hand_cube_local_position.z * Constants.UnitsPerMeter + 0.5f );

            Snapshot.QuaternionToSmallestThree( state.left_hand_cube_local_rotation,
                                                out quantized.left_hand_cube_local_rotation_largest,
                                                out quantized.left_hand_cube_local_rotation_a,
                                                out quantized.left_hand_cube_local_rotation_b,
                                                out quantized.left_hand_cube_local_rotation_c );
        }
        else
        {
            quantized.left_hand_holding_cube = false;
            quantized.left_hand_cube_id = -1;
            quantized.left_hand_authority_sequence = 0;
            quantized.left_hand_ownership_sequence = 0;
            quantized.left_hand_cube_local_position_x = 0;
            quantized.left_hand_cube_local_position_y = 0;
            quantized.left_hand_cube_local_position_z = 0;
            quantized.left_hand_cube_local_rotation_largest = 0;
            quantized.left_hand_cube_local_rotation_a = 0;
            quantized.left_hand_cube_local_rotation_b = 0;
            quantized.left_hand_cube_local_rotation_c = 0;
        }

        quantized.right_hand_position_x = (int) Math.Floor( state.right_hand_position.x * Constants.UnitsPerMeter + 0.5f );
        quantized.right_hand_position_y = (int) Math.Floor( state.right_hand_position.y * Constants.UnitsPerMeter + 0.5f );
        quantized.right_hand_position_z = (int) Math.Floor( state.right_hand_position.z * Constants.UnitsPerMeter + 0.5f );

        Snapshot.QuaternionToSmallestThree( state.right_hand_rotation,
                                            out quantized.right_hand_rotation_largest,
                                            out quantized.right_hand_rotation_a,
                                            out quantized.right_hand_rotation_b,
                                            out quantized.right_hand_rotation_c );

        quantized.right_hand_grip_trigger = (int) Math.Floor( state.right_hand_grip_trigger * Constants.TriggerMaximum + 0.5f );
        quantized.right_hand_index_trigger = (int) Math.Floor( state.right_hand_index_trigger * Constants.TriggerMaximum + 0.5f );
        quantized.right_hand_pointing = state.right_hand_pointing;
        quantized.right_hand_thumbs_up = state.right_hand_thumbs_up;

        if ( state.right_hand_holding_cube )
        {
            quantized.right_hand_holding_cube = true;

            quantized.right_hand_cube_id = state.right_hand_cube_id;
            quantized.right_hand_authority_sequence = state.right_hand_authority_sequence;
            quantized.right_hand_ownership_sequence = state.right_hand_ownership_sequence;

            quantized.right_hand_cube_local_position_x = (int) Math.Floor( state.right_hand_cube_local_position.x * Constants.UnitsPerMeter + 0.5f );
            quantized.right_hand_cube_local_position_y = (int) Math.Floor( state.right_hand_cube_local_position.y * Constants.UnitsPerMeter + 0.5f );
            quantized.right_hand_cube_local_position_z = (int) Math.Floor( state.right_hand_cube_local_position.z * Constants.UnitsPerMeter + 0.5f );

            Snapshot.QuaternionToSmallestThree( state.right_hand_cube_local_rotation,
                                                out quantized.right_hand_cube_local_rotation_largest,
                                                out quantized.right_hand_cube_local_rotation_a,
                                                out quantized.right_hand_cube_local_rotation_b,
                                                out quantized.right_hand_cube_local_rotation_c );
        }
        else
        {
            quantized.right_hand_holding_cube = false;
            quantized.right_hand_cube_id = -1;
            quantized.right_hand_authority_sequence = 0;
            quantized.right_hand_ownership_sequence = 0;
            quantized.right_hand_cube_local_position_x = 0;
            quantized.right_hand_cube_local_position_y = 0;
            quantized.right_hand_cube_local_position_z = 0;
            quantized.right_hand_cube_local_rotation_largest = 0;
            quantized.right_hand_cube_local_rotation_a = 0;
            quantized.right_hand_cube_local_rotation_b = 0;
            quantized.right_hand_cube_local_rotation_c = 0;
        }

        quantized.voice_amplitude = (int) Math.Floor( state.voice_amplitude * Constants.VoiceMaximum + 0.5f );

        // clamp everything

        Snapshot.ClampPosition( ref quantized.head_position_x, ref quantized.head_position_y, ref quantized.head_position_z );

        Snapshot.ClampPosition( ref quantized.left_hand_position_x, ref quantized.left_hand_position_y, ref quantized.left_hand_position_z );

        Snapshot.ClampPosition( ref quantized.right_hand_position_x, ref quantized.right_hand_position_y, ref quantized.right_hand_position_z );

        if ( quantized.left_hand_holding_cube )
        {
            Snapshot.ClampLocalPosition( ref quantized.left_hand_cube_local_position_x, ref quantized.left_hand_cube_local_position_y, ref quantized.left_hand_cube_local_position_z );
        }

        if ( quantized.right_hand_holding_cube )
        {
            Snapshot.ClampLocalPosition( ref quantized.right_hand_cube_local_position_x, ref quantized.right_hand_cube_local_position_y, ref quantized.right_hand_cube_local_position_z );
        }                  
    }

    public static void Unquantize( ref AvatarStateQuantized quantized, out AvatarState state )
    {
        state.client_index = quantized.client_index;

        state.head_position = new Vector3( quantized.head_position_x, quantized.head_position_y, quantized.head_position_z ) * 1.0f / Constants.UnitsPerMeter;

        state.head_rotation = Snapshot.SmallestThreeToQuaternion( quantized.head_rotation_largest,
                                                                  quantized.head_rotation_a,
                                                                  quantized.head_rotation_b,
                                                                  quantized.head_rotation_c );

        state.left_hand_position = new Vector3( quantized.left_hand_position_x, quantized.left_hand_position_y, quantized.left_hand_position_z ) * 1.0f / Constants.UnitsPerMeter;

        state.left_hand_rotation = Snapshot.SmallestThreeToQuaternion( quantized.left_hand_rotation_largest,
                                                                       quantized.left_hand_rotation_a,
                                                                       quantized.left_hand_rotation_b,
                                                                       quantized.left_hand_rotation_c );

        state.left_hand_grip_trigger = quantized.left_hand_grip_trigger * 1.0f / Constants.TriggerMaximum;
        state.left_hand_index_trigger = quantized.left_hand_index_trigger * 1.0f / Constants.TriggerMaximum;
        state.left_hand_pointing = quantized.left_hand_pointing;
        state.left_hand_thumbs_up = quantized.left_hand_thumbs_up;

        state.left_hand_holding_cube = quantized.left_hand_holding_cube;
        state.left_hand_cube_id = quantized.left_hand_cube_id;
        state.left_hand_ownership_sequence = quantized.left_hand_ownership_sequence;
        state.left_hand_authority_sequence = quantized.left_hand_authority_sequence;

        state.left_hand_cube_local_position = new Vector3( quantized.left_hand_cube_local_position_x, quantized.left_hand_cube_local_position_y, quantized.left_hand_cube_local_position_z ) * 1.0f / Constants.UnitsPerMeter;
        state.left_hand_cube_local_rotation = Snapshot.SmallestThreeToQuaternion( quantized.left_hand_cube_local_rotation_largest, quantized.left_hand_cube_local_rotation_a, quantized.left_hand_cube_local_rotation_b, quantized.left_hand_cube_local_rotation_c );

        state.right_hand_position = new Vector3( quantized.right_hand_position_x, quantized.right_hand_position_y, quantized.right_hand_position_z ) * 1.0f / Constants.UnitsPerMeter;

        state.right_hand_rotation = Snapshot.SmallestThreeToQuaternion( quantized.right_hand_rotation_largest,
                                                                        quantized.right_hand_rotation_a,
                                                                        quantized.right_hand_rotation_b,
                                                                        quantized.right_hand_rotation_c );

        state.right_hand_grip_trigger = quantized.right_hand_grip_trigger * 1.0f / Constants.TriggerMaximum;
        state.right_hand_index_trigger = quantized.right_hand_index_trigger * 1.0f / Constants.TriggerMaximum;
        state.right_hand_pointing = quantized.right_hand_pointing;
        state.right_hand_thumbs_up = quantized.right_hand_thumbs_up;

        state.right_hand_holding_cube = quantized.right_hand_holding_cube;
        state.right_hand_cube_id = quantized.right_hand_cube_id;
        state.right_hand_ownership_sequence = quantized.right_hand_ownership_sequence;
        state.right_hand_authority_sequence = quantized.right_hand_authority_sequence;

        state.right_hand_cube_local_position = new Vector3( quantized.right_hand_cube_local_position_x, quantized.right_hand_cube_local_position_y, quantized.right_hand_cube_local_position_z ) * 1.0f / Constants.UnitsPerMeter;
        state.right_hand_cube_local_rotation = Snapshot.SmallestThreeToQuaternion( quantized.right_hand_cube_local_rotation_largest, quantized.right_hand_cube_local_rotation_a, quantized.right_hand_cube_local_rotation_b, quantized.right_hand_cube_local_rotation_c );

        state.voice_amplitude = quantized.voice_amplitude * 1.0f / Constants.VoiceMaximum;
    }

    public static void Interpolate( ref AvatarState a, ref AvatarState b, out AvatarState output, float t )
    {
        // convention: logically everything stays at the oldest sample, but positions and rotations and other continuous quantities are interpolated forward where it makes sense.

        output.client_index = a.client_index;

        output.head_position = a.head_position * ( 1 - t ) + b.head_position * t;
        output.head_rotation = Quaternion.Slerp( a.head_rotation, b.head_rotation, t );

        output.left_hand_position = a.left_hand_position * ( 1 - t ) + b.left_hand_position * t;
        output.left_hand_rotation = Quaternion.Slerp( a.left_hand_rotation, b.left_hand_rotation, t );
        output.left_hand_grip_trigger = a.left_hand_grip_trigger * ( 1 - t ) + b.left_hand_grip_trigger * t;
        output.left_hand_index_trigger = a.left_hand_index_trigger * ( 1 - t ) + b.left_hand_index_trigger * t;
        output.left_hand_pointing = a.left_hand_pointing;
        output.left_hand_thumbs_up = a.left_hand_thumbs_up;
        output.left_hand_holding_cube = a.left_hand_holding_cube;
        output.left_hand_cube_id = a.left_hand_cube_id;
        output.left_hand_authority_sequence = a.left_hand_authority_sequence;
        output.left_hand_ownership_sequence = a.left_hand_ownership_sequence;

        if ( a.left_hand_holding_cube == b.left_hand_holding_cube && a.left_hand_cube_id == b.left_hand_cube_id )
        {
            output.left_hand_cube_local_position = a.left_hand_cube_local_position * ( 1 - t ) + b.left_hand_cube_local_position * t;
            output.left_hand_cube_local_rotation = Quaternion.Slerp( a.left_hand_cube_local_rotation, b.left_hand_cube_local_rotation, t );
        }
        else
        {
            output.left_hand_cube_local_position = a.left_hand_cube_local_position;
            output.left_hand_cube_local_rotation = a.left_hand_cube_local_rotation;
        }

        output.right_hand_position = a.right_hand_position * ( 1 - t ) + b.right_hand_position * t;
        output.right_hand_rotation = Quaternion.Slerp( a.right_hand_rotation, b.right_hand_rotation, t );
        output.right_hand_grip_trigger = a.right_hand_grip_trigger * ( 1 - t ) + b.right_hand_grip_trigger * t;
        output.right_hand_index_trigger = a.right_hand_index_trigger * ( 1 - t ) + b.right_hand_index_trigger * t;
        output.right_hand_pointing = a.right_hand_pointing;
        output.right_hand_thumbs_up = a.right_hand_thumbs_up;
        output.right_hand_holding_cube = a.right_hand_holding_cube;
        output.right_hand_cube_id = a.right_hand_cube_id;
        output.right_hand_authority_sequence = a.right_hand_authority_sequence;
        output.right_hand_ownership_sequence = a.right_hand_ownership_sequence;

        if ( a.right_hand_holding_cube == b.right_hand_holding_cube && a.right_hand_cube_id == b.right_hand_cube_id )
        {
            output.right_hand_cube_local_position = a.right_hand_cube_local_position * ( 1 - t ) + b.right_hand_cube_local_position * t;
            output.right_hand_cube_local_rotation = Quaternion.Slerp( a.right_hand_cube_local_rotation, b.right_hand_cube_local_rotation, t );
        }
        else
        {
            output.right_hand_cube_local_position = a.right_hand_cube_local_position;
            output.right_hand_cube_local_rotation = a.right_hand_cube_local_rotation;
        }

        output.voice_amplitude = a.voice_amplitude * ( t - 1 ) + b.voice_amplitude * t;
    }
};
