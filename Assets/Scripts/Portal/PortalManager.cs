using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalManager : MonoBehaviour
{
    // Sound stuff
    public AudioSource timeStopSound;
    public AudioSource altWorldAmbience;
    public AudioSource portalDragSound;
    public AudioSource timeStartSound;
    public AudioSource objectCutSound;
    public AudioLowPassFilter altWorldAmbienceLPF;
    public MusicManager musicManager;
    public float portalMusicVolume = 0.3f;
    public float portalSoundMaxSize = 20.0f;
    public float dragLpfLow = 200.0f;
    public float dragLpfHigh = 3000.0f;
    public float dragPitchLow = 1.0f;
    public float dragPitchHigh = 2.0f;

    // World info
    public Camera mainCam;
    public Camera portalCam;
    public WorldOffsets offs;
    public CircuitManager circuitManager;
    public CutsceneManager cutsceneManager;

    // Effects
    public GameObject portalParticlePrefab;
    public GameObject portalFlashPrefab;

    // Current portal selection info
    public float minimumPortalSize = 0.1f;
    bool isSelecting = false;
    bool isTransferring = false;
    Vector3 portPos1; // These are in world coords
    Vector3 portPos2;
    Rect portalRect = new Rect();

    // Portal juicy movement variables
    public float portalAcceleration = 0.05f;
    public float portalDamping = 0.82f;
    Vector2 portalSpeed;
    Vector2 portalSizeSpeed;
    Rect movingPortalRect;
    
    // Cut coroutine time info
    public float maxCutTime = 0.01f;
    float cutStartTime = 0.0f;

    AudioEffects afx;
    PortalEffect portalEffect;
    private bool disabled;

    // Use this for initialization
    void Start ()
    {
        disabled = false;
        afx = GetComponent<AudioEffects>();
        portalEffect = Instantiate(portalParticlePrefab).GetComponent<PortalEffect>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        // If we press the left mouse button, save mouse location and portal creation
        if (!isTransferring && Input.GetMouseButtonDown(0) && !disabled)
        {
            portalCam.enabled = true;
            portalEffect.Enable();
            portalEffect.particleIntensity = 1f;

            isSelecting = true;
            portPos1 = mainCam.ScreenToWorldPoint(Input.mousePosition);
            movingPortalRect = new Rect(portPos1, new Vector2());

            timeStopSound.Play();
            portalDragSound.Play();
            altWorldAmbience.Play();
            afx.cancelEffects(portalDragSound);
            afx.cancelEffects(altWorldAmbience);

            cutsceneManager.DisableGemma(true);

            // Disable physics
            foreach (var physicsObject in FindObjectsOfType<Rigidbody2D>())
                physicsObject.simulated = false;
        }

        // Here we update the secondary position of the portal while we're 
        // selecting. We need to do this before checking if mouse button up so
        // we can update the final mouse position
        if (isSelecting)
        {
            Vector2 clampedMousePos = new Vector2(
                Mathf.Clamp(Input.mousePosition.x, 0, mainCam.pixelWidth),
                Mathf.Clamp(Input.mousePosition.y, 0, 2 * mainCam.pixelHeight)
            );
            portPos2 = mainCam.ScreenToWorldPoint(clampedMousePos);

            // The further the current portal rectangle is from the target, the
            // faster it accelerates towards it
            var oldCenter = movingPortalRect.center;
            var targetCenter = (Vector2) (portPos1 + portPos2) / 2f;
            var centerDir = targetCenter - oldCenter;
            portalSpeed += centerDir.magnitude * portalAcceleration * centerDir.normalized;
            
            var oldSize = movingPortalRect.size;
            var targetSize = (Vector2)(portPos1 - portPos2);
            var sizeDir = targetSize - oldSize;
            portalSizeSpeed += sizeDir.magnitude * portalAcceleration * sizeDir.normalized;

            // Apply damping
            portalSpeed *= portalDamping;
            portalSizeSpeed *= portalDamping;

            // Move the portal rectangle
            movingPortalRect.center = movingPortalRect.center + portalSpeed;
            movingPortalRect.size = movingPortalRect.size + portalSizeSpeed;

            musicManager.volume = portalMusicVolume;
        }

        // Make sure portal isn't too small
        portalRect.size = new Vector2(
            Mathf.Max(Mathf.Abs(movingPortalRect.size.x), minimumPortalSize),
            Mathf.Max(Mathf.Abs(movingPortalRect.size.y), minimumPortalSize)
        );
        portalRect.center = movingPortalRect.center; // Need to re-set this in case size was changed
        portalEffect.portalShape = portalRect;
        
        // If we let go of the left mouse button, end selection
        if ((Input.GetMouseButtonUp(0) && !disabled) || (isSelecting && disabled))
        {
            // Stop portal from moving
            portalSpeed = new Vector2();
            portalSizeSpeed = new Vector2();

            // Disable portal effect
            portalEffect.portalShape = new Rect();
            portalEffect.Disable();

            // We're no longer selecting the portal
            isSelecting = false;

            // Create the portal flash effect
            var flash = Instantiate(portalFlashPrefab);
            flash.transform.position = portalRect.center;
            flash.GetComponent<PortalTransferEffect>().startScale = portalRect.size;

            // Do the portal transfer
            StartCoroutine(portalTransfer(portalRect.min, portalRect.max, true));
        }

        if (isSelecting)
        {
            // Get size of selection box
            float selectionSize = portalRect.size.magnitude;
            float sizeFactor = selectionSize / portalSoundMaxSize;

            // Update pitch of drag sound
            portalDragSound.pitch = Mathf.Lerp(dragPitchLow, dragPitchHigh, sizeFactor);

            // Update low-pass filter on alternate world ambience
            altWorldAmbienceLPF.enabled = true;
            altWorldAmbienceLPF.cutoffFrequency = Mathf.Lerp(dragLpfLow, dragLpfHigh, sizeFactor);
        }
    }

    // Unfreeze time after portal is opened
    void unfreeze()
    {
        portalEffect.particleIntensity = 0.2f;
        portalCam.enabled = false;
        portalCam.rect = new Rect();
        
        cutsceneManager.DisableGemma(false);

        // Re-enable physics now that we're no longer building the portal
        foreach (var physicsObject in FindObjectsOfType<Rigidbody2D>())
            physicsObject.simulated = true;

        afx.smoothStop(portalDragSound);
        afx.smoothStop(altWorldAmbience);
        timeStartSound.Play();

        musicManager.volume = 1.0f;
    }

    // Transfers between portals in the main and alternate world
    // Expects two vectors, one is top left, the other bottom right that
    // represent the portal corners.
    IEnumerator portalTransfer(Vector3 min, Vector3 max, bool unfreezeAfter)
    {
        isTransferring = true;

        cutStartTime = Time.realtimeSinceStartup;

        // Make bounds to find objects to split in main world
        var mainBounds = new Bounds();
        mainBounds.SetMinMax(min, max);

        // Make bounds for alternate world to split things and bring them back
        var altBounds = mainBounds;
        altBounds.center += offs.offset;

        // Cut objects in portal bounds
        // This is a potential performans hit because we iterate through
        // every splittable twice. Will it likely matter? No.
        var mainCuts = new List<GameObject>();
        var altCuts = new List<GameObject>();

        // See https://forum.unity3d.com/threads/call-nested-coroutines-without-yielding.145570/#post-996475
        var e = cutInBounds(mainBounds, mainCuts);
        while (e.MoveNext())
            yield return e.Current;

        e = cutInBounds(altBounds, altCuts);
        while (e.MoveNext())
            yield return e.Current;

        // Send and receive objects
        moveBetweenWorlds(mainCuts, true); // True means send
        moveBetweenWorlds(altCuts, false);

        circuitManager.RecalculateGroups();

        // If we cut any objects, play the cut sound
        if (mainCuts.Count > 0 || altCuts.Count > 0)
            objectCutSound.Play();

        if (unfreezeAfter) unfreeze();

        isTransferring = false;
    }

    // Cuts all splittables inside the bounds provided
    // Returns a list of the objects that are inside the bounds post-split in cuts
    IEnumerator cutInBounds(Bounds bounds, List<GameObject> cuts)
    {
        Bounds expandedBounds = bounds;
        expandedBounds.Expand(0.01f); // Add some wiggle room for sending things between worlds

        // Loop over splittables
        foreach (var selectableObject in FindObjectsOfType<Splittable>())
        {
            // Need to check this in case the objects were deleted
            if (selectableObject == null) continue;

            //If the object and the selection box bounds touch figure out where they do for cutting purposes
            if (bounds.Intersects(selectableObject.totalBounds))
            {
                // Intersection means cut
                // See https://forum.unity3d.com/threads/call-nested-coroutines-without-yielding.145570/#post-996475
                var e = cutObject(selectableObject, bounds);
                while (e.MoveNext())
                    yield return e.Current;

                if (selectableObject == null) continue;

                // The original object was cut, or the original object is fully contained in the portal
                if (expandedBounds.Contains(selectableObject.totalBounds.min) && expandedBounds.Contains(selectableObject.totalBounds.max))
                {
                    cuts.Add(selectableObject.gameObject);
                }
            }

            // Cut off iteration if we've exceeded the max time
            if (Time.realtimeSinceStartup - cutStartTime > maxCutTime)
            {
                yield return null;
                cutStartTime = Time.realtimeSinceStartup;
            }
        }
    }

    // Transfers objects between worlds.
    // Send == true means main -> alt
    // Send == false means alt -> main
    void moveBetweenWorlds(List<GameObject> objs, bool send)
    {
        // Send means positive offset, not send means receive (negative offset)
        Vector3 offset = send ? offs.offset : -offs.offset;
        foreach (GameObject obj in objs)
            obj.transform.position += offset;
    }

    // Move a Transform's children to another Transform
    static void transferChildren(Transform oldParent, Transform newParent)
    {
        List<Transform> children = new List<Transform>();

        for (int i = 0; i < oldParent.childCount; ++i)
        {
            children.Add(oldParent.GetChild(i));
        }

        // Do this in a separate loop so we don't modify the loop counter as we change parents
        foreach (Transform child in children)
        {
            child.SetParent(newParent);
        }
    }
    static void checkIfRobot(Splittable selectableObject)
    {
        if (selectableObject.GetComponent<RobotAI>()!=null)
        {
            selectableObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            Destroy(selectableObject.GetComponent<RobotAI>());
        }
        if (selectableObject.transform.parent)
        {
            if (selectableObject.transform.parent.GetComponent<Piston>() != null)
            {
                Destroy(selectableObject.transform.parent.GetComponent<Piston>());
            }
        }

    }
    //Figure out the 4 corners of the bounds for both the selection box and the object and do AABB to figure out where they overlap
    IEnumerator cutObject(Splittable selectableObject, Bounds selectbounds)
    {

        //Check which edge intersects the object or if the selection box is all around the object
        Vector2 selectbotleft = new Vector2(selectbounds.center.x - selectbounds.extents.x, selectbounds.center.y - selectbounds.extents.y);
        Vector2 selecttopleft = new Vector2(selectbounds.center.x - selectbounds.extents.x, selectbounds.center.y + selectbounds.extents.y);
        Vector2 selectbotright = new Vector2(selectbounds.center.x + selectbounds.extents.x, selectbounds.center.y - selectbounds.extents.y);
        Vector2 selecttopright = new Vector2(selectbounds.center.x + selectbounds.extents.x, selectbounds.center.y + selectbounds.extents.y);

        Bounds objbounds = selectableObject.totalBounds;
        Vector2 objbotleft = new Vector2(objbounds.center.x - objbounds.extents.x, objbounds.center.y - objbounds.extents.y);
        Vector2 objtopleft = new Vector2(objbounds.center.x - objbounds.extents.x, objbounds.center.y + objbounds.extents.y);
        //Bottom right of object
        //Vector2 objbotright = new Vector2(objbounds.center.x + objbounds.extents.x, objbounds.center.y - objbounds.extents.y);
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
            checkIfRobot(selectableObject);
            verticalPieces[0] = selectableObject.SplitOnPlane(selectbotright, selecttopright - selectbotright);
        }

        // Cut off iteration if we've exceeded the max time
        if (Time.realtimeSinceStartup - cutStartTime > maxCutTime)
        {
            yield return null;
            cutStartTime = Time.realtimeSinceStartup;
        }

        if (selecttopleft.x < objtopright.x && selecttopleft.x > objtopleft.x)
        {
            //Left of selection is less than right of object
            checkIfRobot(selectableObject);
            verticalPieces[1] = selectableObject.SplitOnPlane(selecttopleft, selectbotleft - selecttopleft);

        }
        // Cut off iteration if we've exceeded the max time
        if (Time.realtimeSinceStartup - cutStartTime > maxCutTime)
        {
            yield return null;
            cutStartTime = Time.realtimeSinceStartup;
        }

        if (selectbotleft.y < objtopleft.y && selectbotleft.y > objbotleft.y)
        {
            //Bottom of selection is less than top of Object
            checkIfRobot(selectableObject);
            horizontalPieces[0] = selectableObject.SplitOnPlane(selectbotleft, selectbotright - selectbotleft);
        }

        // Cut off iteration if we've exceeded the max time
        if (Time.realtimeSinceStartup - cutStartTime > maxCutTime)
        {
            yield return null;
            cutStartTime = Time.realtimeSinceStartup;
        }

        if (selecttopright.y > objbotleft.y && selecttopright.y < objtopleft.y)
        {
            //Top of selection is greater than bottom of Object
            checkIfRobot(selectableObject);
            horizontalPieces[1] = selectableObject.SplitOnPlane(selecttopright, selecttopleft - selecttopright);
        }

        // Cut off iteration if we've exceeded the max time
        if (Time.realtimeSinceStartup - cutStartTime > maxCutTime)
        {
            yield return null;
            cutStartTime = Time.realtimeSinceStartup;
        }

        // Merge parents if multiple cuts at different angles happened
        // Index 0 of the pieces list is the "original" object. Index 1 is the created "Clone"
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
                        // Merge into one parent and delete the other parent
                        GameObject sacrifice = horizontalPieces[i][1];
                        transferChildren(sacrifice.transform, mergedParent.transform);
                        Destroy(sacrifice);
                    }
                }
                if (verticalPieces[i] != null)
                {
                    if (mergedParent == null) mergedParent = verticalPieces[i][1];
                    else
                    {
                        GameObject sacrifice = verticalPieces[i][1];
                        transferChildren(sacrifice.transform, mergedParent.transform);
                        Destroy(sacrifice);
                    }
                }
            }
        }
    }

    void OnRenderObject()
    {
        if (isSelecting || isTransferring)
        {
            // Get camera resolution
            Vector2 camRes = new Vector2(mainCam.pixelWidth, mainCam.pixelHeight);

            var portRectPos1 = new Vector3(portalRect.xMin, portalRect.yMin);
            var portRectPos2 = new Vector3(portalRect.xMax, portalRect.yMax);

            // Get the screen positions of the two points
            Vector2 screenPos1 = mainCam.WorldToScreenPoint(portRectPos1);
            Vector2 screenPos2 = mainCam.WorldToScreenPoint(portRectPos2);

            // Convert to min and max points
            Vector2 minPos = Vector2.Min(screenPos1, screenPos2);
            Vector2 maxPos = Vector2.Max(screenPos1, screenPos2);

            minPos.x /= camRes.x;
            minPos.y /= camRes.y;
            maxPos.x /= camRes.x;
            maxPos.y /= camRes.y;

            Rect newRect = new Rect(minPos, maxPos - minPos);

            if (newRect.xMax < 0 || newRect.xMin > 1 || newRect.yMax < 0 || newRect.yMin > 1
                || newRect.width == 0 || newRect.height == 0)
            {
                return;
            }

            // Change portal size
            portalCam.orthographicSize = Mathf.Abs(portalRect.height) / 2;
            portalCam.rect = newRect;

            // Change portal position
            portalCam.transform.position =
                new Vector3(portalRect.center.x, portalRect.center.y, - 10) + offs.offset;

        }
    }

    // Stop player from activating portals, used during cutscene and before Gemma gets artifact
    public void DisablePortal(bool disable)
    {
        disabled = disable;
    }
}
