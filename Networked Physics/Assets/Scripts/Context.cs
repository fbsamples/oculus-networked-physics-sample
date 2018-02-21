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
using System.Collections.Generic;

public class Context: MonoBehaviour
{
    public GameObject[] remoteAvatar = new GameObject[Constants.MaxClients];
    public GameObject[] remoteLinePrefabs = new GameObject[Constants.MaxClients];
    public Material[] authorityMaterials = new Material[Constants.MaxAuthority];

    public struct PriorityData
    {
        public int cubeId;
        public float accumulator;
    };

    public struct MostRecentAckedState
    {
        public bool acked;
        public ushort sequence;
        public ushort resetSequence;
        public CubeState cubeState;
    };

    public class ConnectionData
    {
        public Network.Connection connection = new Network.Connection();
        public DeltaBuffer sendDeltaBuffer = new DeltaBuffer( Constants.DeltaBufferSize );
        public DeltaBuffer receiveDeltaBuffer = new DeltaBuffer( Constants.DeltaBufferSize );
        public PriorityData[] priorityData = new PriorityData[Constants.NumCubes];
        public MostRecentAckedState[] mostRecentAckedState = new MostRecentAckedState[Constants.NumCubes];
        public bool firstRemotePacket = true;
        public long remoteFrameNumber = -1;
        public JitterBuffer jitterBuffer = new JitterBuffer();

        public ConnectionData()
        {
            Reset();
        }

        public void Reset()
        {
            Profiler.BeginSample( "ConnectionData.Reset" );

            connection.Reset();

            sendDeltaBuffer.Reset();

            receiveDeltaBuffer.Reset();

            for ( int i = 0; i < priorityData.Length; ++i )
            {
                priorityData[i].cubeId = i;
                priorityData[i].accumulator = 0.0f;
            }

            for ( int i = 0; i < mostRecentAckedState.Length; ++i )
            {
                mostRecentAckedState[i].acked = false;
                mostRecentAckedState[i].sequence = 0;
                mostRecentAckedState[i].resetSequence = 0;
            }

            firstRemotePacket = true;
            remoteFrameNumber = -1;
            jitterBuffer.Reset();

            Profiler.EndSample();
        }
    };

    ConnectionData clientConnectionData;

    ConnectionData[] serverConnectionData;

    public ConnectionData GetClientConnectionData()
    {
        Assert.IsTrue( IsClient() );
        return clientConnectionData;
    }

    public ConnectionData GetServerConnectionData( int clientIndex )
    {
        Assert.IsTrue( IsServer() );
        Assert.IsTrue( clientIndex >= 1 );
        Assert.IsTrue( clientIndex <= Constants.MaxClients );
        return serverConnectionData[clientIndex - 1];
    }

    Interactions interactions = new Interactions();

    HashSet<int> visited = new HashSet<int>();

    int clientIndex;
    int authorityIndex;
    int layer;

    bool active = true;

    public void Initialize( int clientIndex )
    {
        this.clientIndex = clientIndex;

        this.authorityIndex = clientIndex + 1;

        Assert.IsTrue( this.clientIndex >= 0 && this.clientIndex < Constants.MaxClients );

        Assert.IsTrue( this.authorityIndex >= 0 && this.authorityIndex < Constants.MaxAuthority );

        if ( clientIndex == 0 )
        {
            // initialize as server

            clientConnectionData = null;

            serverConnectionData = new ConnectionData[Constants.MaxClients - 1];

            for ( int i = 0; i < serverConnectionData.Length; ++i )
            {
                serverConnectionData[i] = new ConnectionData();

                InitializePriorityData( serverConnectionData[i] );
            }
        }
        else
        {
            // initialize as client

            clientConnectionData = new ConnectionData();

            serverConnectionData = null;

            InitializePriorityData( clientConnectionData );
        }
    }

    public void Shutdown()
    {
        clientIndex = 0;
        authorityIndex = 0;
        clientConnectionData = null;
        serverConnectionData = null;
    }

    public void Activate()
    {
        active = true;

        FreezeCubes( false );

        ShowContext( true );
    }

    public void Deactivate()
    {
        active = false;

        FreezeCubes( true );

        ShowContext( false );
    }

    public bool IsActive()
    {
        return active;
    }

    public bool IsServer()
    {
        return IsActive() && clientIndex == 0;
    }

    public bool IsClient()
    {
        return IsActive() && clientIndex != 0;
    }

    public int GetLayer()
    {
        return layer;
    }

    public int GetGripLayer()
    {
        return layer + 1;
    }

    public int GetTouchingLayer()
    {
        return layer + 2;
    }

    public int GetClientIndex()
    {
        return clientIndex;
    }

    public int GetAuthorityIndex()
    {
        return authorityIndex;
    }

    public RemoteAvatar GetRemoteAvatar( int clientIndex )
    {
        Assert.IsTrue( clientIndex >= 0 );
        Assert.IsTrue( clientIndex < Constants.MaxClients );
        var avatar = remoteAvatar[clientIndex];
        if ( avatar == null )
            return null;
        else
            return avatar.GetComponent<RemoteAvatar>();
    }

