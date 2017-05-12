using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public AudioSource altWorldAmbience;
    public MusicManager musicManager;
    public Camera cMain;
    public Camera cAlt;
    public Camera cChar;
    public Camera cGhost;
    public GameControls controls;

    public bool switched
    {
        get
        {
            return _switched;
        }
    }

    bool _switched;
    private bool inCutscene;
    AudioEffects afx;

    void Awake()
    {
        _switched = false;
        inCutscene = false;
        afx = GetComponent<AudioEffects>();
    }

    // Update is called once per frame
    void Update () {
        if (!_switched && controls.CheckOtherWorld(true) && !inCutscene)
        {
            cMain.enabled = false;
            cChar.enabled = false;
            cAlt.enabled = true;
            cGhost.enabled = true;
            _switched = true;

            afx.cancelEffects(altWorldAmbience);
            altWorldAmbience.Play();
        }
        else if ((_switched && controls.CheckOtherWorld(false)) || (_switched && inCutscene))
        {
            cMain.enabled = true;
            cChar.enabled = true;
            cAlt.enabled = false;

            cGhost.enabled = false;
            _switched = false;

            afx.smoothStop(altWorldAmbience);
            musicManager.muffled = false;
        }

        if (_switched) musicManager.muffled = true;
    }

    // Stop player from switching cameras during cutscene
    public void SetCutscene(bool cutscene)
    {
        inCutscene = cutscene;
    }
}
