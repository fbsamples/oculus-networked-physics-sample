using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OvrAvatarHead : MonoBehaviour {

    List<Material> voiceMaterials = new List<Material>();

	// Use this for initialization
	void Start () {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            foreach (Material material in renderer.materials)
            {
                if (material.HasProperty("_VoiceAmplitude"))
                {
                    voiceMaterials.Add(material);
                }
            }
        }
	}
	
    public void UpdatePose(float voiceAmplitude)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        foreach (Material material in voiceMaterials)
        {
            material.SetFloat("_VoiceAmplitude", voiceAmplitude);
        }
    }
}