    void ShowHideObject( GameObject gameObject, bool show )
    {
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
        foreach ( Renderer renderer in renderers )
        {
            renderer.enabled = show;
        }
    }

    public void ShowRemoteAvatar( int clientIndex )
    {
        ShowHideObject( GetRemoteAvatar( clientIndex ).gameObject, true );
    }

    public void HideRemoteAvatar( int clientIndex )
    {
        ShowHideObject( GetRemoteAvatar( clientIndex ).gameObject, false );
    }

    public void ResetAuthorityForClientCubes( int clientIndex )
    {
        int authorityIndex = clientIndex + 1;

        for ( int i = 0; i < Constants.NumCubes; ++i )
        {
            NetworkInfo networkInfo = cubes[i].GetComponent<NetworkInfo>();

            if ( networkInfo.GetAuthorityIndex() == authorityIndex )
            {
                Debug.Log( "Returning cube " + i + " to default authority" );

                networkInfo.DetachCubeFromPlayer();

                networkInfo.SetAuthorityIndex( 0 );
                networkInfo.SetAuthoritySequence( 0 );
                networkInfo.IncreaseOwnershipSequence();

                var rigidBody = cubes[i].GetComponent<Rigidbody>();

                if ( rigidBody.IsSleeping() )
                    rigidBody.WakeUp();

                ResetCubeRingBuffer( i );
            }
        }
    }

    public GameObject GetRemoteAvatarHead( int clientIndex )
    {
        var remoteAvatar = GetRemoteAvatar( clientIndex );
        if ( remoteAvatar )
        {
            return remoteAvatar.GetHead();
        }
        else
        {
            return null;
        }
    }

    public GameObject cubePrefab;

    Vector3[] cubePositions = new Vector3[Constants.NumCubes];

    GameObject[] cubes = new GameObject[Constants.NumCubes];

    public GameObject GetCube( int index )
    {
        return cubes[index];
    }

    Snapshot lastSnapshot = new Snapshot();

    ushort resetSequence = 0;

    public void IncreaseResetSequence() { resetSequence++; }
    public void SetResetSequence( ushort sequence ) { resetSequence = sequence; }
    public ushort GetResetSequence() { return resetSequence; }

    struct RingBufferState
    {
        public Vector3 position;
        public Vector3 axis;
    };

    RingBufferState[] ringBuffer = new RingBufferState[Constants.NumCubes * Constants.RingBufferSize];

    ulong[] lastHighEnergyCollisionFrame = new ulong[Constants.NumCubes];

    ulong renderFrame = 0;
    ulong simulationFrame = 0;

    public ulong GetRenderFrame()
    {
        return renderFrame;
    }

    public ulong GetSimulationFrame()
    {
        return simulationFrame;
    }

    public bool GetMostRecentAckedState( ConnectionData connectionData, int cubeId, ref ushort sequence, ushort resetSequence, ref CubeState cubeState )
    {
        if ( !connectionData.mostRecentAckedState[cubeId].acked )
            return false;

        if ( connectionData.mostRecentAckedState[cubeId].resetSequence != resetSequence )
            return false;

        sequence = connectionData.mostRecentAckedState[cubeId].sequence;
        cubeState = connectionData.mostRecentAckedState[cubeId].cubeState;

        return true;
    }

    public bool UpdateMostRecentAckedState( ConnectionData connectionData, int cubeId, ushort sequence, ushort resetSequence, ref CubeState cubeState )
    {
        if ( connectionData.mostRecentAckedState[cubeId].acked && ( Network.Util.SequenceGreaterThan( connectionData.mostRecentAckedState[cubeId].resetSequence, resetSequence ) || Network.Util.SequenceGreaterThan( connectionData.mostRecentAckedState[cubeId].sequence, sequence ) ) )
            return false;

        connectionData.mostRecentAckedState[cubeId].acked = true;
        connectionData.mostRecentAckedState[cubeId].sequence = sequence;
        connectionData.mostRecentAckedState[cubeId].resetSequence = resetSequence;
        connectionData.mostRecentAckedState[cubeId].cubeState = cubeState;

        return true;
    }

    void Awake()
    {
        Assert.IsTrue( cubePrefab );

        layer = gameObject.layer;

        InitializeAvatars();

        InitializeCubePositionData();

        CreateCubes();
    }

    public void FixedUpdate()
    {
        if ( !IsActive() )
            return;

        ProcessInteractions();

        CaptureSnapshot( lastSnapshot );

        ApplySnapshot( lastSnapshot, true, true );

        AddStateToRingBuffer();

        UpdateRemoteAvatars();

        UpdateCubeAuthority();

        simulationFrame++;
    }

    public void Update()
    {
        if ( !IsActive() )
            return;

        ProcessInteractions();

        Profiler.BeginSample( "UpdateAuthorityMaterials" );
        UpdateAuthorityMaterials();
        Profiler.EndSample();
        
        renderFrame++;
    }

