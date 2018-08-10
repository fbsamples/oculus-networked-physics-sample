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

public class Avatar: OvrAvatarLocalDriver
{
    const float LineWidth = 0.02f;
    const float RaycastDistance = 256.0f;
    const ulong PointDebounceFrames = 10;
    const ulong PointStickyFrames = 45;
    const float PointSphereCastStart = 10.0f;
    const float PointSphereCastRadius = 0.25f;
    const float MinimumCubeHeight = 0.1f;
    const float IndexThreshold = 0.5f;
    const float IndexStickyFrames = 45;
    const float GripThreshold = 0.75f;
    const float GrabDistance = 1.0f;
    const float GrabRadius = 0.05f;
    const float WarpDistance = GrabDistance * 0.5f;
    const float ZoomMinimum = 0.4f;
    const float ZoomMaximum = 20.0f;
    const float ZoomSpeed = 4.0f;
    const float RotateSpeed = 180.0f;
    const float StickThreshold = 0.65f;
    const int PostReleaseDisableSelectFrames = 20;
    const int ThrowRingBufferSize = 16;
    const float ThrowSpeed = 5.0f;
    const float HardThrowSpeed = 10.0f;
    const float MaxThrowSpeed = 20.0f;
    const float ThrowVelocityMinY = 2.5f;

    public GameObject linePrefab;

    OvrAvatar oculusAvatar;

    Context context;

    int contextLayerCubes;
    int contextLayerGrip;
    int contextLayerMask;

    public const int LeftHand = 0;
    public const int RightHand = 1;

    public struct HandInput
    {
        public float handTrigger;
        public float indexTrigger;
        public float previousIndexTrigger;
        public ulong indexPressFrame;
        public bool pointing;
        public bool x;
        public bool y;
        public Vector2 stick;
    }

    public enum HandState
    {
        Neutral,
        Pointing,
        Grip,
    }

    public class HandData
    {
        public int id;

        public string name;

        public HandInput input;

        public HandState state = HandState.Neutral;

        public Animator animator;

        public Transform transform;

        public bool touchingObject;

        public GameObject pointLine;
        public GameObject pointObject;
        public ulong pointObjectFrame;

        public GameObject gripObject;
        public ulong gripInputStartFrame;
        public ulong gripObjectStartFrame;

        public Vector3 previousHandPosition;
        public Vector3 previousGripObjectPosition;

        public struct ThrowRingBufferEntry
        {
            public bool valid;
            public float speed;
        };

        public int throwRingBufferIndex;
        public ThrowRingBufferEntry[] throwRingBufferEntries = new ThrowRingBufferEntry[ThrowRingBufferSize];

        public Quaternion previousHandRotation;
        public Quaternion previousGripObjectRotation;

        public ulong gripObjectReleaseFrame;
        public List<GameObject> gripObjectSupportList;

        public bool disableReleaseVelocity;
    };

    HandData leftHand = new HandData();

    HandData rightHand = new HandData();

    public void SetContext( Context context )
    {
        Assert.IsNotNull( context );

        this.context = context;

        ResetHand( ref leftHand );
        ResetHand( ref rightHand );

        contextLayerCubes = context.gameObject.layer;

        contextLayerGrip = context.gameObject.layer + 1;

        contextLayerMask = ( 1 << contextLayerCubes ) | ( 1 << contextLayerGrip );
    }

    void Start()
    {
        Assert.IsNotNull( linePrefab );

        oculusAvatar = (OvrAvatar) GetComponent( typeof( OvrAvatar ) );

        leftHand.id = LeftHand;
        rightHand.id = RightHand;

        leftHand.name = "left hand";
        rightHand.name = "right hand";

        leftHand.animator = oculusAvatar.HandLeft.animator;
        rightHand.animator = oculusAvatar.HandRight.animator;

        leftHand.transform = oculusAvatar.HandLeftRoot;
        rightHand.transform = oculusAvatar.HandRightRoot;

        Assert.IsNotNull( leftHand.transform );
        Assert.IsNotNull( rightHand.transform );
    }

