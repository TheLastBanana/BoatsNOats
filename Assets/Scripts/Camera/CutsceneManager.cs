using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour {

    public GameObject sceneStart;
    public GameObject sceneTransition;
    public GameObject sceneMid1;
    public GameObject sceneMid2;
    public GameObject sceneMid3;

    public GameObject cameraManager;
    private CameraSwitcher cameraSwitcher;
    private CameraTracker cameraTracker;
    private CameraPanner cameraPanner;
    public PortalManager portalManager;
    public GameControls controls;
    private TypewriterText typewriterText;

    // Gemma
    public GameObject Gemma;
    private PlayerController playerController;
    private GameObject GemmaTextBubble;
    private Text GemmaText;

    // Al
    public GameObject Al;
    private GameObject AlTextBubble;
    private Text AlText;

    private GameObject currentTextBubble;
    private Text currentText;

    private int numTextCurrent;
    private int numTexts;
    private CameraPanInfo previousPan;

    private bool runningCutscene;
    private bool startedText;
    Queue<CameraPanInfo> pans;
    private bool startedPan;
    public bool readyToEndPan;
    private bool endCutscene; // Last cutscene in the level, will do scene transition after

    // Use this for initialization
    void Start () {
        cameraSwitcher = cameraManager.GetComponent<CameraSwitcher>();
        cameraTracker = cameraManager.GetComponent<CameraTracker>();
        cameraPanner = cameraManager.GetComponent<CameraPanner>();
        cameraTracker.enabled = true;
        cameraPanner.enabled = false;
        typewriterText = GetComponent<TypewriterText>();

        // Grab Gemma's stuff
        playerController = Gemma.GetComponent<PlayerController>();
        GemmaTextBubble = Gemma.GetComponentInChildren<Canvas>(true).transform.FindChild("GemmaTextBubble").gameObject;
        GemmaText = GemmaTextBubble.GetComponentInChildren<Text>(true);
        GemmaTextBubble.SetActive(false);

        // Grab Al's stuff
        if (Al != null)
        {
            AlTextBubble = Al.GetComponentInChildren<Canvas>(true).transform.FindChild("AlTextBubble").gameObject;
            AlText = AlTextBubble.GetComponentInChildren<Text>(true);
            AlTextBubble.SetActive(false);
        }

        numTextCurrent = -1;
        numTexts = 0;
        previousPan = new CameraPanInfo(null, Gemma, 0f, 0f);

        runningCutscene = false;
        startedText = false;
        pans = new Queue<CameraPanInfo>();
        startedPan = false;
        readyToEndPan = false;
        endCutscene = false;
    }
	
	// Update is called once per frame
	void Update () {
        // Check if we've gone through all of the texts
        if (numTextCurrent == numTexts)
            EndCutscene();

        // We're not in a cutscene
        if (!runningCutscene)
            return;

        // If we're not in a text or pan, waiting for player input at the end of a pan, and there is a pan to do
        if (!startedText && !startedPan && !readyToEndPan && pans.Count > 0)
            StartPan();

        // Start the next text if we're not currently doing one, the previous has been finished, and we're not panning or waiting on a finished pan
        if (!typewriterText.isTextDone() && !startedText && !startedPan && !readyToEndPan)
        {
            startedText = true;
            typewriterText.startText(numTextCurrent);
        }

        // If the text has gone through, wait for the player to hit a skip key before finishing the text
        if (startedText && !typewriterText.isTextDone() && controls.SkipDialogue())
        {
            startedText = false;
            currentTextBubble.SetActive(false);
            numTextCurrent += 1;
        }

        // Let the player skip a pan
        if (startedPan && controls.SkipDialogue())
            EndPan();

        // Pan is finished, so wait for player input before moving on
        if (readyToEndPan && controls.SkipDialogue())
            readyToEndPan = false;
    }

    public void RunCutscene(TextAsset textFile)
    {
        typewriterText.setTextFile(textFile);
        numTextCurrent = 0;
        numTexts = typewriterText.numDialogsLoaded();

        // Disable player control
        runningCutscene = true;
        playerController.StopForCutscene();
        cameraSwitcher.SetCutscene(true);
        portalManager.DisablePortal(true);
    }

    private void EndCutscene()
    {
        // Reset info about the cutscene to a "no cutscene" state
        numTextCurrent = -1;
        numTexts = 0;
        previousPan = new CameraPanInfo(null, Gemma, 0f, 0f);
        pans.Clear();

        // Resume player control
        runningCutscene = false;
        playerController.ResumeAfterCutscene();
        cameraSwitcher.SetCutscene(false);
        cameraTracker.UpdateTarget(Gemma);
        portalManager.DisablePortal(false);

        // If this was the last cutscene in a level do a scene transition now
        if (endCutscene)
            sceneTransition.GetComponent<SceneChanger>().SetLoadNextScene();
    }

    // TODO: Properly determine speaker based on tag
    public Text DecideSpeaker(string tag)
    {
        tag = tag.ToLower();

        if (tag == "gemma")
        {
            currentTextBubble = GemmaTextBubble;
            currentText = GemmaText;
        }
        else if (tag == "al")
        {
            currentTextBubble = AlTextBubble;
            currentText = AlText;
        }
        else
        {
            // If tag doesn't match we assume Gemma is speaking, she's the only thing guaranteed to be in every scene after all
            currentTextBubble = GemmaTextBubble;
            currentText = GemmaText;
        }

        currentTextBubble.SetActive(true);
        return currentText;
    }

    public void QueuePan(string objName, float delay)
    {
        GameObject to = getLocationFromTag(objName.ToLower());

        // Assume the previous pan to target is where we're panning from
        previousPan = new CameraPanInfo(previousPan.to, to, 0f, delay);
        pans.Enqueue(previousPan);
    }

    private void StartPan()
    {
        startedPan = true;
        CameraPanInfo nextPan = pans.Dequeue();
        nextPan.startTime = Time.time;

        // Switch camera scripts
        cameraTracker.enabled = false;
        cameraTracker.UpdateTarget(nextPan.to);
        cameraPanner.enabled = true;
        cameraPanner.currentPan = nextPan;
    }

    public void EndPan()
    {
        startedPan = false;
        readyToEndPan = true;

        // Switch camera scripts
        cameraTracker.enabled = true;
        cameraPanner.enabled = false;
    }

    // Called to signify the cutscene is the last in the level and we need to level change after
    public void IsEndCutscene()
    {
        endCutscene = true;
    }

    // Freezes Gemma and player input until reenabled
    public void DisableGemma(bool disable)
    {
        if (disable)
            playerController.StopForPortal();
        else
            playerController.ResumeAfterPortal();
    }

    // Get location to pan to or for Al to move to based on tag
    private GameObject getLocationFromTag(string tag)
    {
        if (tag == "start")
            return sceneStart;
        else if (tag == "end")
            return sceneTransition;
        else if (tag == "gemma")
            return Gemma;
        else if (tag == "mid1")
            return sceneMid1;
        else if (tag == "mid2")
            return sceneMid2;
        else if (tag == "mid3")
            return sceneMid3;
        else
            return null;
    }
}

public class CameraPanInfo
{
    public GameObject from;
    public GameObject to;
    public float startTime;
    public float duration;

    public CameraPanInfo(GameObject f, GameObject t, float s, float d)
    {
        from = f;
        to = t;
        startTime = s;
        duration = d;
    }
}