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
using UnityEngine.Profiling;
using UnityEngine.Assertions;

public class Loopback: Common
{
    public Context hostContext;
    public Context guestContext;

    Context currentContext;

    Context GetContext( int clientIndex )
    {
        switch ( clientIndex )
        {
            case 0: return hostContext;
            case 1: return guestContext;
            default: return null;
        }
    }

    void InitializeContexts()
    {
        for ( int clientIndex = 0; clientIndex < Constants.MaxClients; ++clientIndex )
        {
            Context context = GetContext( clientIndex );
            if ( context != null )
                context.Initialize( clientIndex );
        }
    }

    new void Awake()
    {
        InitializeContexts();
    }

    new void Start()
    {
        base.Start();

        SwitchToHostContext();

        networkSimulator.SetLatency( 50.0f );               // 100ms round trip
        networkSimulator.SetJitter( 50.0f );                // add a bunch of jitter!

#if DEBUG_DELTA_COMPRESSION
        networkSimulator.SetPacketLoss( 25.0f );
#endif // #if DEBUG_DELTA_COMPRESSION
    }

    void SwitchToHostContext()
    {
        if ( currentContext == hostContext )
            return;

        Profiler.BeginSample( "SwitchToHostContext" );

        hostContext.HideRemoteAvatar( 0 );
        hostContext.ShowRemoteAvatar( 1 );

        guestContext.ShowRemoteAvatar( 1 );
        guestContext.ShowRemoteAvatar( 1 );

        localAvatar.GetComponent<Avatar>().SetContext( hostContext.GetComponent<Context>() );
        localAvatar.transform.position = hostContext.GetRemoteAvatar( 0 ).gameObject.transform.position;
        localAvatar.transform.rotation = hostContext.GetRemoteAvatar( 0 ).gameObject.transform.rotation;

        currentContext = hostContext;

        Profiler.EndSample();
    }

    void SwitchToGuestContext()
    {
        if ( currentContext == guestContext )
            return;

        Profiler.BeginSample( "SwitchToGuestContext" );

        hostContext.ShowRemoteAvatar( 0 );
        hostContext.ShowRemoteAvatar( 1 );

        guestContext.ShowRemoteAvatar( 0 );
        guestContext.HideRemoteAvatar( 1 );

        localAvatar.GetComponent<Avatar>().SetContext( guestContext.GetComponent<Context>() );
        localAvatar.transform.position = guestContext.GetRemoteAvatar( 1 ).gameObject.transform.position;
        localAvatar.transform.rotation = guestContext.GetRemoteAvatar( 1 ).gameObject.transform.rotation;

        currentContext = guestContext;

        Profiler.EndSample();
    }

    new void Update()
    {
        base.Update();

        if ( Input.GetKeyDown( "return" ) )
        {
            hostContext.TestSmoothing();
        }

        // apply host avatar state at render time with interpolation

        for ( int i = 1; i < Constants.MaxClients; ++i )
        {
            Context context = hostContext;

            Context.ConnectionData connectionData = context.GetServerConnectionData( i );

            int fromClientIndex = i;
            int toClientIndex = 0;

            int numInterpolatedAvatarStates;
            ushort avatarResetSequence;
            if ( connectionData.jitterBuffer.GetInterpolatedAvatarState( ref interpolatedAvatarState, out numInterpolatedAvatarStates, out avatarResetSequence ) )
            {
                if ( avatarResetSequence == context.GetResetSequence() )
                {
                    context.ApplyAvatarStateUpdates( numInterpolatedAvatarStates, ref interpolatedAvatarState, fromClientIndex, toClientIndex );
                }
            }
        }

        // apply guest avatar state at render time with interpolation

        {
            Context context = guestContext;

            Context.ConnectionData connectionData = context.GetClientConnectionData();

            int fromClientIndex = 0;
            int toClientIndex = 1;

            int numInterpolatedAvatarStates;
            ushort avatarResetSequence;
            if ( connectionData.jitterBuffer.GetInterpolatedAvatarState( ref interpolatedAvatarState, out numInterpolatedAvatarStates, out avatarResetSequence ) )
            {
                if ( avatarResetSequence == context.GetResetSequence() )
                {
                    context.ApplyAvatarStateUpdates( numInterpolatedAvatarStates, ref interpolatedAvatarState, fromClientIndex, toClientIndex );
                }
            }
        }

        // advance jitter buffer time

        guestContext.GetClientConnectionData().jitterBuffer.AdvanceTime( Time.deltaTime );

        for ( int i = 1; i < Constants.MaxClients; ++i )
        {
            hostContext.GetServerConnectionData( i ).jitterBuffer.AdvanceTime( Time.deltaTime );
        }
    }

