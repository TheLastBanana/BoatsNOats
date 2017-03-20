using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTracker : MonoBehaviour {

    public GameObject Gemma;
    public Camera mainCam, altCam;
    private float xMin, xMax;
    public float yMin, yMax; // These could be floats
    public WorldOffsets offs;
    public GameObject LeftBoundary;
    public GameObject RightBoundary;
    private GameObject currentTarget;

	// Use this for initialization
	void Start () {
        xMin = (int)LeftBoundary.transform.position.x;
        xMax = (int)RightBoundary.transform.position.x;
        currentTarget = Gemma;
    }
	
	// Update is called once per frame
	void Update ()
    {
        // Get transforms to get player location and  manipulate cameras
        Transform mainCamTrans = mainCam.GetComponent<Transform>();
        Transform altCamTrans = altCam.GetComponent<Transform>();
        Transform targetTrans = currentTarget.GetComponent<Transform>();

        // Get position and calculate where the camera's bounding box is
        Vector3 pos = targetTrans.position;
        float camHeight = 2f * mainCam.orthographicSize;
        float camWidth = camHeight * mainCam.aspect;
        float left = pos.x - (camWidth / 2);
        float right = pos.x + (camWidth / 2);
        float bottom = pos.y - (camHeight / 2);
        float top = pos.y + (camHeight / 2);

        // Limit the camera to the bounds
        if (yMin > bottom)
            pos.y = yMin + (camHeight / 2);
        else if (yMax < top)
            pos.y = yMax - (camHeight / 2);

        if (xMin > left)
            pos.x = xMin + (camWidth / 2);
        else if (xMax < right)
            pos.x = xMax - (camWidth / 2);

        // Don't lose the camera z axis so we don't start clipping things
        pos.z = mainCamTrans.position.z;

        // Set the transform position
        mainCamTrans.position = pos;

        // Offset for the alt cam
        altCamTrans.position = pos + offs.offset;
    }

    // Change what the camera is currently focused on, changes after camera pans
    public void UpdateTarget(GameObject target)
    {
        if (target != null)
            currentTarget = target;
        else
            currentTarget = Gemma;
    }
}
