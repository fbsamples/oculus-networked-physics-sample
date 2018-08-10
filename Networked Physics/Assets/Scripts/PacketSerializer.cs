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
using System.Collections.Generic;

public class PacketSerializer: Network.Serializer
{
    public enum PacketType
    {
        ServerInfo = 1,                     // information about players connected to the server. broadcast from server -> clients whenever a player joins or leaves the game.
        StateUpdate = 0,                    // most recent state of the world, delta encoded relative to most recent state per-object acked by the client. sent 90 times per-second.
    };

    public void WriteServerInfoPacket( Network.WriteStream stream, bool[] clientConnected, ulong[] clientUserId, string[] clientUserName )
    {
        byte packetType = (byte) PacketType.ServerInfo;

        write_bits( stream, packetType, 8 );

        for ( int i = 0; i < Constants.MaxClients; ++i )
        {
            write_bool( stream, clientConnected[i] );

            if ( !clientConnected[i] )
                continue;

            write_bits( stream, clientUserId[i], 64 );

            write_string( stream, clientUserName[i] );
        }
    }

    public void ReadServerInfoPacket( Network.ReadStream stream, bool[] clientConnected, ulong[] clientUserId, string[] clientUserName )
    {
        byte packetType = 0;

        read_bits( stream, out packetType, 8 );

        Debug.Assert( packetType == (byte) PacketType.ServerInfo );

        for ( int i = 0; i < Constants.MaxClients; ++i )
        {
            read_bool( stream, out clientConnected[i] );

            if ( !clientConnected[i] )
                continue;

            read_bits( stream, out clientUserId[i], 64 );

            read_string( stream, out clientUserName[i] );
        }
    }