    public void LateUpdate()
    {
        UpdateCubeSmoothing();
    }

    public void Reset()
    {
        Profiler.BeginSample( "Reset" );

        Assert.IsTrue( IsActive() );

        CreateCubes();

        if ( IsServer() )
        {
            for ( int clientIndex = 1; clientIndex < Constants.MaxClients; ++clientIndex )
            {
                ConnectionData connectionData = GetServerConnectionData( clientIndex );
                connectionData.sendDeltaBuffer.Reset();
                connectionData.receiveDeltaBuffer.Reset();
            }
        }
        else
        {
            ConnectionData connectionData = GetClientConnectionData();
            connectionData.sendDeltaBuffer.Reset();
            connectionData.receiveDeltaBuffer.Reset();
        }

        Profiler.EndSample();
    }

    void UpdateAuthorityMaterials()
    {
        for ( int i = 0; i < Constants.NumCubes; i++ )
        {
            NetworkInfo networkInfo = cubes[i].GetComponent<NetworkInfo>();

            Renderer renderer = networkInfo.smoothed.GetComponent<Renderer>();

            int authorityIndex = networkInfo.GetAuthorityIndex();

            renderer.material.Lerp( renderer.material, authorityMaterials[authorityIndex], authorityIndex != 0 ? 0.3f : 0.04f );
        }
    }

    public Vector3 GetOrigin()
    {
        return this.gameObject.transform.position;
    }

    void CreateCubes()
    {
        Profiler.BeginSample( "CreateCubes" );

        Vector3 origin = GetOrigin();

        for ( int i = 0; i < Constants.NumCubes; i++ )
        {
            if ( !cubes[i] )
            {
                // cube initial create

                cubes[i] = (GameObject) Instantiate( cubePrefab, cubePositions[i] + origin, Quaternion.identity );

                cubes[i].layer = this.gameObject.layer;

                var rigidBody = cubes[i].GetComponent<Rigidbody>();

                rigidBody.maxDepenetrationVelocity = Constants.PushOutVelocity;           // this is *extremely* important to reduce jitter in the remote view of large stacks of rigid bodies

                NetworkInfo networkInfo = cubes[i].GetComponent<NetworkInfo>();

                networkInfo.touching.layer = GetTouchingLayer();

                networkInfo.Initialize( this, i );
            }
            else
            {
                // cube already exists: force it back to initial state

                var rigidBody = cubes[i].GetComponent<Rigidbody>();

                if ( rigidBody.IsSleeping() )
                    rigidBody.WakeUp();

                rigidBody.position = cubePositions[i] + origin;
                rigidBody.rotation = Quaternion.identity;
                rigidBody.velocity = Vector3.zero;
                rigidBody.angularVelocity = Vector3.zero;

                ResetCubeRingBuffer( i );

                NetworkInfo networkInfo = cubes[i].GetComponent<NetworkInfo>();

                networkInfo.DetachCubeFromPlayer();

                networkInfo.SetAuthorityIndex( 0 );
                networkInfo.SetAuthoritySequence( 0 );
                networkInfo.SetOwnershipSequence( 0 );

                Renderer renderer = networkInfo.smoothed.GetComponent<Renderer>();

                renderer.material = authorityMaterials[0];

                networkInfo.m_positionError = Vector3.zero;
                networkInfo.m_rotationError = Quaternion.identity;

                cubes[i].transform.parent = null;
            }
        }

        Profiler.EndSample();
    }

    void ShowContext( bool show )
    {
        ShowHideObject( gameObject, show );

        for ( int i = 0; i < Constants.NumCubes; i++ )
        {
            if ( cubes[i] )
            {
                NetworkInfo networkInfo = cubes[i].GetComponent<NetworkInfo>();
                Renderer renderer = networkInfo.smoothed.GetComponent<Renderer>();
                renderer.enabled = show;
            }
        }
    }

    public void FreezeCubes( bool freeze )
    {
        for ( int i = 0; i < Constants.NumCubes; i++ )
        {
            if ( cubes[i] )
            {
                var rigidBody = cubes[i].GetComponent<Rigidbody>();
                rigidBody.isKinematic = freeze;
            }
        }
    }

    public void CollisionCallback( int cubeId1, int cubeId2, Collision collision )
    {
        if ( collision.relativeVelocity.sqrMagnitude > Constants.HighEnergyCollisionThreshold * Constants.HighEnergyCollisionThreshold )
        {
            lastHighEnergyCollisionFrame[cubeId1] = simulationFrame;
            if ( cubeId2 != -1 )
            {
                lastHighEnergyCollisionFrame[cubeId2] = simulationFrame;
            }
        }
    }

    public void OnTouchStart( int cubeId1, int cubeId2 )
    {
        interactions.AddInteraction( (ushort) cubeId1, (ushort) cubeId2 );
    }

    public void OnTouchFinish( int cubeId1, int cubeId2 )
    {
        interactions.RemoveInteraction( (ushort) cubeId1, (ushort) cubeId2 );
    }

