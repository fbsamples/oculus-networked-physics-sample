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
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Collections.Generic;

public class Guest: Common
{
    const double RetryTime = 0.0;//5.0;                                   // time between retry attempts.

    public Context context;

    enum GuestState
    {
        LoggingIn,                                                  // logging in to oculus platform SDK.
        Matchmaking,                                                // searching for a match
        Connecting,                                                 // connecting to the server
        Connected,                                                  // connected to server. we can send and receive packets.
        Disconnected,                                               // not connected (terminal state).
        WaitingForRetry,                                            // waiting for retry. sit in this state for a few seconds before starting matchmaking again.
    };

    GuestState state = GuestState.LoggingIn;

    ulong userId;                                                   // user id that is signed in.

    string oculusId;                                                // this is our user name.

    ulong hostUserId;                                               // the user id of the room owner (host).

    HashSet<ulong> connectionRequests = new HashSet<ulong>();       // set of connection request ids we have received. used to fix race condition between connection request and room join.

    bool acceptedConnectionRequest;                                 // true if we have accepted the connection request from the host.
    bool successfullyConnected;                                     // true if we have ever successfully connected to a server.
    
    int clientIndex = -1;                                           // while connected to server in [1,Constants.MaxClients-1]. -1 if not connected.

    ulong roomId;                                                   // the id of the room that we have joined.

    public double timeMatchmakingStarted;                           // time matchmaking started
    public double timeConnectionStarted;                            // time the client connection started (used to timeout due to NAT)
    public double timeConnected;                                    // time the client connected to the server
    public double timeLastPacketSent;                               // time the last packet was sent to the server
    public double timeLastPacketReceived;                           // time the last packet was received from the server (used for post-connect timeouts)
    public double timeRetryStarted;                                 // time the retry state started. used to delay in waiting for retry state before retrying matchmaking from scratch.

    private byte[] readBuffer = new byte[Constants.MaxPacketSize];

    bool IsConnectedToServer()
    {
        return state == GuestState.Connected;
    }

    new void Awake()
    {
        Debug.Log( "*** GUEST ***" );

        Assert.IsNotNull( context );

        state = GuestState.LoggingIn;

        InitializePlatformSDK( GetEntitlementCallback );

        Matchmaking.SetMatchFoundNotificationCallback( MatchFoundCallback );

        Rooms.SetUpdateNotificationCallback( RoomUpdatedCallback );

        Users.GetLoggedInUser().OnComplete( GetLoggedInUserCallback );

        Net.SetPeerConnectRequestCallback( PeerConnectRequestCallback );

        Net.SetConnectionStateChangedCallback( ConnectionStateChangedCallback );

        Voip.SetVoipConnectRequestCallback( ( Message<NetworkingPeer> msg ) => 
        {
            Debug.Log( "Accepting voice connection from " + msg.Data.ID );
            Voip.Accept( msg.Data.ID );
        } );

        Voip.SetVoipStateChangeCallback( ( Message<NetworkingPeer> msg ) =>
        {
            Debug.LogFormat( "Voice state changed to {1} for user {0}", msg.Data.ID, msg.Data.State );
        } );
    }

    new void Start()
    {
        base.Start();

        Assert.IsNotNull( context );
        Assert.IsNotNull( localAvatar );

        context.Deactivate();

        localAvatar.GetComponent<Avatar>().SetContext( context.GetComponent<Context>() );
    }

    void RetryUntilConnectedToServer()
    {
        Matchmaking.Cancel();

        DisconnectFromServer();

        if ( successfullyConnected )
            return;

        Debug.Log( "Retrying in " + RetryTime + " seconds..." );

        timeRetryStarted = renderTime;

        state = GuestState.WaitingForRetry;
    }

    void GetEntitlementCallback( Message msg )
    {
        if ( !msg.IsError )
        {
            Debug.Log( "You are entitled to use this app" );

        }
        else
        {
            Debug.Log( "error: You are not entitled to use this app" );
        }
    }

    void GetLoggedInUserCallback( Message<User> msg )
    {
        if ( !msg.IsError )
        {
            Debug.Log( "User successfully logged in" );

            userId = msg.Data.ID;
            oculusId = msg.Data.OculusID;

            Debug.Log( "User id is " + userId );
            Debug.Log( "Oculus id is " + oculusId );

            StartMatchmaking();
        }
        else
        {
            Debug.Log( "error: Could not get signed in user" );
        }
    }

