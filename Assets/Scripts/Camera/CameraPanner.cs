using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPanner : MonoBehaviour {
    public Camera mainCam;
    private float xMin, xMax;
    private float yMin, yMax;
    public WorldOffsets offs;
    private GameObject LeftBoundary;
    private GameObject RightBoundary;
    public GameObject cutsceneManagerObj;
    private CutsceneManager cutsceneManager;
    public CameraPanInfo currentPan;

    // Use this for initialization
    void Start()
    {
        // These will be the same, this is so they aren't in two places
        CameraTracker cameraTracker = this.GetComponent<CameraTracker>();
        yMin = cameraTracker.yMin;
        yMax = cameraTracker.yMax;
        LeftBoundary = cameraTracker.LeftBoundary;
        RightBoundary = cameraTracker.RightBoundary;

        cutsceneManager = cutsceneManagerObj.GetComponent<CutsceneManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentPan.from == null)
            currentPan.from = cutsceneManager.Gemma;
        if (currentPan.to == null)
            currentPan.to = cutsceneManager.Gemma;

        float interpolation = (Time.time - currentPan.startTime) / currentPan.duration;

        if (interpolation > 1.0f)
            cutsceneManager.EndPan();
        else
            panCamera(interpolation, currentPan.from, currentPan.to);
    }

    private void panCamera(float interpolation, GameObject from, GameObject to)
    {
        // Get transforms to get pan and main camera locations
        Transform mainCamTrans = mainCam.GetComponent<Transform>();
        Transform fromTrans = from.GetComponent<Transform>();
        Transform toTrans = to.GetComponent<Transform>();

        // Get position using interpolation between from and to
        Vector3 fromPos = fromTrans.position;
        Vector3 toPos = toTrans.position;

        // Calculate where the camera's bounding box is
        float camHeight = 2f * mainCam.orthographicSize;
        float camWidth = camHeight * mainCam.aspect;
        fromPos = checkCameraBounds(fromPos, camHeight, camWidth);
        toPos = checkCameraBounds(toPos, camHeight, camWidth);

        float newX = Mathf.Lerp(fromPos.x, toPos.x, interpolation);
        float newY = Mathf.Lerp(fromPos.y, toPos.y, interpolation);
        Vector3 pos = new Vector3(newX, newY, mainCamTrans.position.z); // Keep camera z

        // Set the transform position
        mainCamTrans.position = pos;
    }

    // Change the given position such that a camera focused on it is within camera bounds
    private Vector3 checkCameraBounds(Vector3 pos, float camHeight, float camWidth)
    {
        xMin = LeftBoundary.transform.position.x;
        xMax = RightBoundary.transform.position.x;

        // Get camera edges
        float left = pos.x - (camWidth / 2);
        float right = pos.x + (camWidth / 2);
        float bottom = pos.y - (camHeight / 2);
        float top = pos.y + (camHeight / 2);

        // Move position so edges are in camera bounds
        if (yMin > bottom)
            pos.y = yMin + (camHeight / 2);
        else if (yMax < top)
            pos.y = yMax - (camHeight / 2);

        if (xMin > left)
            pos.x = xMin + (camWidth / 2);
        else if (xMax < right)
            pos.x = xMax - (camWidth / 2);

        return pos;
    }
}