    public void RecurseSupportObjects( GameObject gameObject, ref HashSet<GameObject> support )
    {
        if ( support.Contains( gameObject ) )
            return;

        support.Add( gameObject );

        NetworkInfo networkInfo = gameObject.GetComponent<NetworkInfo>();

        int cubeId = networkInfo.GetCubeId();

        Interactions.Entry entry = interactions.GetInteractions( cubeId );

        for ( int i = 0; i < Constants.NumCubes; ++i )
        {
            if ( entry.interactions[i] == 0 )
                continue;

            if ( cubes[i].layer != layer )
                continue;

            if ( cubes[i].transform.position.y < gameObject.transform.position.y + Constants.SupportHeightThreshold )
                continue;

            RecurseSupportObjects( cubes[i], ref support );
        }
    }

    public List<GameObject> GetSupportList( GameObject gameObject )
    {
        // Support objects are used to determine the set of objects that should be woken up when you grab a cube.
        // Without this, objects resting on the cube you grab stay floating in the air. This function tries to only
        // wake up objects that are above (resting on) the game object that is being recursively walked. The idea being
        // if you grab an object in the middle of a stack, it wakes up any objects above it, but not below or to the side.

        HashSet<GameObject> support = new HashSet<GameObject>();

        RecurseSupportObjects( gameObject, ref support );

        var list = new List<GameObject>();

        foreach ( GameObject obj in support )
        {
            list.Add( obj );
        }

        return list;
    }

    public void TakeAuthorityOverObject( NetworkInfo networkInfo )
    {
        Assert.IsTrue( networkInfo.GetAuthorityIndex() == 0 );
#if DEBUG_AUTHORITY
        Debug.Log( "client " + clientIndex + " took authority over cube " + networkInfo.GetCubeId() );
#endif // #if DEBUG_AUTHORITY
        networkInfo.SetAuthorityIndex( authorityIndex );
        networkInfo.IncreaseAuthoritySequence();
        if ( !IsServer() )
            networkInfo.ClearConfirmed();
        else
            networkInfo.SetConfirmed();
    }

    void RecurseInteractions( int cubeId )
    {
        if ( visited.Contains( cubeId ) )
        {
            Assert.IsTrue( cubes[cubeId].GetComponent<NetworkInfo>().GetAuthorityIndex() == authorityIndex );
            return;
        }

        visited.Add( cubeId );

        Interactions.Entry entry = interactions.GetInteractions( cubeId );

        for ( int i = 0; i < Constants.NumCubes; ++i )
        {
            if ( entry.interactions[i] == 0 )
                continue;

            var networkInfo = cubes[i].GetComponent<NetworkInfo>();
            if ( networkInfo.GetAuthorityIndex() != 0 )
                continue;

            TakeAuthorityOverObject( networkInfo );

            RecurseInteractions( i );
        }
    }

    void ProcessInteractions()
    {
        Profiler.BeginSample( "ProcessInteractions" );

        visited.Clear();

        for ( int i = 0; i < Constants.NumCubes; ++i )
        {
            var networkInfo = cubes[i].GetComponent<NetworkInfo>();

            if ( networkInfo.GetAuthorityIndex() != authorityIndex )
                continue;

            if ( networkInfo.IsHeldByPlayer() )
                continue;

            var rigidBody = cubes[i].GetComponent<Rigidbody>();
            if ( rigidBody.IsSleeping() )
                continue;

            RecurseInteractions( (ushort) i );
        }

        Profiler.EndSample();
    }

    void UpdateRemoteAvatars()
    {
        Profiler.BeginSample( "UpdateRemoteAvatars" );

        for ( int i = 0; i < Constants.MaxClients; ++i )
        {
            var remoteAvatar = GetRemoteAvatar( i );

            if ( !remoteAvatar )
                continue;

            remoteAvatar.Update();
        }

        Profiler.EndSample();
    }

    void UpdateCubeAuthority()
    {
        Profiler.BeginSample( "UpdateCubeAuthority" );

        /*
         * After objects have been at rest for some period of time they return to default authority (white).
         * This logic runs on the client that has authority over the object. To avoid race conditions where the 
         * first client to activate an object and put it to rest wins in case of a conflict, the client delays
         * returning an object to default authority until after it has received confirmation from the server that
         * it has authority over that object.
         */

        for ( int i = 0; i < Constants.NumCubes; ++i )
        {
            var networkInfo = cubes[i].GetComponent<NetworkInfo>();
            if ( networkInfo.GetAuthorityIndex() != clientIndex + 1 )
                continue;

            if ( !networkInfo.IsConfirmed() )
                continue;

            var rigidBody = cubes[i].GetComponent<Rigidbody>();
            if ( rigidBody.IsSleeping() && networkInfo.GetLastActiveFrame() + Constants.ReturnToDefaultAuthorityFrames < simulationFrame )
            {
#if DEBUG_AUTHORITY
                Debug.Log( "client " + clientIndex + " returns cube " + i + " to default authority. increases authority sequence (" + networkInfo.GetAuthoritySequence() + "->" + (ushort) ( networkInfo.GetAuthoritySequence() + 1 ) + ") and sets pending commit flag" );
#endif // #if DEBUG_AUTHORITY
                networkInfo.SetAuthorityIndex( 0 );
                networkInfo.IncreaseAuthoritySequence();
                if ( IsClient() )
                    networkInfo.SetPendingCommit();
            }
        }

        Profiler.EndSample();
    }

