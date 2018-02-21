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

public struct AuthoritySystem
{
    /*
     *  This function determines when we should apply state updates to cubes.
     *  It is designed to allow clients to pre-emptively take authority over cubes when
     *  they grab and interact with them indirectly (eg. throwing cubes at other cubes).
     *  In short, ownership sequence increases each time a player grabs a cube, and authority
     *  sequence increases each time a cube is touched by a cube under authority of that player.
     *  When a client sees a cube under its authority has come to rest, it returns that cube to
     *  default authority and commits its result back to the server. The logic below implements
     *  this direction of flow, as well as resolving conflicts when two clients think they both
     *  own the same cube, or have interacted with the same cube. The first player to interact, 
     *  from the point of view of the server (client 0), wins.
     */

    public static bool ShouldApplyCubeUpdate( Context context, int cubeId, ushort ownershipSequence, ushort authoritySequence, int authorityIndex, bool fromAvatar, int fromClientIndex, int toClientIndex )
    {
        var cube = context.GetCube( cubeId );

        var networkInfo = cube.GetComponent<NetworkInfo>();

        ushort localOwnershipSequence = networkInfo.GetOwnershipSequence();
        ushort localAuthoritySequence = networkInfo.GetAuthoritySequence();
        int localAuthorityIndex = networkInfo.GetAuthorityIndex();

        // *** OWNERSHIP SEQUENCE ***

        // Must accept if ownership sequence is newer
        if ( Network.Util.SequenceGreaterThan( ownershipSequence, localOwnershipSequence ) )
        {
#if DEBUG_AUTHORITY
            Debug.Log( "client " + toClientIndex + " sees new ownership sequence (" + localOwnershipSequence + "->" + ownershipSequence + ") for cube " + cubeId + " and accepts update" );
#endif // #if DEBUG_AUTHORITY
            return true;
        }

        // Must reject if ownership sequence is older
        if ( Network.Util.SequenceLessThan( ownershipSequence, localOwnershipSequence ) )
            return false;

        // *** AUTHORITY SEQUENCE ***

        // accept if the authority sequence is newer
        if ( Network.Util.SequenceGreaterThan( authoritySequence, localAuthoritySequence ) )
        {
#if DEBUG_AUTHORITY
            Debug.Log( "client " + toClientIndex + " sees new authority sequence (" + localAuthoritySequence + "->" + authoritySequence + ") for cube " + cubeId + " and accepts update" );
#endif // #if DEBUG_AUTHORITY
            return true;
        }

        // reject if the authority sequence is older
        if ( Network.Util.SequenceLessThan( authoritySequence, localAuthoritySequence ) )
            return false;

        // Both sequence numbers are the same. Resolve authority conflicts!
        if ( fromClientIndex == 0 )
        {
            // =============================
            //       server -> client
            // =============================

            // ignore if the server says the cube is under authority of this client. the server is just confirming we have authority
            if ( authorityIndex == toClientIndex + 1 )
            {
                if ( !networkInfo.IsConfirmed() )
                {
#if DEBUG_AUTHORITY
                    Debug.Log( "client " + fromClientIndex + " confirms client " + toClientIndex + " has authority over cube " + cubeId + " (" + ownershipSequence + "," + authoritySequence + ")" );
#endif // #if DEBUG_AUTHORITY
                    networkInfo.SetConfirmed();
                }
                return false;
            }

            // accept if the server says the cube is under authority of another client
            if ( authorityIndex != 0 && authorityIndex != toClientIndex + 1 )
            {
                if ( localAuthorityIndex == toClientIndex + 1 )
                {
#if DEBUG_AUTHORITY
                    Debug.Log( "client " + toClientIndex + " lost authority over cube " + cubeId + " to client " + ( authorityIndex - 1 ) + " (" + ownershipSequence + "," + authoritySequence + ")" );
#endif // #if DEBUG_AUTHORITY
                }
                return true;
            }

            // ignore if the server says the cube is default authority, but the client has already taken authority over the cube
            if ( authorityIndex == 0 && localAuthorityIndex == toClientIndex + 1 )
                return false;

            // accept if the server says the cube is default authority, and on the client it is also default authority
            if ( authorityIndex == 0 && localAuthorityIndex == 0 )
                return true;
        }
        else
        {
            // =============================
            //       client -> server
            // =============================

            // reject if the cube is not under authority of the client
            if ( authorityIndex != fromClientIndex + 1 )
                return false;

            // accept if the cube is under authority of this client
            if ( localAuthorityIndex == fromClientIndex + 1 )
                return true;
        }

        // otherwise, reject.
        return false;
    }
}
