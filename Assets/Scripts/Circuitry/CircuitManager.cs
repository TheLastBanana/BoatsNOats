using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public Camera circuitCamera;
    public GameObject rightBound;

    static List<List<Circuit>> groups;
    int circuitLayer = -1;

    void Awake()
    {
        circuitLayer = LayerMask.NameToLayer("Circuitry");
    }

    void Start()
    {
        RecalculateGroups();
    }
    
    // Recalculate the power for a given group
    static public void RecalculatePower(int groupId)
    {
        if (groupId == 0)
            return;

        var group = groups[groupId - 1];
        bool powered = false;

        foreach (var circuit in group)
        {
            var powerSource = circuit.GetComponent<PowerSource>();
            if (powerSource == null || !powerSource.IsOn) continue;

            // If this is providing power, we're done
            powered = true;
            break;
        }

        // Now tell the circuits whether they're powered
        for (int i = 0; i < group.Count; ++i)
            group[i].powered = powered;
    }

    // Update circuit objects' connections
    public void RecalculateGroups()
    {
        int curId = 1;
        groups = new List<List<Circuit>>();

        // We run this twice: once for "left" (real world) and once for "right" (alt world)
        for (int run = 0; run < 2; ++run)
        {
            bool side = run == 0 ? true : false;
            UpdateCamera(side);

            // Render to the texture
            var rt = circuitCamera.targetTexture;
            circuitCamera.Render();

            var w = rt.width;
            var h = rt.height;

            // Set it as the active texture
            var oldRT = RenderTexture.active;
            RenderTexture.active = rt;

            // Copy its pixels
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);

            // Reset active RenderTexture
            RenderTexture.active = oldRT;

            // Convert color pixel data into an int array corresponding to group IDs
            var pixels = tex.GetPixels();
            var groupIDs = new int[w * h];
            for (int x = 0; x < w; ++x)
            {
                for (int y = 0; y < h; ++y)
                {
                    var index = y * w + x;

                    // The threshold shader has already been applied, so if r is 0, this is black.
                    // Otherise, it's an area we'll want to flood fill later.
                    groupIDs[index] = pixels[index].r == 0 ? 0 : -1;
                }
            }

            // Now we want to group areas of the circuit. To do that, we flood fill white areas with a color,
            // increasing the R value by 1 each time we do a flood fill. The result is that each group of white
            // pixels (i.e. a connected circuit) has an ID (its R value). Flood fills start at circuit positions,
            // because these are the only groups that can be powered.
            int numGroups = 0;
            foreach (var circuit in FindObjectsOfType<Circuit>())
            {
                // Make sure the object is on the correct side
                if ((circuit.transform.position.x < rightBound.transform.position.x) != side) continue;

                var texPos = GetCircuitPixelPosition(circuit.gameObject);
                if (texPos == null) continue;

                var x = (int)texPos.Value.x;
                var y = (int)texPos.Value.y;

                // If it's -1, it's an unfilled circuit area
                if (groupIDs[y * w + x] == -1)
                {
                    FloodFillIntMap(groupIDs, w, h, x, y, curId);
                    ++curId;
                    ++numGroups;
                }
            }
            
            if (numGroups > 0)
            {
                for (int i = 0; i < numGroups; ++i)
                {
                    groups.Add(new List<Circuit>());
                }

                // Assign circuit objects to circuit groups
                var circuits = FindObjectsOfType<Circuit>();
                foreach (var circuit in circuits)
                {
                    // Make sure the object is on the correct side
                    if ((circuit.transform.position.x < rightBound.transform.position.x) != side) continue;

                    int groupId = 0;
                    var texPos = GetCircuitPixelPosition(circuit.gameObject);

                    // If no valid position, this circuit's group should be left as 0
                    if (texPos != null)
                    {
                        // Find the R value at this pixel and convert it to an integer to get the circuit ID
                        groupId = groupIDs[(int)texPos.Value.y * w + (int)texPos.Value.x];

                        // Add to the group; otherwise, group will be 0 and the circuit can't be powered
                        if (groupId != 0)
                            groups[groupId - 1].Add(circuit);
                    }

                    // No power to ungrouped (i.e. broken) circuit pieces
                    if (groupId == 0) circuit.powered = false;

                    var powerSource = circuit.GetComponent<PowerSource>();
                    if (powerSource != null) powerSource.groupId = groupId;
                }
            }
        }
        
        for (int i = 0; i < groups.Count; ++i)
            RecalculatePower(i + 1);
    }

    // Get a circuit's pixel position in the rendered grid, or null if no position
    Vector2? GetCircuitPixelPosition(GameObject circuit)
    {
        Bounds bounds;

        // If it's splittable, we already have total bounds calculated
        var splittable = circuit.GetComponent<Splittable>();
        if (splittable == null)
        {
            bounds = splittable.TotalBounds;
        }
        else
        {
            // Otherwise, fall back to this (kind of crappy) way of getting a point on the circuit

            // Get one of the circuit graphics' center points to check
            Transform checkChild = null;
            for (int i = 0; i < circuit.transform.childCount; ++i)
            {
                var child = circuit.transform.GetChild(i);
                if (child.gameObject.layer != circuitLayer) continue;

                // This child is on the circuit layer, so it should have been rendered in the circuit camera
                checkChild = child;
                break;
            }

            if (checkChild == null)
                return null;

            bounds = checkChild.GetComponent<Renderer>().bounds;
        }

        var center = bounds.center;
        var texPos = circuitCamera.WorldToScreenPoint(center);

        return texPos;
    }

    // Change camera settings to include all circuitry on a given side
    void UpdateCamera(bool left)
    {
        // Get bounds of all GameObjects in circuit layer
        var objects = FindObjectsOfType<GameObject>();
        var bounds = new Bounds();
        var boundsSet = false;

        foreach (GameObject obj in objects)
        {
            // Make sure the object is on the correct side
            if ((obj.transform.position.x < rightBound.transform.position.x) != left) continue;

            if (obj.layer == circuitLayer)
            {
                Bounds objBounds = obj.GetComponent<Renderer>().bounds;

                // Take the bounds of the first object so we don't end up encapsulating (0, 0)
                if (boundsSet)
                {
                    bounds.Encapsulate(objBounds);
                }
                else
                {
                    bounds = objBounds;
                    boundsSet = true;
                }
            }
        }

        // Move to center of bounds
        Vector3 newPos = bounds.center;
        newPos.z = -10;
        circuitCamera.transform.position = newPos;

        // Resize camera to see all objects
        // Sizes accommodating the horizontal and vertical bounds
        float hSize = circuitCamera.orthographicSize = bounds.extents.x / circuitCamera.aspect;
        float vSize = bounds.extents.y;

        circuitCamera.orthographicSize = Mathf.Max(hSize, vSize);
    }


    // Flood fill stuff adapted from http://wiki.unity3d.com/index.php/TextureFloodFill
    struct Point
    {
        public short x;
        public short y;
        public Point(short aX, short aY) { x = aX; y = aY; }
        public Point(int aX, int aY) : this((short)aX, (short)aY) { }
    }

    static void FloodFillIntMap(int[] array, int w, int h, int aX, int aY, int aFillNum)
    {
        int refNum = array[aX + aY * w];
        Queue<Point> nodes = new Queue<Point>();
        nodes.Enqueue(new Point(aX, aY));
        while (nodes.Count > 0)
        {
            Point current = nodes.Dequeue();
            for (int i = current.x; i < w; i++)
            {
                int N = array[i + current.y * w];
                if (N != refNum || N == aFillNum)
                    break;
                array[i + current.y * w] = aFillNum;
                if (current.y + 1 < h)
                {
                    N = array[i + current.y * w + w];
                    if (N == refNum && N != aFillNum)
                        nodes.Enqueue(new Point(i, current.y + 1));
                }
                if (current.y - 1 >= 0)
                {
                    N = array[i + current.y * w - w];
                    if (N == refNum && N != aFillNum)
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }
            for (int i = current.x - 1; i >= 0; i--)
            {
                int N = array[i + current.y * w];
                if (N != refNum || N == aFillNum)
                    break;
                array[i + current.y * w] = aFillNum;
                if (current.y + 1 < h)
                {
                    N = array[i + current.y * w + w];
                    if (N == refNum && N != aFillNum)
                        nodes.Enqueue(new Point(i, current.y + 1));
                }
                if (current.y - 1 >= 0)
                {
                    N = array[i + current.y * w - w];
                    if (N == refNum && N != aFillNum)
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }
        }
    }
}
