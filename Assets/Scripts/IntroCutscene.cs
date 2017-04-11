using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroCutscene : MonoBehaviour {

    public PlayerController Gemma;
    public GameObject gemmaTarget;
    public Al Al;
    public GameObject alTarget;
    public PortalManager portalManager;
    public GameObject title;
    public GameObject startButton;
    public CameraSwitcher camSwitcher;

    private bool playOnce = false;
    private bool gemmaWalking = false;
    // Use this for initialization
	void Start () {
        Gemma = Gemma.GetComponent<PlayerController>();
        StartCoroutine(Waitforscreen());
        camSwitcher.SetCutscene(true);
    }
	
	// Update is called once per frame
	void Update () {
    }

    IEnumerator Waitforscreen()
    {
        //Wait this long until Gemma starts
        yield return new WaitForSeconds(2);
        if (!playOnce)
        {
            Gemma.FlyToPosition(gemmaTarget.transform.position);
            while (!Gemma.DoneWalking())
            {
                yield return null;
            }
            yield return new WaitForSeconds(2);
            Al.FlyToPosition(alTarget.transform.position);
            while (!Al.DoneFlying())
            {
                yield return null;
            }
            portalManager.initiatePortal();
            yield return new WaitForSeconds(1);
            AudioSource audio = GetComponent<AudioSource>();
            audio.Play();
            portalManager.endSelection(false);
            title.SetActive(true);
            startButton.SetActive(true);
            playOnce = true;
            
        }
    }
}
