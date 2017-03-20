using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public CutsceneManager cutsceneManager;
    private CutsceneInfo cutsceneInfo;

    private int currentScene;
    private int nextScene;
    private bool loadNextScene;

    // Use this for initialization
    void Start()
    {
        cutsceneInfo = this.GetComponent<CutsceneInfo>();

        // Make sure scenes are ordered properly in build settings
        currentScene = SceneManager.GetActiveScene().buildIndex;
        nextScene = currentScene + 1;
        loadNextScene = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(currentScene);

        if (loadNextScene)
            SceneManager.LoadScene(nextScene);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            // No cutscene, end level
            if (cutsceneInfo.textFile == null)
                LoadNextScene();
            // Tell the manager this is an end level cutscene
            else
                cutsceneManager.IsEndCutscene();
        }
    }

    public void LoadNextScene()
    {
        loadNextScene = true;
    }
}
