/**
 * Copyright (c) 2017-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the Scripts directory of this source tree. An additional grant 
 * of patent rights can be found in the PATENTS file in the same directory.
 */

using UnityEngine;
using UnityEditor;

public class Build
{
    [MenuItem( "Build/Build Host" )]
    public static void BuildHost()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/Host.unity" };
        buildPlayerOptions.locationPathName = "Builds/Host.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;
        BuildPipeline.BuildPlayer( buildPlayerOptions );
    }

    [MenuItem( "Build/Build Guest" )]
    public static void BuildGuest()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/Guest.unity" };
        buildPlayerOptions.locationPathName = "Builds/Guest.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;
        BuildPipeline.BuildPlayer( buildPlayerOptions );
    }

    [MenuItem( "Build/Build Loopback" )]
    public static void BuildLoopback()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/Loopback.unity" };
        buildPlayerOptions.locationPathName = "Builds/Loopback.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;
        BuildPipeline.BuildPlayer( buildPlayerOptions );
    }

    [MenuItem( "Build/Build All" )]
    public static void BuildAll()
    {
        BuildHost();
        BuildGuest();
        BuildLoopback();
    }
}
