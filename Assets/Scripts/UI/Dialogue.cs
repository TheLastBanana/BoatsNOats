using System.Collections;
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

    static private bool doneDialogue = false;
    static private bool cameraReset = true;
    static private bool cameraPanning = false;

    public float panTime = 2.0f;  // Time it will take to pan from one place to another
    private int panState = 0;  // To keep track of where we are in a pan cutscene
    public GameObject zoom1 = null;
    private int zoom1AtLine = 1;

    static private CameraPanInfo cameraPanInfo;

	// Use this for initialization
	void Start () {
        
	}

    // Update is called once per frame
    void Update() {
        if (getDoneDialogue())
            return;

        if (zoom1AtLine == currentLine && panState == 0 && zoom1 != null)
        {
            setPanning(true);
            cameraPanInfo = new CameraPanInfo(null, zoom1, Time.time, panTime);
            panState = 1;
        }

        // Camera currently panning to
        if (panState == 1)
        {
            if (getPanning())
                return;
            else
                panState = 2;
        }

        // Waiting for player input
        if (panState == 2)
        {
            if (!Input.GetKeyDown(KeyCode.Return))
            {
                return;
            }
            else
            {
                setPanning(true);
                cameraPanInfo = new CameraPanInfo(zoom1, null, Time.time, panTime);
                panState = 3;
            }
        }

        // Camera currently panning from
        if (panState == 3)
        {
            if (getPanning())
                return;
            else
                panState = 4;
        }

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
            if (panState == 4)
                panState = 0;
        }
    }

    static public bool getDoneDialogue()
    {
        return doneDialogue;
    }

    static public void setCameraReset(bool reset)
    {
        cameraReset = reset;
    }

    static public bool getCameraReset()
    {
        return cameraReset;
    }

    static public void setPanning(bool pan)
    {
        cameraPanning = pan;
    }

    static public bool getPanning()
    {
        return cameraPanning;
    }

    static public CameraPanInfo getCameraPanInfo()
    {
        return cameraPanInfo;
    }
}

public class CameraPanInfo : MonoBehaviour
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