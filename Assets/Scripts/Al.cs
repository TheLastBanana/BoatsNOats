using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Al : MonoBehaviour
{
    Animator anim;
    Coroutine currentFly;
    Transform rig;
    Vector3 rigScale;
    public AudioSource flySound;
    public float flySpeed = 2.0f;
    public float flyAnimStopTime = 0.25f;
    
    void Awake()
    {
        anim = GetComponent<Animator>();

        rig = transform.FindChild("rig");
        rigScale = rig.localScale;
    }

    // Send Al to a specific position
    public void FlyToPosition(Vector3 target)
    {
        // Stop current fly animation
        if (currentFly != null)
            StopCoroutine(currentFly);

        flySound.Play();
        currentFly = StartCoroutine(FlyCoroutine(target));
    }

    public bool DoneFlying()
    {
        return (currentFly == null);
    }

    IEnumerator FlyCoroutine(Vector3 target)
    {
        anim.SetBool("Flying", true);
        Vector3 start = transform.position;
        float startTime = Time.time;
        var delta = 0f;

        // Fly time is based on distance between positions
        float flyTime = (target - start).magnitude / flySpeed;

        // Flip to face movement
        var newScale = rigScale;
        if (target.x > start.x)
            newScale.x *= -1f;

        rig.localScale = newScale;

        bool flippedBack = false;

        // Tween to location
        while (delta < 1)
        {
            var diff = Time.time - startTime;

            delta = diff / flyTime;
            transform.position = Mathfx.Hermite(start, target, delta);

            // Almost at the end, so reset to idle animation
            if (!flippedBack && flyTime - diff < flyAnimStopTime)
            {
                anim.SetBool("Flying", false);
                flippedBack = true;
            }

            yield return null;
        }

        // Return to facing left
        rig.localScale = rigScale;
        currentFly = null;
    }
}
