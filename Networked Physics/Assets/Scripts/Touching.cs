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

public class Touching : MonoBehaviour
{
    public Context context;
    public int cubeId;

    public void Initialize( Context context, int cubeId )
    {
        this.context = context;
        this.cubeId = cubeId;
    }

    void OnTriggerEnter( Collider other )
    {
        Touching otherTouching = other.gameObject.GetComponent<Touching>();

        if ( !otherTouching )
            return;

        int otherCubeId = otherTouching.cubeId;

        context.OnTouchStart( cubeId, otherCubeId );
    }

    void OnTriggerExit( Collider other )
    {
        Touching otherTouching = other.gameObject.GetComponent<Touching>();

        if ( !otherTouching )
            return;

        int otherCubeId = otherTouching.cubeId;

        context.OnTouchFinish( cubeId, otherCubeId );
    }
}
