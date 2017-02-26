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

    bool switched = false;
    AudioEffects afx;

    void Start ()
    {
        afx = GetComponent<AudioEffects>();
    }

    // Update is called once per frame
    void Update () {
		if (!switched && Input.GetKeyDown(KeyCode.Tab))
        {
            cMain.enabled = false;
            cChar.enabled = false;
            cAlt.enabled = true;
            switched = true;

            afx.cancelEffects(altWorldAmbience);
            altWorldAmbience.Play();
        }
        else if (switched && Input.GetKeyUp(KeyCode.Tab))
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
}
