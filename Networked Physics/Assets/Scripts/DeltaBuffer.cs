/**
 * Copyright (c) 2017-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the Scripts directory of this source tree. An additional grant 
 * of patent rights can be found in the PATENTS file in the same directory.
 */

using UnityEngine.Assertions;
using UnityEngine.Profiling;

public class DeltaBuffer
{
    struct Entry
    {
        public ushort resetSequence;
        public int numCubes;
        public int[] cubeLookup;
        public int[] cubeIds;
        public CubeState[] cubeState;
    };

    Network.SequenceBuffer<Entry> sequenceBuffer;

    public DeltaBuffer( int size )
    {
        sequenceBuffer = new Network.SequenceBuffer<Entry>( size );

        for ( int i = 0; i < sequenceBuffer.GetSize(); ++i )
        {
            sequenceBuffer.Entries[i].resetSequence = 0;
            sequenceBuffer.Entries[i].numCubes = 0;
            sequenceBuffer.Entries[i].cubeLookup = new int[Constants.NumCubes];
            sequenceBuffer.Entries[i].cubeIds = new int[Constants.NumCubes];
            sequenceBuffer.Entries[i].cubeState = new CubeState[Constants.NumCubes];
        }

        Reset();
    }

    public void Reset()
    {
        Profiler.BeginSample( "DeltaBuffer.Reset" );

        sequenceBuffer.Reset();

        for ( int i = 0; i < sequenceBuffer.GetSize(); ++i )
        {
            sequenceBuffer.Entries[i].resetSequence = 0;
            sequenceBuffer.Entries[i].numCubes = 0;
        }

        Profiler.EndSample();
    }

    public bool AddPacket( ushort sequence, ushort resetSequence )
    {
        int index = sequenceBuffer.Insert( sequence );
        if ( index == -1 )
            return false;

        sequenceBuffer.Entries[index].resetSequence = resetSequence;

        sequenceBuffer.Entries[index].numCubes = 0;

        for ( int i = 0; i < Constants.NumCubes; ++i )
            sequenceBuffer.Entries[index].cubeLookup[i] = -1;

        return true;
    }

    public bool AddCubeState( ushort sequence, int cubeId, ref CubeState cubeState )
    {
        int index = sequenceBuffer.Find( sequence );

        if ( index == -1 )
            return false;

        int numCubes = sequenceBuffer.Entries[index].numCubes;

        Assert.IsTrue( numCubes < Constants.NumCubes );

        sequenceBuffer.Entries[index].cubeLookup[cubeId] = numCubes;
        sequenceBuffer.Entries[index].cubeIds[numCubes] = cubeId;
        sequenceBuffer.Entries[index].cubeState[numCubes] = cubeState;
        sequenceBuffer.Entries[index].numCubes++;

        return true;
    }

    public bool GetCubeState( ushort sequence, ushort resetSequence, int cubeId, ref CubeState cubeState )
    {
        int index = sequenceBuffer.Find( sequence );
        if ( index == -1 )
            return false;

        if ( sequenceBuffer.Entries[index].resetSequence != resetSequence )
            return false;
        
        if ( sequenceBuffer.Entries[index].numCubes == 0 )
            return false;

        int cubeIndex = sequenceBuffer.Entries[index].cubeLookup[cubeId];
        if ( cubeIndex == -1 )
            return false;

        cubeState = sequenceBuffer.Entries[index].cubeState[cubeIndex];

        return true;
    }

    public bool GetPacketData( ushort sequence, ushort resetSequence, out int numCubes, out int[] cubeIds, out CubeState[] cubeState )
    {
        int index = sequenceBuffer.Find( sequence );

        if ( index == -1 || sequenceBuffer.Entries[index].resetSequence != resetSequence )
        {
            numCubes = 0;
            cubeIds = null;
            cubeState = null;
            return false;
        }

        numCubes = sequenceBuffer.Entries[index].numCubes;
        cubeIds = sequenceBuffer.Entries[index].cubeIds;
        cubeState = sequenceBuffer.Entries[index].cubeState;

        return true;
    }
}