    void StartMatchmaking()
    {
        MatchmakingOptions matchmakingOptions = new MatchmakingOptions();
        matchmakingOptions.SetEnqueueQueryKey( "quickmatch_query" );
        matchmakingOptions.SetCreateRoomJoinPolicy( RoomJoinPolicy.Everyone );
        matchmakingOptions.SetCreateRoomMaxUsers( Constants.MaxClients );
        matchmakingOptions.SetEnqueueDataSettings( "version", Constants.Version.GetHashCode() );

        Matchmaking.Enqueue2( "quickmatch", matchmakingOptions ).OnComplete( MatchmakingEnqueueCallback );

        timeMatchmakingStarted = renderTime;

        state = GuestState.Matchmaking;
    }

    void MatchmakingEnqueueCallback( Message msg )
    {
        if ( !msg.IsError )
        {
            Debug.Log( "Started matchmaking..." );
        }
        else
        {
            Debug.Log( "error: matchmaking error - " + msg.GetError() );

            RetryUntilConnectedToServer();
        }
    }

    void MatchFoundCallback( Message<Room> msg )
    {
        Debug.Log( "Found match. Room id = " + msg.Data.ID );

        roomId = msg.Data.ID;

        Matchmaking.JoinRoom( msg.Data.ID, true ).OnComplete( JoinRoomCallback );
    }

    void JoinRoomCallback( Message<Room> msg )
    {
        if ( !msg.IsError )
        {
            Debug.Log( "Joined room" );

            hostUserId = msg.Data.Owner.ID;
            
            PrintRoomDetails( msg.Data );

            StartConnectionToServer();
        }
        else
        {
            Debug.Log( "error: Failed to join room - " + msg.GetError() );

            RetryUntilConnectedToServer();
        }
    }

    void RoomUpdatedCallback( Message<Room> msg )
    {
        var room = msg.Data;

        if ( room.ID != roomId )
            return;

        if ( !msg.IsError )
        {
            Debug.Log( "Room updated" );

            foreach ( var user in room.Users )
            {
                Debug.Log( " + " + user.OculusID + " [" + user.ID + "]" );
            }

            if ( state == GuestState.Connected && !FindUserById( room.Users, userId ) )
            {
                Debug.Log( "Looks like we got kicked from the room" );

                RetryUntilConnectedToServer();
            }
        }
        else
        {
            Debug.Log( "error: Room updated error (?!) - " + msg.GetError() );
        }
    }

    void LeaveRoomCallback( Message<Room> msg )
    {
        if ( !msg.IsError )
        {
            Debug.Log( "Left room" );
        }
        else
        {
            Debug.Log( "error: Failed to leave room - " + msg.GetError() );
        }
    }

    void PeerConnectRequestCallback( Message<NetworkingPeer> msg )
    {
        Debug.Log( "Received connection request from " + msg.Data.ID );

        connectionRequests.Add( msg.Data.ID );
    }

    void ConnectionStateChangedCallback( Message<NetworkingPeer> msg )
    {
        if ( msg.Data.ID == hostUserId )
        {
            Debug.Log( "Connection state changed to " + msg.Data.State );

            if ( msg.Data.State != PeerConnectionState.Connected )
            { 
                DisconnectFromServer();
            }
        }
    }

    void StartConnectionToServer()
    {
        state = GuestState.Connecting;

        timeConnectionStarted = renderTime;
    }

    void ConnectToServer( int clientIndex )
    {
        Assert.IsTrue( clientIndex >= 1 );
        Assert.IsTrue( clientIndex < Constants.MaxClients );

        localAvatar.transform.position = context.GetRemoteAvatar( clientIndex ).gameObject.transform.position;
        localAvatar.transform.rotation = context.GetRemoteAvatar( clientIndex ).gameObject.transform.rotation;

        state = GuestState.Connected;

        this.clientIndex = clientIndex;

        context.Initialize( clientIndex );

        OnConnectToServer( clientIndex );
    }

    void DisconnectFromServer()
    {
        if ( IsConnectedToServer() )
            OnDisconnectFromServer();

        Net.Close( hostUserId );

        LeaveRoom( roomId, LeaveRoomCallback );

        roomId = 0;

        hostUserId = 0;

        state = GuestState.Disconnected;

        serverInfo.Clear();

        connectionRequests.Clear();

        acceptedConnectionRequest = false;
    }

