using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class logoscript : MonoBehaviour {

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
            SceneManager.LoadScene(0);
        }
    }

    public void startfade()
    {
        gameObject.SetActive(true);
        
    }
}