    public void UpdateCubePriority()
    {
        Profiler.BeginSample( "UpdateCubeAuthority" );

        Assert.IsTrue( IsActive() );

        if ( IsServer() )
        {
            for ( int clientIndex = 1; clientIndex < Constants.MaxClients; ++clientIndex )
            {
                ConnectionData connectionData = GetServerConnectionData( clientIndex );
                UpdateCubePriorityForConnection( connectionData );
            }
        }
        else
        {
            ConnectionData connectionData = GetClientConnectionData();
            UpdateCubePriorityForConnection( connectionData );
        }

        Profiler.EndSample();
    }

    void UpdateCubeSmoothing()
    {
        for ( int i = 0; i < Constants.NumCubes; ++i )
        {
            var networkInfo = cubes[i].GetComponent<NetworkInfo>();

            networkInfo.UpdateSmoothing();
        }
    }

    void UpdateCubePriorityForConnection( ConnectionData connectionData )
    {
        var snapshot = lastSnapshot;

        Assert.IsTrue( snapshot != null );

        long simFrame = (long) GetSimulationFrame();

        for ( int i = 0; i < Constants.NumCubes; ++i )
        {
            var networkInfo = cubes[i].GetComponent<NetworkInfo>();

            // don't send state updates held cubes. they are synchronized differently.
            if ( networkInfo.IsHeldByPlayer() )
            {
                connectionData.priorityData[i].accumulator = -1;
                continue;
            }

            // only send white cubes from client -> server if they are pending commit after returning to default authority
            if ( IsClient() && networkInfo.GetAuthorityIndex() == 0 && !networkInfo.IsPendingCommit() )
            {
                connectionData.priorityData[i].accumulator = -1;
                continue;
            }

            // base priority
            float priority = 1.0f;

            // higher priority for cubes that were recently in a high energy collision
            if ( lastHighEnergyCollisionFrame[i] + Constants.HighEnergyCollisionPriorityBoostNumFrames >= (ulong) simFrame )
                priority = 10.0f;

            // *extremely* high priority for cubes that were just thrown by a player
            if ( networkInfo.GetLastPlayerInteractionFrame() + Constants.ThrownObjectPriorityBoostNumFrames >= simFrame )
                priority = 1000000.0f;

            connectionData.priorityData[i].accumulator += priority;
        }
    }

    public void GetMostImportantCubeStateUpdates( ConnectionData connectionData, ref int numStateUpdates, ref int[] cubeIds, ref CubeState[] cubeState )
    {
        Assert.IsTrue( numStateUpdates >= 0 );
        Assert.IsTrue( numStateUpdates <= Constants.NumCubes );

        if ( numStateUpdates == 0 )
            return;

        var prioritySorted = new PriorityData[Constants.NumCubes];

        for ( int i = 0; i < Constants.NumCubes; ++i )
            prioritySorted[i] = connectionData.priorityData[i];

        Array.Sort( prioritySorted, ( x, y ) => y.accumulator.CompareTo( x.accumulator ) );

        int maxStateUpdates = numStateUpdates;

        numStateUpdates = 0;

        for ( int i = 0; i < Constants.NumCubes; ++i )
        {
            if ( numStateUpdates == maxStateUpdates )
                break;

            // IMPORTANT: Negative priority means don't send this cube!
            if ( prioritySorted[i].accumulator < 0.0f )
                continue;

            cubeIds[numStateUpdates] = prioritySorted[i].cubeId;
            cubeState[numStateUpdates] = lastSnapshot.cubeState[cubeIds[numStateUpdates]];

            ++numStateUpdates;
        }
    }

    void UpdatePendingCommit( NetworkInfo networkInfo, int authorityIndex, int fromClientIndex, int toClientIndex )
    {
        if ( networkInfo.IsPendingCommit() && authorityIndex != toClientIndex + 1 )
        {
#if DEBUG_AUTHORITY
            Debug.Log( "client " + toClientIndex + " sees update for cube " + networkInfo.GetCubeId() + " from client " + fromClientIndex + " with authority index (" + authorityIndex + ") and clears pending commit flag" );
#endif // #if DEBUG_AUTHORITY
            networkInfo.ClearPendingCommit();
        }
    }

