/**
 * Copyright (c) 2017-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the Scripts directory of this source tree. An additional grant 
 * of patent rights can be found in the PATENTS file in the same directory.
 */

using UnityEngine;
using UnityEngine.Assertions;

public class Interactions
{
    public class Entry
    {
        public byte[] interactions = new byte[Constants.NumCubes];

        public void AddInteraction( ushort id )
        {
            interactions[id] = 1;
        }

        public void RemoveInteraction( ushort id )
        {
            interactions[id] = 0;
        }
    }

    Entry[] entries = new Entry[Constants.NumCubes];

    public Interactions()
    {
        for ( int i = 0; i < Constants.NumCubes; ++i )
        {
            entries[i] = new Entry();
        }
    }

    public void AddInteraction( ushort id1, ushort id2 )
    {
        entries[id1].AddInteraction( id2 );
        entries[id2].AddInteraction( id1 );
    }

    public void RemoveInteraction( ushort id1, ushort id2 )
    {
        entries[id1].RemoveInteraction( id2 );
        entries[id2].RemoveInteraction( id1 );
    }

    public Entry GetInteractions( int cubeId )
    {
        Assert.IsTrue( cubeId >= 0 );
        Assert.IsTrue( cubeId < Constants.NumCubes );
        return entries[cubeId];        
    }
}
