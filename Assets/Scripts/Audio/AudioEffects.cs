using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioEffects : MonoBehaviour
{
    // Effects by audio source
    Dictionary<AudioSource, float> effectCancelTimes = new Dictionary<AudioSource, float>();

    //////////////////////
    // PUBLIC FUNCTIONS //
    //////////////////////

    // Wait for a given amount of time, then run the given action
    public void delay(AudioSource audio, Action toRun, float time)
    {
        StartCoroutine(delayCoroutine(audio, toRun, time));
    }

    // Fade out an audio source over the given time
    public void fadeOut(AudioSource audio, float fadeTime)
    {
        StartCoroutine(fadeOutCoroutine(audio, fadeTime));
    }

    // Smoothly stop a sound by fading it out quickly
    public void smoothStop(AudioSource audio)
    {
        fadeOut(audio, 0.05f);
    }

    // Cancel fadeouts for an audio source
    public void cancelEffects(AudioSource audio)
    {
        effectCancelTimes[audio] = Time.time;
    }


    ////////////////
    // COROUTINES //
    ////////////////

    private IEnumerator delayCoroutine(AudioSource audio, Action toRun, float time)
    {
        float startTime = Time.time;

        yield return new WaitForSeconds(time);

        // Delay was cancelled
        float cancelTime;
        if (effectCancelTimes.TryGetValue(audio, out cancelTime) && cancelTime > startTime)
        {
            yield break;
        }

        toRun();
    }

    // Reference: https://forum.unity3d.com/threads/fade-out-audio-source.335031/
    private IEnumerator fadeOutCoroutine(AudioSource audio, float fadeTime)
    {
        float startVolume = audio.volume;
        float startTime = Time.time;

        while (audio.volume > 0)
        {
            audio.volume -= startVolume * Time.deltaTime / fadeTime;

            yield return null;

            // Fade was cancelled
            float cancelTime;
            if (effectCancelTimes.TryGetValue(audio, out cancelTime) && cancelTime > startTime)
            {
                audio.volume = startVolume;
                yield break;
            }
        }

        audio.Stop();
        audio.volume = startVolume;
    }
}
