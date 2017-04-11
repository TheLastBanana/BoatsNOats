using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class logoscript : MonoBehaviour {

    private bool fadein = false;
    float currentTime = 0f;
    float timeToMove = 3f;

    // Use this for initialization
    void Start () {
	    	
	}
	
	// Update is called once per frame
	void Update () {
        if (currentTime <= timeToMove)
        {
            currentTime += Time.deltaTime;
            GetComponent<Image>().color = Color.Lerp(Color.black, Color.white, currentTime / timeToMove);
        }
        else
        {
            //gameObject.SetActive(false);
        }
    }

    public void startfade()
    {
        gameObject.SetActive(true);
        fadein = true;
    }
}
