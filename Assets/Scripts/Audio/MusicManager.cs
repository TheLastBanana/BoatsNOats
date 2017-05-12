using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioEffects))]
public class MusicManager : MonoBehaviour
{
    public AudioSource intro;
    public AudioSource loop;
    public float minLPFCutoff;
    public float maxLPFCutoff;
    public float lpfChangeTime = 0.1f;
    public float baseVolume = 0.6f;
    public float muffleVolume = 0.5f;

    AudioLowPassFilter[] lpfs;
    bool lpfEnabled;
    IEnumerator curLPFLerp;
    AudioEffects afx;

    private bool _muffled = false;
    private float _volume = 1f;

    public bool startPlaying = true;
    public bool isPlaying { get; private set; }

    // Whether the muffle effect is enabled
    public bool muffled
    {
        get
        {
            return _muffled;
        }

        set
        {
            if (value && !_muffled) enableLPF();
            if (!value && _muffled) disableLPF();

            _muffled = value;
            volume = _volume;
        }
    }

    // The music volume (multiplied by the base volume)
    public float volume
    {
        get
        {
            return _volume;
        }

        set
        {
            _volume = value;

            float realVolume = _volume * baseVolume;
            if (_muffled) realVolume *= muffleVolume;
            intro.volume = realVolume;
            loop.volume = realVolume;
        }
    }

    void Awake()
    {
        // Set initial value
        isPlaying = false;

        // Get low-pass filters
        lpfs = new AudioLowPassFilter[]
        {
            intro.GetComponent<AudioLowPassFilter>(),
            loop.GetComponent<AudioLowPassFilter>()
        };

        afx = GetComponent<AudioEffects>();
    }

    void Start()
    {
        volume = _volume;
        
        if (startPlaying)
            playMusic();
    }

    // Play looped music
    public void playMusic()
    {
        intro.Play();
        loop.PlayDelayed(intro.clip.length);
        isPlaying = true;
    }

    // Stop the music
    public void stopMusic()
    {
        afx.smoothStop(intro);
        afx.smoothStop(loop);
        isPlaying = false;
    }

    // Smoothly enable low-pass filters
    private void enableLPF()
    {
        if (curLPFLerp != null) StopCoroutine(curLPFLerp);
        curLPFLerp = lpfLerp(minLPFCutoff);
        StartCoroutine(curLPFLerp);
    }

    // Smoothly disable low-pass filters
    private void disableLPF()
    {
        if (curLPFLerp != null) StopCoroutine(curLPFLerp);
        curLPFLerp = lpfLerp(maxLPFCutoff);
        StartCoroutine(curLPFLerp);
    }

    // Lerp LPFs to a given value
    private IEnumerator lpfLerp(float target)
    {
        float time = 0f;
        float start = lpfs[0].cutoffFrequency;

        while (time < lpfChangeTime)
        {
            foreach (var lpf in lpfs)
                lpf.cutoffFrequency = Mathf.Lerp(start, target, time / lpfChangeTime);

            yield return null;

            time += Time.deltaTime;
        }

        foreach (var lpf in lpfs)
            lpf.cutoffFrequency = target;
    }
}
