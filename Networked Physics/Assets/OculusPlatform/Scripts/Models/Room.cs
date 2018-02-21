// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class Room
  {
    public readonly UInt64 ID;
    public readonly UInt64 ApplicationID;
    public readonly Dictionary<string, string> DataStore;
    public readonly string Description;
    public readonly UserList InvitedUsers;
    public readonly bool IsMembershipLocked;
    public readonly RoomJoinPolicy JoinPolicy;
    public readonly RoomJoinability Joinability;
    public readonly uint MaxUsers;
    public readonly string Name;
    public readonly User Owner;
    public readonly RoomType Type;
    public readonly UserList Users;
    public readonly uint Version;


    public Room(IntPtr o)
    {
      ID = CAPI.ovr_Room_GetID(o);
      ApplicationID = CAPI.ovr_Room_GetApplicationID(o);
      DataStore = CAPI.DataStoreFromNative(CAPI.ovr_Room_GetDataStore(o));
      Description = CAPI.ovr_Room_GetDescription(o);
      InvitedUsers = new UserList(CAPI.ovr_Room_GetInvitedUsers(o));
      IsMembershipLocked = CAPI.ovr_Room_GetIsMembershipLocked(o);
      JoinPolicy = CAPI.ovr_Room_GetJoinPolicy(o);
      Joinability = CAPI.ovr_Room_GetJoinability(o);
      MaxUsers = CAPI.ovr_Room_GetMaxUsers(o);
      Name = CAPI.ovr_Room_GetName(o);
      Owner = new User(CAPI.ovr_Room_GetOwner(o));
      Type = CAPI.ovr_Room_GetType(o);
      Users = new UserList(CAPI.ovr_Room_GetUsers(o));
      Version = CAPI.ovr_Room_GetVersion(o);
    }
  }

  public class RoomList : DeserializableList<Room> {
    public RoomList(IntPtr a) {
      var count = (int)CAPI.ovr_RoomArray_GetSize(a);
      _Data = new List<Room>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new Room(CAPI.ovr_RoomArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_RoomArray_GetNextUrl(a);
    }

  }
}