    new void FixedUpdate()
    {
        var avatar = localAvatar.GetComponent<Avatar>();

        bool reset = Input.GetKey( "space" ) || ( avatar.IsPressingIndex() && avatar.IsPressingX() );

        if ( reset )
        {
            hostContext.Reset();
            hostContext.IncreaseResetSequence();
        }

        if ( Input.GetKey( "1" ) || avatar.IsPressingX() )
        {
            SwitchToHostContext();
        }
        else if ( Input.GetKey( "2" ) || avatar.IsPressingY() )
        {
            SwitchToGuestContext();
        }

        MirrorLocalAvatarToRemote();

        hostContext.CheckForAtRestObjects();
        guestContext.CheckForAtRestObjects();

        byte[] serverToClientPacketData = GenerateStateUpdatePacket( hostContext, hostContext.GetServerConnectionData( 1 ), 0, 1, (float) ( physicsTime - renderTime ) );

        networkSimulator.SendPacket( 0, 1, serverToClientPacketData );

        byte[] clientToServerPacketData = GenerateStateUpdatePacket( guestContext, guestContext.GetClientConnectionData(), 1, 0, (float) ( physicsTime - renderTime ) );

        networkSimulator.SendPacket( 1, 0, clientToServerPacketData );

        networkSimulator.AdvanceTime( frameNumber * 1.0 / Constants.PhysicsFrameRate );

        while ( true )
        {
            int from, to;

            byte[] packetData = networkSimulator.ReceivePacket( out from, out to );

            if ( packetData == null )
                break;

            Context context = GetContext( to );

            if ( to == 0 )
            {
                Assert.IsTrue( from >= 1 );
                Assert.IsTrue( from < Constants.MaxClients );

                if ( enableJitterBuffer )
                {
                    AddStateUpdatePacketToJitterBuffer( context, context.GetServerConnectionData( from ), packetData );
                }
                else
                {
                    ProcessStateUpdatePacket( context, context.GetServerConnectionData( from ), packetData, from, to );
                }
            }
            else
            {
                Assert.IsTrue( from == 0 );

                if ( enableJitterBuffer )
                {
                    AddStateUpdatePacketToJitterBuffer( context, context.GetClientConnectionData(), packetData );
                }
                else
                {
                    ProcessStateUpdatePacket( context, context.GetClientConnectionData(), packetData, from, to );
                }
            }
        }

        // process packet from host jitter buffer

        for ( int from = 1; from < Constants.MaxClients; ++from )
        {
            const int to = 0;

            Context context = GetContext( to );

            ProcessStateUpdateFromJitterBuffer( context, context.GetServerConnectionData( from ), from, to, enableJitterBuffer );
        }

        // process packet from guest jitter buffer

        if ( enableJitterBuffer )
        {
            const int from = 0;
            const int to = 1;

            Context context = GetContext( to );

            ProcessStateUpdateFromJitterBuffer( context, context.GetClientConnectionData(), from, to, enableJitterBuffer );
        }

        // advance host remote frame number for each connected client

        for ( int i = 1; i < Constants.MaxClients; ++i )
        {
            Context context = GetContext( 0 );

            Context.ConnectionData connectionData = context.GetServerConnectionData( i );

            if ( !connectionData.firstRemotePacket )
                connectionData.remoteFrameNumber++;
        }

        // advance guest remote frame number
        {
            Context context = GetContext( 1 );

            Context.ConnectionData connectionData = context.GetClientConnectionData();

            if ( !connectionData.firstRemotePacket )
                connectionData.remoteFrameNumber++;
        }

        hostContext.CheckForAtRestObjects();
        guestContext.CheckForAtRestObjects();

        ProcessAcks();

        base.FixedUpdate();
    }

