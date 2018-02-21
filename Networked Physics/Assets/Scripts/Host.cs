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

public class Host : Common
{
    public Context context;

    ulong roomId;                                       // the room id. valid once the host has created a room and enqueued it on the matchmaker.

    enum ClientState
    {
        Disconnected,                                   // client is not connected
        Connecting,                                     // client is connecting (joined room, but NAT punched yet)
        Connected                                       // client is fully connected and is sending and receiving packets.
    };

    struct ClientData
    {
        public ClientState state;
        public ulong userId;
        public string oculusId;
        public double timeConnectionStarted;
        public double timeConnected;
        public double timeLastPacketSent;
        public double timeLastPacketReceived;

        public void Reset()
        {
            state = ClientState.Disconnected;
            userId = 0;
            oculusId = "";
            timeConnectionStarted = 0.0;
            timeConnected = 0.0f;
            timeLastPacketSent = 0.0;
            timeLastPacketReceived = 0.0;
        }
    };

    ClientData [] client = new ClientData[Constants.MaxClients];

    bool IsClientConnected( int clientIndex )
    {
        Assert.IsTrue( clientIndex >= 0 );
        Assert.IsTrue( clientIndex < Constants.MaxClients );
        return client[clientIndex].state == ClientState.Connected;
    }

    private byte[] readBuffer = new byte[Constants.MaxPacketSize];

