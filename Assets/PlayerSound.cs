using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    public AudioSource[] stepSounds;
    public AudioSource[] jumpSounds;
    public AudioSource[] landSounds;
    public float landingSoundAirTime = 0.5f;

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
            PlayFootstepSound();
        }

        prevFootstep = footstep;
        
        bool grounded = anim.GetBool("Ground");
        if (grounded)
        {
            // Switched from grounded to not grounded and stayed in the air long enough
            if (!wasGrounded && Time.time - lastGroundedTime > landingSoundAirTime)
            {
                PlayLandSound();
            }

            lastGroundedTime = Time.time;
        }

        wasGrounded = grounded;
    }
    
    // Play a random footstep sound
    public void PlayFootstepSound()
    {
        PlayRandomSound(stepSounds);
    }

    // Play the jump sound
    public void PlayJumpSound()
    {
        PlayRandomSound(jumpSounds);
    }

    // Play the landing sound
    public void PlayLandSound()
    {
        PlayRandomSound(landSounds);
    }

    // Play a random sound from an array
    private void PlayRandomSound(AudioSource[] array)
    {
        array[Random.Range(0, array.Length)].Play();
    }
}