    void Update()
    {
        Assert.IsNotNull( context );

        OvrAvatarDriver.PoseFrame frame;

        if ( oculusAvatar.Driver.GetCurrentPose( out frame ) )
        {
            UpdateHand( ref leftHand, frame );
            UpdateHand( ref rightHand, frame );
        }
    }

    void FixedUpdate()
    {
        Assert.IsNotNull( context );

        OvrAvatarDriver.PoseFrame frame;

        if ( oculusAvatar.Driver.GetCurrentPose( out frame ) )
        {
            UpdateHandFixed( ref leftHand, frame );
            UpdateHandFixed( ref rightHand, frame );
        }
    }

    void UpdateHand( ref HandData hand, OvrAvatarDriver.PoseFrame frame )
    {
        OvrAvatarDriver.HandPose handPose = ( hand.id == LeftHand ) ? frame.handLeftPose : frame.handRightPose;

        OvrAvatarDriver.ControllerPose controllerPose = ( hand.id == LeftHand ) ? frame.controllerLeftPose : frame.controllerRightPose;

        TranslateHandPoseToInput( ref handPose, ref controllerPose, ref hand.input );

        UpdateRotate( ref hand );

        UpdateZoom( ref hand );

        UpdateGrip( ref hand );

        DetectStateChanges( ref hand );

        UpdateCurrentState( ref hand );

        UpdateHeldObject( ref hand );
    }

    void UpdateHandFixed( ref HandData hand, OvrAvatarDriver.PoseFrame frame )
    {
        UpdateSnapToHand( ref hand );
    }

    void UpdateRotate( ref HandData hand )
    {
        if ( !hand.gripObject )
            return;

        float angle = 0.0f;

        if ( hand.input.stick.x <= -StickThreshold )
            angle = +RotateSpeed * Time.deltaTime;

        if ( hand.input.stick.x >= +StickThreshold )
            angle = -RotateSpeed * Time.deltaTime;

        Vector3 axis = hand.transform.forward;

        hand.gripObject.transform.RotateAround( hand.gripObject.transform.position, axis, angle );
    }

    void UpdateZoom( ref HandData hand )
    {
        if ( !hand.gripObject )
            return;

        Vector3 pointStart, pointDirection;

        GetIndexFingerStartPointAndDirection( ref hand, out pointStart, out pointDirection );

        Vector3 objectPosition = hand.gripObject.transform.position;

        float pointDistance = Vector3.Dot( objectPosition - pointStart, pointDirection );

        if ( hand.input.stick.y <= -StickThreshold )
        {
            // zoom in: sneaky trick, pull center of mass towards hand on zoom in!

            Vector3 delta = objectPosition - pointStart;

            float distance = delta.magnitude;

            Vector3 direction = delta / distance;

            if ( distance > ZoomMinimum )
            {
                distance -= ZoomSpeed * Time.deltaTime;

                if ( distance < ZoomMinimum )
                    distance = ZoomMinimum;

                hand.gripObject.transform.position = pointStart + direction * distance;
            }
        }

        if ( hand.input.stick.y >= +StickThreshold )
        {
            // zoom out: push out strictly along point direction. this lets objects grabbed up close always zoom out in a consistent direction

            if ( pointDistance < ZoomMaximum )
            {
                hand.gripObject.transform.position += ( ZoomSpeed * Time.deltaTime ) * pointDirection;
            }
        }
    }

