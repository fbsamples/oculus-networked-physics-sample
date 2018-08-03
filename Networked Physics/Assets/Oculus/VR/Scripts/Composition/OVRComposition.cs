using UnityEngine;
using System.Collections;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

public abstract class OVRComposition {

	public abstract OVRManager.CompositionMethod CompositionMethod();

	public abstract void Update(Camera mainCamera);
	public abstract void Cleanup();

	public virtual void RecenterPose() { }

	protected bool usingLastAttachedNodePose = false;
	protected OVRPose lastAttachedNodePose = new OVRPose();            // Sometimes the attach node pose is not readable (lose tracking, low battery, etc.) Use the last pose instead when it happens

	internal OVRPose ComputeCameraWorldSpacePose(OVRPlugin.CameraExtrinsics extrinsics)
	{
		OVRPose worldSpacePose = new OVRPose();
		OVRPose trackingSpacePose = new OVRPose();

		OVRPose cameraTrackingSpacePose = extrinsics.RelativePose.ToOVRPose();
		trackingSpacePose = cameraTrackingSpacePose;

		if (extrinsics.AttachedToNode != OVRPlugin.Node.None && OVRPlugin.GetNodePresent(extrinsics.AttachedToNode))
		{
			if (usingLastAttachedNodePose)
			{
				Debug.Log("The camera attached node get tracked");
				usingLastAttachedNodePose = false;
			}
			OVRPose attachedNodePose = OVRPlugin.GetNodePose(extrinsics.AttachedToNode, OVRPlugin.Step.Render).ToOVRPose();
			lastAttachedNodePose = attachedNodePose;
			trackingSpacePose = attachedNodePose * trackingSpacePose;
		}
		else
		{
			if (extrinsics.AttachedToNode != OVRPlugin.Node.None)
			{
				if (!usingLastAttachedNodePose)
				{
					Debug.LogWarning("The camera attached node could not be tracked, using the last pose");
					usingLastAttachedNodePose = true;
				}
				trackingSpacePose = lastAttachedNodePose * trackingSpacePose;
			}
		}

		worldSpacePose = OVRExtensions.ToWorldSpacePose(trackingSpacePose);
		return worldSpacePose;
	}

}

#endif
