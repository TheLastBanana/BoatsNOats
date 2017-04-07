using UnityEngine;

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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayBonkAnimation();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GetComponent<Animator>().SetTrigger("Get Up");
        }
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
}