    public void ApplyCubeStateUpdates( int numStateUpdates, ref int[] cubeIds, ref CubeState[] cubeState, int fromClientIndex, int toClientIndex, bool applySmoothing = true )
    {
        Vector3 origin = this.gameObject.transform.position;

        for ( int i = 0; i < numStateUpdates; ++i )
        {
            if ( AuthoritySystem.ShouldApplyCubeUpdate( this, cubeIds[i], cubeState[i].ownershipSequence, cubeState[i].authoritySequence, cubeState[i].authorityIndex, false, fromClientIndex, toClientIndex ) )
            {
                GameObject cube = cubes[cubeIds[i]];

                var networkInfo = cube.GetComponent<NetworkInfo>();
                var rigidBody = cube.GetComponent<Rigidbody>();

                UpdatePendingCommit( networkInfo, cubeState[i].authorityIndex, fromClientIndex, toClientIndex );

                Snapshot.ApplyCubeState( rigidBody, networkInfo, ref cubeState[i], ref origin, applySmoothing );
            }
        }
    }

    public void ApplyAvatarStateUpdates( int numAvatarStates, ref AvatarState[] avatarState, int fromClientIndex, int toClientIndex )
    {
        for ( int i = 0; i < numAvatarStates; ++i )
        {
            if ( toClientIndex == 0 && avatarState[i].client_index != fromClientIndex )
                continue;

            var remoteAvatar = GetRemoteAvatar( avatarState[i].client_index );

            if ( avatarState[i].left_hand_holding_cube && AuthoritySystem.ShouldApplyCubeUpdate( this, avatarState[i].left_hand_cube_id, avatarState[i].left_hand_ownership_sequence, avatarState[i].left_hand_authority_sequence, avatarState[i].client_index + 1, true, fromClientIndex, toClientIndex ) )
            {
                GameObject cube = cubes[avatarState[i].left_hand_cube_id];

                var networkInfo = cube.GetComponent<NetworkInfo>();

                UpdatePendingCommit( networkInfo, avatarState[i].client_index + 1, fromClientIndex, toClientIndex );

                remoteAvatar.ApplyLeftHandUpdate( ref avatarState[i] );
            }

            if ( avatarState[i].right_hand_holding_cube && AuthoritySystem.ShouldApplyCubeUpdate( this, avatarState[i].right_hand_cube_id, avatarState[i].right_hand_ownership_sequence, avatarState[i].right_hand_authority_sequence, avatarState[i].client_index + 1, true, fromClientIndex, toClientIndex ) )
            {
                GameObject cube = cubes[avatarState[i].right_hand_cube_id];

                var networkInfo = cube.GetComponent<NetworkInfo>();

                UpdatePendingCommit( networkInfo, avatarState[i].client_index + 1, fromClientIndex, toClientIndex );

                remoteAvatar.ApplyRightHandUpdate( ref avatarState[i] );
            }

            remoteAvatar.ApplyAvatarPose( ref avatarState[i] );
        }
    }

    public void ResetCubePriority( ConnectionData connectionData, int numCubes, int[] cubeIds )
    {
        for ( int i = 0; i < numCubes; ++i )
        {
            connectionData.priorityData[cubeIds[i]].accumulator = 0.0f;
        }
    }

    void AddStateToRingBuffer()
    {
        Profiler.BeginSample( "AddStateToRingBuffer" );

        int baseIndex = 0;

        var axis = new Vector3( 1, 0, 0 );

        for ( int i = 0; i < Constants.NumCubes; i++ )
        {
            var rigidBody = cubes[i].GetComponent<Rigidbody>();

            int index = baseIndex + (int) ( simulationFrame % Constants.RingBufferSize );

            ringBuffer[index].position = rigidBody.position;
            ringBuffer[index].axis = rigidBody.rotation * axis;

            baseIndex += Constants.RingBufferSize;
        }

        Profiler.EndSample();
    }

    public void ResetCubeRingBuffer( int cubeId )
    {
        int baseIndex = Constants.RingBufferSize * cubeId;

        for ( int i = 0; i < Constants.RingBufferSize; ++i )
        {
            ringBuffer[baseIndex + i].position = new Vector3( 1000000, 1000000, 1000000 );
        }

        int index = baseIndex + (int) ( simulationFrame % Constants.RingBufferSize );

        ringBuffer[index].position = Vector3.zero;
    }

    public void CheckForAtRestObjects()
    {
        Profiler.BeginSample( "CheckForAtRestObjects" );

        int baseIndex = 0;

        for ( int i = 0; i < Constants.NumCubes; i++ )
        {
            var rigidBody = cubes[i].GetComponent<Rigidbody>();
            var networkInfo = cubes[i].GetComponent<NetworkInfo>();
            if ( rigidBody.IsSleeping() )
            {
                baseIndex += Constants.RingBufferSize;
                continue;
            }

            networkInfo.SetLastActiveFrame( simulationFrame );

            Vector3 currentPosition = ringBuffer[baseIndex].position;
            Vector3 currentAxis = ringBuffer[baseIndex].axis;

            bool goToSleep = true;

            for ( int j = 1; j < Constants.RingBufferSize; ++j )
            {
                int index = baseIndex + j;

                Vector3 positionDifference = ringBuffer[index].position - currentPosition;

                if ( positionDifference.sqrMagnitude > 0.01f * 0.01f )
                {
                    goToSleep = false;
                    break;
                }

                float axisDot = Vector3.Dot( ringBuffer[index].axis, currentAxis );
                if ( axisDot < 0.9999f )
                {
                    goToSleep = false;
                    break;
                }
            }

            if ( goToSleep )
            {
                rigidBody.Sleep();
            }

            baseIndex += Constants.RingBufferSize;
        }

        Profiler.EndSample();
    }

