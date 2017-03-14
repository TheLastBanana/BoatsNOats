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
    private GameObject panTo;

    private bool running;
    private bool startedText;
    private bool firstText;

	// Use this for initialization
	void Start () {
        GemmaTT = GemmaText.GetComponent<TypewriterText>();
        GemmaTextBubble.SetActive(false);

        currentText = 0;
        numTexts = 0;

        running = false;
        startedText = false;
        firstText = false;
	}
	
	// Update is called once per frame
	void Update () {
        // Check if we've gone through all of the texts
        if (currentText == numTexts)
            running = false;

        // We're not in a cutscene
        if (!running)
            return;

        if (!GemmaTT.isTextDone() && !startedText)
        {
            if (firstText || Input.GetKeyDown(KeyCode.Return))
            {
                firstText = false;
                startedText = true;
                GemmaTextBubble.SetActive(true);
                GemmaTT.startText(currentText);
            }
        }

        if (startedText && !GemmaTT.isTextDone() && Input.GetKeyDown(KeyCode.Return))
        {
            startedText = false;
            GemmaTextBubble.SetActive(false);
            currentText += 1;
        }

    }

    public void RunCutscene (int NumTexts, TextAsset[] Texts, GameObject PanTo)
    {
        for (int i = 0; i < NumTexts; i++)
        {
            GemmaTT.setText(Texts[i]);
        }

        numTexts += NumTexts;
        panTo = PanTo;
        running = true;
        firstText = true;
    }
}