    void UpdateSnapToHand( ref HandData hand )
    {
        if ( !hand.gripObject )
            return;

        Vector3 pointStart, pointDirection;

        GetIndexFingerStartPointAndDirection( ref hand, out pointStart, out pointDirection );

        if ( hand.input.indexTrigger >= IndexThreshold && hand.input.indexPressFrame + IndexStickyFrames >= context.GetRenderFrame() )
        {
            hand.input.indexPressFrame = 0;

            // warp to hand on index grip

            Vector3 delta = hand.gripObject.transform.position - pointStart;

            float distance = delta.magnitude;

            Vector3 direction = delta / distance;

            if ( distance > WarpDistance )
            {
                distance = WarpDistance;

                if ( distance < ZoomMinimum )
                    distance = ZoomMinimum;

                Rigidbody rigidBody = hand.gripObject.GetComponent<Rigidbody>();

                NetworkInfo networkInfo = hand.gripObject.GetComponent<NetworkInfo>();

                networkInfo.MoveWithSmoothing( pointStart + direction * distance, rigidBody.rotation );
            }

            // clear the throw ring buffer

            for ( int i = 0; i < ThrowRingBufferSize; ++i )
            {
                hand.throwRingBufferEntries[i].valid = false;
                hand.throwRingBufferEntries[i].speed = 0.0f;
            }                                         
        }
    }

    void UpdateGrip( ref HandData hand )
    {
        if ( hand.input.handTrigger > GripThreshold )
        {
            if ( hand.gripInputStartFrame == 0 )
            {
                hand.gripInputStartFrame = context.GetRenderFrame();
            }
        }
        else
        {
            hand.gripInputStartFrame = 0;
        }
    }

    void UpdateHeldObject( ref HandData hand )
    {
        if ( hand.gripObject )
        {
            // track data to improve throw release in ring buffer

            int index = ( hand.throwRingBufferIndex++ ) % ThrowRingBufferSize;
            hand.throwRingBufferEntries[index].valid = true;
            Vector3 difference = hand.gripObject.transform.position - hand.previousGripObjectPosition;
            hand.throwRingBufferEntries[index].speed = (float) Math.Sqrt( difference.x*difference.x + difference.z*difference.z ) * Constants.RenderFrameRate;

            // track previous positions and rotations for hand and index finger so we can use this to determine linear and angular velocity at time of release

            hand.previousHandPosition = hand.transform.position;
            hand.previousHandRotation = hand.transform.rotation;

            hand.previousGripObjectPosition = hand.gripObject.transform.position;
            hand.previousGripObjectRotation = hand.gripObject.transform.rotation;

            // while an object is held set its last interaction frame to the current sim frame. this is used to boost priority for this object when it is thrown.

            NetworkInfo networkInfo = hand.gripObject.GetComponent<NetworkInfo>();

            networkInfo.SetLastPlayerInteractionFrame( (long) context.GetSimulationFrame() );
        }
    }

    void TranslateHandPoseToInput( ref OvrAvatarDriver.HandPose handPose, ref OvrAvatarDriver.ControllerPose controllerPose, ref HandInput input )
    {
        input.handTrigger = handPose.gripFlex;

        input.previousIndexTrigger = input.indexTrigger;
        input.indexTrigger = handPose.indexFlex;

        if ( input.indexTrigger >= IndexThreshold && input.previousIndexTrigger < IndexThreshold )
            input.indexPressFrame = context.GetRenderFrame();

        input.pointing = true;

        input.x = controllerPose.button1IsDown;
        input.y = controllerPose.button2IsDown;

        input.stick = controllerPose.joystickPosition;
    }

    void DetectStateChanges( ref HandData hand )
    {
        switch ( hand.state )
        {
            case HandState.Neutral:
                {
                    if ( DetectGripTransition( ref hand ) )
                        return;

                    if ( hand.input.pointing )
                    {
                        TransitionToState( ref hand, HandState.Pointing );
                        return;
                    }
                }
                break;

            case HandState.Pointing:
                {
                    if ( DetectGripTransition( ref hand ) )
                        return;

                    if ( !hand.input.pointing )
                    {
                        TransitionToState( ref hand, HandState.Neutral );
                        return;
                    }
                }
                break;

            case HandState.Grip:
                {
                    if ( hand.input.handTrigger < GripThreshold )
                    {
                        if ( hand.input.pointing )
                            TransitionToState( ref hand, HandState.Pointing );
                        else
                            TransitionToState( ref hand, HandState.Neutral );
                    }
                }
                break;
        }
    }

