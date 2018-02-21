// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class Party
  {
    public readonly UInt64 ID;
    public readonly UserList InvitedUsers;
    public readonly User Leader;
    public readonly Room Room;
    public readonly UserList Users;


    public Party(IntPtr o)
    {
      ID = CAPI.ovr_Party_GetID(o);
      InvitedUsers = new UserList(CAPI.ovr_Party_GetInvitedUsers(o));
      Leader = new User(CAPI.ovr_Party_GetLeader(o));
      Room = new Room(CAPI.ovr_Party_GetRoom(o));
      Users = new UserList(CAPI.ovr_Party_GetUsers(o));
    }
  }

}
