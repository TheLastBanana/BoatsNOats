﻿using System.Collections;
using UnityEngine;

[RequireComponent (typeof (AudioSource))]
public class Voice : MonoBehaviour
{
    WeightedClip[] clips;
    AudioSource source;

    IEnumerator coroutine = null;
    public float delay = 0f;

    public bool isPlaying
    {
        get { return coroutine != null; }
    }

    public void Awake()
    {
        clips = transform.GetComponentsInChildren<WeightedClip>();
        source = GetComponent<AudioSource>();
    }

    public void Play()
    {
        if (coroutine != null) return;

        coroutine = PlayClip();
        StartCoroutine(coroutine);
    }

    public void Stop()
    {
        if (coroutine == null) return;

        StopCoroutine(coroutine);
        coroutine = null;
    }

    // Coroutine that plays clips
    IEnumerator PlayClip()
    {
        var prevSources = new WeightedClip[] { null, null };

        while (true)
        {
            var wClip = GetRandomClip();

            // Don't repeat yourself more than once
            if (clips.Length > 1)
            {
                while (wClip == prevSources[0] && wClip == prevSources[1])
                {
                    wClip = GetRandomClip();
                }
            }
            
            source.PlayOneShot(wClip.clip, wClip.volume);

            // Wait to play the next clip
            yield return new WaitForSecondsRealtime(wClip.clip.length - wClip.overplay + delay);

            prevSources[1] = prevSources[0];
            prevSources[0] = wClip;
        }
    }

    // Randomly pick one of the clips, taking into account their weights.
    WeightedClip GetRandomClip()
    {
        // Determine total weight of clips
        float total = 0f;
        foreach (var clip in clips)
            total += clip.weight;

        // Pick a random number in the range of the total weight
        float chosen = Random.Range(0f, total);

        // Now iterate, summing weights until we've exceeded the chosen number. The last clip is the one to use
        float sum = 0f;
        foreach (var clip in clips)
        {
            if (sum + clip.weight > chosen)
            {
                return clip;
            }

            sum += clip.weight;
        }

        Debug.LogError("Failed to pick a random clip");

        return null;
    }
}
