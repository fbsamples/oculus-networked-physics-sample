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

public class NetworkInfo : MonoBehaviour
{
    public GameObject smoothed;
    public GameObject touching;

    public enum HoldType
    {
        None,                                               // not currently being held
        LeftHand,                                           // held by left touch controller
        RightHand,                                          // held by right touch controller
    };

    public void Initialize( Context context, int cubeId )
    {
        m_context = context;
        m_cubeId = cubeId;
    
        touching.GetComponent<Touching>().Initialize( context, cubeId );

        smoothed.transform.parent = null;
    }

    public void SetAuthorityIndex( int authorityIndex ) { m_authorityIndex = authorityIndex; }
    public void IncreaseAuthoritySequence() { m_authoritySequence++; }
    public void IncreaseOwnershipSequence() { m_ownershipSequence++; }
    public void SetAuthoritySequence( ushort sequence ) { m_authoritySequence = sequence; }
    public void SetOwnershipSequence( ushort sequence ) { m_ownershipSequence = sequence; }
    public void SetLastActiveFrame( ulong frame ) { m_lastActiveFrame = frame; }
    public void ClearConfirmed() { m_confirmed = false; }
    public void SetConfirmed() { m_confirmed = true; }
    public bool IsConfirmed() { return m_confirmed; }
    public void SetPendingCommit() { m_pendingCommit = true; }
    public void ClearPendingCommit() { m_pendingCommit = false; }
    public bool IsPendingCommit() { return m_pendingCommit; }

    public int GetCubeId() { return m_cubeId; }
    public int GetAuthorityIndex() { return m_authorityIndex; }
    public int GetHoldClientIndex() { return m_holdClientIndex; }
    public long GetLastPlayerInteractionFrame() { return m_lastPlayerInteractionFrame; }
    public void SetLastPlayerInteractionFrame( long frame ) { m_lastPlayerInteractionFrame = (long) frame; }
    public bool IsHeldByPlayer() { return m_holdClientIndex != -1; }
    public bool IsHeldByLocalPlayer() { return m_localAvatar != null; }
    public bool IsHeldByRemotePlayer( RemoteAvatar remoteAvatar, RemoteAvatar.HandData hand ) { return m_remoteAvatar == remoteAvatar && m_remoteHand == hand; }
    public ushort GetOwnershipSequence() { return m_ownershipSequence; }
    public ushort GetAuthoritySequence() { return m_authoritySequence; }
    public ulong GetLastActiveFrame() { return m_lastActiveFrame; }
    public HoldType GetHoldType() { return m_holdType; }

    /*
     * Return true if the local player can grab this cube
     * This is true if:
     *  1. No other player is currently grabbing that cube (common case)
     *  2. The local player already grabbing the cube, and the time the cube was grabbed is older than the current input to grab this cube. This allows passing cubes from hand to hand.
     */

    public bool CanLocalPlayerGrabCube( ulong gripInputStartFrame )
    {      
        if ( m_holdClientIndex == -1 )
            return true;

        if ( m_localHand != null && m_localHand.gripObjectStartFrame < gripInputStartFrame )
            return true;

        return false;
    }

    /*
     * Attach cube to local player.
     */

    public void AttachCubeToLocalPlayer( Avatar avatar, Avatar.HandData hand )
    {
        DetachCubeFromPlayer();

        m_localAvatar = avatar;
        m_localHand = hand;
        m_holdType = ( hand.id == Avatar.LeftHand ) ? HoldType.LeftHand : HoldType.RightHand;
        m_holdClientIndex = m_context.GetClientIndex();
        m_authorityIndex = m_context.GetAuthorityIndex();
        
        IncreaseOwnershipSequence();
        
        SetAuthoritySequence( 0 );

        touching.GetComponent<BoxCollider>().isTrigger = false;

        var rigidBody = gameObject.GetComponent<Rigidbody>();

        rigidBody.isKinematic = true;

        hand.gripObject = gameObject;

        gameObject.layer = m_context.GetGripLayer();
               
        gameObject.transform.SetParent( hand.transform, true );

        hand.gripObjectSupportList = m_context.GetSupportList( hand.gripObject );

        avatar.CubeAttached( ref hand );
    }

