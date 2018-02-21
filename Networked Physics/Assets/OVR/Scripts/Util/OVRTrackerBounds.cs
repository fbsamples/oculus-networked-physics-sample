/************************************************************************************

Copyright   :   Copyright 2014-2017 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.4.1 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.4.1/

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using VR = UnityEngine.VR;

/// <summary>
/// Calculates distance to tracking volume and displays arrows or icons when close.
/// </summary>
[ExecuteInEditMode]
public class OVRTrackerBounds : MonoBehaviour
{
    private static readonly int numPlanes = 6;
    private Plane[] plane = new Plane[numPlanes];

    public bool enableFade = true;
    [Tooltip("Distance from volume to start fading")]
    public float fadeDistance = 0.1f;
    [Tooltip("Maximum fade amount (from 0.0 to 1.0)")]
    public float fadeMaximum = 0.2f;
    public Color fadeColor = Color.black;

    public bool enableIcons = false;
    public Texture2D [] iconTextures;
    public RawImage iconImage;

    public bool enableArrow = true;
    public GameObject arrowObject;
    public Vector3 arrowOffset = Vector3.zero;
    public float arrowDistance = 0.2f;
    public float animSpeed = 10.0f;
    public float animDistance = 0.2f;
	public float waitTime = 10f;

    private Material fadeMaterial = null;
	private Color iconColor = new Color(1,1,1,1);

    public void SetEnableFade(bool b)
    {
        enableFade = b;
    }

    public void SetEnableArrow(bool b)
    {
        enableArrow = b;
    }

    void Awake()
    {
		if (!Application.isPlaying)
			return;

		fadeMaterial = new Material(Shader.Find("Oculus/Unlit Transparent Color"));
    }

    void OnDestroy()
    {
        if (fadeMaterial != null)
            Destroy(fadeMaterial);
    }

	/// <summary>
	/// Computes frustum planes from the sensor's frustum parameters.
	/// </summary>
	void ComputePlanes()
    {
        OVRTracker.Frustum frustum = OVRManager.tracker.GetFrustum();
        float nearZ = frustum.nearZ;
        float farZ = frustum.farZ;
        float hFOV = Mathf.Deg2Rad * frustum.fov.x * 0.5f;
        float vFOV = Mathf.Deg2Rad * frustum.fov.y * 0.5f;
        float sx = Mathf.Sin(hFOV);
        float sy = Mathf.Sin(vFOV);

        plane[0] = new Plane(Vector3.zero, farZ * new Vector3(sx, sy, 1f), farZ * new Vector3(sx, -sy, 1f));    // right
        plane[1] = new Plane(Vector3.zero, farZ * new Vector3(-sx, -sy, 1f), farZ * new Vector3(-sx, sy, 1f));  // left
        plane[2] = new Plane(Vector3.zero, farZ * new Vector3(-sx, sy, 1f), farZ * new Vector3(sx, sy, 1f));    // top
        plane[3] = new Plane(Vector3.zero, farZ * new Vector3(sx, -sy, 1f), farZ * new Vector3(-sx, -sy, 1f));  // bottom
        plane[4] = new Plane(farZ * new Vector3(sx, sy, 1f), farZ * new Vector3(-sx, sy, 1f), farZ * new Vector3(-sx, -sy, 1f));		// far
		plane[5] = new Plane(nearZ * new Vector3(-sx, -sy, 1f), nearZ * new Vector3(-sx, sy, 1f), nearZ * new Vector3(sx, sy, 1f) );	// near
    }
	
	/// <summary>
	/// Computes signed distance to frustum planes as maximum of distance to each plane
    /// negative inside volume, positive outside.
	/// </summary>
	float DistanceToPlanes(Vector3 p, out int closestPlane)
    {
        float maxd = Mathf.NegativeInfinity;
        closestPlane = 0;
        for (int i = 0; i < numPlanes; i++)
        {
            float d = plane[i].GetDistanceToPoint(p);
            if (d > maxd)
            {
                closestPlane = i;
                maxd = d;
            }
        }
        return maxd;
    }

    /// <summary>
    /// Interpolates between a and b with custom smoothing.
    /// </summary>
    static float SmoothStep(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3.0f - 2.0f * t);
    }

	void Update ()
	{
		if (!Application.isPlaying || !OVRManager.tracker.isPresent || Time.time < waitTime)
		{
			if (arrowObject && arrowObject.activeSelf)
				arrowObject.SetActive(false);

			if (iconImage != null)
			{
				iconImage.enabled = false;
				iconImage.gameObject.SetActive(false);
			}

			return;
		}

        // TODO - probably don't have to do this every frame!
        ComputePlanes();

        OVRPose trackerPose = OVRManager.tracker.GetPose();
        Matrix4x4 trackerMat = Matrix4x4.TRS(trackerPose.position, trackerPose.orientation, Vector3.one);

        // Transform point into volume space
		OVRPose headPose;
		headPose.position = VR.InputTracking.GetLocalPosition(VR.VRNode.Head);

		Vector3 localPos = trackerMat.inverse.MultiplyPoint(headPose.position);

        int closestPlane;
        float dist = DistanceToPlanes(localPos, out closestPlane);
        //Debug.Log("dist = " + dist);

        if (enableIcons)
        {
            // Display arrow icon if approaching edge of volume
            if (dist > -fadeDistance)
            {
                iconImage.gameObject.SetActive(true);
				iconImage.enabled = true;
                iconImage.texture = iconTextures[closestPlane];
				iconColor.a = SmoothStep(-fadeDistance, 0.0f, dist);
				iconImage.color = iconColor;
            }
            else
            {
                iconImage.enabled = false;
				iconImage.gameObject.SetActive(false);
			}
        }

        if (arrowObject)
        {
            // Display animated arrow object pointing back towards volume
            if (enableArrow && dist > -arrowDistance)
            {
                arrowObject.SetActive(true);

                Vector3 planeNormal = plane[closestPlane].normal;
                Vector3 planeNormalTS = trackerMat.MultiplyVector(planeNormal);

                arrowObject.transform.localPosition = arrowOffset + planeNormalTS * Mathf.Sin(Time.time * animSpeed) * animDistance;
                arrowObject.transform.rotation = Quaternion.FromToRotation(Vector3.forward, -planeNormalTS);
            }
            else
            {
                arrowObject.SetActive(false);
            }
        }

        // Fade based on distance from planes
        fadeMaximum = Mathf.Clamp01(fadeMaximum);
        fadeColor.a = SmoothStep(-fadeDistance, 0.0f, dist) * fadeMaximum;
	}

    void OnRenderObject()
	{
		if (!Application.isPlaying || !OVRManager.tracker.isPresent || Time.time < waitTime)
			return;

        // Full-screen fade
        if (enableFade && fadeColor.a > 0.0)
        {
            fadeMaterial.color = fadeColor;
            fadeMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.QUADS);
            GL.Vertex3(0f, 0f, 0f);
            GL.Vertex3(0f, 1f, 0f);
            GL.Vertex3(1f, 1f, 0f);
            GL.Vertex3(1f, 0f, 0f);
            GL.End();
            GL.PopMatrix();
        }
    }

}
