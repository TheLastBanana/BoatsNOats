using UnityEngine;

[RequireComponent(typeof(Splittable))]
public class SplittableBreakEffect : MonoBehaviour
{
    public GameObject effectPrefab;
    public Color newColor = new Color(0.5f, 0.48f, 0.46f);

    void OnSplitMergeFinished()
    {
        var splittable = GetComponent<Splittable>();
        if (splittable == null) return;

        // The splittable has been split, so add the effect
        if (splittable.isSplit)
        {
            var totalBounds = splittable.totalBounds;

            var effect = Instantiate(effectPrefab, transform, false);
            effect.transform.position = totalBounds.center;
            effect.transform.localRotation = transform.rotation;

            var size = totalBounds.extents;
            var localScale = splittable.transform.localScale;

            // If local scales are 0, don't scale size
            if (localScale.x != 0)
                size.x /= localScale.x;
            
            if (localScale.y != 0)
                size.y /= localScale.y;

            effect.transform.localScale = size;

            // Apply new color
            var meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in meshRenderers)
            {
                renderer.material.color = newColor;
            }

            // This is a one-shot effect, so remove it
            Destroy(this);
        }
    }
}

