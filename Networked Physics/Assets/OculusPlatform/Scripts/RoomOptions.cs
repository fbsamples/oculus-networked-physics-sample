// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class RoomOptions {

    public RoomOptions() {
      Handle = CAPI.ovr_RoomOptions_Create();
    }

    public void SetOrdering(UserOrdering value) {
      CAPI.ovr_RoomOptions_SetOrdering(Handle, value);
    }

    public void SetRoomId(UInt64 value) {
      CAPI.ovr_RoomOptions_SetRoomId(Handle, value);
    }


    // For passing to native C
    public static explicit operator IntPtr(RoomOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~RoomOptions() {
      CAPI.ovr_RoomOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
