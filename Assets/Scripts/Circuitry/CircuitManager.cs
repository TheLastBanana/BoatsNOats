using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public Vector2 resolution;
    public Camera circuitCamera;

    static List<List<Circuit>> groups;

	void Start()
    {
        RecalculateGroups();
    }

    // Recalculate the power for a given group
    static public void RecalculatePower(int groupId)
    {
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

        // Apply a threshold. Any non-black pixels become fully white.
        var white = new Color(1f, 1f, 1f);
        var pixels = tex.GetPixels();
        for (int i = 0; i < pixels.Length; ++i)
        {
            if (pixels[i].r != 0f) pixels[i] = white;
        }
        tex.SetPixels(pixels);

        // Now we want to group areas of the circuit. To do that, we flood fill white areas with a color,
        // increasing the R value by 1 each time we do a flood fill. The result is that each group of white
        // pixels (i.e. a connected circuit) has an ID (its R value).
        int curId = 1;
        for (int x = 0; x < tex.width; ++x)
        {
            for (int y = 0; y < tex.height; ++y)
            {
                // If it's white, it's an unfilled circuit area
                if (tex.GetPixel(x, y).r == 1.0f)
                {
                    tex.FloodFillArea(x, y, new Color(curId / 255.0f, 0, 0));
                    ++curId;
                }
            }
        }

        int numGroups = curId - 1;
        if (numGroups > 1)
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
                // Find the R value at this pixel and convert it to an integer to get the circuit ID
                var texPos = circuitCamera.WorldToScreenPoint(circuit.transform.position);
                var pixel = tex.GetPixel((int)texPos.x, (int)texPos.y);
                int groupId = (int)(pixel.r * 255);
                
                Debug.Assert(groupId != 0, "Circuit \"" + circuit.gameObject.name + "\" has invalid group ID (0)");

                // Add to the group
                groups[groupId - 1].Add(circuit);

                var powerSource = circuit.GetComponent<PowerSource>();
                if (powerSource != null) powerSource.groupId = groupId;
            }

            for (int i = 0; i < numGroups; ++i)
                RecalculatePower(i + 1);
        }
    }

    // Change camera settings to include all circuitry
    void UpdateCamera()
    {
        // Get bounds of all GameObjects in circuit layer
        var objects = FindObjectsOfType<GameObject>();
        var circuitLayer = LayerMask.NameToLayer("Circuitry");
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
