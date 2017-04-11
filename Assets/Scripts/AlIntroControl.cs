using UnityEngine;
using System.Collections;

public class AlIntroControl : MonoBehaviour
{
    public ParticleSystem sparks;
    public ParticleSystem bonkEffect;
    public ParticleSystem landEffect;
    public AudioSource thudSound;
    public AudioSource zapSound;
    public AudioSource flySound;

    ParticleSystem.EmissionModule em;

    void Awake()
    {
        em = sparks.emission;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GetComponent<Animator>().SetTrigger("Get Up");
        }
    }

    public void PlayFlySound()
    {
        flySound.Play();
    }

    public void PlayBonkAnimation()
    {
        GetComponent<Animator>().SetTrigger("Bonk");
        bonkEffect.Play();
    }

    public void PlayLandEffect()
    {
        landEffect.Play();
        thudSound.Play();
    }

    public void EnableSparks()
    {
        zapSound.Play();
        em.enabled = true;
    }

    public void DisableSparks()
    {
        zapSound.Stop();
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
