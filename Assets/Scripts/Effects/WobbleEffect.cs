using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WobbleEffect : MonoBehaviour
{
    public Material effectMaterial;
    public Texture2D displacement;
    public int resolution = 256;

    void Awake()
    {
        var pixels = new Color[resolution * resolution];
        for (int x = 0; x < resolution; ++x)
        {
            // Calculate the brightness
            float value = (Mathf.Sin(Mathf.PI * 2f * x / resolution) + 1f) * 0.5f;
            Color color = new Color(value, value, value);

            // Set pixels in this row
            for (int y = 0; y < resolution; ++y)
                pixels[y * resolution + x] = color;
        }

        displacement = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        displacement.SetPixels(pixels);
        displacement.Apply();

        effectMaterial.SetTexture("_Displacement", displacement);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, effectMaterial);
    }
}
