using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTracker : MonoBehaviour {

    public GameObject player;
    public Camera mainCam, altCam;
    public int xMin, xMax, yMin, yMax; // These could be floats
    public WorldOffsets offs;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Dialogue.getPanning())
        {
            CameraPanInfo currentPan = Dialogue.getCameraPanInfo();
            if (currentPan.from == null)
            {
                currentPan.from = player;
            }
            else if (currentPan.to == null)
            {
                currentPan.to = player;
            }

            float interpolation = (Time.time - currentPan.startTime) / currentPan.duration;

            if (interpolation > 1.0f)
                Dialogue.setPanning(false);
            else
                panCamera(interpolation, currentPan.from, currentPan.to);
            return;
        }

        // Return if still in dialogue and camera doesn't need reset
        if (!Dialogue.getDoneDialogue() && !Dialogue.getCameraReset())
            return;

        updateCamera();
        Dialogue.setCameraReset(false);
    }

    private void updateCamera()
    {
        // Get transforms to get player location and  manipulate cameras
        Transform mainCamTrans = mainCam.GetComponent<Transform>();
        Transform altCamTrans = altCam.GetComponent<Transform>();
        Transform playerTrans = player.GetComponent<Transform>();

        // Get position and calculate where the camera's bounding box is
        Vector3 pos = playerTrans.position;
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

    private void panCamera(float interpolation, GameObject from, GameObject to)
    {
        // Get transforms to get pan and main camera locations
        Transform mainCamTrans = mainCam.GetComponent<Transform>();
        Transform fromTrans = from.GetComponent<Transform>();
        Transform toTrans = to.GetComponent<Transform>();

        // Get position using interpolation between from and to
        Vector3 fromPos = fromTrans.position;
        Vector3 toPos = toTrans.position;
        float newX = Mathf.Lerp(fromPos.x, toPos.x, interpolation);
        float newY = Mathf.Lerp(fromPos.y, toPos.y, interpolation);
        Vector3 pos = new Vector3(newX, newY, mainCamTrans.position.z); // Keep camera z

        // Calculate where the camera's bounding box is
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

        // Set the transform position
        mainCamTrans.position = pos;
    }

}
