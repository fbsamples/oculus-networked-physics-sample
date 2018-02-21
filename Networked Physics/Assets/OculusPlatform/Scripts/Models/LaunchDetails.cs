// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LaunchDetails
  {
    public readonly LaunchType LaunchType;
    public readonly UInt64 RoomID;
    public readonly UserList Users;


    public LaunchDetails(IntPtr o)
    {
      LaunchType = CAPI.ovr_LaunchDetails_GetLaunchType(o);
      RoomID = CAPI.ovr_LaunchDetails_GetRoomID(o);
      Users = new UserList(CAPI.ovr_LaunchDetails_GetUsers(o));
    }
  }

}
