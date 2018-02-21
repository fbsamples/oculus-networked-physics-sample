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

static class Prediction
{
    const int FixedPointQuantizeBits = Constants.PositionBits;
    const int FixedPointIntermediateBits = 10;
    const int FixedPointFractionalBits = FixedPointQuantizeBits + FixedPointIntermediateBits;
    
    const float GravityFudge = +0.001f;                                     // these values are hand-tuned to minimize prediction error. see TestPrediction for details.
    const float LinearDragFudge = 0.0f;
    const float AngularDragFudge = -0.0001f;

    const float UnityGravity = 9.8f + GravityFudge;                         // Physics.gravity
    const float UnityLinearDrag = 0.1f + LinearDragFudge;                   // rigidBody.drag
    const float UnityAngularDrag = 0.05f + AngularDragFudge;                // rigidBody.angularDrag

    const long FixedPointOne = ( 1L << FixedPointFractionalBits );
    const long FixedPointOneHalf = (long) ( 0.5f * FixedPointOne );

    const long FixedPointGravity = (long) ( UnityGravity * FixedPointOne );
    const long FixedPointLinearDrag = (long) ( ( 1.0f - UnityLinearDrag * ( 1.0f / Constants.PhysicsFrameRate ) ) * FixedPointOne );
    const long FixedPointAngularDrag = (long) ( ( 1.0f - UnityAngularDrag * ( 1.0f / Constants.PhysicsFrameRate ) ) * FixedPointOne );
    const long FixedPointDeltaTime = (long) ( ( 1.0 / Constants.PhysicsFrameRate ) * FixedPointOne );
    const long FixedPointQuantizeMask = ~ ( ( 1L << FixedPointQuantizeBits ) - 1 );
    const long FixedPointQuantizeRound = (long) ( 0.5f * ( 1L << FixedPointQuantizeBits ) );

    const long FixedPointPositionMinimumXZ = FixedPointOne * Constants.PositionMinimumXZ;
    const long FixedPointPositionMaximumXZ = FixedPointOne * Constants.PositionMaximumXZ;
    const long FixedPointPositionMinimumY = FixedPointOne * Constants.PositionMinimumY;
    const long FixedPointPositionMaximumY = FixedPointOne * Constants.PositionMaximumY;
    const long FixedPointLinearVelocityMinimum = FixedPointOne * Constants.LinearVelocityMinimum;
    const long FixedPointLinearVelocityMaximum = FixedPointOne * Constants.LinearVelocityMaximum;
    const long FixedPointAngularVelocityMinimum = FixedPointOne * Constants.AngularVelocityMinimum;
    const long FixedPointAngularVelocityMaximum = FixedPointOne * Constants.AngularVelocityMaximum;

