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
using System.Collections.Generic;

public struct CubeState
{
    public bool active;

    public int authorityIndex;
    public ushort authoritySequence;
    public ushort ownershipSequence;

    public int position_x;
    public int position_y;
    public int position_z;

    public uint rotation_largest;
    public uint rotation_a;
    public uint rotation_b;
    public uint rotation_c;

    public int linear_velocity_x;
    public int linear_velocity_y;
    public int linear_velocity_z;

    public int angular_velocity_x;
    public int angular_velocity_y;
    public int angular_velocity_z;

    public static CubeState defaults;
};

public struct CubeDelta
{
#if DEBUG_DELTA_COMPRESSION
    public int absolute_position_x;
    public int absolute_position_y;
    public int absolute_position_z;
#endif // #if DEBUG_DELTA_COMPRESSION

    public int position_delta_x;
    public int position_delta_y;
    public int position_delta_z;

    public int linear_velocity_delta_x;
    public int linear_velocity_delta_y;
    public int linear_velocity_delta_z;

    public int angular_velocity_delta_x;
    public int angular_velocity_delta_y;
    public int angular_velocity_delta_z;
};

public class Snapshot
{
    public CubeState[] cubeState = new CubeState[Constants.NumCubes];

    public static void QuaternionToSmallestThree( Quaternion quaternion, out uint largest, out uint integer_a, out uint integer_b, out uint integer_c )
    {
        const float minimum = - 1.0f / 1.414214f;       // 1.0f / sqrt(2)
        const float maximum = + 1.0f / 1.414214f;

        const float scale = (float) ( ( 1 << Constants.RotationBits ) - 1 );

        float x = quaternion.x;
        float y = quaternion.y;
        float z = quaternion.z;
        float w = quaternion.w;

        float abs_x = Math.Abs( x );
        float abs_y = Math.Abs( y );
        float abs_z = Math.Abs( z );
        float abs_w = Math.Abs( w );

        float largest_value = abs_x;

        largest = 0;

        if ( abs_y > largest_value )
        {
            largest = 1;
            largest_value = abs_y;
        }

        if ( abs_z > largest_value )
        {
            largest = 2;
            largest_value = abs_z;
        }

        if ( abs_w > largest_value )
        {
            largest = 3;
            largest_value = abs_w;
        }

        float a = 0;
        float b = 0;
        float c = 0;

        switch ( largest )
        {
            case 0:
                if ( x >= 0 )
                {
                    a = y;
                    b = z;
                    c = w;
                }
                else
                {
                    a = -y;
                    b = -z;
                    c = -w;
                }
                break;

            case 1:
                if ( y >= 0 )
                {
                    a = x;
                    b = z;
                    c = w;
                }
                else
                {
                    a = -x;
                    b = -z;
                    c = -w;
                }
                break;

            case 2:
                if ( z >= 0 )
                {
                    a = x;
                    b = y;
                    c = w;
                }
                else
                {
                    a = -x;
                    b = -y;
                    c = -w;
                }
                break;

            case 3:
                if ( w >= 0 )
                {
                    a = x;
                    b = y;
                    c = z;
                }
                else
                {
                    a = -x;
                    b = -y;
                    c = -z;
                }
                break;
        }

        float normal_a = ( a - minimum ) / ( maximum - minimum ); 
        float normal_b = ( b - minimum ) / ( maximum - minimum );
        float normal_c = ( c - minimum ) / ( maximum - minimum );

        integer_a = (uint) Math.Floor( normal_a * scale + 0.5f );
        integer_b = (uint) Math.Floor( normal_b * scale + 0.5f );
        integer_c = (uint) Math.Floor( normal_c * scale + 0.5f );
    }

