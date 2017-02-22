using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public Vector2 resolution;
    public Texture2D tex;

	void Start()
    {
        UpdateConnections();	
	}

    // Update circuit objects' connections
    void UpdateConnections()
    {
        UpdateCamera();

        // Render to the texture
        var cam = GetComponent<Camera>();
        var rt = cam.targetTexture;
        cam.Render();

        // Set it as the active texture
        var oldRT = RenderTexture.active;
        RenderTexture.active = rt;

        // Copy its pixels
        tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
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
        int curID = 1;
        for (int x = 0; x < tex.width; ++x)
        {
            for (int y = 0; y < tex.height; ++y)
            {
                // If it's white, it's an unfilled circuit area
                if (tex.GetPixel(x, y).r == 1.0f)
                {
                    tex.FloodFillArea(x, y, new Color(curID / 255.0f, 0, 0));
                    ++curID;
                }
            }
        }

        // With IDs assigned to pixels, we can determine each circuit's ID.
        var circuits = FindObjectsOfType<Circuit>();
        foreach (var circuit in circuits)
        {
            var texPos = cam.WorldToScreenPoint(circuit.transform.position);
            var pixel = tex.GetPixel((int)texPos.x, (int)texPos.y);
            circuit.circuitId = (int)(pixel.r * 255);
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
        transform.position = newPos;

        // Resize camera to see all objects
        Camera cam = GetComponent<Camera>();

        // Sizes accommodating the horizontal and vertical bounds
        float hSize = cam.orthographicSize = bounds.extents.x / cam.aspect;
        float vSize = bounds.extents.y;

        cam.orthographicSize = Mathf.Max(hSize, vSize);
    }
}
