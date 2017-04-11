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
    public GameObject sceneMid4;
    public GameObject sceneMid5;

    public GameObject cameraManager;
    private CameraSwitcher cameraSwitcher;
    private CameraTracker cameraTracker;
    private CameraPanner cameraPanner;
    public PortalManager portalManager;
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

    private Text currentText;

    private int numTextCurrent;
    private int numTexts;
    private CameraPanInfo previousPan;

    private bool runningCutscene;
    private bool startedText;
    Queue<CameraPanInfo> pans;
    Queue<MoveInfo> moves;
    private bool startedPan;
    private bool doAPortal;
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
        startedPan = false;
        doAPortal = false;
        endCutscene = false;
    }
	
	// Update is called once per frame
	void Update () {
        // Check if we've gone through everything
        if (numTextCurrent == numTexts && pans.Count <= 0 && moves.Count <= 0 && !Busy())
            EndCutscene();

        // We're not in a cutscene
        if (!runningCutscene)
            return;

        if (!Busy())
        {
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
            else if (!typewriterText.isTextDone())
            {
                startedText = true;
                typewriterText.startText(numTextCurrent);
            }
        }

        // If the text has gone through, wait for the player to hit a skip key before finishing the text
        // OR if there's no speaker then just auto skip
        if (startedText && !typewriterText.isTextDone() && (controls.SkipDialogue() || !typewriterText.hasSpeaker(numTextCurrent)))
        {
            startedText = false;
            if (currentText != null)
                currentText.gameObject.SetActive(false);
            numTextCurrent += 1;
        }

        if (controls.SkipDialogue())
        {
            // Let the player skip a pan
            if (startedPan)
                EndPan();

            if (!playerController.DoneWalking())
                playerController.SkipWalking();

            if (Al != null && !AlScript.DoneFlying())
                AlScript.SkipFlying();
         }
    }

    private bool Busy()
    {
        // Are we busy with either dialogue, doing a pan, or moving Gemma or Al?
        if (Al != null)
            return (startedText || startedPan || !playerController.DoneWalking() || !AlScript.DoneFlying());
        else
            return (startedText || startedPan || !playerController.DoneWalking());
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

        if (tag == "gemma")
            currentText = GemmaText;
        else if (tag == "al")
            currentText = AlText;
        else if (tag == "loudspeakerwet")
            currentText = LSWetText;
        else if (tag == "loudspeakerdry")
            currentText = LSDryText;
        else
            currentText = null;

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
        // TODO: Portal effect + move Gemma

        // TODO: Add to Busy() return a check for being done here
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