﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour {

    public GameObject sceneStart;
    public GameObject sceneTransition;
    public GameObject sceneMid1;
    public GameObject sceneMid2;
    public GameObject sceneMid3;
    public GameObject sceneMid4;
    public GameObject sceneMid5;
    public GameObject leftBound;
    public GameObject rightBound;
    public GameObject portalFlashPrefab;
    public GameObject dryBG;
    public GameObject wetBG;
    public WorldOffsets offs;
    public FadeOut fader;

    public GameObject cameraManager;
    private CameraSwitcher cameraSwitcher;
    private CameraTracker cameraTracker;
    private CameraPanner cameraPanner;
    public PortalManager portalManager;
    public MusicManager musicManager;
    public GameControls controls;
    private TypewriterText typewriterText;
    public Canvas dialogueCanvas;

    // Gemma
    public GameObject Gemma;
    private PlayerController playerController;
    private Text GemmaText;

    // Al
    public GameObject Al;
    private Al AlScript;
    private Text AlText;

    // Loudspeaker1
    public GameObject LoudspeakerWet;
    private Text LSWetText;

    // Loudspeaker2
    public GameObject LoudspeakerDry;
    private Text LSDryText;

    // FadeIn info
    public bool fadeIn = true;
    private bool isFading = false;

    private Text currentText;

    private int numTextCurrent;
    private int numTexts;
    private CameraPanInfo previousPan;

    private bool runningCutscene;
    private bool startedText;
    Queue<CameraPanInfo> pans;
    Queue<MoveInfo> moves;
    Queue<string> soundToggles;
    private bool startedPan;
    private bool doAPortal;
    private bool endCutscene; // Last cutscene in the level, will do scene transition after
    private GameObject currentFlash;

    private bool skipEverything;

    private GameObject speaker;

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
        GemmaText = dialogueCanvas.transform.FindChild("GemmaText").gameObject.GetComponent<Text>();
        GemmaText.gameObject.SetActive(false);

        // Grab Al's stuff
        if (Al != null)
            AlScript = Al.GetComponent<Al>();
        AlText = dialogueCanvas.transform.FindChild("AlText").gameObject.GetComponent<Text>();
        AlText.gameObject.SetActive(false);

        // Loudspeakers
        LSWetText = dialogueCanvas.transform.FindChild("LSWetText").gameObject.GetComponent<Text>();
        LSWetText.gameObject.SetActive(false);
        LSDryText = dialogueCanvas.transform.FindChild("LSDryText").gameObject.GetComponent<Text>();
        LSDryText.gameObject.SetActive(false);

        numTextCurrent = -1;
        numTexts = 0;
        previousPan = new CameraPanInfo(0, null, Gemma, 0f, 0f);

        runningCutscene = false;
        startedText = false;
        pans = new Queue<CameraPanInfo>();
        moves = new Queue<MoveInfo>();
        soundToggles = new Queue<string>();
        startedPan = false;
        doAPortal = false;
        endCutscene = false;

        skipEverything = false;

        // Disable player control for fade
        DisableControls(true);

        // Do fade
        isFading = true;
        StartCoroutine(FadeIn(32));
    }
    
    // Update is called once per frame
    void Update () {
        // We're not in a cutscene
        if (!runningCutscene || skipEverything)
            return;

        // Check if we've gone through everything
        if (numTextCurrent == numTexts && pans.Count <= 0 && moves.Count <= 0 && !doAPortal && !Busy())
        {
            EndCutscene();
            return;
        }

        if (controls.SkipWholeCutscene())
        {
            skipEverything = true;
            StartCoroutine(SkipWholeCutscene());
            return;
        }

        if (!Busy())
        {
            // Toggle sounds when they ask to be
            while (soundToggles.Count > 0)
            {
                ToggleSound(soundToggles.Dequeue());
            }

            if (doAPortal)
            {
                doAPortal = false;
                DoPortalEffect();
            }

            // Pans and moves
            else if (pans.Count > 0 && moves.Count <= 0)
                StartPan();
            else if (pans.Count <= 0 && moves.Count > 0)
                StartMove();
            else if (pans.Count > 0 && moves.Count > 0)
            {
                // If there is both a pan and a move queued up, whichever was added first will go first
                if (moves.Peek().tagStart <= pans.Peek().tagStart)
                    StartMove();
                else
                    StartPan();
            }

            // Start the next text if we're not currently doing one
            else if (!typewriterText.isTextStarted())
            {
                startedText = true;
                typewriterText.startText(numTextCurrent);
                
                if (speaker && !skipEverything)
                {
                    var animator = speaker.GetComponent<Animator>();
                    if (animator)
                    {
                        animator.SetBool("Talking", true);
                    }
                }
            }
        }

        // If the typewriter text script has gone through and there's no speaker than auto skip
        if (startedText && !typewriterText.isTextStarted() && !typewriterText.hasSpeaker(numTextCurrent))
            DoAfterText();

        // If player hits the skip button, see if anything can be skipped
        if (controls.SkipDialogue())
            SkipCheck();
    }

    private bool Busy()
    {
        // Are we busy with either dialogue, doing a pan, or moving Gemma or Al?
        if (Al != null)
            return (startedText || startedPan || !playerController.DoneWalking()
                    || currentFlash || !AlScript.DoneFlying() || isFading);
        else
            return (startedText || startedPan || !playerController.DoneWalking()
                    || currentFlash || isFading);
    }

    private void DoAfterText()
    {
        if (speaker)
        {
            var animator = speaker.GetComponent<Animator>();
            if (animator)
            {
                animator.SetBool("Talking", false);
            }
        }

        startedText = false;
        if (currentText != null)
            currentText.gameObject.SetActive(false);
        numTextCurrent += 1;
    }

    // A skip has happened, so if we can skip something to do that and return true, otherwise nothing to skip so false
    private bool SkipCheck()
    {
        // Skip text typewriter effect and display whole text
        if (typewriterText.isTextStarted())
        {
            typewriterText.doSkipText();
            return true;
        }

        // If text has gone through we're waiting for user input to close the text bubble
        if (startedText && !typewriterText.isTextStarted())
        {
            DoAfterText();
            return true;
        }

        // Skip camera panning
        if (startedPan)
        {
            EndPan();
            return true;
        }

        // Skip Gemma walking
        if (!playerController.DoneWalking())
        {
            playerController.SkipWalking();
            return true;
        }

        // Skip Al flying
        if (Al != null && !AlScript.DoneFlying())
        {
            AlScript.SkipFlying();
            return true;
        }

        return false;
    }

    public void DisableControls(bool disable)
    {
        if (disable)
            playerController.StopForCutscene();
        else
            playerController.ResumeAfterCutscene();

        cameraSwitcher.SetCutscene(disable);
        portalManager.DisablePortal(disable);
    }

    public void RunCutscene(TextAsset textFile)
    {
        typewriterText.setTextFile(textFile);
        numTextCurrent = 0;
        numTexts = typewriterText.numDialogsLoaded();

        // Disable player control
        runningCutscene = true;
        DisableControls(true);
    }

    private void EndCutscene()
    {
        // Reset info about the cutscene to a "no cutscene" state
        numTextCurrent = -1;
        numTexts = 0;
        previousPan = new CameraPanInfo(0, null, Gemma, 0f, 0f);
        pans.Clear();
        moves.Clear();

        runningCutscene = false;
        skipEverything = false;

        // Resume player control
        if (!endCutscene)
        {
            cameraTracker.UpdateTarget(Gemma);
            DisableControls(false);
        }

        // If this was the last cutscene in a level do a scene transition now
        else
        {
            sceneTransition.GetComponent<SceneChanger>().SetLoadNextScene();
        }
    }

    public Text DecideSpeaker(string tag)
    {
        tag = tag.ToLower();

        if (skipEverything)
        {
            currentText = null;
            speaker = null;
        }
        else if (tag == "gemma")
        {
            currentText = GemmaText;
            speaker = Gemma;
        }
        else if (tag == "al")
        {
            currentText = AlText;
            speaker = Al;
        }
        else if (tag == "loudspeakerwet")
        {
            currentText = LSWetText;
            speaker = LoudspeakerWet;
        }
        else if (tag == "loudspeakerdry")
        {
            currentText = LSDryText;
            speaker = LoudspeakerDry;
        }
        else
        {
            currentText = null;
            speaker = null;
        }

        if (currentText != null)
            currentText.gameObject.SetActive(true);
        return currentText;
    }

    public void QueueMove(string dest, int tagStart, string mover)
    {
        GameObject target = getLocationFromTag(dest.ToLower());
        if (target != null)
        {
            moves.Enqueue(new MoveInfo(tagStart, target.GetComponent<Transform>().position, mover));
        }
    }

    private void StartMove()
    {
        MoveInfo move = moves.Dequeue();
        string mover = move.mover.ToLower();
        Vector3 target = move.target;

        if (mover == "gemma")
        {
            playerController.WalkToPosition(target);
        }
        else if (mover == "al")
        {
            Debug.Assert(Al != null, "Al not assigned, can't do a move!");
            AlScript.FlyToPosition(target);
        }
        else
        {
            Debug.LogError("Unrecognized mover, not doing move!");
        }
    }

    public void QueuePan(string objName, int tagStart, float delay)
    {
        GameObject to = getLocationFromTag(objName.ToLower());

        // Assume the previous pan to target is where we're panning from
        previousPan = new CameraPanInfo(tagStart, previousPan.to, to, 0f, delay);
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

        // Switch camera scripts
        cameraTracker.enabled = true;
        cameraPanner.enabled = false;
    }

    public void StartAnimation(string name, string animation)
    {
        name = name.ToLower();
        GameObject target = null;

        if (name == "gemma")
            target = Gemma;
        else if (name == "al")
            target = Al;
        else if (name == "loudspeakerwet")
            target = LoudspeakerWet;
        else if (name == "loudspeakerdry")
            target = LoudspeakerDry;

        if (target != null)
            target.GetComponent<Animator>().SetTrigger(animation);
        else
            Debug.LogError("Null animation target. Are you sure object exsits / tag is right?");
    }

    public void QueuePortalEffect()
    {
        doAPortal = true;
    }

    // Do a screen wide portal effect and move Gemma
    private void DoPortalEffect()
    {
        Gemma.transform.position += offs.offset;
        leftBound.transform.position += offs.offset;
        rightBound.transform.position += offs.offset;

        // Make artifact visible as she's picked it up now
        Gemma.transform.FindChild("c_body").FindChild("c_strap").FindChild("arti_handle").gameObject.SetActive(true);

        // Move Al too in the last level
        if (Al != null)
            Al.transform.position += offs.offset;

        currentFlash = Instantiate(portalFlashPrefab);
        var flashPos = Camera.main.transform.position + offs.offset;
        flashPos.z = 0;

        currentFlash.transform.position = flashPos;

        // Scale to screen size
        float camHeight = 2f * Camera.main.orthographicSize;
        float camWidth = camHeight / Screen.height * Screen.width;

        currentFlash.GetComponent<PortalTransferEffect>().startScale = new Vector3(camWidth, camHeight, 1f);

        // Swap backgrounds
        if (dryBG) dryBG.SetActive(!dryBG.activeInHierarchy);
        if (wetBG) wetBG.SetActive(!wetBG.activeInHierarchy);
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
        else if (tag == "al")
            return Al;
        else if (tag == "loudspeakerwet")
            return LoudspeakerWet;
        else if (tag == "loudspeakerdry")
            return LoudspeakerDry;
        else if (tag == "mid1")
            return sceneMid1;
        else if (tag == "mid2")
            return sceneMid2;
        else if (tag == "mid3")
            return sceneMid3;
        else if (tag == "mid4")
            return sceneMid4;
        else if (tag == "mid5")
            return sceneMid5;
        else
            return null;
    }


    private IEnumerator FadeIn(float frameCount)
    {
        // Fade in
        fader.StartFade(Gemma.transform.position, frameCount, true);

        // Lambda wait while we are animating
        yield return new WaitWhile(() => fader.isAnimating());

        // Enable controls if we don't have a cutscene at the start of the level
        if (!runningCutscene)
            DisableControls(false);

        // Done fading
        isFading = false;
    }

    private IEnumerator SkipWholeCutscene()
    {
        SkipCheck();
        DoAfterText();

        for(int i = numTextCurrent; i < numTexts; i++)
        {
            typewriterText.startText(i);
            typewriterText.doSkipText();
            yield return new WaitWhile(() => typewriterText.isTextStarted());
        }

        if (doAPortal)
        {
            doAPortal = false;
            DoPortalEffect();
        }

        while (moves.Count > 0)
        {
            StartMove();
            playerController.SkipWalking();
            if (Al != null)
                AlScript.SkipFlying();
        }

        EndCutscene();
    }

    public void QueueToggleSound(string name)
    {
        soundToggles.Enqueue(name);
    }

    private void ToggleSound(string name)
    {
        // Special case "music" since we need to ask the music manager to start/stop playing
        if (name.Equals("music")) {
            if (musicManager.isPlaying)
                musicManager.stopMusic();
            else
                musicManager.playMusic();

            // We did the toggle
            return;
        }

        // Try the audio sources then
        AudioSource[] sources = cameraTracker.mainCam.GetComponentsInChildren<AudioSource>();
        foreach (AudioSource a in sources)
            if (a.name.Equals(name))
            {
                if (a.isPlaying)
                    a.Stop();
                else
                    a.Play();
                return;
            }

        Debug.LogError("Tried to toggle sound that didn't exist: " + name);
    }
}

public class CameraPanInfo
{
    public int tagStart;
    public GameObject from;
    public GameObject to;
    public float startTime;
    public float duration;

    public CameraPanInfo(int ts, GameObject f, GameObject t, float st, float dt)
    {
        tagStart = ts;
        from = f;
        to = t;
        startTime = st;
        duration = dt;
    }
}

public class MoveInfo
{
    public int tagStart;
    public Vector3 target;
    public string mover;

    public MoveInfo(int ts, Vector3 t, string m)
    {
        tagStart = ts;
        target = t;
        mover = m;
    }
}