    void OnConnectToServer( int clientIndex )
    {
        Debug.Log( "Connected to server as client " + clientIndex );

        timeConnected = renderTime;

        context.Activate();

        for ( int i = 0; i < Constants.MaxClients; ++i )
        {
            context.HideRemoteAvatar( i );
        }

        successfullyConnected = true;
    }

    void OnDisconnectFromServer()
    {
        Debug.Log( "Disconnected from server" );

        context.GetClientConnectionData().Reset();

        context.SetResetSequence( 0 );

        context.Reset();

        context.Deactivate();
    }

    bool readyToShutdown = false;

    protected override void OnQuit()
    {
        Matchmaking.Cancel();

        if ( IsConnectedToServer() )
        {
            DisconnectFromServer();
        }

        if ( roomId != 0 )
        {
            LeaveRoom( roomId, LeaveRoomOnQuitCallback );
        }
        else
        {
            readyToShutdown = true;
        }
    }

    protected override bool ReadyToShutdown()
    {
        return readyToShutdown;
    }

    void LeaveRoomOnQuitCallback( Message<Room> msg )
    {
        if ( !msg.IsError )
        {
            Debug.Log( "Left room" );
        }

        readyToShutdown = true;

        roomId = 0;
    }

    new void Update()
    {
        base.Update();

        if ( Input.GetKeyDown( "space" ) )
        {
            Debug.Log( "Forcing reconnect" );

            successfullyConnected = false;

            RetryUntilConnectedToServer();
        }

        if ( state == GuestState.Matchmaking && timeMatchmakingStarted + 30.0 < renderTime )
        {
            Debug.Log( "No result from matchmaker" );

            RetryUntilConnectedToServer();

            return;
        }

        if ( state == GuestState.Connecting && !acceptedConnectionRequest )
        {
            if ( hostUserId != 0 && connectionRequests.Contains( hostUserId ) )
            {
                Debug.Log( "Accepting connection request from host" );

                Net.Accept( hostUserId );

                acceptedConnectionRequest = true;
            }
        }

        if ( state == GuestState.Connected )
        {
            // apply guest avatar state at render time with interpolation

            Context.ConnectionData connectionData = context.GetClientConnectionData();
            int numInterpolatedAvatarStates;
            ushort avatarResetSequence;
            if ( connectionData.jitterBuffer.GetInterpolatedAvatarState( ref interpolatedAvatarState, out numInterpolatedAvatarStates, out avatarResetSequence ) )
            {
                if ( avatarResetSequence == context.GetResetSequence() )
                {
                    context.ApplyAvatarStateUpdates( numInterpolatedAvatarStates, ref interpolatedAvatarState, 0, clientIndex );
                }
            }

            // advance jitter buffer time

            context.GetClientConnectionData().jitterBuffer.AdvanceTime( Time.deltaTime );
        }

        if ( state == GuestState.WaitingForRetry && timeRetryStarted + RetryTime < renderTime )
        {
            StartMatchmaking();
            return;
        }

        CheckForTimeouts();
    }

    void CheckForTimeouts()
    {
        if ( state == GuestState.Connecting )
        {
            if ( timeConnectionStarted + ConnectTimeout < renderTime )
            {
                Debug.Log( "Timed out while trying to connect to server" );

                RetryUntilConnectedToServer();
            }
        }
        else if ( state == GuestState.Connected )
        {
            if ( timeLastPacketReceived + ConnectionTimeout < renderTime )
            {
                Debug.Log( "Connection to server timed out" );

                DisconnectFromServer();
            }
        }
    }

    new void FixedUpdate()
    {
        if ( IsConnectedToServer() )
        {
            context.CheckForAtRestObjects();
        }

        ProcessPacketsFromServer();

        if ( IsConnectedToServer() )
        {
            ProcessAcks();

            SendPacketToServer();

            context.CheckForAtRestObjects();
        }

        base.FixedUpdate();
    }

    void SendPacketToServer()
    {
        if ( !IsConnectedToServer() )
            return;

        Context.ConnectionData connectionData = context.GetClientConnectionData();

        byte[] packetData = GenerateStateUpdatePacket( connectionData, (float) ( physicsTime - renderTime ) );

        Net.SendPacket( hostUserId, packetData, SendPolicy.Unreliable );

        timeLastPacketSent = renderTime;
    }