    /*
     * Attach cube to remote player
     */

    public void AttachCubeToRemotePlayer( RemoteAvatar avatar, RemoteAvatar.HandData hand, int clientIndex )
    {
        Assert.IsTrue( clientIndex != m_context.GetClientIndex() );

        DetachCubeFromPlayer();

        hand.gripObject = gameObject;

        var rigidBody = gameObject.GetComponent<Rigidbody>();

        rigidBody.isKinematic = true;
        rigidBody.detectCollisions = false;

        gameObject.transform.SetParent( hand.transform, true );

        m_remoteAvatar = avatar;
        m_remoteHand = hand;
        m_holdClientIndex = clientIndex;
        m_authorityIndex = clientIndex + 1;

        avatar.CubeAttached( ref hand );
    }

    /*
     * Detach cube from any player who is holding it (local or remote).
     */

    public void DetachCubeFromPlayer()
    {
        if ( m_holdClientIndex == -1 )
            return;

        if ( m_localAvatar )
        {
            m_localAvatar.CubeDetached( ref m_localHand );

            touching.GetComponent<BoxCollider>().isTrigger = true;
        }

        if ( m_remoteAvatar )
        {
            m_remoteAvatar.CubeDetached( ref m_remoteHand );
        }

        m_localAvatar = null;
        m_localHand = null;
        m_remoteHand = null;
        m_remoteAvatar = null;
        m_holdType = HoldType.None;
        m_holdClientIndex = -1;
    }

    /*
     * Collision Callback.
     * This is used to call into the collision callback on the context that owns these cubes,
     * which is used to track authority transfer (poorly), and to increase network priority 
     * for cubes that were recently in high energy collisions with other cubes, or the floor.
     */

    void OnCollisionEnter( Collision collision )
    {
        GameObject gameObject2 = collision.gameObject;

        NetworkInfo networkInfo2 = gameObject2.GetComponent<NetworkInfo>();
        
        int cubeId1 = m_cubeId;
        int cubeId2 = -1;                   // IMPORTANT: cube id of -1 represents a collision with the floor

        if ( networkInfo2 != null )
            cubeId2 = networkInfo2.GetCubeId();
        
        m_context.CollisionCallback( cubeId1, cubeId2, collision );
    }	

    /*
     * Moves the physical cube immediately, while the visual cube smoothly eases towards the corrected position over time.
     */
    
    public void MoveWithSmoothing( Vector3 position, Quaternion rotation )
    {
        Rigidbody rigidBody = gameObject.GetComponent<Rigidbody>();

        Vector3 oldSmoothedPosition = rigidBody.position + m_positionError;
        Quaternion oldSmoothedRotation = rigidBody.rotation * m_rotationError;

        rigidBody.position = position;
        rigidBody.rotation = rotation;

        gameObject.transform.position = position;
        gameObject.transform.rotation = rotation;

        m_positionError = oldSmoothedPosition - position;
        m_rotationError = Quaternion.Inverse( rotation ) * oldSmoothedRotation;
    }

    /*
     * Local version of function to move with smoothing. Used for cubes held in remote avatar hands.
     */

    public void MoveWithSmoothingLocal( Vector3 localPosition, Quaternion localRotation )
    {
        Assert.IsTrue( gameObject.transform.parent != null );

        Vector3 oldSmoothedPosition = gameObject.transform.position + m_positionError;
        Quaternion oldSmoothedRotation = gameObject.transform.rotation * m_rotationError;

        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localRotation = localRotation;

        Vector3 position = gameObject.transform.position;
        Quaternion rotation = gameObject.transform.rotation;

        m_positionError = oldSmoothedPosition - position;
        m_rotationError = oldSmoothedRotation * Quaternion.Inverse( rotation );
    }

