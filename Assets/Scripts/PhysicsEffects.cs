using UnityEngine;
using System.Collections.Generic;

public class PhysicsEffects : MonoBehaviour
{
    public float velocityThreshold = 0.3f;
    public GameObject effectPrefab;
    Dictionary<Vector2, Vector2> collisionOffsets = new Dictionary<Vector2, Vector2>();
	
	void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.relativeVelocity.magnitude > velocityThreshold)
        {
            var contacts = collision.contacts;

            // Get average direction
            var avgNormal = new Vector2();

            foreach (var contact in contacts)
                avgNormal += contact.normal;

            avgNormal /= contacts.Length;

            // Snap to a 90-degree increment
            var snappedDir = (Mathf.Abs(avgNormal.x) > Mathf.Abs(avgNormal.y)) ? new Vector2(1, 0) : new Vector2(0, 1);

            // Now determine how far the points are in that direction
            var avgOffset = new Vector2();
            foreach (var contact in contacts)
            {
                avgOffset.x += (contact.point.x - transform.position.x) * snappedDir.x;
                avgOffset.y += (contact.point.y - transform.position.y) * snappedDir.y;
            }
            avgOffset /= contacts.Length;

            // Store the collision point depending on its direction
            collisionOffsets[snappedDir] = avgOffset;
        }
	}

    void FixedUpdate()
    {
        if (collisionOffsets.Count == 0) return;

        // Create an effect for each collision direction
        foreach (var pair in collisionOffsets)
        {
            var offset = pair.Value;
            var effect = Instantiate(effectPrefab, transform, false);

            // Rotation should use the original angle, but modify X axis (our particle effects are generally rotated in Y and Z by default)
            var euler = effect.transform.localEulerAngles;
            euler.x = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            effect.transform.localEulerAngles = euler;

            var particles = effect.GetComponent<ParticleSystem>();

            // Account for splittable size
            var splittable = GetComponent<Splittable>();
            if (splittable)
            {
                var dir = pair.Key;
                var totalBounds = splittable.totalBounds;
                var totalExtents = totalBounds.extents;

                // Get the size of the side tangent to the direction. We do this by multiplying the extents
                // component-wise by the perpendicular of the direction (i.e. if dir is (0, 1), we cancel out the
                // Y of totalExtents by multiplying with 0).
                var tangentExtents = new Vector2(
                    totalExtents.x * dir.y,
                    totalExtents.y * dir.x
                );

                var shape = particles.shape;
                shape.radius = tangentExtents.magnitude;

                effect.transform.position = totalBounds.center + (Vector3)offset;
            }
            else
            {
                effect.transform.position = transform.position + (Vector3)offset;
            }

            particles.Play();
        }
        
        collisionOffsets.Clear();
    }
}
