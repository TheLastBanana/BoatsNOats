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
    public GameControls controls;

    bool switched;
    private bool inCutscene;
    AudioEffects afx;

    void Start ()
    {
        switched = false;
        inCutscene = false;
        afx = GetComponent<AudioEffects>();
    }

    // Update is called once per frame
    void Update () {
		if (!switched && controls.CheckOtherWorld(true) && !inCutscene)
        {
            cMain.enabled = false;
            cChar.enabled = false;
            cAlt.enabled = true;
            switched = true;

            afx.cancelEffects(altWorldAmbience);
            altWorldAmbience.Play();
        }
        else if ((switched && controls.CheckOtherWorld(false)) || (switched && inCutscene))
        {
            cMain.enabled = true;
            cChar.enabled = true;
            cAlt.enabled = false;
            switched = false;

            afx.smoothStop(altWorldAmbience);
            musicManager.muffled = false;
        }

        if (switched) musicManager.muffled = true;
    }

    // Stop player from switching cameras during cutscene
    public void SetCutscene(bool cutscene)
    {
        inCutscene = cutscene;
    }
}
