using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piston : MonoBehaviour
{

    public GameObject head;
    public GameObject[] rods;
    public GameObject bottom;
    public float maxDisplacement;
    public bool startExtended;
    public bool debugRaycasts;

    public AudioSource startSound;
    public AudioSource loopSound;
    public AudioSource endSound;

    //private Vector3 botSize;
    private Vector3 rodSize;
    private Vector3 headSize;
    private float offset;
    private float curDisp = 0;
    private float speed;
    private bool movingUp;
    private bool wasMoving = false;
    private bool muted = true;

    private List<Splittable> splittables;

    // Collisions info
    // Maximum distance between raycasts, there can be less but no more
    private const float maxDistBetweenCasts = 0.5f;
    private const float planeEpsilon = 0.0001f;
    private const float raycastLength = 0.1f;
    private int collLayerMask;
    private List<Vector2> raycastOrigins = new List<Vector2>();

    private void Awake()
    {
        // Get sprite sizes
        //botSize = bottom.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size;
        rodSize = rods[0].transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size;
        headSize = head.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size;
        
        // Speed is arbitrarily a fifth of the head size I guess
        speed = headSize.y / 5;

        // Get the initial offset of the head to scale the rods to
        offset = head.transform.localPosition.y - (headSize.y / 2) - rods[0].transform.localPosition.y;

        // Get all splittables associated with this
        splittables = new List<Splittable>();

        var splittable = GetComponent<Splittable>();
        if (splittable != null) splittables.Add(splittable);

        splittables.AddRange(transform.GetComponentsInChildren<Splittable>());

        // Don't allow sounds for a moment so we don't spam them on spawn
        StartCoroutine(Unmute());

        // We want a maximum distance between, so calculate how many we need
        int castCount = Mathf.CeilToInt(headSize.x / maxDistBetweenCasts);
        float distBetween = headSize.x / castCount;

        // We want the raycasts to originate from just above the plane of the
        // piston, calculate and then add a small epsilon. We add the head's
        // yPos later to account for the head moving
        float yOrigin = headSize.y / 2 + planeEpsilon;

        // We want the list of ray casts to begin from the left side and then
        // end on the right
        float xOrigin = head.transform.localPosition.x - headSize.x / 2;

        // Create raycasts for Gemma detection
        // Do one extra for the right edge
        for (int i = 0; i < castCount + 1; ++i)
            raycastOrigins.Add(new Vector2(xOrigin + i * distBetween, yOrigin));

        // Get collision mask for character
        collLayerMask = LayerMask.GetMask("Character");
        Debug.Assert(collLayerMask != 0,  "Character layer mask wasn't valid: " + collLayerMask);

        // Should we start extended
        if (startExtended)
        {
            // Say we're at maxDisp
            curDisp = maxDisplacement;

            // Move head
            Vector3 headPos = head.transform.localPosition;
            headPos.y += maxDisplacement;
            head.transform.localPosition = headPos;

            // Rescale
            foreach (GameObject rod in rods)
            {
                Vector3 rodScale = rod.transform.localScale;
                rodScale.y = (curDisp + offset) / rodSize.y;
                rod.transform.localScale = rodScale;
            }
        }
    }

    // FixedUpdate is called once per physics frame
    void FixedUpdate ()
    {
        // Break when this is split
        foreach (var splittable in splittables)
        {
            if (splittable.IsSplit)
            {
                Destroy(this);
                return;
            }
        }

        float posDelta = 0;

        bool powered = bottom.GetComponent<Circuit>().powered;
        
        // If it's powered we move up
        if (powered)
        {
            float distToMove = maxDisplacement - curDisp;

            // No more distance to move, not moving up
            if (distToMove == 0)
                movingUp = false;
            // Still got at least one more tick before we hit max
            else if (distToMove >= speed)
            {
                movingUp = true;
                posDelta = speed;
            }
            // We're within speed distance of max, so move that much
            else if (maxDisplacement - curDisp < speed)
            {
                movingUp = true;
                posDelta = distToMove;
            }
        }
        // If it's not powered we move down
        else if (!powered)
        {
            // Unpowered, we're definitely not moving up
            movingUp = false;
            
            // Still got at least one more tick before we hit min
            if (curDisp >= speed)
                posDelta = -speed;

            // We're within speed distance of min, so move that much
            else if (curDisp < speed && curDisp > 0)
                posDelta = -curDisp;
        }

        if (posDelta != 0)
        {
            // Track changes to curDisp
            curDisp += posDelta;

            Vector3 headPos = head.transform.localPosition;
            headPos.y += posDelta;
            head.transform.localPosition = headPos;

            foreach (GameObject rod in rods)
            {
                Vector3 rodScale = rod.transform.localScale;
                rodScale.y = (curDisp + offset )/ rodSize.y;
                rod.transform.localScale = rodScale;
            }

            // Make movement sounds
            if (!muted && !wasMoving)
            {
                startSound.Play();
                loopSound.Play();
            }

            wasMoving = true;
        }
        else
        {
            // Stop movement sounds
            if (!muted && wasMoving)
            {
                endSound.Play();
                loopSound.Stop();
            }

            wasMoving = false;
        }

        // Have we already moved Gemma this frame?
        bool moved = false;
        
        // Find out if we should move Gemma
        foreach (Vector2 orig in raycastOrigins)
        {
            // Transform the local origin to world origin. Add the head's
            // current local y position.
            Vector3 origin = new Vector3(orig.x, orig.y + head.transform.localPosition.y);
            origin = transform.TransformPoint(origin);

            // The length of the raycast, only goes in y direction, since we're
            // working in local coords
            Vector3 length = new Vector3(0, raycastLength);

            // Rotate length to make end point
            Quaternion rot = transform.rotation;
            length = rot * length;
            Vector3 end = origin + length;

            // Piston raycast debug
            if (debugRaycasts)
                Debug.DrawLine(origin, end, Color.magenta, 0, true);

            // Do actual line casts
            if (!moved && movingUp)
            {
                RaycastHit2D hit = Physics2D.Linecast(origin, end, layerMask: collLayerMask);
                if (hit.transform != null) {
                    // Make a speed vector to add to Gemma's transform
                    Vector3 speedVec = new Vector3(0, speed, 0);
                    Vector3 rotSpeed = rot * speedVec;

                    hit.transform.position += rotSpeed;
                    moved = true;
                }
            }
        }
	}

    public IEnumerator Unmute()
    {
        yield return new WaitForSeconds(0.1f);
        muted = false;
    }
}
