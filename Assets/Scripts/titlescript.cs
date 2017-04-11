using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class titlescript : MonoBehaviour {


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
            GetComponent<Image>().color = Color.Lerp(Color.white, Color.black, currentTime / timeToMove);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
