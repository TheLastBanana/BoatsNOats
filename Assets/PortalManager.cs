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

    // World info
    public Camera mainCam;
    public WorldOffsets offs;

    // Current portal selection info
    bool isSelecting = false;
    bool isOpen = false;
    Vector3 portPos1; // These are in world coords
    Vector3 portPos2;

    AudioEffects afx;

    // Use this for initialization
    void Start ()
    {
        afx = GetComponent<AudioEffects>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (isOpen && Input.GetMouseButtonDown(1))
        {
            // Kill the portal
            isOpen = false;
            // TODO close Mike's portal

            // Transfer objects in the portal home
            bool anyCuts = portalTransfer(portPos1, portPos2);

            // Play cut noises if necessary
            if (anyCuts)
                objectCutSound.Play();
        }

        // If we press the right mouse button, save mouse location and portal creation
        if (!isOpen && Input.GetMouseButtonDown(0))
        {
            isSelecting = true;
            portPos1 = mainCam.ScreenToWorldPoint(Input.mousePosition);

            timeStopSound.Play();
            portalDragSound.Play();
            altWorldAmbience.Play();
            afx.cancelEffects(portalDragSound);
            afx.cancelEffects(altWorldAmbience);

            // Disable physics
            foreach (var physicsObject in FindObjectsOfType<Rigidbody2D>())
                physicsObject.simulated = false;
        }

        // Here we update the secondary position of the portal while we're 
        // selecting. We need to do this before checking if mouse button up so
        // we can update the final mouse position
        if (isSelecting)
            portPos2 = mainCam.ScreenToWorldPoint(Input.mousePosition);

        // If we let go of the right mouse button, end selection
        if (!isOpen && Input.GetMouseButtonUp(0))
        {
            // We're no longer selecting the portal
            isSelecting = false;
            isOpen = true; // But it IS open

            // Find max and min of vectors to make bounding box
            var min = Vector3.Min(portPos1, portPos2);
            var max = Vector3.Max(portPos1, portPos2);
            min.z = 0;
            max.z = 0;

            // Change portpos 1 and 2 to be these new vectors so that more
            // math isn't needed later. Might as well do it right when we
            // know it's relevant
            portPos1 = min; // top left corner
            portPos2 = max; // bottom right corner
            
            // Do the portal transfer
            bool anyCuts = portalTransfer(portPos1, portPos2);

            // TODO open mike's portal object business

            // Re-enable physics now that we're no longer building the portal
            foreach (var physicsObject in FindObjectsOfType<Rigidbody2D>())
                physicsObject.simulated = true;

            afx.smoothStop(portalDragSound);
            afx.smoothStop(altWorldAmbience);
            timeStartSound.Play();
            if (anyCuts)
                objectCutSound.Play();
        }

        if (isSelecting)
        {
            // Get size of selection box
            float selectionSize = (portPos2 - portPos1).magnitude;
            float sizeFactor = selectionSize / portalSoundMaxSize;

            // Update pitch of drag sound
            portalDragSound.pitch = Mathf.Lerp(dragPitchLow, dragPitchHigh, sizeFactor);

            // Update low-pass filter on alternate world ambience
            altWorldAmbienceLPF.enabled = true;
            altWorldAmbienceLPF.cutoffFrequency = Mathf.Lerp(dragLpfLow, dragLpfHigh, sizeFactor);
        }
    }

    // Transfers between portals in the main and alternate world
    // Expects two vectors, one is top left, the other bottom right that
    // represent the portal corners.
    // Returns true if any objects were sent
    bool portalTransfer(Vector3 min, Vector3 max)
    {
        // Make bounds to find objects to split in main world
        var mainBounds = new Bounds();
        mainBounds.SetMinMax(min, max);

        // Make bounds for alternate world to split things and bring them back
        var altBounds = mainBounds;
        altBounds.center += offs.offset;

        // Cut objects in portal bounds
        // This is a potential performans hit because we iterate through
        // every splittable twice. Will it likely matter? No.
        List<GameObject> mainCuts = cutInBounds(mainBounds);
        List<GameObject> altCuts = cutInBounds(altBounds);

        // Send and receive objects
        moveBetweenWorlds(mainCuts, true); // True means send
        moveBetweenWorlds(altCuts, false);

        // Return if we cut any objects
        return mainCuts.Count > 0 || altCuts.Count > 0;
    }

    // Cuts all splittables inside the bounds provided
    // Returns a list of the objects that are inside the bounds post-split
    List<GameObject> cutInBounds(Bounds bounds)
    {
        // List of cut objects inside portal
        List<GameObject> cuts = new List<GameObject>();

        // Loop over splittables
        foreach (var selectableObject in FindObjectsOfType<Splittable>())
        {
            //If the object and the selection box bounds touch figure out where they do for cutting purposes
            if (bounds.Intersects(selectableObject.totalBounds))
            {
                // Intersection means cut
                cutObject(selectableObject, bounds);

                // The original object is now the object inside the portal
                cuts.Add(selectableObject.gameObject);
            }
        }

        return cuts;
    }

    // Transfers objects between worlds.
    // Send == true means main -> alt
    // Send == false means alt -> main
    void moveBetweenWorlds(List<GameObject> objs, bool send)
    {
        Debug.Log("Got objs " + objs.Count + " send " + send);
        // Send means positive offset, not send means receive (negative offset)
        Vector3 offset = send ? offs.offset : -offs.offset;
        foreach (GameObject obj in objs)
            obj.transform.position += offset;
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

        // TODO Note that this is debug code, this will eventually be replaced with Mike's object
        if (isSelecting || isOpen)
        {
            // Create a rect from both mouse positions
            Vector3 topLeft = mainCam.WorldToScreenPoint(portPos1);
            Vector3 bottomRight = mainCam.WorldToScreenPoint(portPos2);
            Rect rect = Selectionbox.GetScreenRect(topLeft, bottomRight);
            Selectionbox.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
            Selectionbox.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));
        }
    }
}
