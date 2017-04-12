using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public bool playLevelComplete = true;
    public CutsceneManager cutsceneManager;
    private CutsceneInfo cutsceneInfo;
    public GameControls controls;
    public AudioSource levelComplete;
    public MusicManager musicManager;

    // Need the fader from the Main Camera
    public FadeOut fader;
    public float frameCountRestart = 128;
    public float frameCountNextLevel = 256;

    private int currentScene;
    private int nextScene;
    private bool loadNextScene;
    private bool restarted;

    // Use this for initialization
    void Start()
    {
        cutsceneInfo = this.GetComponent<CutsceneInfo>();

        // Make sure scenes are ordered properly in build settings
        currentScene = SceneManager.GetActiveScene().buildIndex;
        nextScene = currentScene + 1;
        loadNextScene = false;
        restarted = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!restarted && controls.RestartLevel())
        {
            restarted = true;
            DoLoadScene(currentScene, frameCountRestart, false);
        }

        if (!restarted && loadNextScene)
        {
            restarted = true;
            DoLoadScene(nextScene, frameCountNextLevel, true);
        }

        if (controls.QuitGame())
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #endif

            #if UNITY_STANDALONE_OSX
                Application.Quit();
            #endif

            #if UNITY_STANDALONE_WIN
                Application.Quit();
            #endif   
            
                
  
        }
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            // No cutscene, end level
            if (cutsceneInfo.textFile == null)
                SetLoadNextScene();
            // Tell the manager this is an end level cutscene
            else
                cutsceneManager.IsEndCutscene();
        }
    }

    public void SetLoadNextScene()
    {
        loadNextScene = true;
    }

    // Do everything to load another scene
    // TODO: Transition stuff goes here
    private void DoLoadScene(int scene, float frameCount, bool nextLevel)
    {
        // Disables player controls (moving Gemma, creating portals, Tabbing to view other world)
        cutsceneManager.DisableControls(true);
        controls.DisableSkipping(true);

        // TODO: Play sound if nextLevel == true
        StartCoroutine(Transition(scene, frameCount, nextLevel));
    }
    // Does a transition by animating the fade out, then loading the next scene
    private IEnumerator Transition(int scene, float frameCount, bool nextLevel)
    {
        if (nextLevel && playLevelComplete)
        {
            // Stop music and play level complete
            musicManager.stopMusic();

            yield return new WaitForSecondsRealtime(0.75f);

            levelComplete.PlayDelayed(0);

            // Wait an unscaled amount of time, we don't scale our music playing
            yield return new WaitForSecondsRealtime(3);

        }
        fader.StartFade(cutsceneManager.Gemma.transform.position, frameCount, false);

        // Lambda wait while we are animating
        yield return new WaitWhile(() => fader.isAnimating());

        SceneManager.LoadScene(scene);
    }
}
