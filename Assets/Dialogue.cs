﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// Based on script from video https://www.youtube.com/watch?v=ehmBIP5sj0M
// Accessed February 8th, 2017
public class Dialogue : MonoBehaviour {

    public GameObject textBubble;
    public Text theText;

    private static int size = 12;
    public string[] textLines = new string[size];
    private int currentLine = 0;
    static public bool doneDialogue;

	// Use this for initialization
	void Start () {
        doneDialogue = false;
	}

    // Update is called once per frame
    void Update() {
        if (!doneDialogue)
        {
            string currentText = textLines[currentLine];

            if (currentLine >= size || currentText == "")
            {
                doneDialogue = true;
                textBubble.SetActive(false);
                theText.text = "";
            }
            else
            {
                theText.text = currentText;
            }


            if (Input.GetKeyDown(KeyCode.Return))
            {
                currentLine += 1;
            }
        }
    }
}
