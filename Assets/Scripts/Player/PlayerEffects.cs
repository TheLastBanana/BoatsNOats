using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    public AudioSource[] stepSounds;
    public AudioSource[] jumpSounds;
    public AudioSource[] landSounds;
    public float landingSoundAirTime = 0.5f;
    public ParticleSystem landEffect;
    public ParticleSystem stepEffect;
    public ParticleSystem jumpEffect;
    public float stepEffectMinSpeed = 0.5f;

    private Animator anim;              // Animator component
    private float prevFootstep;         // Previous footstep value
    private float lastGroundedTime;     // Last time at which this was grounded
    private bool wasGrounded;           // Was this grounded last update?

    public void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void Update()
    {
        float footstep = anim.GetFloat("Footstep");

        // If footstep oscillates from low to high, play the sound
        // This allows us to blend animations and not repeat the sound
        if (prevFootstep < 0 && footstep > 0)
        {
            PlayStepEffect();
        }

        prevFootstep = footstep;
        
        bool grounded = anim.GetBool("Ground");
        if (grounded)
        {
            // Switched from grounded to not grounded and stayed in the air long enough
            if (!wasGrounded && Time.time - lastGroundedTime > landingSoundAirTime)
            {
                PlayLandEffect();
            }

            lastGroundedTime = Time.time;
        }

        wasGrounded = grounded;
    }
    
    // Play a random footstep sound
    public void PlayStepEffect()
    {
        PlayRandomSound(stepSounds);
        if (anim.GetFloat("Speed") > stepEffectMinSpeed) stepEffect.Play();
    }

    // Play the jump sound
    public void PlayJumpEffect()
    {
        PlayRandomSound(jumpSounds);
        jumpEffect.Play();
    }

    // Play the landing effect
    public void PlayLandEffect()
    {
        PlayRandomSound(landSounds);
        landEffect.Play();
    }

    // Play a random sound from an array
    private void PlayRandomSound(AudioSource[] array)
    {
        array[Random.Range(0, array.Length)].Play();
    }
}