    public static void PredictBallistic( int numFrames, 
                                         int start_position_x, int start_position_y, int start_position_z,
                                         int start_linear_velocity_x, int start_linear_velocity_y, int start_linear_velocity_z,
                                         int start_angular_velocity_x, int start_angular_velocity_y, int start_angular_velocity_z,
                                         out int predicted_position_x, out int predicted_position_y, out int predicted_position_z,
                                         out int predicted_linear_velocity_x, out int predicted_linear_velocity_y, out int predicted_linear_velocity_z,
                                         out int predicted_angular_velocity_x, out int predicted_angular_velocity_y, out int predicted_angular_velocity_z )
    {
        // convert network format values to fixed point for prediction. fixed point ensures determinism.

        long position_x = ( (long) start_position_x ) << FixedPointIntermediateBits;
        long position_y = ( (long) start_position_y ) << FixedPointIntermediateBits;
        long position_z = ( (long) start_position_z ) << FixedPointIntermediateBits;

        long linear_velocity_x = ( (long) start_linear_velocity_x ) << FixedPointIntermediateBits;
        long linear_velocity_y = ( (long) start_linear_velocity_y ) << FixedPointIntermediateBits;
        long linear_velocity_z = ( (long) start_linear_velocity_z ) << FixedPointIntermediateBits;

        long angular_velocity_x = ( (long) start_angular_velocity_x ) << FixedPointIntermediateBits;
        long angular_velocity_y = ( (long) start_angular_velocity_y ) << FixedPointIntermediateBits;
        long angular_velocity_z = ( (long) start_angular_velocity_z ) << FixedPointIntermediateBits;

        for ( int i = 0; i < numFrames; ++i )
        {
            // apply gravity

            linear_velocity_y -= ( FixedPointGravity * FixedPointDeltaTime ) >> FixedPointFractionalBits;

            // apply linear drag

            linear_velocity_x *= FixedPointLinearDrag;
            linear_velocity_y *= FixedPointLinearDrag;
            linear_velocity_z *= FixedPointLinearDrag;

            linear_velocity_x >>= FixedPointFractionalBits;
            linear_velocity_y >>= FixedPointFractionalBits;
            linear_velocity_z >>= FixedPointFractionalBits;

            // apply angular drag

            angular_velocity_x *= FixedPointAngularDrag;
            angular_velocity_y *= FixedPointAngularDrag;
            angular_velocity_z *= FixedPointAngularDrag;

            angular_velocity_x >>= FixedPointFractionalBits;
            angular_velocity_y >>= FixedPointFractionalBits;
            angular_velocity_z >>= FixedPointFractionalBits;

            // integrate position from linear velocity

            position_x += ( linear_velocity_x * FixedPointDeltaTime ) >> FixedPointFractionalBits;
            position_y += ( linear_velocity_y * FixedPointDeltaTime ) >> FixedPointFractionalBits;
            position_z += ( linear_velocity_z * FixedPointDeltaTime ) >> FixedPointFractionalBits;

            // quantize and bound position

            position_x += FixedPointQuantizeRound;
            position_y += FixedPointQuantizeRound;
            position_z += FixedPointQuantizeRound;

            position_x &= FixedPointQuantizeMask;
            position_y &= FixedPointQuantizeMask;
            position_z &= FixedPointQuantizeMask;

            if ( position_x < FixedPointPositionMinimumXZ )
                position_x = FixedPointPositionMinimumXZ;
            else if ( position_x > FixedPointPositionMaximumXZ )
                position_x = FixedPointPositionMaximumXZ;

            if ( position_y < FixedPointPositionMinimumY )
                position_y = FixedPointPositionMinimumY;
            else if ( position_y > FixedPointPositionMaximumY )
                position_y = FixedPointPositionMaximumY;

            if ( position_z < FixedPointPositionMinimumXZ )
                position_z = FixedPointPositionMinimumXZ;
            else if ( position_z > FixedPointPositionMaximumXZ )
                position_z = FixedPointPositionMaximumXZ;

            // quantize and bound linear velocity

            linear_velocity_x += FixedPointQuantizeRound;
            linear_velocity_y += FixedPointQuantizeRound;
            linear_velocity_z += FixedPointQuantizeRound;

            linear_velocity_x &= FixedPointQuantizeMask;
            linear_velocity_y &= FixedPointQuantizeMask;
            linear_velocity_z &= FixedPointQuantizeMask;

            if ( linear_velocity_x < FixedPointLinearVelocityMinimum )
                linear_velocity_x = FixedPointLinearVelocityMinimum;
            else if ( linear_velocity_x > FixedPointLinearVelocityMaximum )
                linear_velocity_x = FixedPointLinearVelocityMaximum;

            if ( linear_velocity_y < FixedPointLinearVelocityMinimum )
                linear_velocity_y = FixedPointLinearVelocityMinimum;
            else if ( linear_velocity_y > FixedPointLinearVelocityMaximum )
                linear_velocity_y = FixedPointLinearVelocityMaximum;

            if ( linear_velocity_z < FixedPointLinearVelocityMinimum )
                linear_velocity_z = FixedPointLinearVelocityMinimum;
            else if ( linear_velocity_z > FixedPointLinearVelocityMaximum )
                linear_velocity_z = FixedPointLinearVelocityMaximum;

            // quantize and bound angular velocity

            angular_velocity_x += FixedPointQuantizeRound;
            angular_velocity_y += FixedPointQuantizeRound;
            angular_velocity_z += FixedPointQuantizeRound;

            angular_velocity_x &= FixedPointQuantizeMask;
            angular_velocity_y &= FixedPointQuantizeMask;
            angular_velocity_z &= FixedPointQuantizeMask;

            if ( angular_velocity_x < FixedPointAngularVelocityMinimum )
                angular_velocity_x = FixedPointAngularVelocityMinimum;
            else if ( angular_velocity_x > FixedPointAngularVelocityMaximum )
                angular_velocity_x = FixedPointAngularVelocityMaximum;

            if ( angular_velocity_y < FixedPointAngularVelocityMinimum )
                angular_velocity_y = FixedPointAngularVelocityMinimum;
            else if ( angular_velocity_y > FixedPointAngularVelocityMaximum )
                angular_velocity_y = FixedPointAngularVelocityMaximum;

            if ( angular_velocity_z < FixedPointAngularVelocityMinimum )
                angular_velocity_z = FixedPointAngularVelocityMinimum;
            else if ( angular_velocity_z > FixedPointAngularVelocityMaximum )
                angular_velocity_z = FixedPointAngularVelocityMaximum;
        }

        // convert fixed point values back to network format

        predicted_position_x = (int) ( position_x >> FixedPointIntermediateBits );
        predicted_position_y = (int) ( position_y >> FixedPointIntermediateBits );
        predicted_position_z = (int) ( position_z >> FixedPointIntermediateBits );

        predicted_linear_velocity_x = (int) ( linear_velocity_x >> FixedPointIntermediateBits );
        predicted_linear_velocity_y = (int) ( linear_velocity_y >> FixedPointIntermediateBits );
        predicted_linear_velocity_z = (int) ( linear_velocity_z >> FixedPointIntermediateBits );

        predicted_angular_velocity_x = (int) ( angular_velocity_x >> FixedPointIntermediateBits );
        predicted_angular_velocity_y = (int) ( angular_velocity_y >> FixedPointIntermediateBits );
        predicted_angular_velocity_z = (int) ( angular_velocity_z >> FixedPointIntermediateBits );
    }

