// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace Oculus.Platform
{
  using UnityEngine;
  using System;
  using System.Collections;
  using System.Collections.Generic;

  public static class PlatformInternal
  {
    // Keep this enum in sync with ovrMessageTypeInternal in OVR_Platform_Internal.h
    public enum MessageTypeInternal : uint { //TODO - rename this to type; it's already in Message class
      Application_GetInstalledApplications = 0x520F744C,
      GraphAPI_Get                         = 0x30FF006E,
      GraphAPI_Post                        = 0x76A5A7C4,
      HTTP_Get                             = 0x6FB63223,
      HTTP_GetToFile                       = 0x4E81DC59,
      HTTP_Post                            = 0x6B36A54F,
      Party_Create                         = 0x1AD31B4F,
      Party_GatherInApplication            = 0x7287C183,
      Party_Get                            = 0x5E8953BD,
      Party_GetCurrentForUser              = 0x58CBFF2A,
      Party_Invite                         = 0x35B5C4E3,
      Party_Join                           = 0x68027C73,
      Party_Leave                          = 0x329206D1,
      Room_CreateOrUpdateAndJoinNamed      = 0x7C8E0A91,
      Room_GetNamedRooms                   = 0x077D6E8C,
      Room_GetSocialRooms                  = 0x61881D76,
      User_NewEntitledTestUser             = 0x11741F03,
      User_NewTestUser                     = 0x36E84F8C,
      User_NewTestUserFriends              = 0x1ED726C7
    };

    public static void CrashApplication() {
      CAPI.ovr_CrashApplication();
    }

    internal static Message ParseMessageHandle(IntPtr messageHandle, Message.MessageType messageType)
    {
      Message message = null;
      switch ((PlatformInternal.MessageTypeInternal)messageType)
      {
        case MessageTypeInternal.Party_Leave:
          message = new Message(messageHandle);
          break;

        case MessageTypeInternal.Application_GetInstalledApplications:
          message = new MessageWithInstalledApplicationList(messageHandle);
          break;

        case MessageTypeInternal.Party_Get:
          message = new MessageWithParty(messageHandle);
          break;

        case MessageTypeInternal.Party_GetCurrentForUser:
          message = new MessageWithPartyUnderCurrentParty(messageHandle);
          break;

        case MessageTypeInternal.Room_CreateOrUpdateAndJoinNamed:
          message = new MessageWithRoomUnderViewerRoom(messageHandle);
          break;

        case MessageTypeInternal.Room_GetNamedRooms:
        case MessageTypeInternal.Room_GetSocialRooms:
          message = new MessageWithRoomList(messageHandle);
          break;

        case MessageTypeInternal.GraphAPI_Get:
        case MessageTypeInternal.GraphAPI_Post:
        case MessageTypeInternal.HTTP_Get:
        case MessageTypeInternal.HTTP_GetToFile:
        case MessageTypeInternal.HTTP_Post:
        case MessageTypeInternal.User_NewEntitledTestUser:
        case MessageTypeInternal.User_NewTestUser:
        case MessageTypeInternal.User_NewTestUserFriends:
          message = new MessageWithString(messageHandle);
          break;

      }
      return message;
    }

    public static class HTTP
    {
      public static void SetHttpTransferUpdateCallback(Message<Models.HttpTransferUpdate>.Callback callback)
      {
        Callback.SetNotificationCallback(
          Message.MessageType.Notification_HTTP_Transfer,
          callback
        );
      }
    }

  }
}