    bool DetectGripTransition( ref HandData hand )
    {
        if ( hand.state == HandState.Grip && hand.gripObject == null )
        {
            TransitionToState( ref hand, HandState.Neutral );

            return true;
        }

        if ( hand.input.handTrigger >= GripThreshold )
        {
            if ( hand.pointObject && hand.pointObjectFrame + PointStickyFrames >= context.GetRenderFrame() )
            {
                NetworkInfo networkInfo = hand.pointObject.GetComponent<NetworkInfo>();

                if ( networkInfo.CanLocalPlayerGrabCube( hand.gripInputStartFrame ) )
                {
                    AttachToHand( ref hand, hand.pointObject );

                    TransitionToState( ref hand, HandState.Grip );

                    return true;
                }
            }
        }

        return false;
    }

    void AttachToHand( ref HandData hand, GameObject gameObject )
    {
        NetworkInfo networkInfo = gameObject.GetComponent<NetworkInfo>();

        networkInfo.AttachCubeToLocalPlayer( this, hand );

#if DEBUG_AUTHORITY
        Debug.Log( "client " + context.GetClientIndex() + " grabbed cube " + networkInfo.GetCubeId() + " and set ownership sequence to " + networkInfo.GetOwnershipSequence() );
#endif // #if DEBUG_AUTHORITY

        if ( !context.IsServer() )
            networkInfo.ClearConfirmed();
        else
            networkInfo.SetConfirmed();

        for ( int i = 0; i < ThrowRingBufferSize; ++i )
        {
            hand.throwRingBufferEntries[i].valid = false;
            hand.throwRingBufferEntries[i].speed = 0.0f;
        }
    }

    bool IsCloseGrip( ref HandData hand )
    {
        if ( !hand.gripObject )
            return false;

        Vector3 delta = hand.gripObject.transform.position - hand.transform.position;

        float distance = delta.magnitude;

        return distance <= GrabDistance;
    }

    bool IsThrowing( ref HandData hand )
    {
        int numEntries = 0;

        float totalSpeed = 0.0f;

        for ( int i = 0; i < ThrowRingBufferSize; ++i )
        {
            if ( hand.throwRingBufferEntries[i].valid )
                totalSpeed += hand.throwRingBufferEntries[i].speed;
            numEntries++;
        }

        if ( numEntries == 0 )
            return false;

        float averageSpeed = totalSpeed / numEntries;

        return averageSpeed >= ThrowSpeed;
    }

    void CalculateAndApplyReleaseVelocity( ref HandData hand, Rigidbody rigidBody, bool disableReleaseVelocity = false )
    {
        if ( disableReleaseVelocity )
        {
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }
        else
        {
            if ( IsCloseGrip( ref hand ) || IsThrowing( ref hand ) )
            {
                // throw mode

                rigidBody.velocity = ( hand.gripObject.transform.position - hand.previousGripObjectPosition ) * Constants.RenderFrameRate;
                rigidBody.angularVelocity = CalculateAngularVelocity( hand.previousGripObjectRotation, hand.gripObject.transform.rotation, 1.0f / Constants.RenderFrameRate, 0.001f );
                float speed = rigidBody.velocity.magnitude;
                if ( rigidBody.velocity.magnitude > MaxThrowSpeed )
                {
                    rigidBody.velocity = ( rigidBody.velocity / speed ) * MaxThrowSpeed;
                }

                float vx = rigidBody.velocity.x;
                float vz = rigidBody.velocity.z;
                if ( vx * vx + vz * vz > HardThrowSpeed*HardThrowSpeed )
                {
                    if ( rigidBody.velocity.y < ThrowVelocityMinY )
                        rigidBody.velocity = new Vector3( rigidBody.velocity.x, ThrowVelocityMinY, rigidBody.velocity.z );
                }
            }
            else
            {
                // placement mode

                rigidBody.velocity = 3 * ( hand.transform.position - hand.previousHandPosition ) * Constants.RenderFrameRate;
                rigidBody.angularVelocity = 2 * CalculateAngularVelocity( hand.previousHandRotation, hand.transform.rotation, 1.0f / Constants.RenderFrameRate, 0.1f );
            }
        }
    }

