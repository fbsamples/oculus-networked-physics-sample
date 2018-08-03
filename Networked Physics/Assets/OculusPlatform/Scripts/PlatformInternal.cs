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
      Application_ExecuteCoordinatedLaunch          = 0x267DB4F4,
      Application_GetInstalledApplications          = 0x520F744C,
      Avatar_UpdateMetaData                         = 0x7BCFD98E,
      GraphAPI_Get                                  = 0x30FF006E,
      GraphAPI_Post                                 = 0x76A5A7C4,
      HTTP_Get                                      = 0x6FB63223,
      HTTP_GetToFile                                = 0x4E81DC59,
      HTTP_MultiPartPost                            = 0x5842D210,
      HTTP_Post                                     = 0x6B36A54F,
      Livestreaming_IsAllowedForApplication         = 0x0B6D8D76,
      Livestreaming_StartPartyStream                = 0x7B2F5CDC,
      Livestreaming_StartStream                     = 0x501AC7BE,
      Livestreaming_StopPartyStream                 = 0x27670F58,
      Livestreaming_StopStream                      = 0x44E40DCA,
      Livestreaming_UpdateCommentsOverlayVisibility = 0x1F7D8034,
      Livestreaming_UpdateMicStatus                 = 0x1C577D87,
      Party_Create                                  = 0x1AD31B4F,
      Party_GatherInApplication                     = 0x7287C183,
      Party_Get                                     = 0x5E8953BD,
      Party_GetCurrentForUser                       = 0x58CBFF2A,
      Party_Invite                                  = 0x35B5C4E3,
      Party_Join                                    = 0x68027C73,
      Party_Leave                                   = 0x329206D1,
      Room_CreateOrUpdateAndJoinNamed               = 0x7C8E0A91,
      Room_GetNamedRooms                            = 0x077D6E8C,
      Room_GetSocialRooms                           = 0x61881D76,
      SystemPermissions_GetStatus                   = 0x1D6A2C09,
      SystemPermissions_LaunchDeeplink              = 0x1A5A8431,
      User_CancelRecordingForReportFlow             = 0x03E0D149,
      User_GetLinkedAccounts                        = 0x5793F456,
      User_LaunchBlockFlow                          = 0x6FD62528,
      User_LaunchReportFlow                         = 0x5662A011,
      User_LaunchUnblockFlow                        = 0x14A22A97,
      User_NewEntitledTestUser                      = 0x11741F03,
      User_NewTestUser                              = 0x36E84F8C,
      User_NewTestUserFriends                       = 0x1ED726C7,
      User_StartRecordingForReportFlow              = 0x6C6E33E3,
      User_StopRecordingAndLaunchReportFlow         = 0x60788C8B
    };

    public static void CrashApplication() {
      CAPI.ovr_CrashApplication();
    }

    internal static Message ParseMessageHandle(IntPtr messageHandle, Message.MessageType messageType)
    {
      Message message = null;
      switch ((PlatformInternal.MessageTypeInternal)messageType)
      {
        case MessageTypeInternal.User_StartRecordingForReportFlow:
          message = new MessageWithAbuseReportRecording(messageHandle);
          break;

        case MessageTypeInternal.Application_ExecuteCoordinatedLaunch:
        case MessageTypeInternal.Livestreaming_StopPartyStream:
        case MessageTypeInternal.Livestreaming_UpdateMicStatus:
        case MessageTypeInternal.Party_Leave:
        case MessageTypeInternal.User_CancelRecordingForReportFlow:
        case MessageTypeInternal.User_LaunchBlockFlow:
        case MessageTypeInternal.User_LaunchUnblockFlow:
          message = new Message(messageHandle);
          break;

        case MessageTypeInternal.Application_GetInstalledApplications:
          message = new MessageWithInstalledApplicationList(messageHandle);
          break;

        case MessageTypeInternal.User_GetLinkedAccounts:
          message = new MessageWithLinkedAccountList(messageHandle);
          break;

        case MessageTypeInternal.Livestreaming_IsAllowedForApplication:
          message = new MessageWithLivestreamingApplicationStatus(messageHandle);
          break;

        case MessageTypeInternal.Livestreaming_StartPartyStream:
        case MessageTypeInternal.Livestreaming_StartStream:
          message = new MessageWithLivestreamingStartResult(messageHandle);
          break;

        case MessageTypeInternal.Livestreaming_UpdateCommentsOverlayVisibility:
          message = new MessageWithLivestreamingStatus(messageHandle);
          break;

        case MessageTypeInternal.Livestreaming_StopStream:
          message = new MessageWithLivestreamingVideoStats(messageHandle);
          break;

        case MessageTypeInternal.Party_Get:
          message = new MessageWithParty(messageHandle);
          break;

        case MessageTypeInternal.Party_GetCurrentForUser:
          message = new MessageWithPartyUnderCurrentParty(messageHandle);
          break;

        case MessageTypeInternal.Party_Create:
        case MessageTypeInternal.Party_GatherInApplication:
        case MessageTypeInternal.Party_Invite:
        case MessageTypeInternal.Party_Join:
          message = new MessageWithPartyID(messageHandle);
          break;

        case MessageTypeInternal.Room_CreateOrUpdateAndJoinNamed:
          message = new MessageWithRoomUnderViewerRoom(messageHandle);
          break;

        case MessageTypeInternal.Room_GetNamedRooms:
        case MessageTypeInternal.Room_GetSocialRooms:
          message = new MessageWithRoomList(messageHandle);
          break;

        case MessageTypeInternal.Avatar_UpdateMetaData:
        case MessageTypeInternal.GraphAPI_Get:
        case MessageTypeInternal.GraphAPI_Post:
        case MessageTypeInternal.HTTP_Get:
        case MessageTypeInternal.HTTP_GetToFile:
        case MessageTypeInternal.HTTP_MultiPartPost:
        case MessageTypeInternal.HTTP_Post:
        case MessageTypeInternal.User_NewEntitledTestUser:
        case MessageTypeInternal.User_NewTestUser:
        case MessageTypeInternal.User_NewTestUserFriends:
          message = new MessageWithString(messageHandle);
          break;

        case MessageTypeInternal.SystemPermissions_GetStatus:
        case MessageTypeInternal.SystemPermissions_LaunchDeeplink:
          message = new MessageWithSystemPermission(messageHandle);
          break;

        case MessageTypeInternal.User_LaunchReportFlow:
        case MessageTypeInternal.User_StopRecordingAndLaunchReportFlow:
          message = new MessageWithUserReportID(messageHandle);
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
