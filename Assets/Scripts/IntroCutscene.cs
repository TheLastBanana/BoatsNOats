﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroCutscene : MonoBehaviour {

    public PlayerController Gemma;
    public GameObject target;

    private bool testvar = false;
	// Use this for initialization
	void Start () {
        Gemma = Gemma.GetComponent<PlayerController>();
        
    }
	
	// Update is called once per frame
	void Update () {
        if (!testvar)
        {
            Gemma.walkToPosition(target.transform.position);
            testvar = true;
        }

        if(Gemma.transform.position.x > target.transform.position.x)
        {
            Gemma.normalizedHorizontalSpeed = 0;
        }

    }
}
