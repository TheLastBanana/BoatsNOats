using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour {

    public GameObject sceneStart;

    private PlayerMovement pm;

    public GameObject Gemma;
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

    private bool running;
    private bool startedText;

	// Use this for initialization
	void Start () {
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

        running = false;
        startedText = false;
	}
	
	// Update is called once per frame
	void Update () {
        // Check if we've gone through all of the texts
        if (currentText == numTexts)
        {
            running = false;
            pm.ResumeAfterCutscene();
        }

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
        GemmaTT.setText(textFile);

        currentText = 0;
        numTexts = GemmaTT.numDialogsLoaded();

        running = true;
        pm.StopForCutscene();
    }
}
