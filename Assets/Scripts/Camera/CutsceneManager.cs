using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour {

    public GameObject sceneStart;
    public GameObject sceneTransition;

    public GameObject cameraManager;
    private CameraSwitcher cameraSwitcher;
    private CameraTracker cameraTracker;
    private CameraPanner cameraPanner;
    public GameObject portalManagerObj;
    private PortalManager portalManager;

    public GameObject Gemma;
    private PlayerMovement pm;
    public GameObject Al;

    // Gemma's text stuff
    private Canvas GemmaCanvas;
    private GameObject GemmaTextBubble;
    private Text GemmaText;
    private TypewriterText GemmaTT;

    // Al's text stuff
    private Canvas AlCanvas;
    private GameObject AlTextBubble;
    private Text AlText;
    private TypewriterText AlTT;

    private int currentText;
    private int numTexts;
    private CameraPanInfo currentPan;

    private bool running;
    private bool startedText;
    private bool endCutscene;

	// Use this for initialization
	void Start () {
        cameraSwitcher = cameraManager.GetComponent<CameraSwitcher>();
        cameraTracker = cameraManager.GetComponent<CameraTracker>();
        cameraPanner = cameraManager.GetComponent<CameraPanner>();
        cameraTracker.enabled = true;
        cameraPanner.enabled = false;

        portalManager = portalManagerObj.GetComponent<PortalManager>();

        pm = Gemma.GetComponent<PlayerMovement>();

        // Grab Gemma's text stuff
        if (Gemma != null)
        {
            GemmaCanvas = Gemma.GetComponentInChildren<Canvas>(true);
            GemmaTextBubble = GemmaCanvas.transform.FindChild("GemmaTextBubble").gameObject;
            GemmaText = GemmaTextBubble.GetComponentInChildren<Text>(true);
            GemmaTT = GemmaText.GetComponent<TypewriterText>();
            GemmaTextBubble.SetActive(false);
        }

        // Grab Al's text stuff
        if (Al != null)
        {
            AlCanvas = Al.GetComponentInChildren<Canvas>(true);
            AlTextBubble = AlCanvas.transform.FindChild("AlTextBubble").gameObject;
            AlText = AlTextBubble.GetComponentInChildren<Text>(true);
            AlTT = AlText.GetComponent<TypewriterText>();
            AlTextBubble.SetActive(false);
        }

        currentText = 0;
        numTexts = 0;
        currentPan = new CameraPanInfo(null, Gemma, 0f, 0f);

        running = false;
        startedText = false;
        endCutscene = false;
	}
	
	// Update is called once per frame
	void Update () {
        // Check if we've gone through all of the texts
        if (currentText == numTexts)
            EndCutscene();

        // We're not in a cutscene
        if (!running)
            return;

        // Start the next text if we're not currently doing one and the previous has been finished
        if (!GemmaTT.isTextDone() && !startedText)
        {
            startedText = true;
            GemmaTextBubble.SetActive(true);
            GemmaTT.startText(currentText);
        }

        // If the text has gone through, wait for the player to hit enter before finishing the text
        if (startedText && !GemmaTT.isTextDone() && Input.GetKeyDown(KeyCode.Return))
        {
            startedText = false;
            GemmaTextBubble.SetActive(false);
            currentText += 1;
        }

    }

    public void RunCutscene (TextAsset textFile)
    {
        // TODO: Set up properly to handle more than just Gemma talking
        // TODO: Utilize custom pan tag to handle panning
        if (GemmaTT != null)
            GemmaTT.setText(textFile);
        if (AlTT != null)
            AlTT.setText(textFile);

        currentText = 0;
        numTexts = GemmaTT.numDialogsLoaded();

        running = true;
        pm.StopForCutscene();
        cameraSwitcher.SetCutscene(true);
        portalManager.SetCutscene(true);
    }

    private void EndCutscene()
    {
        running = false;
        pm.ResumeAfterCutscene();
        cameraSwitcher.SetCutscene(false);
        cameraTracker.UpdateTarget(Gemma);
        portalManager.SetCutscene(false);

        if (endCutscene)
            sceneTransition.GetComponent<SceneChanger>().LoadNextScene();
    }

    public void StartPan(string objName, float delay)
    {
        objName = objName.ToLower();
        GameObject to = null;

        // Valid pans
        if (objName == "start")
            to = sceneStart;
        else if (objName == "end")
            to = sceneTransition;
        else if (objName == "gemma")
            to = Gemma;

        // Assume the old pan to target is where we're panning from
        currentPan = new CameraPanInfo(currentPan.to, to, Time.time, delay);

        cameraTracker.enabled = false;
        cameraTracker.UpdateTarget(to);
        cameraPanner.enabled = true;
        cameraPanner.currentPan = currentPan;
    }

    public void EndPan()
    {
        cameraTracker.enabled = true;
        cameraPanner.enabled = false;
    }

    // Called to signify the cutscene is the last in the level and we need to level change after
    public void IsEndCutscene()
    {
        endCutscene = true;
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