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
    Vector3 currentTarget;
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
        currentTarget = target;
        currentFly = StartCoroutine(FlyCoroutine());
    }

    public bool DoneFlying()
    {
        return (currentFly == null);
    }

    public void SkipFlying()
    {
        if (currentFly != null)
        {
            StopCoroutine(currentFly);
            FinishFly();
        }
    }

    void FinishFly()
    {
        anim.SetBool("Flying", false);
        transform.position = currentTarget;
        rig.localScale = rigScale;
        currentFly = null;
    }

    IEnumerator FlyCoroutine()
    {
        var startScale = rig.localScale;

        anim.SetBool("Flying", true);
        Vector3 start = transform.position;
        float startTime = Time.time;
        var delta = 0f;

        // Fly time is based on distance between positions
        float flyTime = (currentTarget - start).magnitude / flySpeed;

        // Flip to face movement
        var newScale = rigScale;
        if (currentTarget.x > start.x)
            newScale.x *= -1f;

        // If this is flipped, compensate
        newScale.x *= Mathf.Sign(transform.localScale.x);

        rig.localScale = newScale;

        bool flippedBack = false;

        // Tween to location
        while (delta < 1)
        {
            var diff = Time.time - startTime;

            delta = diff / flyTime;
            transform.position = Mathfx.Hermite(start, currentTarget, delta);

            // Almost at the end, so reset to idle animation
            if (!flippedBack && flyTime - diff < flyAnimStopTime)
            {
                anim.SetBool("Flying", false);
                flippedBack = true;
            }

            yield return null;
        }

        // Return to facing left
        FinishFly();
    }
}
