using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour {

    public GameObject sceneStart;

    public GameObject Gemma;
    public GameObject GemmaTextBubble;
    public Text GemmaText;

    private TypewriterText GemmaTT;

    private int currentText;
    private int numTexts;

    private bool running;
    private bool startedText;

	// Use this for initialization
	void Start () {
        GemmaTT = GemmaText.GetComponent<TypewriterText>();
        GemmaTextBubble.SetActive(false);

        currentText = 0;
        numTexts = 0;

        running = false;
        startedText = false;
	}
	
	// Update is called once per frame
	void Update () {
        // Check if we've gone through all of the texts
        if (currentText == numTexts)
            running = false;

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

    public void RunCutscene (int NumTexts, TextAsset[] Texts)
    {
        // TODO: Set up properly to handle more than just Gemma talking
        // TODO: Utilize custom pan tag to handle panning
        GemmaTT.setText(Texts[0]);

        currentText = 0;
        numTexts = GemmaTT.numDialogsLoaded();
        running = true;
    }
}
