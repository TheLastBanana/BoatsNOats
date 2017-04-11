using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialoguePointer : MonoBehaviour {

    public GameObject pointTo;

    // Left and right bound in relation to parent text bubble
    private float leftBound = -3.35f;
    private float rightBound = 3.35f;

    // Actual X world positions of bounds
    private float leftX;
    private float rightX;

    private Transform objTransform;
    private Transform pointToTransform;
    private float y;
    private float z;
    private Transform dummy;

	// Use this for initialization
	void Start () {
        objTransform = GetComponent<Transform>();
        if (pointTo != null)
            pointToTransform = pointTo.GetComponent<Transform>();
        else
            this.gameObject.SetActive(false);

        y = objTransform.localPosition.y;
        z = objTransform.localPosition.z;
        dummy = new GameObject().GetComponent<Transform>();
        dummy.parent = objTransform.parent;
	}
	
	// Update is called once per frame
	void Update () {
        if (pointTo == null)
            return;

        changeXFromCameraPosition();

        // Match X position of object we're pointing to, within bounds
        float newX = pointToTransform.position.x;
        if (newX < leftX)
            newX = leftX;
        else if (newX > rightX)
            newX = rightX;

        // Scale X position to local coordinates
        newX = Mathf.Lerp(leftBound, rightBound, Mathf.InverseLerp(leftX, rightX, newX));
        objTransform.localPosition = new Vector3(newX, y, z);

        // Now rotate to face object we're pointing to
        // Taken from http://answers.unity3d.com/answers/651344/view.html
        Vector3 vectorToTarget = pointToTransform.position - objTransform.position;
        float angle = (Mathf.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg) + 90;
        Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
        objTransform.rotation = Quaternion.RotateTowards(objTransform.rotation, q, Time.deltaTime * 1000);
	}

    private void changeXFromCameraPosition()
    {
        dummy.localPosition = new Vector3(0f + leftBound, y, z);
        leftX = dummy.position.x;
        dummy.localPosition = new Vector3(0f + rightBound, y, z);
        rightX = dummy.position.x;
    }
}
