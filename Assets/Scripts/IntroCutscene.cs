using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroCutscene : MonoBehaviour {

    public PlayerController Gemma;
    public GameObject gemmaTarget;
    public Al Al;
    public GameObject alTarget;

    private bool playOnce = false;
	// Use this for initialization
	void Start () {
        Gemma = Gemma.GetComponent<PlayerController>();
        StartCoroutine(Waitforscreen());
        Al.FlyToPosition(alTarget.transform.position);
    }
	
	// Update is called once per frame
	void Update () {
    }
    IEnumerator Waitforscreen()
    {
        yield return new WaitForSeconds(5);
        if (!playOnce)
        {
            Gemma.WalkToPosition(gemmaTarget.transform.position);
            playOnce = true;
        }
    }
}
