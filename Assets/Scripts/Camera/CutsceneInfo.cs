using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneInfo : MonoBehaviour {

    public CutsceneManager cutsceneManager;
    public TextAsset textFile = null;
    private bool doneDialogue;

    // Use this for initialization
    void Start()
    {
        doneDialogue = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!doneDialogue && other.tag == "Player" && textFile != null)
        {
            // Start the cutscene indicated
            cutsceneManager.RunCutscene(textFile);
            // Prevent cutscene from being started again, don't disable object though as this script might be on a SceneTransition object
            doneDialogue = true;
        }
    }
}