    void ProcessAcks()
    {
        Profiler.BeginSample( "Process Acks" );
        {
            // host context
            {
                Context context = GetContext( 0 );

                if ( context )
                {
                    for ( int i = 1; i < Constants.MaxClients; ++i )
                    {
                        Context.ConnectionData connectionData = context.GetServerConnectionData( i );

                        ProcessAcksForConnection( context, connectionData );
                    }
                }
            }

            // guest contexts

            for ( int clientIndex = 1; clientIndex < Constants.MaxClients; ++clientIndex )
            {
                Context context = GetContext( clientIndex );

                if ( !context )
                    continue;

                Context.ConnectionData connectionData = context.GetClientConnectionData();

                ProcessAcksForConnection( context, connectionData );
            }
        }

        Profiler.EndSample();
    }

    public byte[] GenerateStateUpdatePacket( Context context, Context.ConnectionData connectionData, int fromClientIndex, int toClientIndex, float avatarSampleTimeOffset = 0.0f )
    {
        Profiler.BeginSample( "GenerateStateUpdatePacket" );

        int maxStateUpdates = Math.Min( Constants.NumCubes, Constants.MaxStateUpdates );

        int numStateUpdates = maxStateUpdates;

        context.UpdateCubePriority();

        context.GetMostImportantCubeStateUpdates( connectionData, ref numStateUpdates, ref cubeIds, ref cubeState );

        Network.PacketHeader writePacketHeader;

        connectionData.connection.GeneratePacketHeader( out writePacketHeader );

        writePacketHeader.avatarSampleTimeOffset = avatarSampleTimeOffset;

        writePacketHeader.frameNumber = (uint) frameNumber;

        writePacketHeader.resetSequence = context.GetResetSequence();

        DetermineNotChangedAndDeltas( context, connectionData, writePacketHeader.sequence, numStateUpdates, ref cubeIds, ref notChanged, ref hasDelta, ref baselineSequence, ref cubeState, ref cubeDelta );

        DeterminePrediction( context, connectionData, writePacketHeader.sequence, numStateUpdates, ref cubeIds, ref notChanged, ref hasDelta, ref perfectPrediction, ref hasPredictionDelta, ref baselineSequence, ref cubeState, ref predictionDelta );

        int numAvatarStates = 0;

        if ( fromClientIndex == 0 )
        {
            // server -> client: send avatar state for other clients only

            numAvatarStates = 0;

            for ( int i = 0; i < Constants.MaxClients; ++i )
            {
                if ( i == toClientIndex )
                    continue;

                if ( currentContext == GetContext( i ) )
                {
                   // grab state from the local avatar.

                    localAvatar.GetComponent<Avatar>().GetAvatarState( out avatarState[numAvatarStates] );
                    numAvatarStates++;
                }
                else
                {
                    // grab state from a remote avatar.

                    var remoteAvatar = context.GetRemoteAvatar( i );
                    if ( remoteAvatar )
                    {
                        remoteAvatar.GetAvatarState( out avatarState[numAvatarStates] );
                        numAvatarStates++;
                    }
                }
            }
        }
        else
        {
            // client -> server: send avatar state for this client only

            numAvatarStates = 1;

            if ( currentContext == GetContext( fromClientIndex ) )
            {
                localAvatar.GetComponent<Avatar>().GetAvatarState( out avatarState[0] );
            }
            else
            {
                GetContext( fromClientIndex ).GetRemoteAvatar( fromClientIndex ).GetAvatarState( out avatarState[0] );
            }
        }

        for ( int i = 0; i < numAvatarStates; ++i )
            AvatarState.Quantize( ref avatarState[i], out avatarStateQuantized[i] );

        WriteStateUpdatePacket( ref writePacketHeader, numAvatarStates, ref avatarStateQuantized, numStateUpdates, ref cubeIds, ref notChanged, ref hasDelta, ref perfectPrediction, ref hasPredictionDelta, ref baselineSequence, ref cubeState, ref cubeDelta, ref predictionDelta );

        byte[] packetData = writeStream.GetData();

        // add the sent cube states to the send delta buffer

        AddPacketToDeltaBuffer( ref connectionData.sendDeltaBuffer, writePacketHeader.sequence, context.GetResetSequence(), numStateUpdates, ref cubeIds, ref cubeState );

        // reset cube priority for the cubes that were included in the packet (so other cubes have a chance to be sent...)

        context.ResetCubePriority( connectionData, numStateUpdates, cubeIds );

        Profiler.EndSample();

        return packetData;
    }