    public void CaptureSnapshot( Snapshot snapshot )
    {
        Profiler.BeginSample( "CaptureSnapshot" );

        Vector3 origin = this.gameObject.transform.position;

        for ( int i = 0; i < Constants.NumCubes; i++ )
        {
            var rigidBody = cubes[i].GetComponent<Rigidbody>();

            var networkInfo = cubes[i].GetComponent<NetworkInfo>();

            Snapshot.GetCubeState( rigidBody, networkInfo, ref snapshot.cubeState[i], ref origin );
        }

        Profiler.EndSample();
    }

    public void ApplySnapshot( Snapshot snapshot, bool skipAlreadyAtRest, bool skipHeldObjects )
    {
        Profiler.BeginSample( "ApplySnapshot" );

        Vector3 origin = this.gameObject.transform.position;

        for ( int i = 0; i < Constants.NumCubes; i++ )
        {
            var networkInfo = cubes[i].GetComponent<NetworkInfo>();

            if ( skipHeldObjects && networkInfo.IsHeldByPlayer() )
                continue;

            var rigidBody = cubes[i].GetComponent<Rigidbody>();

            if ( skipAlreadyAtRest && !snapshot.cubeState[i].active && rigidBody.IsSleeping() )
                continue;

            Snapshot.ApplyCubeState( rigidBody, networkInfo, ref snapshot.cubeState[i], ref origin );
        }

        Profiler.EndSample();
    }

    public Snapshot GetLastSnapshot()
    {
        return lastSnapshot;
    }

    void InitializeAvatars()
    {
        for ( int i = 0; i < Constants.MaxClients; ++i )
        {
            RemoteAvatar remoteAvatar = GetRemoteAvatar( i );

            if ( !remoteAvatar )
                continue;

            remoteAvatar.SetContext( this );
            remoteAvatar.SetClientIndex( i );
        }
    }

    void InitializePriorityData( ConnectionData connectionData )
    {
        for ( int i = 0; i < Constants.NumCubes; ++i )
        {
            connectionData.priorityData[i].cubeId = i;
        }
    }