    public static Quaternion SmallestThreeToQuaternion( uint largest, uint integer_a, uint integer_b, uint integer_c )
    {
        const float minimum = - 1.0f / 1.414214f;       // 1.0f / sqrt(2)
        const float maximum = + 1.0f / 1.414214f;

        const float scale = (float) ( ( 1 << Constants.RotationBits ) - 1 );

        const float inverse_scale = 1.0f / scale;

        float a = integer_a * inverse_scale * ( maximum - minimum ) + minimum;
        float b = integer_b * inverse_scale * ( maximum - minimum ) + minimum;
        float c = integer_c * inverse_scale * ( maximum - minimum ) + minimum;

        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;
        float w = 0.0f;

        switch ( largest )
        {
            case 0:
            {
                x = (float) Math.Sqrt( 1 - a*a - b*b - c*c );
                y = a;
                z = b;
                w = c;
            }
            break;

            case 1:
            {
                x = a;
                y = (float) Math.Sqrt( 1 - a*a - b*b - c*c );
                z = b;
                w = c;
            }
            break;

            case 2:
            {
                x = a;
                y = b;
                z = (float) Math.Sqrt( 1 - a*a - b*b - c*c );
                w = c;
            }
            break;

            case 3:
            {
                x = a;
                y = b;
                z = c;
                w = (float) Math.Sqrt( 1 - a*a - b*b - c*c );
            }
            break;
        }

        // IMPORTANT: We must normalize the quaternion here because it will have slight drift otherwise due to being quantized

        float norm = x*x + y*y + z*z + w*w;

        if ( norm > 0.000001f )
        {
            var quaternion = new Quaternion( x, y, z, w );
            float length = (float) Math.Sqrt( norm );
            quaternion.x /= length;
            quaternion.y /= length;
            quaternion.z /= length;
            quaternion.w /= length;
            return quaternion;
        }
        else
        {
            return new Quaternion( 0, 0, 0, 1 );
        }
    }

    public static void ClampPosition( ref int position_x, ref int position_y, ref int position_z )
    {
        if ( position_x < Constants.PositionMinimumXZ )
            position_x = Constants.PositionMinimumXZ;
        else if ( position_x > Constants.PositionMaximumXZ )
            position_x = Constants.PositionMaximumXZ;

        if ( position_y < Constants.PositionMinimumY )
            position_y = Constants.PositionMinimumY;
        else if ( position_y > Constants.PositionMaximumY )
            position_y = Constants.PositionMaximumY;

        if ( position_z < Constants.PositionMinimumXZ )
            position_z = Constants.PositionMinimumXZ;
        else if ( position_z > Constants.PositionMaximumXZ )
            position_z = Constants.PositionMaximumXZ;
    }

    public static void ClampLinearVelocity( ref int linear_velocity_x, ref int linear_velocity_y, ref int linear_velocity_z )
    {
        if ( linear_velocity_x < Constants.LinearVelocityMinimum )
            linear_velocity_x = Constants.LinearVelocityMinimum;
        else if ( linear_velocity_x > Constants.LinearVelocityMaximum )
            linear_velocity_x = Constants.LinearVelocityMaximum;

        if ( linear_velocity_y < Constants.LinearVelocityMinimum )
            linear_velocity_y = Constants.LinearVelocityMinimum;
        else if ( linear_velocity_y > Constants.LinearVelocityMaximum )
            linear_velocity_y = Constants.LinearVelocityMaximum;

        if ( linear_velocity_z < Constants.LinearVelocityMinimum )
            linear_velocity_z = Constants.LinearVelocityMinimum;
        else if ( linear_velocity_z > Constants.LinearVelocityMaximum )
            linear_velocity_z = Constants.LinearVelocityMaximum;
    }

    public static void ClampAngularVelocity( ref int angular_velocity_x, ref int angular_velocity_y, ref int angular_velocity_z )
    { 
        if ( angular_velocity_x < Constants.AngularVelocityMinimum )
            angular_velocity_x = Constants.AngularVelocityMinimum;
        else if ( angular_velocity_x > Constants.AngularVelocityMaximum )
            angular_velocity_x = Constants.AngularVelocityMaximum;

        if ( angular_velocity_y < Constants.AngularVelocityMinimum )
            angular_velocity_y = Constants.AngularVelocityMinimum;
        else if ( angular_velocity_y > Constants.AngularVelocityMaximum )
            angular_velocity_y = Constants.AngularVelocityMaximum;

        if ( angular_velocity_z < Constants.AngularVelocityMinimum )
            angular_velocity_z = Constants.AngularVelocityMinimum;
        else if ( angular_velocity_z > Constants.AngularVelocityMaximum )
            angular_velocity_z = Constants.AngularVelocityMaximum;
    }

    public static void ClampLocalPosition( ref int position_x, ref int position_y, ref int position_z )
    {
        if ( position_x < Constants.LocalPositionMinimum )
            position_x = Constants.LocalPositionMinimum;
        else if ( position_x > Constants.LocalPositionMaximum )
            position_x = Constants.LocalPositionMaximum;

        if ( position_y < Constants.LocalPositionMinimum )
            position_y = Constants.LocalPositionMinimum;
        else if ( position_y > Constants.LocalPositionMaximum )
            position_y = Constants.LocalPositionMaximum;

        if ( position_z < Constants.LocalPositionMinimum )
            position_z = Constants.LocalPositionMinimum;
        else if ( position_z > Constants.LocalPositionMaximum )
            position_z = Constants.LocalPositionMaximum;
   }

