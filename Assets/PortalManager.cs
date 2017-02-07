using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalManager : MonoBehaviour
{
    public AudioSource timeStopSound;
    public AudioSource altWorldAmbience;
    public AudioSource portalDragSound;
    public AudioSource timeStartSound;
    public AudioSource objectCutSound;
    public AudioLowPassFilter altWorldAmbienceLPF;
    public float portalSoundMaxSize = 20.0f;
    public float dragLpfLow = 200.0f;
    public float dragLpfHigh = 3000.0f;
    public float dragPitchLow = 1.0f;
    public float dragPitchHigh = 2.0f;

    bool isSelecting = false;
    Vector3 mousePosition1;
    Rect rect;

    AudioEffects afx;

    // Use this for initialization
    void Start ()
    {
        afx = GetComponent<AudioEffects>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        // If we press the right mouse button, save mouse location and begin selection box
        if (Input.GetMouseButtonDown(1))
        {
            isSelecting = true;
            mousePosition1 = Input.mousePosition;

            timeStopSound.Play();
            portalDragSound.Play();
            altWorldAmbience.Play();
            afx.cancelEffects(portalDragSound);
            afx.cancelEffects(altWorldAmbience);

            // Disable physics
            foreach (var physicsObject in FindObjectsOfType<Rigidbody2D>())
                physicsObject.simulated = false;
        }
        // If we let go of the right mouse button, end selection
        if (Input.GetMouseButtonUp(1))
        {
            var v1 = Camera.main.ScreenToWorldPoint(mousePosition1);
            var v2 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var min = Vector3.Min(v1, v2);
            var max = Vector3.Max(v1, v2);
            min.z = 0;
            max.z = 0;
            var bounds = new Bounds();
            bounds.SetMinMax(min, max);

            //Iterate through the splittable objects
            bool anyCuts = false;
            foreach (var selectableObject in FindObjectsOfType<Splittable>())
            {
                //If the object and the selection box bounds touch figure out where they do for cutting purposes
                if (bounds.Intersects(selectableObject.totalBounds))
                {
                    cutObject(selectableObject, bounds);
                    anyCuts = true;
                }

            }
            isSelecting = false;

            // Enable physics
            foreach (var physicsObject in FindObjectsOfType<Rigidbody2D>())
                physicsObject.simulated = true;

            afx.smoothStop(portalDragSound);
            afx.smoothStop(altWorldAmbience);
            timeStartSound.Play();
            if (anyCuts) objectCutSound.Play();
        }

        if (isSelecting)
        {
            // Get size of selection box
            float selectionSize = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.ScreenToWorldPoint(mousePosition1)).magnitude;
            float sizeFactor = selectionSize / portalSoundMaxSize;

            // Update pitch of drag sound
            portalDragSound.pitch = Mathf.Lerp(dragPitchLow, dragPitchHigh, sizeFactor);

            // Update low-pass filter on alternate world ambience
            altWorldAmbienceLPF.cutoffFrequency = Mathf.Lerp(dragLpfLow, dragLpfHigh, sizeFactor);
        }
    }

    // Move a Transform's children to another Transform
    static void transferChildren(Transform oldParent, Transform newParent)
    {
        for (int i = 0; i < oldParent.childCount; ++i)
        {
            oldParent.GetChild(i).SetParent(newParent);
        }
    }

    //Figure out the 4 corners of the bounds for both the selection box and the object and do AABB to figure out where they overlap
    void cutObject(Splittable selectableObject, Bounds selectbounds)
    {

        //Check which edge intersects the object or if the selection box is all around the object
        Vector2 selectbotleft = new Vector2(selectbounds.center.x - selectbounds.extents.x, selectbounds.center.y - selectbounds.extents.y);
        Vector2 selecttopleft = new Vector2(selectbounds.center.x - selectbounds.extents.x, selectbounds.center.y + selectbounds.extents.y);
        Vector2 selectbotright = new Vector2(selectbounds.center.x + selectbounds.extents.x, selectbounds.center.y - selectbounds.extents.y);
        Vector2 selecttopright = new Vector2(selectbounds.center.x + selectbounds.extents.x, selectbounds.center.y + selectbounds.extents.y);

        Bounds objbounds = selectableObject.totalBounds;
        Vector2 objbotleft = new Vector2(objbounds.center.x - objbounds.extents.x, objbounds.center.y - objbounds.extents.y);
        Vector2 objtopleft = new Vector2(objbounds.center.x - objbounds.extents.x, objbounds.center.y + objbounds.extents.y);
        Vector2 objbotright = new Vector2(objbounds.center.x + objbounds.extents.x, objbounds.center.y - objbounds.extents.y);
        Vector2 objtopright = new Vector2(objbounds.center.x + objbounds.extents.x, objbounds.center.y + objbounds.extents.y);

        List<List<GameObject>> verticalPieces = new List<List<GameObject>>();
        verticalPieces.Add(null);
        verticalPieces.Add(null);
        List<List<GameObject>> horizontalPieces = new List<List<GameObject>>();
        horizontalPieces.Add(null);
        horizontalPieces.Add(null);

        if (selecttopright.x > objtopleft.x && selecttopright.x < objtopright.x)
        {
            //Right of selection is greater than left of object
            verticalPieces[0] = selectableObject.SplitOnPlane(selectbotright, selecttopright - selectbotright);
        }

        if (selecttopleft.x < objtopright.x && selecttopleft.x > objtopleft.x)
        {
            //Left of selection is less than right of object
            verticalPieces[1] = selectableObject.SplitOnPlane(selecttopleft, selectbotleft - selecttopleft);

        }

        if (selectbotleft.y < objtopleft.y && selectbotleft.y > objbotleft.y)
        {
            //Bottom of selection is less than top of Object
            horizontalPieces[0] = selectableObject.SplitOnPlane(selectbotleft, selectbotright - selectbotleft);
        }

        if (selecttopright.y > objbotleft.y && selecttopright.y < objtopleft.y)
        {
            //Top of selection is greater than bottom of Object
            horizontalPieces[1] = selectableObject.SplitOnPlane(selecttopright, selecttopleft - selecttopright);
        }


        //Index 0 of the pieces list is the "original" object. Index 1 is the created "Clone"
        if ((horizontalPieces[0] != null || horizontalPieces[1] != null) && (verticalPieces[0] != null || verticalPieces[1] != null))
        {
            GameObject mergedParent = null;
            for (int i = 0; i < 2; i++)
            {
                if (horizontalPieces[i] != null)
                {
                    if (mergedParent == null) mergedParent = horizontalPieces[i][1];
                    else
                    {
                        transferChildren(horizontalPieces[i][1].transform, mergedParent.transform);
                        Destroy(horizontalPieces[i][1]);
                    }
                }
                if (verticalPieces[i] != null)
                {
                    if (mergedParent == null) mergedParent = verticalPieces[i][1];
                    else
                    {
                        transferChildren(verticalPieces[i][1].transform, mergedParent.transform);
                        Destroy(verticalPieces[i][1]);
                    }
                }
            }
        }
    }


    void OnGUI()
    {
        if (isSelecting)
        {
            // Create a rect from both mouse positions
            rect = Selectionbox.GetScreenRect(mousePosition1, Input.mousePosition);
            Selectionbox.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
            Selectionbox.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));
        }
    }
}