    void ProcessPacketsFromServer()
    {
        Packet packet;

        while ( ( packet = Net.ReadPacket() ) != null )
        {
            if ( packet.SenderID != hostUserId )
                continue;

            packet.ReadBytes( readBuffer );

            byte packetType = readBuffer[0];

            if ( ( state == GuestState.Connecting || state == GuestState.Connected ) && packetType == (byte) PacketSerializer.PacketType.ServerInfo )
            {
                ProcessServerInfoPacket( readBuffer );
            }

            if ( !IsConnectedToServer() )
                continue;

            if ( packetType == (byte) PacketSerializer.PacketType.StateUpdate )
            {
                if ( enableJitterBuffer )
                {
                    AddStateUpdatePacketToJitterBuffer( context, context.GetClientConnectionData(), readBuffer );
                }
                else
                {
                    ProcessStateUpdatePacket( context.GetClientConnectionData(), readBuffer );
                }
            }

            timeLastPacketReceived = renderTime;
        }

        // process state update from jitter buffer

        if ( enableJitterBuffer && IsConnectedToServer() )
        {
            ProcessStateUpdateFromJitterBuffer( context, context.GetClientConnectionData(), 0, clientIndex, enableJitterBuffer && renderTime > timeConnected + 0.25 );
        }

        // advance remote frame number

        if ( IsConnectedToServer() )
        {
            Context.ConnectionData connectionData = context.GetClientConnectionData();

            if ( !connectionData.firstRemotePacket )
                connectionData.remoteFrameNumber++;
        }
    }

    public byte[] GenerateStateUpdatePacket( Context.ConnectionData connectionData, float avatarSampleTimeOffset )
    {
        Profiler.BeginSample( "GenerateStateUpdatePacket" );

        int maxStateUpdates = Math.Min( Constants.NumCubes, Constants.MaxStateUpdates );

        int numStateUpdates = maxStateUpdates;

        context.UpdateCubePriority();

        context.GetMostImportantCubeStateUpdates( connectionData, ref numStateUpdates, ref cubeIds, ref cubeState );

        Network.PacketHeader writePacketHeader;

        connectionData.connection.GeneratePacketHeader( out writePacketHeader );

        writePacketHeader.resetSequence = context.GetResetSequence();

        writePacketHeader.frameNumber = (uint) frameNumber;

        writePacketHeader.avatarSampleTimeOffset = avatarSampleTimeOffset;

        DetermineNotChangedAndDeltas( context, connectionData, writePacketHeader.sequence, numStateUpdates, ref cubeIds, ref notChanged, ref hasDelta, ref baselineSequence, ref cubeState, ref cubeDelta );

        DeterminePrediction( context, connectionData, writePacketHeader.sequence, numStateUpdates, ref cubeIds, ref notChanged, ref hasDelta, ref perfectPrediction, ref hasPredictionDelta, ref baselineSequence, ref cubeState, ref predictionDelta );

        int numAvatarStates = 1;

        localAvatar.GetComponent<Avatar>().GetAvatarState( out avatarState[0] );

        AvatarState.Quantize( ref avatarState[0], out avatarStateQuantized[0] );

        WriteStateUpdatePacket( ref writePacketHeader, numAvatarStates, ref avatarStateQuantized, numStateUpdates, ref cubeIds, ref notChanged, ref hasDelta, ref perfectPrediction, ref hasPredictionDelta, ref baselineSequence, ref cubeState, ref cubeDelta, ref predictionDelta );

        byte[] packetData = writeStream.GetData();

        AddPacketToDeltaBuffer( ref connectionData.sendDeltaBuffer, writePacketHeader.sequence, context.GetResetSequence(), numStateUpdates, ref cubeIds, ref cubeState );

        context.ResetCubePriority( connectionData, numStateUpdates, cubeIds );

        Profiler.EndSample();

        return packetData;
    }

    ServerInfo packetServerInfo = new ServerInfo();