   public static void GetCubeState( Rigidbody rigidBody, NetworkInfo networkInfo, ref CubeState cubeState, ref Vector3 origin )
    {
        cubeState.active = !rigidBody.IsSleeping();

        cubeState.authorityIndex = networkInfo.GetAuthorityIndex();
        cubeState.authoritySequence = networkInfo.GetAuthoritySequence();
        cubeState.ownershipSequence = networkInfo.GetOwnershipSequence();

        Vector3 position = rigidBody.position - origin;

        cubeState.position_x = (int) Math.Floor( position.x * Constants.UnitsPerMeter + 0.5f );
        cubeState.position_y = (int) Math.Floor( position.y * Constants.UnitsPerMeter + 0.5f );
        cubeState.position_z = (int) Math.Floor( position.z * Constants.UnitsPerMeter + 0.5f );

        Snapshot.QuaternionToSmallestThree( rigidBody.rotation, 
                                            out cubeState.rotation_largest, 
                                            out cubeState.rotation_a, 
                                            out cubeState.rotation_b, 
                                            out cubeState.rotation_c );

        cubeState.linear_velocity_x = (int) Math.Floor( rigidBody.velocity.x * Constants.UnitsPerMeter + 0.5f  );
        cubeState.linear_velocity_y = (int) Math.Floor( rigidBody.velocity.y * Constants.UnitsPerMeter + 0.5f  );
        cubeState.linear_velocity_z = (int) Math.Floor( rigidBody.velocity.z * Constants.UnitsPerMeter + 0.5f  );

        cubeState.angular_velocity_x = (int) Math.Floor( rigidBody.angularVelocity.x * Constants.UnitsPerMeter + 0.5f  );
        cubeState.angular_velocity_y = (int) Math.Floor( rigidBody.angularVelocity.y * Constants.UnitsPerMeter + 0.5f  );
        cubeState.angular_velocity_z = (int) Math.Floor( rigidBody.angularVelocity.z * Constants.UnitsPerMeter + 0.5f  );

        ClampPosition( ref cubeState.position_x, ref cubeState.position_y, ref cubeState.position_z );

        ClampLinearVelocity( ref cubeState.linear_velocity_x, ref cubeState.linear_velocity_y, ref cubeState.linear_velocity_z );

        ClampAngularVelocity( ref cubeState.angular_velocity_x, ref cubeState.angular_velocity_y, ref cubeState.angular_velocity_z );
    }

    public static void ApplyCubeState( Rigidbody rigidBody, NetworkInfo networkInfo, ref CubeState cubeState, ref Vector3 origin, bool smoothing = false )
    {
        if ( networkInfo.IsHeldByPlayer() )
            networkInfo.DetachCubeFromPlayer();

        if ( cubeState.active )
        {
            if ( rigidBody.IsSleeping() )
                rigidBody.WakeUp();
        }

        if ( !cubeState.active )
        {
            if ( !rigidBody.IsSleeping() )
                rigidBody.Sleep();
        }

        networkInfo.SetAuthorityIndex( cubeState.authorityIndex );
        networkInfo.SetAuthoritySequence( cubeState.authoritySequence );
        networkInfo.SetOwnershipSequence( cubeState.ownershipSequence );

        Vector3 position = new Vector3( cubeState.position_x, cubeState.position_y, cubeState.position_z ) * 1.0f / Constants.UnitsPerMeter + origin;

        Quaternion rotation = SmallestThreeToQuaternion( cubeState.rotation_largest, cubeState.rotation_a, cubeState.rotation_b, cubeState.rotation_c );

        if ( smoothing )
        {
            networkInfo.MoveWithSmoothing( position, rotation );
        }
        else
        {
            rigidBody.position = position;
            rigidBody.rotation = rotation;
        }

        rigidBody.velocity = new Vector3( cubeState.linear_velocity_x, cubeState.linear_velocity_y, cubeState.linear_velocity_z ) * 1.0f / Constants.UnitsPerMeter;

        rigidBody.angularVelocity = new Vector3( cubeState.angular_velocity_x, cubeState.angular_velocity_y, cubeState.angular_velocity_z ) * 1.0f / Constants.UnitsPerMeter;
    }
};
