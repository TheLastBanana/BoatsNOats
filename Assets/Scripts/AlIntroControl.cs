using UnityEngine;
using System.Collections;

public class AlIntroControl : MonoBehaviour
{
    public ParticleSystem sparks;
    public ParticleSystem bonkEffect;
    public ParticleSystem landEffect;
    ParticleSystem.EmissionModule em;

    void Awake()
    {
        em = sparks.emission;
    }

    public void PlayBonkAnimation()
    {
        GetComponent<Animator>().SetTrigger("Bonk");
        bonkEffect.Play();
    }

    public void PlayLandEffect()
    {
        landEffect.Play();
    }

    public void EnableSparks()
    {
        em.enabled = true;
    }

    public void DisableSparks()
    {
        em.enabled = false;
    }

    public void DelayedFlip()
    {
        StartCoroutine(Flip());
    }

    IEnumerator Flip()
    {
        yield return new WaitForSeconds(0.02f);

        var rig = transform.FindChild("rig");
        var scale = rig.localScale;
        scale.x = -scale.x;
        rig.localScale = scale;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        PlayBonkAnimation();
        GetComponent<CircleCollider2D>().enabled = false;

        // Make the box fly away a bit
        collision.gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(3, 1.5f);
    }
}