    public void ProcessServerInfoPacket( byte[] packetData )
    {
        Profiler.BeginSample( "ProcessServerInfoPacket" );

        if ( ReadServerInfoPacket( packetData, packetServerInfo.clientConnected, packetServerInfo.clientUserId, packetServerInfo.clientUserName ) )
        {
            Debug.Log( "Received server info:" );

            packetServerInfo.Print();

            // client searches for its own user id in the first server info. this is how the client knows what client slot it has been assigned.

            if ( state == GuestState.Connecting )
            {
                int clientIndex = packetServerInfo.FindClientByUserId( userId );

                if ( clientIndex != -1 )
                {
                    ConnectToServer( clientIndex );
                }
                else
                {
                    Debug.Log( "error: Could not find our user id " + userId + " in server info? Something is horribly wrong!" );
                    DisconnectFromServer();
                    return;
                }
            }

            // track remote clients joining and leaving by detecting edge triggers on the server info.

            for ( int i = 0; i < Constants.MaxClients; ++i )
            {
                if ( i == clientIndex )
                    continue;

                if ( !serverInfo.clientConnected[i] && packetServerInfo.clientConnected[i] )
                {
                    OnRemoteClientConnected( i, packetServerInfo.clientUserId[i], packetServerInfo.clientUserName[i] );
                }
                else if ( serverInfo.clientConnected[i] && !packetServerInfo.clientConnected[i] )
                {
                    OnRemoteClientDisconnected( i, serverInfo.clientUserId[i], serverInfo.clientUserName[i] );
                }
            }

            // copy across the packet server info to our current server info

            serverInfo.CopyFrom( packetServerInfo );
        }

        Profiler.EndSample();
    }

    void OnRemoteClientConnected( int clientIndex, ulong userId, string userName )
    {
        Debug.Log( userName + " connected as client " + clientIndex );

        context.ShowRemoteAvatar( clientIndex );

        Voip.Start( userId );

        var headGameObject = context.GetRemoteAvatarHead( clientIndex );
        var audioSource = headGameObject.GetComponent<VoipAudioSourceHiLevel>();
        if ( !audioSource )
            audioSource = headGameObject.AddComponent<VoipAudioSourceHiLevel>();
        audioSource.senderID = userId;
    }

    void OnRemoteClientDisconnected( int clientIndex, ulong userId, string userName )
    {
        Debug.Log( userName + " disconnected" );

        var headGameObject = context.GetRemoteAvatarHead( clientIndex );
        var audioSource = headGameObject.GetComponent<VoipAudioSourceHiLevel>();
        if ( audioSource )
            audioSource.senderID = 0;

        Voip.Stop( userId );

        context.HideRemoteAvatar( clientIndex );
    }

    public void ProcessStateUpdatePacket( Context.ConnectionData connectionData, byte[] packetData )
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

            // ignore updates from before the last server reset

            if ( Network.Util.SequenceGreaterThan( context.GetResetSequence(), readPacketHeader.resetSequence ) )
                return;

            // reset if the server reset sequence is more recent than ours

            if ( Network.Util.SequenceGreaterThan( readPacketHeader.resetSequence, context.GetResetSequence() ) )
            {
                context.Reset();
                context.SetResetSequence( readPacketHeader.resetSequence );
            }
            
            // decode the predicted cube states from baselines

            DecodePrediction( connectionData.receiveDeltaBuffer, readPacketHeader.sequence, context.GetResetSequence(), readNumStateUpdates, ref readCubeIds, ref readPerfectPrediction, ref readHasPredictionDelta, ref readBaselineSequence, ref readCubeState, ref readPredictionDelta );

            // decode the not changed and delta cube states from baselines

            DecodeNotChangedAndDeltas( connectionData.receiveDeltaBuffer, context.GetResetSequence(), readNumStateUpdates, ref readCubeIds, ref readNotChanged, ref readHasDelta, ref readBaselineSequence, ref readCubeState, ref readCubeDelta );

            // add the cube states to the receive delta buffer

            AddPacketToDeltaBuffer( ref connectionData.receiveDeltaBuffer, readPacketHeader.sequence, context.GetResetSequence(), readNumStateUpdates, ref readCubeIds, ref readCubeState );

            // apply the state updates to cubes

            int fromClientIndex = 0;
            int toClientIndex = clientIndex;

            context.ApplyCubeStateUpdates( readNumStateUpdates, ref readCubeIds, ref readCubeState, fromClientIndex, toClientIndex, enableJitterBuffer && renderTime > timeConnected + 0.25 );

            // apply avatar state updates

            context.ApplyAvatarStateUpdates( readNumAvatarStates, ref readAvatarState, fromClientIndex, toClientIndex );

            // process the packet header

            connectionData.connection.ProcessPacketHeader( ref readPacketHeader );
        }

        Profiler.EndSample();
    }

    void ProcessAcks()
    {
        Profiler.BeginSample( "Process Acks" );
        {
            Context.ConnectionData connectionData = context.GetClientConnectionData();

            ProcessAcksForConnection( context, connectionData );
        }

        Profiler.EndSample();
    }
}
