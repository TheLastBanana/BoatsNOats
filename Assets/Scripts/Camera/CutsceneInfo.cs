﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneInfo : MonoBehaviour {

    public GameObject CutsceneManager;
    public static int size = 1;
    public TextAsset[] texts = new TextAsset[size];

    private CutsceneManager script;

    // Use this for initialization
    void Start () {
        script = CutsceneManager.GetComponent<CutsceneManager>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            // Start the cutscene indicated in this object and deactivate this object
            script.RunCutscene(size, texts);
            this.gameObject.SetActive(false);
        }
    }
}