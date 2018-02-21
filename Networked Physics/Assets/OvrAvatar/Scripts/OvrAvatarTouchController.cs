using UnityEngine;
using System.Collections;

public class OvrAvatarTouchController : MonoBehaviour {

    public Animator animator;

    public void UpdatePose(OvrAvatarDriver.ControllerPose pose)
    {
        if (!gameObject.activeInHierarchy || animator == null)
        {
            return;
        }
        animator.SetFloat("Button 1", pose.button1IsDown ? 1.0f : 0.0f);
        animator.SetFloat("Button 2", pose.button2IsDown ? 1.0f : 0.0f);
        animator.SetFloat("Joy X", pose.joystickPosition.x);
        animator.SetFloat("Joy Y", pose.joystickPosition.y);
        animator.SetFloat("Trigger", pose.indexTrigger);
        animator.SetFloat("Grip", pose.gripTrigger);
    }
}