    public void WriteStateUpdatePacket( Network.WriteStream stream, ref Network.PacketHeader header, int numAvatarStates, AvatarStateQuantized[] avatarState, int numStateUpdates, int[] cubeIds, bool[] notChanged, bool[] hasDelta, bool[] perfectPrediction, bool[] hasPredictionDelta, ushort[] baselineSequence, CubeState[] cubeState, CubeDelta[] cubeDelta, CubeDelta[] predictionDelta )
    {
        byte packetType = (byte) PacketType.StateUpdate;

        write_bits( stream, packetType, 8 );

        write_bits( stream, header.sequence, 16 );
        write_bits( stream, header.ack, 16 );
        write_bits( stream, header.ack_bits, 32 );
        write_bits( stream, header.frameNumber, 32 );
        write_bits( stream, header.resetSequence, 16 );
        write_float( stream, header.avatarSampleTimeOffset );

        write_int( stream, numAvatarStates, 0, Constants.MaxClients );
        for ( int i = 0; i < numAvatarStates; ++i )
        {
            write_avatar_state( stream, ref avatarState[i] );
        }

        write_int( stream, numStateUpdates, 0, Constants.MaxStateUpdates );

        for ( int i = 0; i < numStateUpdates; ++i )
        {
            write_int( stream, cubeIds[i], 0, Constants.NumCubes - 1 );

#if DEBUG_DELTA_COMPRESSION
            write_int( stream, cubeDelta[i].absolute_position_x, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
            write_int( stream, cubeDelta[i].absolute_position_y, Constants.PositionMinimumY, Constants.PositionMaximumY );
            write_int( stream, cubeDelta[i].absolute_position_z, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
#endif // #if DEBUG_DELTA_COMPRESSION

            write_int( stream, cubeState[i].authorityIndex, 0, Constants.MaxAuthority - 1 );
            write_bits( stream, cubeState[i].authoritySequence, 16 );
            write_bits( stream, cubeState[i].ownershipSequence, 16 );

            write_bool( stream, notChanged[i] );

            if ( notChanged[i] )
            {
                write_bits( stream, baselineSequence[i], 16 );
            }
            else
            {
                write_bool( stream, perfectPrediction[i] );

                if ( perfectPrediction[i] )
                {
                    write_bits( stream, baselineSequence[i], 16 );

                    write_bits( stream, cubeState[i].rotation_largest, 2 );
                    write_bits( stream, cubeState[i].rotation_a, Constants.RotationBits );
                    write_bits( stream, cubeState[i].rotation_b, Constants.RotationBits );
                    write_bits( stream, cubeState[i].rotation_c, Constants.RotationBits );
                }
                else
                {
                    write_bool( stream, hasPredictionDelta[i] );

                    if ( hasPredictionDelta[i] )
                    {
                        write_bits( stream, baselineSequence[i], 16 );

                        write_bool( stream, cubeState[i].active );

                        write_linear_velocity_delta( stream, predictionDelta[i].linear_velocity_delta_x, predictionDelta[i].linear_velocity_delta_y, predictionDelta[i].linear_velocity_delta_z );

                        write_angular_velocity_delta( stream, predictionDelta[i].angular_velocity_delta_x, predictionDelta[i].angular_velocity_delta_y, predictionDelta[i].angular_velocity_delta_z );

                        write_position_delta( stream, predictionDelta[i].position_delta_x, predictionDelta[i].position_delta_y, predictionDelta[i].position_delta_z );

                        write_bits( stream, cubeState[i].rotation_largest, 2 );
                        write_bits( stream, cubeState[i].rotation_a, Constants.RotationBits );
                        write_bits( stream, cubeState[i].rotation_b, Constants.RotationBits );
                        write_bits( stream, cubeState[i].rotation_c, Constants.RotationBits );
                    }
                    else
                    {
                        write_bool( stream, hasDelta[i] );

                        if ( hasDelta[i] )
                        {
                            write_bits( stream, baselineSequence[i], 16 );

                            write_bool( stream, cubeState[i].active );

                            write_linear_velocity_delta( stream, cubeDelta[i].linear_velocity_delta_x, cubeDelta[i].linear_velocity_delta_y, cubeDelta[i].linear_velocity_delta_z );

                            write_angular_velocity_delta( stream, cubeDelta[i].angular_velocity_delta_x, cubeDelta[i].angular_velocity_delta_y, cubeDelta[i].angular_velocity_delta_z );

                            write_position_delta( stream, cubeDelta[i].position_delta_x, cubeDelta[i].position_delta_y, cubeDelta[i].position_delta_z );

                            write_bits( stream, cubeState[i].rotation_largest, 2 );
                            write_bits( stream, cubeState[i].rotation_a, Constants.RotationBits );
                            write_bits( stream, cubeState[i].rotation_b, Constants.RotationBits );
                            write_bits( stream, cubeState[i].rotation_c, Constants.RotationBits );
                        }
                        else
                        {
                            write_bool( stream, cubeState[i].active );

                            write_int( stream, cubeState[i].position_x, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
                            write_int( stream, cubeState[i].position_y, Constants.PositionMinimumY, Constants.PositionMaximumY );
                            write_int( stream, cubeState[i].position_z, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );

                            write_bits( stream, cubeState[i].rotation_largest, 2 );
                            write_bits( stream, cubeState[i].rotation_a, Constants.RotationBits );
                            write_bits( stream, cubeState[i].rotation_b, Constants.RotationBits );
                            write_bits( stream, cubeState[i].rotation_c, Constants.RotationBits );

                            if ( cubeState[i].active )
                            {
                                write_int( stream, cubeState[i].linear_velocity_x, Constants.LinearVelocityMinimum, Constants.LinearVelocityMaximum );
                                write_int( stream, cubeState[i].linear_velocity_y, Constants.LinearVelocityMinimum, Constants.LinearVelocityMaximum );
                                write_int( stream, cubeState[i].linear_velocity_z, Constants.LinearVelocityMinimum, Constants.LinearVelocityMaximum );

                                write_int( stream, cubeState[i].angular_velocity_x, Constants.AngularVelocityMinimum, Constants.AngularVelocityMaximum );
                                write_int( stream, cubeState[i].angular_velocity_y, Constants.AngularVelocityMinimum, Constants.AngularVelocityMaximum );
                                write_int( stream, cubeState[i].angular_velocity_z, Constants.AngularVelocityMinimum, Constants.AngularVelocityMaximum );
                            }
                        }
                    }
                }
            }
        }
    }

    public void ReadStateUpdatePacketHeader( Network.ReadStream stream, out Network.PacketHeader header )
    {
        byte packetType = 0;

        read_bits( stream, out packetType, 8 );

        Debug.Assert( packetType == (byte) PacketType.StateUpdate );

        read_bits( stream, out header.sequence, 16 );
        read_bits( stream, out header.ack, 16 );
        read_bits( stream, out header.ack_bits, 32 );
        read_bits( stream, out header.frameNumber, 32 );
        read_bits( stream, out header.resetSequence, 16 );
        read_float( stream, out header.avatarSampleTimeOffset );
    }

    public void ReadStateUpdatePacket( Network.ReadStream stream, out Network.PacketHeader header, out int numAvatarStates, AvatarStateQuantized[] avatarState, out int numStateUpdates, int[] cubeIds, bool[] notChanged, bool[] hasDelta, bool[] perfectPrediction, bool[] hasPredictionDelta, ushort[] baselineSequence, CubeState[] cubeState, CubeDelta[] cubeDelta, CubeDelta[] predictionDelta )
    {
        byte packetType = 0;

        read_bits( stream, out packetType, 8 );

        Debug.Assert( packetType == (byte) PacketType.StateUpdate );

        read_bits( stream, out header.sequence, 16 );
        read_bits( stream, out header.ack, 16 );
        read_bits( stream, out header.ack_bits, 32 );
        read_bits( stream, out header.frameNumber, 32 );
        read_bits( stream, out header.resetSequence, 16 );
        read_float( stream, out header.avatarSampleTimeOffset );

        read_int( stream, out numAvatarStates, 0, Constants.MaxClients );
        for ( int i = 0; i < numAvatarStates; ++i )
        {
            read_avatar_state( stream, out avatarState[i] );
        }

        read_int( stream, out numStateUpdates, 0, Constants.MaxStateUpdates );

        for ( int i = 0; i < numStateUpdates; ++i )
        {
            hasDelta[i] = false;
            perfectPrediction[i] = false;
            hasPredictionDelta[i] = false;

            read_int( stream, out cubeIds[i], 0, Constants.NumCubes - 1 );

#if DEBUG_DELTA_COMPRESSION
            read_int( stream, out cubeDelta[i].absolute_position_x, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
            read_int( stream, out cubeDelta[i].absolute_position_y, Constants.PositionMinimumY, Constants.PositionMaximumY );
            read_int( stream, out cubeDelta[i].absolute_position_z, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
#endif // #if DEBUG_DELTA_COMPRESSION

            read_int( stream, out cubeState[i].authorityIndex, 0, Constants.MaxAuthority - 1 );
            read_bits( stream, out cubeState[i].authoritySequence, 16 );
            read_bits( stream, out cubeState[i].ownershipSequence, 16 );

            read_bool( stream, out notChanged[i] );

            if ( notChanged[i] )
            {
                read_bits( stream, out baselineSequence[i], 16 );
            }
            else
            {
                read_bool( stream, out perfectPrediction[i] );

                if ( perfectPrediction[i] )
                {
                    read_bits( stream, out baselineSequence[i], 16 );

                    read_bits( stream, out cubeState[i].rotation_largest, 2 );
                    read_bits( stream, out cubeState[i].rotation_a, Constants.RotationBits );
                    read_bits( stream, out cubeState[i].rotation_b, Constants.RotationBits );
                    read_bits( stream, out cubeState[i].rotation_c, Constants.RotationBits );

                    cubeState[i].active = true;
                }
                else
                {
                    read_bool( stream, out hasPredictionDelta[i] );

                    if ( hasPredictionDelta[i] )
                    {
                        read_bits( stream, out baselineSequence[i], 16 );

                        read_bool( stream, out cubeState[i].active );

                        read_linear_velocity_delta( stream, out predictionDelta[i].linear_velocity_delta_x, out predictionDelta[i].linear_velocity_delta_y, out predictionDelta[i].linear_velocity_delta_z );

                        read_angular_velocity_delta( stream, out predictionDelta[i].angular_velocity_delta_x, out predictionDelta[i].angular_velocity_delta_y, out predictionDelta[i].angular_velocity_delta_z );

                        read_position_delta( stream, out predictionDelta[i].position_delta_x, out predictionDelta[i].position_delta_y, out predictionDelta[i].position_delta_z );

                        read_bits( stream, out cubeState[i].rotation_largest, 2 );
                        read_bits( stream, out cubeState[i].rotation_a, Constants.RotationBits );
                        read_bits( stream, out cubeState[i].rotation_b, Constants.RotationBits );
                        read_bits( stream, out cubeState[i].rotation_c, Constants.RotationBits );
                    }
                    else
                    {
                        read_bool( stream, out hasDelta[i] );

                        if ( hasDelta[i] )
                        {
                            read_bits( stream, out baselineSequence[i], 16 );

                            read_bool( stream, out cubeState[i].active );

                            read_linear_velocity_delta( stream, out cubeDelta[i].linear_velocity_delta_x, out cubeDelta[i].linear_velocity_delta_y, out cubeDelta[i].linear_velocity_delta_z );

                            read_angular_velocity_delta( stream, out cubeDelta[i].angular_velocity_delta_x, out cubeDelta[i].angular_velocity_delta_y, out cubeDelta[i].angular_velocity_delta_z );

                            read_position_delta( stream, out cubeDelta[i].position_delta_x, out cubeDelta[i].position_delta_y, out cubeDelta[i].position_delta_z );

                            read_bits( stream, out cubeState[i].rotation_largest, 2 );
                            read_bits( stream, out cubeState[i].rotation_a, Constants.RotationBits );
                            read_bits( stream, out cubeState[i].rotation_b, Constants.RotationBits );
                            read_bits( stream, out cubeState[i].rotation_c, Constants.RotationBits );
                        }
                        else
                        {
                            read_bool( stream, out cubeState[i].active );

                            read_int( stream, out cubeState[i].position_x, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
                            read_int( stream, out cubeState[i].position_y, Constants.PositionMinimumY, Constants.PositionMaximumY );
                            read_int( stream, out cubeState[i].position_z, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );

                            read_bits( stream, out cubeState[i].rotation_largest, 2 );
                            read_bits( stream, out cubeState[i].rotation_a, Constants.RotationBits );
                            read_bits( stream, out cubeState[i].rotation_b, Constants.RotationBits );
                            read_bits( stream, out cubeState[i].rotation_c, Constants.RotationBits );

                            if ( cubeState[i].active )
                            {
                                read_int( stream, out cubeState[i].linear_velocity_x, Constants.LinearVelocityMinimum, Constants.LinearVelocityMaximum );
                                read_int( stream, out cubeState[i].linear_velocity_y, Constants.LinearVelocityMinimum, Constants.LinearVelocityMaximum );
                                read_int( stream, out cubeState[i].linear_velocity_z, Constants.LinearVelocityMinimum, Constants.LinearVelocityMaximum );

                                read_int( stream, out cubeState[i].angular_velocity_x, Constants.AngularVelocityMinimum, Constants.AngularVelocityMaximum );
                                read_int( stream, out cubeState[i].angular_velocity_y, Constants.AngularVelocityMinimum, Constants.AngularVelocityMaximum );
                                read_int( stream, out cubeState[i].angular_velocity_z, Constants.AngularVelocityMinimum, Constants.AngularVelocityMaximum );
                            }
                            else
                            {
                                cubeState[i].linear_velocity_x = 0;
                                cubeState[i].linear_velocity_y = 0;
                                cubeState[i].linear_velocity_z = 0;

                                cubeState[i].angular_velocity_x = 0;
                                cubeState[i].angular_velocity_y = 0;
                                cubeState[i].angular_velocity_z = 0;
                            }
                        }
                    }
                }
            }
        }
    }

    void write_position_delta( Network.WriteStream stream, int delta_x, int delta_y, int delta_z )
    {
        Assert.IsTrue( delta_x >= -Constants.PositionDeltaMax );
        Assert.IsTrue( delta_x <= +Constants.PositionDeltaMax );
        Assert.IsTrue( delta_y >= -Constants.PositionDeltaMax );
        Assert.IsTrue( delta_y <= +Constants.PositionDeltaMax );
        Assert.IsTrue( delta_z >= -Constants.PositionDeltaMax );
        Assert.IsTrue( delta_z <= +Constants.PositionDeltaMax );

        uint unsigned_x = Network.Util.SignedToUnsigned( delta_x );
        uint unsigned_y = Network.Util.SignedToUnsigned( delta_y );
        uint unsigned_z = Network.Util.SignedToUnsigned( delta_z );

        bool small_x = unsigned_x <= Constants.PositionDeltaSmallThreshold;
        bool small_y = unsigned_y <= Constants.PositionDeltaSmallThreshold;
        bool small_z = unsigned_z <= Constants.PositionDeltaSmallThreshold;

        bool all_small = small_x && small_y && small_z;

        write_bool( stream, all_small );

        if ( all_small )
        {
            write_bits( stream, unsigned_x, Constants.PositionDeltaSmallBits );
            write_bits( stream, unsigned_y, Constants.PositionDeltaSmallBits );
            write_bits( stream, unsigned_z, Constants.PositionDeltaSmallBits );
        }
        else
        {
            write_bool( stream, small_x );

            if ( small_x )
            {
                write_bits( stream, unsigned_x, Constants.PositionDeltaSmallBits );
            }
            else
            {
                unsigned_x -= Constants.PositionDeltaSmallThreshold;

                bool medium_x = unsigned_x < Constants.PositionDeltaMediumThreshold;

                write_bool( stream, medium_x );

                if ( medium_x )
                {
                    write_bits( stream, unsigned_x, Constants.PositionDeltaMediumBits );
                }
                else
                {
                    write_int( stream, delta_x, -Constants.PositionDeltaMax, +Constants.PositionDeltaMax );
                }
            }

            write_bool( stream, small_y );

            if ( small_y )
            {
                write_bits( stream, unsigned_y, Constants.PositionDeltaSmallBits );
            }
            else
            {
                unsigned_y -= Constants.PositionDeltaSmallThreshold;

                bool medium_y = unsigned_y < Constants.PositionDeltaMediumThreshold;

                write_bool( stream, medium_y );

                if ( medium_y )
                {
                    write_bits( stream, unsigned_y, Constants.PositionDeltaMediumBits );
                }
                else
                {
                    write_int( stream, delta_y, -Constants.PositionDeltaMax, +Constants.PositionDeltaMax );
                }
            }

            write_bool( stream, small_z );

            if ( small_z )
            {
                write_bits( stream, unsigned_z, Constants.PositionDeltaSmallBits );
            }
            else
            {
                unsigned_z -= Constants.PositionDeltaSmallThreshold;

                bool medium_z = unsigned_z < Constants.PositionDeltaMediumThreshold;

                write_bool( stream, medium_z );

                if ( medium_z )
                {
                    write_bits( stream, unsigned_z, Constants.PositionDeltaMediumBits );
                }
                else
                {
                    write_int( stream, delta_z, -Constants.PositionDeltaMax, +Constants.PositionDeltaMax );
                }
            }
        }
    }

    void read_position_delta( Network.ReadStream stream, out int delta_x, out int delta_y, out int delta_z )
    {
        bool all_small;

        read_bool( stream, out all_small );

        uint unsigned_x;
        uint unsigned_y;
        uint unsigned_z;

        if ( all_small )
        {
            read_bits( stream, out unsigned_x, Constants.PositionDeltaSmallBits );
            read_bits( stream, out unsigned_y, Constants.PositionDeltaSmallBits );
            read_bits( stream, out unsigned_z, Constants.PositionDeltaSmallBits );

            delta_x = Network.Util.UnsignedToSigned( unsigned_x );
            delta_y = Network.Util.UnsignedToSigned( unsigned_y );
            delta_z = Network.Util.UnsignedToSigned( unsigned_z );
        }
        else
        {
            bool small_x;

            read_bool( stream, out small_x );

            if ( small_x )
            {
                read_bits( stream, out unsigned_x, Constants.PositionDeltaSmallBits );

                delta_x = Network.Util.UnsignedToSigned( unsigned_x );
            }
            else
            {
                bool medium_x;

                read_bool( stream, out medium_x );

                if ( medium_x )
                {
                    read_bits( stream, out unsigned_x, Constants.PositionDeltaMediumBits );

                    delta_x = Network.Util.UnsignedToSigned( unsigned_x + Constants.PositionDeltaSmallThreshold );
                }
                else
                {
                    read_int( stream, out delta_x, -Constants.PositionDeltaMax, +Constants.PositionDeltaMax );
                }
            }

            bool small_y;

            read_bool( stream, out small_y );

            if ( small_y )
            {
                read_bits( stream, out unsigned_y, Constants.PositionDeltaSmallBits );

                delta_y = Network.Util.UnsignedToSigned( unsigned_y );
            }
            else
            {
                bool medium_y;

                read_bool( stream, out medium_y );

                if ( medium_y )
                {
                    read_bits( stream, out unsigned_y, Constants.PositionDeltaMediumBits );

                    delta_y = Network.Util.UnsignedToSigned( unsigned_y + Constants.PositionDeltaSmallThreshold );
                }
                else
                {
                    read_int( stream, out delta_y, -Constants.PositionDeltaMax, +Constants.PositionDeltaMax );
                }
            }

            bool small_z;

            read_bool( stream, out small_z );

            if ( small_z )
            {
                read_bits( stream, out unsigned_z, Constants.PositionDeltaSmallBits );

                delta_z = Network.Util.UnsignedToSigned( unsigned_z );
            }
            else
            {
                bool medium_z;

                read_bool( stream, out medium_z );

                if ( medium_z )
                {
                    read_bits( stream, out unsigned_z, Constants.PositionDeltaMediumBits );

                    delta_z = Network.Util.UnsignedToSigned( unsigned_z + Constants.PositionDeltaSmallThreshold );
                }
                else
                {
                    read_int( stream, out delta_z, -Constants.PositionDeltaMax, +Constants.PositionDeltaMax );
                }
            }
        }
    }

    void write_linear_velocity_delta( Network.WriteStream stream, int delta_x, int delta_y, int delta_z )
    {
        Assert.IsTrue( delta_x >= -Constants.LinearVelocityDeltaMax );
        Assert.IsTrue( delta_x <= +Constants.LinearVelocityDeltaMax );
        Assert.IsTrue( delta_y >= -Constants.LinearVelocityDeltaMax );
        Assert.IsTrue( delta_y <= +Constants.LinearVelocityDeltaMax );
        Assert.IsTrue( delta_z >= -Constants.LinearVelocityDeltaMax );
        Assert.IsTrue( delta_z <= +Constants.LinearVelocityDeltaMax );

        uint unsigned_x = Network.Util.SignedToUnsigned( delta_x );
        uint unsigned_y = Network.Util.SignedToUnsigned( delta_y );
        uint unsigned_z = Network.Util.SignedToUnsigned( delta_z );

        bool small_x = unsigned_x <= Constants.LinearVelocityDeltaSmallThreshold;
        bool small_y = unsigned_y <= Constants.LinearVelocityDeltaSmallThreshold;
        bool small_z = unsigned_z <= Constants.LinearVelocityDeltaSmallThreshold;

        bool all_small = small_x && small_y && small_z;

        write_bool( stream, all_small );

        if ( all_small )
        {
            write_bits( stream, unsigned_x, Constants.LinearVelocityDeltaSmallBits );
            write_bits( stream, unsigned_y, Constants.LinearVelocityDeltaSmallBits );
            write_bits( stream, unsigned_z, Constants.LinearVelocityDeltaSmallBits );
        }
        else
        {
            write_bool( stream, small_x );

            if ( small_x )
            {
                write_bits( stream, unsigned_x, Constants.LinearVelocityDeltaSmallBits );
            }
            else
            {
                unsigned_x -= Constants.LinearVelocityDeltaSmallThreshold;

                bool medium_x = unsigned_x < Constants.LinearVelocityDeltaMediumThreshold;

                write_bool( stream, medium_x );

                if ( medium_x )
                {
                    write_bits( stream, unsigned_x, Constants.LinearVelocityDeltaMediumBits );
                }
                else
                {
                    write_int( stream, delta_x, -Constants.LinearVelocityDeltaMax, +Constants.LinearVelocityDeltaMax );
                }
            }

            write_bool( stream, small_y );

            if ( small_y )
            {
                write_bits( stream, unsigned_y, Constants.LinearVelocityDeltaSmallBits );
            }
            else
            {
                unsigned_y -= Constants.LinearVelocityDeltaSmallThreshold;

                bool medium_y = unsigned_y < Constants.LinearVelocityDeltaMediumThreshold;

                write_bool( stream, medium_y );

                if ( medium_y )
                {
                    write_bits( stream, unsigned_y, Constants.LinearVelocityDeltaMediumBits );
                }
                else
                {
                    write_int( stream, delta_y, -Constants.LinearVelocityDeltaMax, +Constants.LinearVelocityDeltaMax );
                }
            }

            write_bool( stream, small_z );

            if ( small_z )
            {
                write_bits( stream, unsigned_z, Constants.LinearVelocityDeltaSmallBits );
            }
            else
            {
                unsigned_z -= Constants.LinearVelocityDeltaSmallThreshold;

                bool medium_z = unsigned_z < Constants.LinearVelocityDeltaMediumThreshold;

                write_bool( stream, medium_z );

                if ( medium_z )
                {
                    write_bits( stream, unsigned_z, Constants.LinearVelocityDeltaMediumBits );
                }
                else
                {
                    write_int( stream, delta_z, -Constants.LinearVelocityDeltaMax, +Constants.LinearVelocityDeltaMax );
                }
            }
        }
    }

    void read_linear_velocity_delta( Network.ReadStream stream, out int delta_x, out int delta_y, out int delta_z )
    {
        bool all_small;

        read_bool( stream, out all_small );

        uint unsigned_x;
        uint unsigned_y;
        uint unsigned_z;

        if ( all_small )
        {
            read_bits( stream, out unsigned_x, Constants.LinearVelocityDeltaSmallBits );
            read_bits( stream, out unsigned_y, Constants.LinearVelocityDeltaSmallBits );
            read_bits( stream, out unsigned_z, Constants.LinearVelocityDeltaSmallBits );

            delta_x = Network.Util.UnsignedToSigned( unsigned_x );
            delta_y = Network.Util.UnsignedToSigned( unsigned_y );
            delta_z = Network.Util.UnsignedToSigned( unsigned_z );
        }
        else
        {
            bool small_x;

            read_bool( stream, out small_x );

            if ( small_x )
            {
                read_bits( stream, out unsigned_x, Constants.LinearVelocityDeltaSmallBits );

                delta_x = Network.Util.UnsignedToSigned( unsigned_x );
            }
            else
            {
                bool medium_x;

                read_bool( stream, out medium_x );

                if ( medium_x )
                {
                    read_bits( stream, out unsigned_x, Constants.LinearVelocityDeltaMediumBits );

                    delta_x = Network.Util.UnsignedToSigned( unsigned_x + Constants.LinearVelocityDeltaSmallThreshold );
                }
                else
                {
                    read_int( stream, out delta_x, -Constants.LinearVelocityDeltaMax, +Constants.LinearVelocityDeltaMax );
                }
            }

            bool small_y;

            read_bool( stream, out small_y );

            if ( small_y )
            {
                read_bits( stream, out unsigned_y, Constants.LinearVelocityDeltaSmallBits );

                delta_y = Network.Util.UnsignedToSigned( unsigned_y );
            }
            else
            {
                bool medium_y;

                read_bool( stream, out medium_y );

                if ( medium_y )
                {
                    read_bits( stream, out unsigned_y, Constants.LinearVelocityDeltaMediumBits );

                    delta_y = Network.Util.UnsignedToSigned( unsigned_y + Constants.LinearVelocityDeltaSmallThreshold );
                }
                else
                {
                    read_int( stream, out delta_y, -Constants.LinearVelocityDeltaMax, +Constants.LinearVelocityDeltaMax );
                }
            }

            bool small_z;

            read_bool( stream, out small_z );

            if ( small_z )
            {
                read_bits( stream, out unsigned_z, Constants.LinearVelocityDeltaSmallBits );

                delta_z = Network.Util.UnsignedToSigned( unsigned_z );
            }
            else
            {
                bool medium_z;

                read_bool( stream, out medium_z );

                if ( medium_z )
                {
                    read_bits( stream, out unsigned_z, Constants.LinearVelocityDeltaMediumBits );

                    delta_z = Network.Util.UnsignedToSigned( unsigned_z + Constants.LinearVelocityDeltaSmallThreshold );
                }
                else
                {
                    read_int( stream, out delta_z, -Constants.LinearVelocityDeltaMax, +Constants.LinearVelocityDeltaMax );
                }
            }
        }
    }

    void write_angular_velocity_delta( Network.WriteStream stream, int delta_x, int delta_y, int delta_z )
    {
        Assert.IsTrue( delta_x >= -Constants.AngularVelocityDeltaMax );
        Assert.IsTrue( delta_x <= +Constants.AngularVelocityDeltaMax );
        Assert.IsTrue( delta_y >= -Constants.AngularVelocityDeltaMax );
        Assert.IsTrue( delta_y <= +Constants.AngularVelocityDeltaMax );
        Assert.IsTrue( delta_z >= -Constants.AngularVelocityDeltaMax );
        Assert.IsTrue( delta_z <= +Constants.AngularVelocityDeltaMax );

        uint unsigned_x = Network.Util.SignedToUnsigned( delta_x );
        uint unsigned_y = Network.Util.SignedToUnsigned( delta_y );
        uint unsigned_z = Network.Util.SignedToUnsigned( delta_z );

        bool small_x = unsigned_x <= Constants.AngularVelocityDeltaSmallThreshold;
        bool small_y = unsigned_y <= Constants.AngularVelocityDeltaSmallThreshold;
        bool small_z = unsigned_z <= Constants.AngularVelocityDeltaSmallThreshold;

        bool all_small = small_x && small_y && small_z;

        write_bool( stream, all_small );

        if ( all_small )
        {
            write_bits( stream, unsigned_x, Constants.AngularVelocityDeltaSmallBits );
            write_bits( stream, unsigned_y, Constants.AngularVelocityDeltaSmallBits );
            write_bits( stream, unsigned_z, Constants.AngularVelocityDeltaSmallBits );
        }
        else
        {
            write_bool( stream, small_x );

            if ( small_x )
            {
                write_bits( stream, unsigned_x, Constants.AngularVelocityDeltaSmallBits );
            }
            else
            {
                unsigned_x -= Constants.AngularVelocityDeltaSmallThreshold;

                bool medium_x = unsigned_x < Constants.AngularVelocityDeltaMediumThreshold;

                write_bool( stream, medium_x );

                if ( medium_x )
                {
                    write_bits( stream, unsigned_x, Constants.AngularVelocityDeltaMediumBits );
                }
                else
                {
                    write_int( stream, delta_x, -Constants.AngularVelocityDeltaMax, +Constants.AngularVelocityDeltaMax );
                }
            }

            write_bool( stream, small_y );

            if ( small_y )
            {
                write_bits( stream, unsigned_y, Constants.AngularVelocityDeltaSmallBits );
            }
            else
            {
                unsigned_y -= Constants.AngularVelocityDeltaSmallThreshold;

                bool medium_y = unsigned_y < Constants.AngularVelocityDeltaMediumThreshold;

                write_bool( stream, medium_y );

                if ( medium_y )
                {
                    write_bits( stream, unsigned_y, Constants.AngularVelocityDeltaMediumBits );
                }
                else
                {
                    write_int( stream, delta_y, -Constants.AngularVelocityDeltaMax, +Constants.AngularVelocityDeltaMax );
                }
            }

            write_bool( stream, small_z );

            if ( small_z )
            {
                write_bits( stream, unsigned_z, Constants.AngularVelocityDeltaSmallBits );
            }
            else
            {
                unsigned_z -= Constants.AngularVelocityDeltaSmallThreshold;

                bool medium_z = unsigned_z < Constants.AngularVelocityDeltaMediumThreshold;

                write_bool( stream, medium_z );

                if ( medium_z )
                {
                    write_bits( stream, unsigned_z, Constants.AngularVelocityDeltaMediumBits );
                }
                else
                {
                    write_int( stream, delta_z, -Constants.AngularVelocityDeltaMax, +Constants.AngularVelocityDeltaMax );
                }
            }
        }
    }

    void read_angular_velocity_delta( Network.ReadStream stream, out int delta_x, out int delta_y, out int delta_z )
    {
        bool all_small;

        read_bool( stream, out all_small );

        uint unsigned_x;
        uint unsigned_y;
        uint unsigned_z;

        if ( all_small )
        {
            read_bits( stream, out unsigned_x, Constants.AngularVelocityDeltaSmallBits );
            read_bits( stream, out unsigned_y, Constants.AngularVelocityDeltaSmallBits );
            read_bits( stream, out unsigned_z, Constants.AngularVelocityDeltaSmallBits );

            delta_x = Network.Util.UnsignedToSigned( unsigned_x );
            delta_y = Network.Util.UnsignedToSigned( unsigned_y );
            delta_z = Network.Util.UnsignedToSigned( unsigned_z );
        }
        else
        {
            bool small_x;

            read_bool( stream, out small_x );

            if ( small_x )
            {
                read_bits( stream, out unsigned_x, Constants.AngularVelocityDeltaSmallBits );

                delta_x = Network.Util.UnsignedToSigned( unsigned_x );
            }
            else
            {
                bool medium_x;

                read_bool( stream, out medium_x );

                if ( medium_x )
                {
                    read_bits( stream, out unsigned_x, Constants.AngularVelocityDeltaMediumBits );

                    delta_x = Network.Util.UnsignedToSigned( unsigned_x + Constants.AngularVelocityDeltaSmallThreshold );
                }
                else
                {
                    read_int( stream, out delta_x, -Constants.AngularVelocityDeltaMax, +Constants.AngularVelocityDeltaMax );
                }
            }

            bool small_y;

            read_bool( stream, out small_y );

            if ( small_y )
            {
                read_bits( stream, out unsigned_y, Constants.AngularVelocityDeltaSmallBits );

                delta_y = Network.Util.UnsignedToSigned( unsigned_y );
            }
            else
            {
                bool medium_y;

                read_bool( stream, out medium_y );

                if ( medium_y )
                {
                    read_bits( stream, out unsigned_y, Constants.AngularVelocityDeltaMediumBits );

                    delta_y = Network.Util.UnsignedToSigned( unsigned_y + Constants.AngularVelocityDeltaSmallThreshold );
                }
                else
                {
                    read_int( stream, out delta_y, -Constants.AngularVelocityDeltaMax, +Constants.AngularVelocityDeltaMax );
                }
            }

            bool small_z;

            read_bool( stream, out small_z );

            if ( small_z )
            {
                read_bits( stream, out unsigned_z, Constants.AngularVelocityDeltaSmallBits );

                delta_z = Network.Util.UnsignedToSigned( unsigned_z );
            }
            else
            {
                bool medium_z;

                read_bool( stream, out medium_z );

                if ( medium_z )
                {
                    read_bits( stream, out unsigned_z, Constants.AngularVelocityDeltaMediumBits );

                    delta_z = Network.Util.UnsignedToSigned( unsigned_z + Constants.AngularVelocityDeltaSmallThreshold );
                }
                else
                {
                    read_int( stream, out delta_z, -Constants.AngularVelocityDeltaMax, +Constants.AngularVelocityDeltaMax );
                }
            }
        }
    }

    void write_avatar_state( Network.WriteStream stream, ref AvatarStateQuantized avatarState )
    {
        write_int( stream, avatarState.client_index, 0, Constants.MaxClients - 1 );

        write_int( stream, avatarState.head_position_x, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
        write_int( stream, avatarState.head_position_y, Constants.PositionMinimumY, Constants.PositionMaximumY );
        write_int( stream, avatarState.head_position_z, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );

        write_bits( stream, avatarState.head_rotation_largest, 2 );
        write_bits( stream, avatarState.head_rotation_a, Constants.RotationBits );
        write_bits( stream, avatarState.head_rotation_b, Constants.RotationBits );
        write_bits( stream, avatarState.head_rotation_c, Constants.RotationBits );

        write_int( stream, avatarState.left_hand_position_x, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
        write_int( stream, avatarState.left_hand_position_y, Constants.PositionMinimumY, Constants.PositionMaximumY );
        write_int( stream, avatarState.left_hand_position_z, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );

        write_bits( stream, avatarState.left_hand_rotation_largest, 2 );
        write_bits( stream, avatarState.left_hand_rotation_a, Constants.RotationBits );
        write_bits( stream, avatarState.left_hand_rotation_b, Constants.RotationBits );
        write_bits( stream, avatarState.left_hand_rotation_c, Constants.RotationBits );

        write_int( stream, avatarState.left_hand_grip_trigger, Constants.TriggerMinimum, Constants.TriggerMaximum );
        write_int( stream, avatarState.left_hand_index_trigger, Constants.TriggerMinimum, Constants.TriggerMaximum );
        write_bool( stream, avatarState.left_hand_pointing );
        write_bool( stream, avatarState.left_hand_thumbs_up );

        write_bool( stream, avatarState.left_hand_holding_cube );

        if ( avatarState.left_hand_holding_cube )
        {
            write_int( stream, avatarState.left_hand_cube_id, 0, Constants.NumCubes - 1 );
            write_bits( stream, avatarState.left_hand_authority_sequence, 16 );
            write_bits( stream, avatarState.left_hand_ownership_sequence, 16 );

            write_int( stream, avatarState.left_hand_cube_local_position_x, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );
            write_int( stream, avatarState.left_hand_cube_local_position_y, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );
            write_int( stream, avatarState.left_hand_cube_local_position_z, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );

            write_bits( stream, avatarState.left_hand_cube_local_rotation_largest, 2 );
            write_bits( stream, avatarState.left_hand_cube_local_rotation_a, Constants.RotationBits );
            write_bits( stream, avatarState.left_hand_cube_local_rotation_b, Constants.RotationBits );
            write_bits( stream, avatarState.left_hand_cube_local_rotation_c, Constants.RotationBits );
        }

        write_int( stream, avatarState.right_hand_position_x, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
        write_int( stream, avatarState.right_hand_position_y, Constants.PositionMinimumY, Constants.PositionMaximumY );
        write_int( stream, avatarState.right_hand_position_z, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );

        write_bits( stream, avatarState.right_hand_rotation_largest, 2 );
        write_bits( stream, avatarState.right_hand_rotation_a, Constants.RotationBits );
        write_bits( stream, avatarState.right_hand_rotation_b, Constants.RotationBits );
        write_bits( stream, avatarState.right_hand_rotation_c, Constants.RotationBits );

        write_int( stream, avatarState.right_hand_grip_trigger, Constants.TriggerMinimum, Constants.TriggerMaximum );
        write_int( stream, avatarState.right_hand_index_trigger, Constants.TriggerMinimum, Constants.TriggerMaximum );
        write_bool( stream, avatarState.right_hand_pointing );
        write_bool( stream, avatarState.right_hand_thumbs_up );

        write_bool( stream, avatarState.right_hand_holding_cube );

        if ( avatarState.right_hand_holding_cube )
        {
            write_int( stream, avatarState.right_hand_cube_id, 0, Constants.NumCubes - 1 );
            write_bits( stream, avatarState.right_hand_authority_sequence, 16 );
            write_bits( stream, avatarState.right_hand_ownership_sequence, 16 );

            write_int( stream, avatarState.right_hand_cube_local_position_x, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );
            write_int( stream, avatarState.right_hand_cube_local_position_y, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );
            write_int( stream, avatarState.right_hand_cube_local_position_z, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );

            write_bits( stream, avatarState.right_hand_cube_local_rotation_largest, 2 );
            write_bits( stream, avatarState.right_hand_cube_local_rotation_a, Constants.RotationBits );
            write_bits( stream, avatarState.right_hand_cube_local_rotation_b, Constants.RotationBits );
            write_bits( stream, avatarState.right_hand_cube_local_rotation_c, Constants.RotationBits );
        }

        write_int( stream, avatarState.voice_amplitude, Constants.VoiceMinimum, Constants.VoiceMaximum );
    }

    void read_avatar_state( Network.ReadStream stream, out AvatarStateQuantized avatarState )
    {
        read_int( stream, out avatarState.client_index, 0, Constants.MaxClients - 1 );

        read_int( stream, out avatarState.head_position_x, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
        read_int( stream, out avatarState.head_position_y, Constants.PositionMinimumY, Constants.PositionMaximumY );
        read_int( stream, out avatarState.head_position_z, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );

        read_bits( stream, out avatarState.head_rotation_largest, 2 );
        read_bits( stream, out avatarState.head_rotation_a, Constants.RotationBits );
        read_bits( stream, out avatarState.head_rotation_b, Constants.RotationBits );
        read_bits( stream, out avatarState.head_rotation_c, Constants.RotationBits );

        read_int( stream, out avatarState.left_hand_position_x, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
        read_int( stream, out avatarState.left_hand_position_y, Constants.PositionMinimumY, Constants.PositionMaximumY );
        read_int( stream, out avatarState.left_hand_position_z, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );

        read_bits( stream, out avatarState.left_hand_rotation_largest, 2 );
        read_bits( stream, out avatarState.left_hand_rotation_a, Constants.RotationBits );
        read_bits( stream, out avatarState.left_hand_rotation_b, Constants.RotationBits );
        read_bits( stream, out avatarState.left_hand_rotation_c, Constants.RotationBits );

        read_int( stream, out avatarState.left_hand_grip_trigger, Constants.TriggerMinimum, Constants.TriggerMaximum );
        read_int( stream, out avatarState.left_hand_index_trigger, Constants.TriggerMinimum, Constants.TriggerMaximum );
        read_bool( stream, out avatarState.left_hand_pointing );
        read_bool( stream, out avatarState.left_hand_thumbs_up );

        read_bool( stream, out avatarState.left_hand_holding_cube );

        if ( avatarState.left_hand_holding_cube )
        {
            read_int( stream, out avatarState.left_hand_cube_id, 0, Constants.NumCubes - 1 );
            read_bits( stream, out avatarState.left_hand_authority_sequence, 16 );
            read_bits( stream, out avatarState.left_hand_ownership_sequence, 16 );

            read_int( stream, out avatarState.left_hand_cube_local_position_x, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );
            read_int( stream, out avatarState.left_hand_cube_local_position_y, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );
            read_int( stream, out avatarState.left_hand_cube_local_position_z, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );

            read_bits( stream, out avatarState.left_hand_cube_local_rotation_largest, 2 );
            read_bits( stream, out avatarState.left_hand_cube_local_rotation_a, Constants.RotationBits );
            read_bits( stream, out avatarState.left_hand_cube_local_rotation_b, Constants.RotationBits );
            read_bits( stream, out avatarState.left_hand_cube_local_rotation_c, Constants.RotationBits );
        }
        else
        {
            avatarState.left_hand_cube_id = 0;
            avatarState.left_hand_authority_sequence = 0;
            avatarState.left_hand_ownership_sequence = 0;
            avatarState.left_hand_cube_local_position_x = 0;
            avatarState.left_hand_cube_local_position_y = 0;
            avatarState.left_hand_cube_local_position_z = 0;
            avatarState.left_hand_cube_local_rotation_largest = 0;
            avatarState.left_hand_cube_local_rotation_a = 0;
            avatarState.left_hand_cube_local_rotation_b = 0;
            avatarState.left_hand_cube_local_rotation_c = 0;
        }

        read_int( stream, out avatarState.right_hand_position_x, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );
        read_int( stream, out avatarState.right_hand_position_y, Constants.PositionMinimumY, Constants.PositionMaximumY );
        read_int( stream, out avatarState.right_hand_position_z, Constants.PositionMinimumXZ, Constants.PositionMaximumXZ );

        read_bits( stream, out avatarState.right_hand_rotation_largest, 2 );
        read_bits( stream, out avatarState.right_hand_rotation_a, Constants.RotationBits );
        read_bits( stream, out avatarState.right_hand_rotation_b, Constants.RotationBits );
        read_bits( stream, out avatarState.right_hand_rotation_c, Constants.RotationBits );

        read_int( stream, out avatarState.right_hand_grip_trigger, Constants.TriggerMinimum, Constants.TriggerMaximum );
        read_int( stream, out avatarState.right_hand_index_trigger, Constants.TriggerMinimum, Constants.TriggerMaximum );
        read_bool( stream, out avatarState.right_hand_pointing );
        read_bool( stream, out avatarState.right_hand_thumbs_up );

        read_bool( stream, out avatarState.right_hand_holding_cube );

        if ( avatarState.right_hand_holding_cube )
        {
            read_int( stream, out avatarState.right_hand_cube_id, 0, Constants.NumCubes - 1 );
            read_bits( stream, out avatarState.right_hand_authority_sequence, 16 );
            read_bits( stream, out avatarState.right_hand_ownership_sequence, 16 );

            read_int( stream, out avatarState.right_hand_cube_local_position_x, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );
            read_int( stream, out avatarState.right_hand_cube_local_position_y, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );
            read_int( stream, out avatarState.right_hand_cube_local_position_z, Constants.LocalPositionMinimum, Constants.LocalPositionMaximum );

            read_bits( stream, out avatarState.right_hand_cube_local_rotation_largest, 2 );
            read_bits( stream, out avatarState.right_hand_cube_local_rotation_a, Constants.RotationBits );
            read_bits( stream, out avatarState.right_hand_cube_local_rotation_b, Constants.RotationBits );
            read_bits( stream, out avatarState.right_hand_cube_local_rotation_c, Constants.RotationBits );
        }
        else
        {
            avatarState.right_hand_cube_id = 0;
            avatarState.right_hand_authority_sequence = 0;
            avatarState.right_hand_ownership_sequence = 0;
            avatarState.right_hand_cube_local_position_x = 0;
            avatarState.right_hand_cube_local_position_y = 0;
            avatarState.right_hand_cube_local_position_z = 0;
            avatarState.right_hand_cube_local_rotation_largest = 0;
            avatarState.right_hand_cube_local_rotation_a = 0;
            avatarState.right_hand_cube_local_rotation_b = 0;
            avatarState.right_hand_cube_local_rotation_c = 0;
        }

        read_int( stream, out avatarState.voice_amplitude, Constants.VoiceMinimum, Constants.VoiceMaximum );
    }
}
