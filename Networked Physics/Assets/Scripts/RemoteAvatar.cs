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

public class RemoteAvatar : OvrAvatarDriver
{
    const float LineWidth = 0.25f;

    int clientIndex;

    public void SetClientIndex( int clientIndex )
    {
        this.clientIndex = clientIndex;
    }

    public class HandData
    {
        public Animator animator;
        public Transform transform;
        public GameObject pointLine;
        public GameObject gripObject;
    };

    HandData leftHand = new HandData();
    HandData rightHand = new HandData();
    PoseFrame remotePose = new PoseFrame();
    Context context;

    public HandData GetLeftHand() { return leftHand; }
    public HandData GetRightHand() { return rightHand; }

    public void SetContext( Context context )
    {
        this.context = context;
    }

	void Start()
    {
        var oculusAvatar = (OvrAvatar) GetComponent( typeof( OvrAvatar ) );

        leftHand.animator = oculusAvatar.HandLeft.animator;
        rightHand.animator = oculusAvatar.HandRight.animator;

        leftHand.transform = oculusAvatar.HandLeftRoot;
        rightHand.transform = oculusAvatar.HandRightRoot;

        Assert.IsNotNull( leftHand.transform );
        Assert.IsNotNull( rightHand.transform );
	}

    void CreatePointingLine( ref HandData hand )
    {
        if ( !hand.pointLine )
        {
            hand.pointLine = (GameObject) Instantiate( context.remoteLinePrefabs[clientIndex], Vector3.zero, Quaternion.identity );

            Assert.IsNotNull( hand.pointLine );

            UpdatePointingLine( ref hand );
        }
    }

    void UpdatePointingLine( ref HandData hand )
    {
        if ( hand.pointLine )
        {
            var lineRenderer = hand.pointLine.GetComponent<LineRenderer>();

            Vector3 start = hand.transform.position;
            Vector3 finish = hand.gripObject.transform.position;

            if ( lineRenderer )
            {
                if ( ( finish - start ).magnitude >= 1 )
                {
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition( 0, start );
                    lineRenderer.SetPosition( 1, finish );
                    lineRenderer.startWidth = LineWidth;
                    lineRenderer.endWidth = LineWidth;
                }
                else
                {
                    lineRenderer.positionCount = 0;
                }
            }
        }
    }

    void DestroyPointingLine( ref HandData hand )
    {
        if ( hand.pointLine )
        {
            DestroyObject( hand.pointLine );

            hand.pointLine = null;
        }
    }

    public void CubeAttached( ref HandData hand )
    {
        CreatePointingLine( ref hand );
    }

    public void CubeDetached( ref HandData hand )
    {
        if ( !hand.gripObject )
            return;

        DestroyPointingLine( ref hand );

        var rigidBody = hand.gripObject.GetComponent<Rigidbody>();

        rigidBody.isKinematic = false;
        rigidBody.detectCollisions = true;

        hand.gripObject.transform.SetParent( null );

        hand.gripObject = null;
    }

    public void Update()
    {
        UpdateHand( ref leftHand );
        UpdateHand( ref rightHand );

        UpdatePointingLine( ref leftHand );
        UpdatePointingLine( ref rightHand );
    }

    public void UpdateHand( ref HandData hand )
    {
        if ( hand.gripObject )
        {
            // while an object is held, set its last interaction frame to the current sim frame. this is used to boost priority for the object when it is thrown.
            NetworkInfo networkInfo = hand.gripObject.GetComponent<NetworkInfo>();
            networkInfo.SetLastPlayerInteractionFrame( (long) context.GetSimulationFrame() );
        }
    }

    public bool GetAvatarState( out AvatarState state )
    {
        AvatarState.Initialize( out state, clientIndex, remotePose, leftHand.gripObject, rightHand.gripObject );
        return true;
    }

    public void ApplyAvatarPose( ref AvatarState state )
    {
        AvatarState.ApplyPose( ref state, clientIndex, remotePose, context );
    }

    public void ApplyLeftHandUpdate( ref AvatarState state )
    {
        AvatarState.ApplyLeftHandUpdate( ref state, clientIndex, context, this );
    }

    public void ApplyRightHandUpdate( ref AvatarState state )
    {
        AvatarState.ApplyRightHandUpdate( ref state, clientIndex, context, this );
    }

    public override bool GetCurrentPose( out PoseFrame pose )
    {
        pose = remotePose;
        return true;
    }

    public GameObject GetHead()
    {
        var oculusAvatar = (OvrAvatar) GetComponent( typeof( OvrAvatar ) );
        return oculusAvatar.Head.gameObject;
    }
}