    void InitializeCubePositionData()
    {
#if DEBUG_AUTHORITY

        cubePositions[0] = new Vector3( -2, 10, 0 );
        cubePositions[1] = new Vector3( -1, 10, 0 );
        cubePositions[2] = new Vector3( -0, 10, 0 );
        cubePositions[3] = new Vector3( +1, 10, 0 );
        cubePositions[4] = new Vector3( +2, 10, 0 );

#else // #if DEBUG_AUTHORITY

        cubePositions[0] = new Vector3( 3.299805f, 11.08789f, 0.2001948f );
        cubePositions[1] = new Vector3( -0.9501953f, 19.88574f, 0.7001948f );
        cubePositions[2] = new Vector3( -0.5996094f, 20.81055f, -1.008789f );
        cubePositions[3] = new Vector3( 3.816406f, 20.78223f, 1.022461f );
        cubePositions[4] = new Vector3( 3.922852f, 22.29199f, 1.323242f );
        cubePositions[5] = new Vector3( -0.04296875f, 11.92383f, -0.8212891f );
        cubePositions[6] = new Vector3( 0.2001953f, 18.6875f, -0.2001948f );
        cubePositions[7] = new Vector3( -2.599609f, 20.08789f, -0.0996089f );
        cubePositions[8] = new Vector3( 2.299805f, 20.48535f, 0.2001948f );
        cubePositions[9] = new Vector3( -3.482422f, 21.58398f, -0.9824219f );
        cubePositions[10] = new Vector3( -1.5f, 15.08496f, -0.2001948f );
        cubePositions[11] = new Vector3( 1.099609f, 18.8877f, -0.4003911f );
        cubePositions[12] = new Vector3( -1.299805f, 12.48535f, -0.5996089f );
        cubePositions[13] = new Vector3( 2.200195f, 21.1875f, 1.900391f );
        cubePositions[14] = new Vector3( 2.900391f, 22.58789f, -0.5996089f );
        cubePositions[15] = new Vector3( -0.5996094f, 21.89941f, -0.7998052f );
        cubePositions[16] = new Vector3( 1.200195f, 19.6875f, -1.200195f );
        cubePositions[17] = new Vector3( -2.900391f, 13.6875f, -0.5f );
        cubePositions[18] = new Vector3( -1.773438f, 16.29688f, -0.6669922f );
        cubePositions[19] = new Vector3( -1.200195f, 17.59766f, -0.5f );
        cubePositions[20] = new Vector3( 0f, 13.98828f, 0.5996089f );
        cubePositions[21] = new Vector3( -1.299805f, 18.6875f, 0.4003911f );
        cubePositions[22] = new Vector3( -4f, 12.1875f, -1.799805f );
        cubePositions[23] = new Vector3( 3.5f, 15.10449f, 0.109375f );
        cubePositions[24] = new Vector3( -0.02050781f, 16.66699f, 0.202148f );
        cubePositions[25] = new Vector3( 1.099609f, 17.19043f, -1.617188f );
        cubePositions[26] = new Vector3( 1.299805f, 21.58789f, 0.0996089f );
        cubePositions[27] = new Vector3( 1.799805f, 12.1875f, 0.4003911f );
        cubePositions[28] = new Vector3( 3.828125f, 20.28418f, 1.139648f );
        cubePositions[29] = new Vector3( 1f, 14.98828f, -2f );
        cubePositions[30] = new Vector3( 3.700195f, 19.3877f, 0f );
        cubePositions[31] = new Vector3( 3.400391f, 12.78809f, 0.5f );
        cubePositions[32] = new Vector3( 2.599609f, 17.1875f, -1.5f );
        cubePositions[33] = new Vector3( -2.700195f, 20.3877f, 1.599609f );
        cubePositions[34] = new Vector3( 1.900391f, 13.78809f, 0.5996089f );
        cubePositions[35] = new Vector3( -0.9003906f, 15.1875f, -2f );
        cubePositions[36] = new Vector3( -1.400391f, 18.08789f, 0.5f );
        cubePositions[37] = new Vector3( 0.2558594f, 20.7168f, 0.9023442f );
        cubePositions[38] = new Vector3( -0.09960938f, 12.8877f, 0.4003911f );
        cubePositions[39] = new Vector3( -3.900391f, 15.98828f, -1.099609f );
        cubePositions[40] = new Vector3( 1.823242f, 13.60254f, -0.2412109f );
        cubePositions[41] = new Vector3( -2.900391f, 15.6875f, 0f );
        cubePositions[42] = new Vector3( -0.7998047f, 18.1875f, -0.5996089f );
        cubePositions[43] = new Vector3( -4f, 12.8877f, -2.5f );
        cubePositions[44] = new Vector3( 2.356445f, 24.45703f, 1.677734f );
        cubePositions[45] = new Vector3( 1.999023f, 16.95703f, 0.1943359f );
        cubePositions[46] = new Vector3( 3.246094f, 11.16699f, -0.7314448f );
        cubePositions[47] = new Vector3( 2.319336f, 21.33887f, 1.157227f );
        cubePositions[48] = new Vector3( 0.2998047f, 20.28809f, 1.700195f );
        cubePositions[49] = new Vector3( -1.299805f, 16.78906f, 0.2998052f );
        cubePositions[50] = new Vector3( 1.900391f, 13.5957f, -1.099609f );
        cubePositions[51] = new Vector3( 2.700195f, 17.6875f, -1.400391f );
        cubePositions[52] = new Vector3( 3.396484f, 12.81934f, -0.3037109f );
        cubePositions[53] = new Vector3( 0f, 13.28809f, -1.200195f );
        cubePositions[54] = new Vector3( 0.2001953f, 19.78809f, 1.599609f );
        cubePositions[55] = new Vector3( 3.799805f, 22.98828f, 2.299805f );
        cubePositions[56] = new Vector3( 0.07128906f, 18.74121f, 0.6630859f );
        cubePositions[57] = new Vector3( -1f, 14.3877f, -1.299805f );
        cubePositions[58] = new Vector3( -0.01367188f, 13.70801f, -0.390625f );
        cubePositions[59] = new Vector3( 2.202148f, 20.27637f, 0.7470698f );
        cubePositions[60] = new Vector3( 0.078125f, 18.02441f, 0.7080078f );
        cubePositions[61] = new Vector3( 0.2998047f, 21.48828f, 1.900391f );
        cubePositions[62] = new Vector3( -2.799805f, 16.78809f, 1f );
        cubePositions[63] = new Vector3( -1.529297f, 19.92676f, -0.07519484f );

#endif // #if DEBUG_AUTHORITY
    }

    public void WriteCubePositionsToFile( String filename )
    {
        Vector3 origin = this.gameObject.transform.position;

        using ( System.IO.StreamWriter file = new System.IO.StreamWriter( filename ) )
        {
            for ( int i = 0; i < Constants.NumCubes; i++ )
            {
                var rigidBody = cubes[i].GetComponent<Rigidbody>();

                Vector3 position = rigidBody.position - origin;

                file.WriteLine( "cubePositions[" + i + "] = new Vector3( " + position.x + "f, " + position.y + "f, " + position.z + "f );" );
            }
        }
    }

    public void TestSmoothing()
    {
        for ( int i = 0; i < Constants.NumCubes; i++ )
        {
            var networkInfo = cubes[i].GetComponent<NetworkInfo>();

            networkInfo.MoveWithSmoothing( cubes[i].transform.position + new Vector3( 0, 10, 0 ), Quaternion.identity );

            var rigidBody = cubes[i].GetComponent<Rigidbody>();

            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }
    }
}
