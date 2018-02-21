using UnityEngine;
using System.Collections;

public class OvrAvatarHand : MonoBehaviour
{

    bool showControllers = false;
    public Animator animator;

    public void HoldController(bool show)
    {
        showControllers = show;
    }

    public void UpdatePose(OvrAvatarDriver.HandPose pose)
    {
        if (!gameObject.activeInHierarchy || animator == null)
        {
            return;
        }
        animator.SetBool("HoldController", showControllers);
        animator.SetFloat("Flex", pose.gripFlex);
        animator.SetFloat("Pinch", pose.indexFlex);
        animator.SetLayerWeight(animator.GetLayerIndex("Point Layer"), pose.isPointing ? 1.0f : 0.0f);
        animator.SetLayerWeight(animator.GetLayerIndex("Thumb Layer"), pose.isThumbUp ? 1.0f : 0.0f);
    }
}