    public static void TestPrediction()
    {
        Debug.Log( "Prediction.TestPrediction" );

        string[] lines = System.IO.File.ReadAllLines( "prediction_deltas.txt" );

        string prediction_filename = "prediction.txt";

        using ( System.IO.StreamWriter file = new System.IO.StreamWriter( prediction_filename ) )
        {
            foreach ( string line in lines )
            {
                string[] stringValues = line.Split( ',' );

                int[] intValues = new int[stringValues.Length];

                for ( int i = 0; i < stringValues.Length; ++i )
                {
                    intValues[i] = Int32.Parse( stringValues[i] );
                }

                int current_sequence = intValues[0];
                int baseline_sequence = intValues[1];

                int baseline_position_x = intValues[15];
                int baseline_position_y = intValues[16];
                int baseline_position_z = intValues[17];
                
                int baseline_linear_velocity_x = intValues[22];
                int baseline_linear_velocity_y = intValues[23];
                int baseline_linear_velocity_z = intValues[24];

                int current_position_x = intValues[29];
                int current_position_y = intValues[30];
                int current_position_z = intValues[31];

                int current_linear_velocity_x = intValues[36];
                int current_linear_velocity_y = intValues[37];
                int current_linear_velocity_z = intValues[38];

                int baseline_angular_velocity_x = intValues[25];
                int baseline_angular_velocity_y = intValues[26];
                int baseline_angular_velocity_z = intValues[27];

                int current_angular_velocity_x = intValues[39];
                int current_angular_velocity_y = intValues[40];
                int current_angular_velocity_z = intValues[41];

                if ( current_sequence < baseline_sequence )
                    current_sequence += 65536;

                int numFrames = current_sequence - baseline_sequence;

                int predicted_position_x;
                int predicted_position_y;
                int predicted_position_z;

                int predicted_linear_velocity_x;
                int predicted_linear_velocity_y;
                int predicted_linear_velocity_z;

                int predicted_angular_velocity_x;
                int predicted_angular_velocity_y;
                int predicted_angular_velocity_z;

                PredictBallistic( numFrames, 
                                  baseline_position_x, baseline_position_y, baseline_position_z,
                                  baseline_linear_velocity_x, baseline_linear_velocity_y, baseline_linear_velocity_z,
                                  baseline_angular_velocity_x, baseline_angular_velocity_y, baseline_angular_velocity_z,
                                  out predicted_position_x, out predicted_position_y, out predicted_position_z, 
                                  out predicted_linear_velocity_x, out predicted_linear_velocity_y, out predicted_linear_velocity_z,
                                  out predicted_angular_velocity_x, out predicted_angular_velocity_y, out predicted_angular_velocity_z );

                int position_error_x = predicted_position_x - current_position_x;
                int position_error_y = predicted_position_y - current_position_y;
                int position_error_z = predicted_position_z - current_position_z;

                int linear_velocity_error_x = predicted_linear_velocity_x - current_linear_velocity_x;
                int linear_velocity_error_y = predicted_linear_velocity_y - current_linear_velocity_y;
                int linear_velocity_error_z = predicted_linear_velocity_z - current_linear_velocity_z;

                int angular_velocity_error_x = predicted_angular_velocity_x - current_angular_velocity_x;
                int angular_velocity_error_y = predicted_angular_velocity_y - current_angular_velocity_y;
                int angular_velocity_error_z = predicted_angular_velocity_z - current_angular_velocity_z;

                file.WriteLine( numFrames + "," +
                                position_error_x + "," +
                                position_error_y + "," +
                                position_error_z + "," +
                                linear_velocity_error_x + "," +
                                linear_velocity_error_y + "," +
                                linear_velocity_error_z + "," +
                                angular_velocity_error_x + "," +
                                angular_velocity_error_y + "," +
                                angular_velocity_error_z );
            }
        }

        Debug.Log( "Updated " + prediction_filename );
    }
}
