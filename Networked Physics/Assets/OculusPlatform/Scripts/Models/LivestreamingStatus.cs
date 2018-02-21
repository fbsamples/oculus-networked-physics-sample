// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LivestreamingStatus
  {
    public readonly bool LivestreamingEnabled;
    public readonly bool MicEnabled;


    public LivestreamingStatus(IntPtr o)
    {
      LivestreamingEnabled = CAPI.ovr_LivestreamingStatus_GetLivestreamingEnabled(o);
      MicEnabled = CAPI.ovr_LivestreamingStatus_GetMicEnabled(o);
    }
  }

}