    void WakeUpObjects( List<GameObject> list )
    {
        foreach ( GameObject gameObject in list )
        {
            var networkInfo = gameObject.GetComponent<NetworkInfo>();
            context.ResetCubeRingBuffer( networkInfo.GetCubeId() );
            if ( networkInfo.GetAuthorityIndex() == 0 )
                context.TakeAuthorityOverObject( networkInfo );
            var rigidBody = gameObject.GetComponent<Rigidbody>();
            rigidBody.WakeUp();
        }
    }

    void DetachFromHand( ref HandData hand )
    {
        // IMPORTANT: This happens when passing a cube from hand-to-hand
        if ( hand.gripObject == null )
            return;

        NetworkInfo networkInfo = hand.gripObject.GetComponent<NetworkInfo>();

        networkInfo.DetachCubeFromPlayer();

#if DEBUG_AUTHORITY
        Debug.Log( "client " + context.GetClientIndex() + " released cube " + networkInfo.GetCubeId() + ". ownership sequence is " + networkInfo.GetOwnershipSequence() + ", authority sequence is " + networkInfo.GetAuthoritySequence() );
#endif // #if DEBUG_AUTHORITY
    }

    void TransitionToState( ref HandData hand, HandState nextState )
    {
        ExitState( ref hand, nextState );

        EnterState( ref hand, nextState );
    }

    void ExitState( ref HandData hand, HandState nextState )
    {
        switch ( hand.state )
        {
            case HandState.Neutral:
                {
                    // ...
                }
                break;

            case HandState.Pointing:
                {
                    DestroyPointingLine( ref hand );
                }
                break;

            case HandState.Grip:
                {
                    DetachFromHand( ref hand );
                }
                break;
        }
    }

    void EnterState( ref HandData hand, HandState nextState )
    {
        switch ( nextState )
        {
            case HandState.Pointing:
                {
                    CreatePointingLine( ref hand );
                }
                break;

            case HandState.Grip:
                {
                    // ...
                }
                break;
        }

        hand.state = nextState;
    }

    void UpdateCurrentState( ref HandData hand )
    {
        switch ( hand.state )
        {
            case HandState.Pointing:
                {
                    UpdatePointingLine( ref hand );

                    ForcePointAnimation( ref hand );
                }
                break;

            case HandState.Grip:
                {
                    if ( IsCloseGrip( ref hand ) )
                        ForceGripAnimation( ref hand );
                    else
                        ForcePointAnimation( ref hand );

                    if ( hand.gripObject )
                    {
                        if ( hand.gripObject.transform.position.y < 0.0f )
                        {
                            Vector3 fixedPosition = hand.gripObject.transform.position;
                            fixedPosition.y = 0.0f;
                            hand.gripObject.transform.position = fixedPosition;
                        }
                    }
                }
                break;
        }
    }

    void SetPointObject( ref HandData hand, GameObject gameObject )
    {
        hand.pointObject = gameObject;
        hand.pointObjectFrame = context.GetRenderFrame();
    }

    void CreatePointingLine( ref HandData hand )
    {
        if ( !hand.pointLine )
        {
            hand.pointLine = (GameObject) Instantiate( linePrefab, Vector3.zero, Quaternion.identity );

            Assert.IsNotNull( hand.pointLine );
        }
    }

    bool FilterPointObject( ref HandData hand, Rigidbody rigidBody )
    {
        if ( !rigidBody )
            return false;

        GameObject gameObject = rigidBody.gameObject;

        if ( !gameObject )
            return false;

        if ( gameObject.layer != contextLayerCubes && gameObject.layer != contextLayerGrip )
            return false;

        NetworkInfo networkInfo = gameObject.GetComponent<NetworkInfo>();
        if ( !networkInfo )
            return false;

        return true;
    }

