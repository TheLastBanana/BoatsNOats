using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneInfo : MonoBehaviour {

    public CutsceneManager cutsceneManager;
    public TextAsset textFile = null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player" && textFile != null)
        {
            // Start the cutscene indicated (if there is one) in this object and deactivate this object
            cutsceneManager.RunCutscene(textFile);
            this.gameObject.SetActive(false);
        }
    }
}
