using UnityEngine;

public class AlIntroControl : MonoBehaviour
{
    public ParticleSystem sparks;
    ParticleSystem.EmissionModule em;

    void Awake()
    {
        em = sparks.emission;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            GetComponent<Animator>().SetTrigger("Bonk");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GetComponent<Animator>().SetTrigger("Get Up");
        }
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
