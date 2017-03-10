using System.Collections;
using UnityEngine;

public class Voice : MonoBehaviour
{
    WeightedClip[] clips;
    IEnumerator coroutine = null;
    public float delay = 0f;

    public bool isPlaying
    {
        get { return coroutine != null; }
    }

    public void Awake()
    {
        clips = transform.GetComponentsInChildren<WeightedClip>();
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

    IEnumerator PlayClip()
    {
        while (true)
        {
            var source = GetRandomSource();
            source.Play();

            yield return new WaitForSecondsRealtime(source.clip.length + delay);
        }
    }

    // Randomly pick one of the clips, taking into account their weights.
	AudioSource GetRandomSource()
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
                var source = clip.GetComponent<AudioSource>();
                return source;
            }

            sum += clip.weight;
        }

        Debug.LogError("Failed to pick a random clip");

        return null;
	}
}