    /*
     * Ease the smoothed cube towards the physical cube by reducing the local error factors towards zero/identity.
     */

    public void UpdateSmoothing()
    {
#if DISABLE_SMOOTHING

        smoothed.transform.position = gameObject.transform.position;
        smoothed.transform.rotation = gameObject.transform.rotation;

#else // #if DISABLE_SMOOTHING

        const float epsilon = 0.000001f;

        float positionSmoothingFactor = 0.95f;
        float rotationSmoothingFactor = 0.95f;

        if ( gameObject.transform.parent != null )
        {
            // tight smoothing while held for player "snap to hand"
            positionSmoothingFactor = 0.7f;
            rotationSmoothingFactor = 0.85f;
        }

        if ( m_positionError.sqrMagnitude > epsilon )
        {
            m_positionError *= positionSmoothingFactor;
        }
        else
        {
            m_positionError = Vector3.zero;
        }
  
        if ( Math.Abs( m_rotationError.x ) > epsilon ||
             Math.Abs( m_rotationError.y ) > epsilon ||
             Math.Abs( m_rotationError.y ) > epsilon ||
             Math.Abs( 1.0f - m_rotationError.w ) > epsilon )
        {
            m_rotationError = Quaternion.Slerp( m_rotationError, Quaternion.identity, 1.0f - rotationSmoothingFactor );
        }
        else
        {
            m_rotationError = Quaternion.identity;
        }

        smoothed.transform.position = gameObject.transform.position + m_positionError;
        smoothed.transform.rotation = gameObject.transform.rotation * m_rotationError;

#endif // #if DISABLE_SMOOTHING
    }

    // =================================

    public Context m_context;                                           // the context that this cube exists in. eg. blue context, red context, for loopback testing.

    public int m_cubeId = -1;                                           // the cube id in range [0,NumCubes-1]

    public bool m_confirmed = false;                                    // true if this cube has been confirmed under client authority by the server.
                                                                    
    public bool m_pendingCommit = false;                                // true if this cube has returned to default authority and needs to be committed back to the server.
   
    public int m_authorityIndex;                                        // 0 = default authority (white), 1 = blue (client 0), 2 = red (client 2), and so on.
    
    public ushort m_ownershipSequence;                                  // sequence number increased on each ownership change (players grabs/release this cube)
    
    public ushort m_authoritySequence;                                  // sequence number increased on each authority change (eg. indirect interaction, such as being hit by an object thrown by a player)
    
    public int m_holdClientIndex = -1;                                  // client id of player currently holding this cube. -1 if not currently being held.

    public HoldType m_holdType = HoldType.None;                         // while this cube is being held, this identifies whether it is being held in the left or right hand, or by the headset + controller fallback.

    public Avatar m_localAvatar;                                        // while this cube is held by the local player, this points to the local avatar.
    
    public Avatar.HandData m_localHand;                                 // while this cube is held by the local player, this points to the local avatar hand that is holding it.
                                                                    
    public RemoteAvatar m_remoteAvatar;                                 // while this cube is held by a remote player, this points to the remote avatar.

    public RemoteAvatar.HandData m_remoteHand;                          // while this cube is held by a remote player, this points to the remote avatar hand that is holding it.

    public ulong m_lastActiveFrame = 0;                                 // the frame number this cube was last active (not at rest). used to return to default authority (white) some amount of time after coming to rest.
    
    public long m_lastPlayerInteractionFrame = -100000;                 // the last frame number this cube was held by a player. used to increase priority for objects for a few seconds after they are thrown.

    public Vector3 m_positionError = Vector3.zero;                      // the current position error between the physical cube and its visual representation.

    public Quaternion m_rotationError = Quaternion.identity;            // the current rotation error between the physical cube and its visual representation.
}
