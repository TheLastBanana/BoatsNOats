using UnityEngine;

public class CircuitThresholdEffect : MonoBehaviour
{
    public Material effectMaterial;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, effectMaterial);
    }
}