    new void Awake()
    {
        Debug.Log( "*** HOST ***" );

        Assert.IsNotNull( context );

        // IMPORTANT: the host is *always* client 0

        for ( int i = 0; i < Constants.MaxClients; ++i )
            client[i].Reset();

        context.Initialize( 0 );

        context.SetResetSequence( 100 );

        InitializePlatformSDK( GetEntitlementCallback );

        Rooms.SetUpdateNotificationCallback( RoomUpdatedCallback );

        Net.SetConnectionStateChangedCallback( ConnectionStateChangedCallback );

        Voip.SetVoipConnectRequestCallback( ( Message<NetworkingPeer> msg ) =>
        {
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

        for ( int i = 0; i < Constants.MaxClients; ++i )
            context.HideRemoteAvatar( i );

        localAvatar.GetComponent<Avatar>().SetContext( context.GetComponent<Context>() );
        localAvatar.transform.position = context.GetRemoteAvatar( 0 ).gameObject.transform.position;
        localAvatar.transform.rotation = context.GetRemoteAvatar( 0 ).gameObject.transform.rotation;
    }

    void GetEntitlementCallback( Message msg )
    {
        if ( !msg.IsError )
        {
            Debug.Log( "You are entitled to use this app" );

            Users.GetLoggedInUser().OnComplete( GetLoggedInUserCallback );
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
            Debug.Log( "User id is " + msg.Data.ID );
            Debug.Log( "Oculus id is " + msg.Data.OculusID  );

            client[0].state = ClientState.Connected;
            client[0].userId = msg.Data.ID;
            client[0].oculusId = msg.Data.OculusID;

            MatchmakingOptions matchmakingOptions = new MatchmakingOptions();
            matchmakingOptions.SetEnqueueQueryKey( "quickmatch_query" );
            matchmakingOptions.SetCreateRoomJoinPolicy( RoomJoinPolicy.Everyone );
            matchmakingOptions.SetCreateRoomMaxUsers( Constants.MaxClients );
            matchmakingOptions.SetEnqueueDataSettings( "version", Constants.Version.GetHashCode() );

            Matchmaking.CreateAndEnqueueRoom2( "quickmatch", matchmakingOptions ).OnComplete( CreateAndEnqueueRoomCallback );
        }
        else
        {
            Debug.Log( "error: Could not get signed in user" );
        }
    }

    void CreateAndEnqueueRoomCallback( Message<MatchmakingEnqueueResultAndRoom> msg )
    {
        if ( !msg.IsError )
        {
            Debug.Log( "Created and enqueued room" );

            PrintRoomDetails( msg.Data.Room );

            roomId = msg.Data.Room.ID;
        }
        else
        {
            Debug.Log( "error: Failed to create and enqueue room - " + msg.GetError() );
        }
    }

    int FindClientByUserId( ulong userId )
    {
        for ( int i = 1; i < Constants.MaxClients; ++i )
        {
            if ( client[i].state != ClientState.Disconnected && client[i].userId == userId )
                return i;
        }
        return -1;
    }

    int FindFreeClientIndex()
    {
        for ( int i = 1; i < Constants.MaxClients; ++i )
        {
            if ( client[i].state == ClientState.Disconnected )
                return i;
        }
        return -1;
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

            // disconnect any clients that are connecting/connected in our state machine, but are no longer in the room

            for ( int i = 1; i < Constants.MaxClients; ++i )
            {
                if ( client[i].state != ClientState.Disconnected && !FindUserById( room.Users, client[i].userId ) )
                {
                    Debug.Log( "Client " + i + " is no longer in the room" );

                    DisconnectClient( i );
                }
            }

            // connect any clients who are in the room, but aren't connecting/connected in our state machine (excluding the room owner)

            foreach ( var user in room.Users )
            {
                if ( user.ID == room.Owner.ID )
                    continue;

                if ( FindClientByUserId( user.ID ) == -1 )
                {
                    int clientIndex = FindFreeClientIndex();
                    if ( clientIndex != -1 )
                        StartClientConnection( clientIndex, user.ID, user.OculusID );
                }
            }
        }
        else
        {
            Debug.Log( "error: Room updated error (?!) - " + msg.GetError() );
        }
    }

    void StartClientConnection( int clientIndex, ulong userId, string oculusId )
    {
        Debug.Log( "Starting connection to client " + oculusId + " [" + userId + "]" );

        Assert.IsTrue( clientIndex != 0 );

        if ( client[clientIndex].state != ClientState.Disconnected )
            DisconnectClient( clientIndex );

        client[clientIndex].state = ClientState.Connecting;
        client[clientIndex].oculusId = oculusId;
        client[clientIndex].userId = userId;
        client[clientIndex].timeConnectionStarted = renderTime;

        Net.Connect( userId );
    }

    void ConnectClient( int clientIndex, ulong userId )
    {
        Assert.IsTrue( clientIndex != 0 );

        if ( client[clientIndex].state != ClientState.Connecting || client[clientIndex].userId != userId )
            return;

        client[clientIndex].state = ClientState.Connected;
        client[clientIndex].timeConnected = renderTime;
        client[clientIndex].timeLastPacketSent = renderTime;
        client[clientIndex].timeLastPacketReceived = renderTime;

        OnClientConnect( clientIndex );

        BroadcastServerInfo();
    }

    void DisconnectClient( int clientIndex )
    {
        Assert.IsTrue( clientIndex != 0 );
        Assert.IsTrue( IsClientConnected( clientIndex ) );

        OnClientDisconnect( clientIndex );

        Rooms.KickUser( roomId, client[clientIndex].userId, 0 );

        Net.Close( client[clientIndex].userId );

        client[clientIndex].Reset();

        BroadcastServerInfo();
    }

    void OnClientConnect( int clientIndex )
    {
        Debug.Log( client[clientIndex].oculusId + " joined the game as client " + clientIndex );

        context.ShowRemoteAvatar( clientIndex );

        Voip.Start( client[clientIndex].userId );

        var headGameObject = context.GetRemoteAvatarHead( clientIndex );
        var audioSource = headGameObject.GetComponent<VoipAudioSourceHiLevel>();
        if ( !audioSource )
            audioSource = headGameObject.AddComponent<VoipAudioSourceHiLevel>();
        audioSource.senderID = client[clientIndex].userId;
    }

    void OnClientDisconnect( int clientIndex )
    {
        Debug.Log( client[clientIndex].oculusId + " left the game" );

        var headGameObject = context.GetRemoteAvatarHead( clientIndex );
        var audioSource = headGameObject.GetComponent<VoipAudioSourceHiLevel>();
        if ( audioSource )
            audioSource.senderID = 0;

        Voip.Stop( client[clientIndex].userId );
        
        context.HideRemoteAvatar( clientIndex );

        context.ResetAuthorityForClientCubes( clientIndex );

        context.GetServerConnectionData( clientIndex ).Reset();
    }

    void ConnectionStateChangedCallback( Message<NetworkingPeer> msg )
    {
        ulong userId = msg.Data.ID;

        int clientIndex = FindClientByUserId( userId );

        if ( clientIndex != -1 )
        {
            Debug.Log( "Connection state changed to " + msg.Data.State + " for client " + clientIndex );

            if ( msg.Data.State == PeerConnectionState.Connected )
            {
                ConnectClient( clientIndex, userId );
            }
            else
            {
                if ( client[clientIndex].state != ClientState.Disconnected )
                {
                    DisconnectClient( clientIndex );
                }
            }
        }
    }

    bool readyToShutdown = false;

    protected override void OnQuit()
    {
        if ( roomId != 0 )
        {
            for ( int i = 1; i < Constants.MaxClients; ++i )
            {
                if ( IsClientConnected( i ) )
                    DisconnectClient( i );
            }

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

        // apply host avatar per-remote client at render time with interpolation

        for ( int i = 1; i < Constants.MaxClients; ++i )
        {
            if ( client[i].state != ClientState.Connected )
                continue;

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

        // advance jitter buffer time

        for ( int i = 1; i < Constants.MaxClients; ++i )
        {
            if ( client[i].state == ClientState.Connected )
            {
                context.GetServerConnectionData( i ).jitterBuffer.AdvanceTime( Time.deltaTime );
            }
        }

        // check for timeouts

        CheckForTimeouts();
    }

    new void FixedUpdate()
    {
        var avatar = localAvatar.GetComponent<Avatar>();

        bool reset = Input.GetKey( "space" ) || ( avatar.IsPressingIndex() && avatar.IsPressingX() );

        if ( reset )
        {
            context.Reset();
            context.IncreaseResetSequence();
        }

        context.CheckForAtRestObjects();

        ProcessPacketsFromConnectedClients();

        SendPacketsToConnectedClients();

        context.CheckForAtRestObjects();

        base.FixedUpdate();
    }

    void CheckForTimeouts()
    {
        for ( int i = 1; i < Constants.MaxClients; ++i )
        {
            if ( client[i].state == ClientState.Connecting )
            {
                if ( client[i].timeConnectionStarted + ConnectionTimeout < renderTime )
                {
                    Debug.Log( "Client " + i + " timed out while connecting" );

                    DisconnectClient( i );
                }
            }
            else if ( client[i].state == ClientState.Connected )
            {
                if ( client[i].timeLastPacketReceived + ConnectionTimeout < renderTime )
                {
                    Debug.Log( "Client " + i + " timed out" );

                    DisconnectClient( i );
                }
            }
        }
    }

    void ProcessPacketsFromConnectedClients()
    {
        Packet packet;

        while ( ( packet = Net.ReadPacket() ) != null )
        {
            int clientIndex = FindClientByUserId( packet.SenderID );
            if ( clientIndex == -1 )
                continue;

            if ( !IsClientConnected( clientIndex ) )
                continue;

            packet.ReadBytes( readBuffer );

            byte packetType = readBuffer[0];

            if ( packetType == (byte) PacketSerializer.PacketType.StateUpdate )
            {
                if ( enableJitterBuffer )
                {
                    AddStateUpdatePacketToJitterBuffer( context, context.GetServerConnectionData( clientIndex ), readBuffer );
                }
                else
                {
                    ProcessStateUpdatePacket( readBuffer, clientIndex );
                }
            }

            client[clientIndex].timeLastPacketReceived = renderTime;
        }

        ProcessAcks();

        // process client state update from jitter buffer

        if ( enableJitterBuffer )
        {
            for ( int i = 1; i < Constants.MaxClients; ++i )
            {
                if ( client[i].state == ClientState.Connected )
                {
                    ProcessStateUpdateFromJitterBuffer( context, context.GetServerConnectionData( i ), i, 0, enableJitterBuffer );
                }
            }
        }

        // advance remote frame number

        for ( int i = 1; i < Constants.MaxClients; ++i )
        {
            if ( client[i].state == ClientState.Connected )
            {
                Context.ConnectionData connectionData = context.GetServerConnectionData( i );

                if ( !connectionData.firstRemotePacket )
                    connectionData.remoteFrameNumber++;
            }
        }
    }

    void SendPacketsToConnectedClients()
    {
        for ( int clientIndex = 1; clientIndex < Constants.MaxClients; ++clientIndex )
        {
            if ( !IsClientConnected( clientIndex ) )
                continue;

            Context.ConnectionData connectionData = context.GetServerConnectionData( clientIndex );

            byte[] packetData = GenerateStateUpdatePacket( connectionData, clientIndex, (float) ( physicsTime - renderTime ) );

            Net.SendPacket( client[clientIndex].userId, packetData, SendPolicy.Unreliable );

            client[clientIndex].timeLastPacketSent = renderTime;
        }
    }

    public void BroadcastServerInfo()
    {
        byte[] packetData = GenerateServerInfoPacket();

        for ( int clientIndex = 1; clientIndex < Constants.MaxClients; ++clientIndex )
        {
            if ( !IsClientConnected( clientIndex ) )
                continue;

            Net.SendPacket( client[clientIndex].userId, packetData, SendPolicy.Unreliable );

            client[clientIndex].timeLastPacketSent = renderTime;
        }
    }
   
    public byte[] GenerateServerInfoPacket()
    {
        for ( int i = 0; i < Constants.MaxClients; ++i )
        {
            if ( IsClientConnected( i ) )
            {
                serverInfo.clientConnected[i] = true;
                serverInfo.clientUserId[i] = client[i].userId;
                serverInfo.clientUserName[i] = client[i].oculusId;
            }
            else
            {
                serverInfo.clientConnected[i] = false;
                serverInfo.clientUserId[i] = 0;
                serverInfo.clientUserName[i] = "";
            }
        }

        WriteServerInfoPacket( serverInfo.clientConnected, serverInfo.clientUserId, serverInfo.clientUserName );

        byte[] packetData = writeStream.GetData();

        return packetData;
    }

    public byte[] GenerateStateUpdatePacket( Context.ConnectionData connectionData, int toClientIndex, float avatarSampleTimeOffset )
    {
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

        int numAvatarStates = 0;

        numAvatarStates = 0;

        for ( int i = 0; i < Constants.MaxClients; ++i )
        {
            if ( i == toClientIndex )
                continue;

            if ( i == 0 )
            {
                // grab state from the local avatar.

                localAvatar.GetComponent<Avatar>().GetAvatarState( out avatarState[numAvatarStates] );
                AvatarState.Quantize( ref avatarState[numAvatarStates], out avatarStateQuantized[numAvatarStates] );
                numAvatarStates++;
            }
            else
            {
                // grab state from a remote avatar.

                var remoteAvatar = context.GetRemoteAvatar( i );

                if ( remoteAvatar )
                {
                    remoteAvatar.GetAvatarState( out avatarState[numAvatarStates] );
                    AvatarState.Quantize( ref avatarState[numAvatarStates], out avatarStateQuantized[numAvatarStates] );
                    numAvatarStates++;
                }
            }
        }

        WriteStateUpdatePacket( ref writePacketHeader, numAvatarStates, ref avatarStateQuantized, numStateUpdates, ref cubeIds, ref notChanged, ref hasDelta, ref perfectPrediction, ref hasPredictionDelta, ref baselineSequence, ref cubeState, ref cubeDelta, ref predictionDelta );

        byte[] packetData = writeStream.GetData();

        // add the sent cube states to the send delta buffer

        AddPacketToDeltaBuffer( ref connectionData.sendDeltaBuffer, writePacketHeader.sequence, context.GetResetSequence(), numStateUpdates, ref cubeIds, ref cubeState );

        // reset cube priority for the cubes that were included in the packet (so other cubes have a chance to be sent...)

        context.ResetCubePriority( connectionData, numStateUpdates, cubeIds );

        return packetData;
    }

    public void ProcessStateUpdatePacket( byte[] packetData, int fromClientIndex )
    {
        int readNumAvatarStates = 0;
        int readNumStateUpdates = 0;

        Context.ConnectionData connectionData = context.GetServerConnectionData( fromClientIndex );

        Network.PacketHeader readPacketHeader;

        if ( ReadStateUpdatePacket( packetData, out readPacketHeader, out readNumAvatarStates, ref readAvatarStateQuantized, out readNumStateUpdates, ref readCubeIds, ref readNotChanged, ref readHasDelta, ref readPerfectPrediction, ref readHasPredictionDelta, ref readBaselineSequence, ref readCubeState, ref readCubeDelta, ref readPredictionDelta ) )
        {
            // unquantize avatar states

            for ( int i = 0; i < readNumAvatarStates; ++i )
                AvatarState.Unquantize( ref readAvatarStateQuantized[i], out readAvatarState[i] );

            // ignore any updates from a client with a different reset sequence #

            if ( context.GetResetSequence() != readPacketHeader.resetSequence )
                return;

            // decode the predicted cube states from baselines

            DecodePrediction( connectionData.receiveDeltaBuffer, readPacketHeader.sequence, context.GetResetSequence(), readNumStateUpdates, ref readCubeIds, ref readPerfectPrediction, ref readHasPredictionDelta, ref readBaselineSequence, ref readCubeState, ref readPredictionDelta );

            // decode the not changed and delta cube states from baselines

            DecodeNotChangedAndDeltas( connectionData.receiveDeltaBuffer, context.GetResetSequence(), readNumStateUpdates, ref readCubeIds, ref readNotChanged, ref readHasDelta, ref readBaselineSequence, ref readCubeState, ref readCubeDelta );

            // add the cube states to the receive delta buffer

            AddPacketToDeltaBuffer( ref connectionData.receiveDeltaBuffer, readPacketHeader.sequence, context.GetResetSequence(), readNumStateUpdates, ref readCubeIds, ref readCubeState );

            // apply the state updates to cubes

            context.ApplyCubeStateUpdates( readNumStateUpdates, ref readCubeIds, ref readCubeState, fromClientIndex, 0, enableJitterBuffer );

            // apply avatar state updates

            context.ApplyAvatarStateUpdates( readNumAvatarStates, ref readAvatarState, fromClientIndex, 0 );

            // process the packet header

            connectionData.connection.ProcessPacketHeader( ref readPacketHeader );
        }                            
    }

    void ProcessAcks()
    {
        Profiler.BeginSample( "Process Acks" );
        {
            for ( int clientIndex = 1; clientIndex < Constants.MaxClients; ++clientIndex )
            {
                for ( int i = 1; i < Constants.MaxClients; ++i )
                {
                    Context.ConnectionData connectionData = context.GetServerConnectionData( i );

                    ProcessAcksForConnection( context, connectionData );
                }
            }
        }

        Profiler.EndSample();
    }
}