    void GetIndexFingerStartPointAndDirection( ref HandData hand, out Vector3 start, out Vector3 direction )
    {
        Vector3[] startLocalPosition = { new Vector3( -0.05f, 0.0f, 0.0f ), new Vector3( +0.05f, 0.0f, 0.0f ) };
        start = hand.transform.TransformPoint( startLocalPosition[hand.id] );
        direction = hand.transform.forward;
    }

    void UpdatePointingLine( ref HandData hand )
    {
        Assert.IsNotNull( hand.pointLine );

        Vector3 start, direction;

        GetIndexFingerStartPointAndDirection( ref hand, out start, out direction );

        Vector3 finish = start + direction * RaycastDistance;

        // don't allow any selection for a few frames after releasing an object
        if ( hand.gripObjectReleaseFrame + PostReleaseDisableSelectFrames < context.GetRenderFrame() )
        {
            // first select any object overlapping the hand

            Collider[] hitColliders = Physics.OverlapSphere( hand.transform.position, GrabRadius, contextLayerMask );

            if ( hitColliders.Length > 0 && FilterPointObject( ref hand, hitColliders[0].attachedRigidbody ) )
            {
                finish = start;

                SetPointObject( ref hand, hitColliders[0].gameObject );

                hand.touchingObject = true;
            }
            else
            {
                // otherwise, raycast forward along point direction for accurate selection up close

                hand.touchingObject = false;

                RaycastHit hitInfo;

                if ( Physics.Linecast( start, finish, out hitInfo, contextLayerMask ) )
                {
                    if ( FilterPointObject( ref hand, hitInfo.rigidbody ) )
                    {
                        finish = start + direction * hitInfo.distance;

                        SetPointObject( ref hand, hitInfo.rigidbody.gameObject );
                    }
                }
                else if ( Physics.SphereCast( start + direction * PointSphereCastStart, PointSphereCastRadius, finish, out hitInfo, contextLayerMask ) )
                {
                    // failing an accurate hit, sphere cast starting from a bit further away to provide easier selection of far away objects

                    if ( FilterPointObject( ref hand, hitInfo.rigidbody ) )
                    {
                        finish = start + direction * ( PointSphereCastStart + hitInfo.distance );

                        SetPointObject( ref hand, hitInfo.rigidbody.gameObject );
                    }
                }
            }
        }

        var lineRenderer = hand.pointLine.GetComponent<LineRenderer>();

        if ( lineRenderer )
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition( 0, start );
            lineRenderer.SetPosition( 1, finish );
            lineRenderer.startWidth = LineWidth;
            lineRenderer.endWidth = LineWidth;
        }
    }

    void DestroyPointingLine( ref HandData hand )
    {
        Assert.IsNotNull( hand.pointLine );

        DestroyObject( hand.pointLine );

        hand.pointLine = null;
    }

    void ForcePointAnimation( ref HandData hand )
    {
        if ( hand.touchingObject && hand.state == HandState.Pointing )
            hand.animator.SetLayerWeight( hand.animator.GetLayerIndex( "Point Layer" ), 0.0f );         // indicates state of touching an object to player (for up-close grip)
        else
            hand.animator.SetLayerWeight( hand.animator.GetLayerIndex( "Point Layer" ), 1.0f );

        hand.animator.SetLayerWeight( hand.animator.GetLayerIndex( "Thumb Layer" ), 0.0f );
    }

    void ForceGripAnimation( ref HandData hand )
    {
        hand.animator.SetLayerWeight( hand.animator.GetLayerIndex( "Point Layer" ), 0.0f );
        hand.animator.SetLayerWeight( hand.animator.GetLayerIndex( "Thumb Layer" ), 0.0f );
    }

    Vector3 CalculateAngularVelocity( Quaternion previous, Quaternion current, float dt, float minimumAngle )
    {
        Assert.IsTrue( dt > 0.0f );

        Quaternion r = current * Quaternion.Inverse( previous );

        float theta = (float) ( 2.0f * Math.Acos( r.w ) );

        if ( float.IsNaN( theta ) )
            return Vector3.zero;

        if ( Math.Abs( theta ) < minimumAngle )
            return Vector3.zero;

        if ( theta > Math.PI )
            theta -= 2.0f * (float) Math.PI;

        float s = theta / dt / (float) Math.Sqrt( r.x * r.x + r.y * r.y + r.z * r.z );

        Vector3 angularVelocity = new Vector3( s * r.x, s * r.y, s * r.z );

        Assert.IsFalse( float.IsNaN( angularVelocity.x ) );
        Assert.IsFalse( float.IsNaN( angularVelocity.y ) );
        Assert.IsFalse( float.IsNaN( angularVelocity.z ) );

        return angularVelocity;
    }

    public bool GetAvatarState( out AvatarState state )
    {
        OvrAvatarDriver.PoseFrame frame;

        if ( !oculusAvatar.Driver.GetCurrentPose( out frame ) )
        {
            state = AvatarState.defaults;
            return false;
        }

        AvatarState.Initialize( out state, context.GetClientIndex(), frame, leftHand.gripObject, rightHand.gripObject );

        return true;
    }

    public void CubeAttached( ref HandData hand )
    {
        UpdateHeldObject( ref hand );
    }

    public void CubeDetached( ref HandData hand )
    {
        var rigidBody = hand.gripObject.GetComponent<Rigidbody>();

        rigidBody.isKinematic = false;

        hand.gripObject.layer = contextLayerCubes;

        CalculateAndApplyReleaseVelocity( ref hand, rigidBody, hand.disableReleaseVelocity );

        hand.gripObject.transform.SetParent( null );

        hand.gripObject = null;

        if ( rigidBody.position.y < MinimumCubeHeight )
        {
            Vector3 position = rigidBody.position;
            position.y = MinimumCubeHeight;
            rigidBody.position = position;
        }

        if ( hand.gripObjectSupportList != null )
        {
            WakeUpObjects( hand.gripObjectSupportList );
            hand.gripObjectSupportList = null;
        }

        hand.pointObject = null;
        hand.pointObjectFrame = 0;

        hand.gripObjectReleaseFrame = context.GetRenderFrame();
    }

    public void ResetHand( ref HandData hand )
    {
        hand.disableReleaseVelocity = true;

        TransitionToState( ref hand, HandState.Neutral );

        hand.disableReleaseVelocity = false;

        hand.touchingObject = false;

        hand.pointLine = null;
        hand.pointObject = null;
        hand.pointObjectFrame = 0;

        hand.gripObject = null;
        hand.gripInputStartFrame = 0;
        hand.gripObjectStartFrame = 0;

        hand.previousHandPosition = Vector3.zero;
        hand.previousGripObjectPosition = Vector3.zero;

        hand.previousHandRotation = Quaternion.identity;
        hand.previousGripObjectRotation = Quaternion.identity;

        hand.gripObjectReleaseFrame = 0;
        hand.gripObjectSupportList = null;
    }

    public bool IsPressingGrip()
    {
        return leftHand.input.handTrigger > GripThreshold || rightHand.input.handTrigger > GripThreshold;
    }

    public bool IsPressingIndex()
    {
        return leftHand.input.indexTrigger > IndexThreshold || rightHand.input.indexTrigger > IndexThreshold;
    }

    public bool IsPressingX()
    {
        return leftHand.input.x || rightHand.input.x;
    }

    public bool IsPressingY()
    {
        return leftHand.input.y || rightHand.input.y;
    }

    public override bool GetCurrentPose( out PoseFrame pose )
    {
        return base.GetCurrentPose( out pose );
    }
}
