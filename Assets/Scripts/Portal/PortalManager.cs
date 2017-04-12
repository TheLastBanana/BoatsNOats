using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PortalManager : MonoBehaviour
{
    // Sound stuff
    public AudioSource timeStopSound;
    public AudioSource altWorldAmbience;
    public AudioSource portalDragSound;
    public AudioSource timeStartSound;
    public AudioSource objectCutSound;
    public AudioSource blockedPortalSound;
    public AudioLowPassFilter altWorldAmbienceLPF;
    public MusicManager musicManager;
    public float portalMusicVolume = 0.3f;
    public float portalSoundMaxSize = 20.0f;
    public float dragLpfLow = 200.0f;
    public float dragLpfHigh = 3000.0f;
    public float dragPitchLow = 1.0f;
    public float dragPitchHigh = 2.0f;
    public Transform artifact;
    public GameControls gameControls;

    // World info
    public Camera mainCam;
    public Camera altCam;
    public Camera portalCam;
    public Camera blockedPortalCam;
    public WorldOffsets offs;
    public CircuitManager circuitManager;
    public CutsceneManager cutsceneManager;
    public CameraSwitcher cameraSwitcher;

    // Effects
    public GameObject portalParticlePrefab;
    public GameObject portalFlashPrefab;
    public GameObject portalBlockedFlashPrefab;

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
    GameControls controls;

    private bool disabledForLevel;
    private bool disabled;

    private Vector3 lastStart;
    private Vector3 lastEnd;

    // Use this for initialization
    void Awake()
    {
        afx = GetComponent<AudioEffects>();
        portalEffect = Instantiate(portalParticlePrefab).GetComponent<PortalEffect>();

        if (artifact != null)
        {
            portalEffect.artifact = artifact;
        }

        disabledForLevel = false;
        disabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        // For when Gemma doesn't have the artifact
        if (disabledForLevel)
            return;

        // If we press the left mouse button, save mouse location and portal creation
        if (!isTransferring && Input.GetMouseButtonDown(0) && !disabled && !(SceneManager.GetActiveScene().name == "Intro Screen 1.1"))
        {
            initiatePortal();
        }

        // Here we update the secondary position of the portal while we're 
        // selecting. We need to do this before checking if mouse button up so
        // we can update the final mouse position
        if (isSelecting)
        {
            isDragging();
            
        }

        // Make sure portal isn't too small
        portalRect.size = new Vector2(
            Mathf.Max(Mathf.Abs(movingPortalRect.size.x), minimumPortalSize),
            Mathf.Max(Mathf.Abs(movingPortalRect.size.y), minimumPortalSize)
        );
        portalRect.center = movingPortalRect.center; // Need to re-set this in case size was changed

        if (cameraSwitcher.switched) portalRect.center += (Vector2)offs.offset;

        portalEffect.portalShape = portalRect;

        // If we let go of the left mouse button, end selection
        if (isSelecting && Input.GetMouseButtonUp(0) && !disabled)
        {
            endSelection(false);
        }

        // If we press the right mouse button or the portal is disabled, end selection with no flash or cutting
        if (isSelecting && (Input.GetMouseButtonDown(1) || disabled))
        {
            endSelection(true);
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

        if (isSelecting || isTransferring)
        {
            var currentCam = cameraSwitcher.switched ? altCam : mainCam;

            // Get camera resolution
            Vector2 camRes = new Vector2(currentCam.pixelWidth, currentCam.pixelHeight);

            var portRectPos1 = new Vector3(portalRect.xMin, portalRect.yMin);
            var portRectPos2 = new Vector3(portalRect.xMax, portalRect.yMax);

            // Get the screen positions of the two points
            Vector2 screenPos1 = currentCam.WorldToScreenPoint(portRectPos1);
            Vector2 screenPos2 = currentCam.WorldToScreenPoint(portRectPos2);

            // Convert to min and max points
            Vector2 minPos = Vector2.Min(screenPos1, screenPos2);
            Vector2 maxPos = Vector2.Max(screenPos1, screenPos2);

            minPos.x /= camRes.x;
            minPos.y /= camRes.y;
            maxPos.x /= camRes.x;
            maxPos.y /= camRes.y;

            Rect newRect = new Rect(minPos, maxPos - minPos);

            // Out of bounds, so don't try to move the camera here
            if (newRect.xMax < 0 || newRect.xMin > 1 || newRect.yMax < 0 || newRect.yMin > 1
                || newRect.width == 0 || newRect.height == 0)
            {
                return;
            }

            // Change portal size
            portalCam.orthographicSize = Mathf.Abs(portalRect.height) / 2;
            portalCam.rect = newRect;

            blockedPortalCam.orthographicSize = portalCam.orthographicSize;
            blockedPortalCam.rect = portalCam.rect;

            // Change portal position
            portalCam.transform.position = new Vector3(portalRect.center.x, portalRect.center.y, -10);
            blockedPortalCam.transform.position = portalCam.transform.position;

            // Blocked camera should always be in real world; portal camera should be opposite of real camera
            if (!cameraSwitcher.switched)
            {
                portalCam.transform.position += offs.offset;
            }
            else
            {
                portalCam.transform.position -= offs.offset;
                blockedPortalCam.transform.position -= offs.offset;
            }

            portalEffect.blocked = !checkPortalValid();
            portalEffect.drawBeam = !cameraSwitcher.switched;
        }
    }

    private void Undo()
    {
        if(lastEnd != Vector3.zero && lastStart != Vector3.zero)
        {
            initiatePortal(lastStart);
            isDragging(lastEnd);
        }
    }

    public void initiatePortal(Vector3 startPoint = default(Vector3))
    {
        portalCam.enabled = true;
        blockedPortalCam.enabled = true;
        portalEffect.Enable();
        portalEffect.particleIntensity = 1f;
        
        isSelecting = true;
        if (SceneManager.GetActiveScene().name == "Intro Screen 1.1")
        {
            portPos1 = new Vector3(-14.72f, 11.45f, 0);
        }
        else if(startPoint != Vector3.zero)
        {
            portPos1 = startPoint;
        }
        else
        {
            portPos1 = mainCam.ScreenToWorldPoint(Input.mousePosition);
            lastStart = portPos1;
        }
        movingPortalRect = new Rect(portPos1, new Vector2());

        timeStopSound.Play();
        portalDragSound.Play();
        altWorldAmbience.Play();
        afx.cancelEffects(portalDragSound);
        afx.cancelEffects(altWorldAmbience);

        freeze();
    }

    public void isDragging(Vector3 endPoint = default(Vector3))
    {
        if (SceneManager.GetActiveScene().name == "Intro Screen 1.1")
        {
            portPos2 = new Vector3(28.36f, 2.59f, 0);
        }
        else if (endPoint != Vector3.zero)
        {
            portPos2 = endPoint;
        }
        else 
        {
            Vector2 clampedMousePos;
            clampedMousePos = new Vector2(
            Mathf.Clamp(Input.mousePosition.x, 0, mainCam.pixelWidth),
            Mathf.Clamp(Input.mousePosition.y, 0, 2 * mainCam.pixelHeight)
            );
            portPos2 = mainCam.ScreenToWorldPoint(clampedMousePos);
            lastEnd = portPos2;
        }



        // The further the current portal rectangle is from the target, the
        // faster it accelerates towards it
        var oldCenter = movingPortalRect.center;
        var targetCenter = (Vector2)(portPos1 + portPos2) / 2f;
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

    // End portal selection, and do cutting and extra effects if the portal wasn't cancelled
    public void endSelection(bool cancelled)
    {
        // Stop portal from moving
        portalSpeed = new Vector2();
        portalSizeSpeed = new Vector2();

        // Disable portal effect
        portalEffect.portalShape = new Rect();
        portalEffect.Disable();

        // We're no longer selecting the portal
        isSelecting = false;

        if (!cancelled)
        {
            var flashPrefab = portalFlashPrefab;

            if (checkPortalValid())
            {
                // Do the portal transfer
                var transferRect = new Rect(portalRect);
                if (cameraSwitcher.switched) transferRect.center -= (Vector2)offs.offset;
                StartCoroutine(portalTransfer(transferRect.min, transferRect.max, true));
            }
            else
            {
                flashPrefab = portalBlockedFlashPrefab;
                blockedPortalSound.Play();
                unfreeze();
            }

            // Create the portal flash effect
            var flash = Instantiate(flashPrefab);
            flash.transform.position = portalRect.center;
            flash.GetComponent<PortalTransferEffect>().startScale = portalRect.size;
        }

        else
        {
            unfreeze();
        }

    }

    // Check if anything is blocking the portal
    bool checkPortalValid()
    {
        var portalBounds = new Bounds(portalRect.center, portalRect.size);
        if (cameraSwitcher.switched)
        {
            portalBounds.center -= offs.offset;
        }

        bool blocked = false;

        foreach (var obj in FindObjectsOfType<GameObject>())
        {
            // Get any objects in the character layer
            if (obj.layer == LayerMask.NameToLayer("Character"))
            {
                // Only mesh and sprite renderers (e.g. not particles) should block the portal
                Renderer renderer = obj.GetComponent<MeshRenderer>();
                if (!renderer) renderer = obj.GetComponent<SpriteRenderer>();
                if (!renderer) continue;

                // If it's in the portal bounds, the portal is blocked
                if (portalBounds.Intersects(renderer.bounds))
                {
                    blocked = true;
                }
            }
        }

        return !blocked;
    }

    // Freeze time while portal is being dragged
    void freeze()
    {
        cutsceneManager.DisableGemma(true);

        // Disable physics and animation
        foreach (var physicsObject in FindObjectsOfType<Rigidbody2D>())
            physicsObject.simulated = false;

        foreach (var animator in FindObjectsOfType<Animator>())
            animator.enabled = false;

        foreach (var button in FindObjectsOfType<Button>())
            button.GetComponent<PolygonCollider2D>().enabled = false;

        foreach (var piston in FindObjectsOfType<Piston>())
            piston.enabled = false;

        foreach (var robot in FindObjectsOfType<RobotAI>())
            robot.enabled = false;
    }

    // Unfreeze time after portal is opened
    void unfreeze()
    {
        portalEffect.particleIntensity = 0.2f;
        portalCam.enabled = false;
        blockedPortalCam.enabled = false;
        portalCam.rect = new Rect();
        
        cutsceneManager.DisableGemma(false);

        // Re-enable physics and animations now that we're no longer building the portal
        foreach (var physicsObject in FindObjectsOfType<Rigidbody2D>())
            physicsObject.simulated = true;

        foreach (var animator in FindObjectsOfType<Animator>())
            animator.enabled = true;

        foreach (var button in FindObjectsOfType<Button>())
            button.GetComponent<PolygonCollider2D>().enabled = true;

        foreach (var robot in FindObjectsOfType<RobotAI>())
            robot.enabled = true;

        // Enable pistons again
        foreach (var piston in FindObjectsOfType<Piston>())
            piston.enabled = true;


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
                cuts.Add(selectableObject.gameObject);
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
        {
            if (obj == null) continue;

            obj.transform.position += offset;
        }
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

    //Figure out the 4 corners of the bounds for both the selection box and the object and do AABB to figure out where they overlap
    IEnumerator cutObject(Splittable selectableObject, Bounds selectBounds)
    {
        var objBounds = selectableObject.totalBounds;
        var objMin = objBounds.min;
        var objMax = objBounds.max;
        var selectMin = selectBounds.min;
        var selectMax = selectBounds.max;

        // Check which edges intersect the object. Note that for all the lines below, we wind around
        // the portal in counter-clockwise order, so an object in the middle will be on the "left" of
        // every line.
        var cutLines = new List<Vector2[]>();

        // Right side
        if (selectMax.x > objMin.x && selectMax.x < objMax.x)
            cutLines.Add(new Vector2[] { selectMax, Vector2.up });

        // Left side
        if (selectMin.x > objMin.x && selectMin.x < objMax.x)
            cutLines.Add(new Vector2[] { selectMin, Vector2.down });

        // Top side
        if (selectMax.y > objMin.y && selectMax.y < objMax.y)
            cutLines.Add(new Vector2[] { selectMax, Vector2.left });

        // Bottom side
        if (selectMin.y > objMin.y && selectMin.y < objMax.y)
            cutLines.Add(new Vector2[] { selectMin, Vector2.right });

        // Iterate the list of lines, cutting along each of them.
        var outer = new List<GameObject>();
        foreach (var line in cutLines)
        {
            var cuts = selectableObject.SplitOnPlane(line[0], line[1]);

            // Object 1 is the "right" object (if it exists). This will be outside the portal
            if (cuts[1] != null) outer.Add(cuts[1]);

            // Object 0 should always be the original object
            Debug.Assert(cuts[0] == selectableObject.gameObject);

            // Cut off iteration if we've exceeded the max time
            if (Time.realtimeSinceStartup - cutStartTime > maxCutTime)
            {
                yield return null;
                cutStartTime = Time.realtimeSinceStartup;
            }
        }

        // Pieces along the outer edge should be merged into one
        if (outer.Count > 1)
        {
            var mergeTarget = outer[0];

            for (int i = 1; i < outer.Count; ++i)
            {
                var obj = outer[i];
                transferChildren(obj.transform, mergeTarget.transform);
                Destroy(obj);
            }
        }

        if (outer.Count > 0 && outer[0] != null)
            outer[0].SendMessage("OnSplitMergeFinished", null, SendMessageOptions.DontRequireReceiver);

        if (selectableObject != null)
            selectableObject.SendMessage("OnSplitMergeFinished", null, SendMessageOptions.DontRequireReceiver);
    }

    // Stop player from activating portals, used before Gemma gets artifact
    public void DisablePortalForLevel(bool disable)
    {
        disabledForLevel = disable;
        DisablePortal(disable);
    }

    // Stop player from activating portals, used during cutscene
    public void DisablePortal(bool disable)
    {
        disabled = disable;
    }
}
