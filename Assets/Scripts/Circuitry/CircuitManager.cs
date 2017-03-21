using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public Camera circuitCamera;

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
            if (powerSource == null || !powerSource.isOn) continue;

            // If this is providing power, we're done
            powered = true;
            break;
        }

        // Now tell the circuits whether they're powered
        foreach (var circuit in group)
            circuit.powered = powered;
    }

    // Update circuit objects' connections
    public void RecalculateGroups()
    {
        UpdateCamera();

        // Render to the texture
        var rt = circuitCamera.targetTexture;
        circuitCamera.Render();

        // Set it as the active texture
        var oldRT = RenderTexture.active;
        RenderTexture.active = rt;

        // Copy its pixels
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);

        // Reset active RenderTexture
        RenderTexture.active = oldRT;

        // Now we want to group areas of the circuit. To do that, we flood fill white areas with a color,
        // increasing the R value by 1 each time we do a flood fill. The result is that each group of white
        // pixels (i.e. a connected circuit) has an ID (its R value). Flood fills start at circuit positions,
        // because these are the only groups that can be powered.
        int curId = 1;
        foreach (var circuit in FindObjectsOfType<Circuit>()) {
            var texPos = GetCircuitPixelPosition(circuit.gameObject);
            if (texPos == null) continue;

            var x = (int)texPos.Value.x;
            var y = (int)texPos.Value.y;

            // If it's white, it's an unfilled circuit area
            if (tex.GetPixel(x, y).r == 1.0f)
            {
                tex.FloodFillArea(x, y, new Color(curId / 255.0f, 0, 0));
                ++curId;
            }
        }

        int numGroups = curId - 1;
        if (numGroups > 0)
        {
            groups = new List<List<Circuit>>();
            for (int i = 0; i < numGroups; ++i)
            {
                groups.Add(new List<Circuit>());
            }

            // Assign circuit objects to circuit groups
            var circuits = FindObjectsOfType<Circuit>();
            foreach (var circuit in circuits)
            {
                int groupId = 0;
                var texPos = GetCircuitPixelPosition(circuit.gameObject);

                // If no valid position, this circuit's group should be left as 0
                if (texPos != null)
                {
                    // Find the R value at this pixel and convert it to an integer to get the circuit ID
                    var pixel = tex.GetPixel((int)texPos.Value.x, (int)texPos.Value.y);
                    groupId = (int)(pixel.r * 255);

                    // Add to the group; otherwise, group will be 0 and the circuit can't be powered
                    if (groupId != 0)
                        groups[groupId - 1].Add(circuit);
                }

                // No power to ungrouped (i.e. broken) circuit pieces
                if (groupId == 0) circuit.powered = false;

                var powerSource = circuit.GetComponent<PowerSource>();
                if (powerSource != null) powerSource.groupId = groupId;
            }

            for (int i = 0; i < numGroups; ++i)
                RecalculatePower(i + 1);
        }
    }

    // Get a circuit's pixel position in the rendered grid, or null if no position
    Vector2? GetCircuitPixelPosition(GameObject circuit)
    {
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

        var center = checkChild.GetComponent<Renderer>().bounds.center;
        var texPos = circuitCamera.WorldToScreenPoint(center);

        return texPos;
    }

    // Change camera settings to include all circuitry
    void UpdateCamera()
    {
        // Get bounds of all GameObjects in circuit layer
        var objects = FindObjectsOfType<GameObject>();
        var bounds = new Bounds();
        var boundsSet = false;

        foreach (GameObject obj in objects)
        {
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
}