    public void ProcessStateUpdatePacket( Context context, Context.ConnectionData connectionData, byte[] packetData, int fromClientIndex, int toClientIndex )
    {
        Profiler.BeginSample( "ProcessStateUpdatePacket" );

        int readNumAvatarStates = 0;
        int readNumStateUpdates = 0;

        Network.PacketHeader readPacketHeader;

        if ( ReadStateUpdatePacket( packetData, out readPacketHeader, out readNumAvatarStates, ref readAvatarStateQuantized, out readNumStateUpdates, ref readCubeIds, ref readNotChanged, ref readHasDelta, ref readPerfectPrediction, ref readHasPredictionDelta, ref readBaselineSequence, ref readCubeState, ref readCubeDelta, ref readPredictionDelta ) )
        {
            // unquantize avatar states

            for ( int i = 0; i < readNumAvatarStates; ++i )
                AvatarState.Unquantize( ref readAvatarStateQuantized[i], out readAvatarState[i] );

            // reset sequence handling

            if ( fromClientIndex == 0 )
            {
                // server -> client

                // Ignore updates from before the last reset.
                if ( Network.Util.SequenceGreaterThan( context.GetResetSequence(), readPacketHeader.resetSequence ) )
                    return;

                // Reset if the server reset sequence is more recent than ours.
                if ( Network.Util.SequenceGreaterThan( readPacketHeader.resetSequence, context.GetResetSequence() ) )
                {
                    context.Reset();
                    context.SetResetSequence( readPacketHeader.resetSequence );
                }
            }
            else
            {
                // server -> client

                // Ignore any updates from the client with a different reset sequence #
                if ( context.GetResetSequence() != readPacketHeader.resetSequence )
                    return;
            }

            // decode the predicted cube states from baselines

            DecodePrediction( connectionData.receiveDeltaBuffer, context.GetResetSequence(), readPacketHeader.sequence, readNumStateUpdates, ref readCubeIds, ref readPerfectPrediction, ref readHasPredictionDelta, ref readBaselineSequence, ref readCubeState, ref readPredictionDelta );

            // decode the not changed and delta cube states from baselines

            DecodeNotChangedAndDeltas( connectionData.receiveDeltaBuffer, context.GetResetSequence(), readNumStateUpdates, ref readCubeIds, ref readNotChanged, ref readHasDelta, ref readBaselineSequence, ref readCubeState, ref readCubeDelta );

            // add the cube states to the receive delta buffer

            AddPacketToDeltaBuffer( ref connectionData.receiveDeltaBuffer, readPacketHeader.sequence, context.GetResetSequence(), readNumStateUpdates, ref readCubeIds, ref readCubeState );

            // apply the state updates to cubes

            context.ApplyCubeStateUpdates( readNumStateUpdates, ref readCubeIds, ref readCubeState, fromClientIndex, toClientIndex, enableJitterBuffer );

            // apply avatar state updates

            context.ApplyAvatarStateUpdates( readNumAvatarStates, ref readAvatarState, fromClientIndex, toClientIndex );

            // process the packet header

            connectionData.connection.ProcessPacketHeader( ref readPacketHeader );
        }

        Profiler.EndSample();
    }

    void MirrorLocalAvatarToRemote()
    {
        Profiler.BeginSample( "MirrorLocalAvatarToRemote" );

        // Mirror the local avatar onto its remote avatar on the current context.
        AvatarState avatarState;
        localAvatar.GetComponent<Avatar>().GetAvatarState( out avatarState );
        currentContext.GetRemoteAvatar( currentContext.GetClientIndex() ).ApplyAvatarPose( ref avatarState );

        Profiler.EndSample();
    }
}
