// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class MatchmakingEnqueueResult
  {
    public readonly MatchmakingAdminSnapshot AdminSnapshot;
    public readonly uint AverageWait;
    public readonly uint MatchesInLastHourCount;
    public readonly uint MaxExpectedWait;
    public readonly string Pool;
    public readonly uint RecentMatchPercentage;
    public readonly string RequestHash;


    public MatchmakingEnqueueResult(IntPtr o)
    {
      AdminSnapshot = new MatchmakingAdminSnapshot(CAPI.ovr_MatchmakingEnqueueResult_GetAdminSnapshot(o));
      AverageWait = CAPI.ovr_MatchmakingEnqueueResult_GetAverageWait(o);
      MatchesInLastHourCount = CAPI.ovr_MatchmakingEnqueueResult_GetMatchesInLastHourCount(o);
      MaxExpectedWait = CAPI.ovr_MatchmakingEnqueueResult_GetMaxExpectedWait(o);
      Pool = CAPI.ovr_MatchmakingEnqueueResult_GetPool(o);
      RecentMatchPercentage = CAPI.ovr_MatchmakingEnqueueResult_GetRecentMatchPercentage(o);
      RequestHash = CAPI.ovr_MatchmakingEnqueueResult_GetRequestHash(o);
    }
  }

}
