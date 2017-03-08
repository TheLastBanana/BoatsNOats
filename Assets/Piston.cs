﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piston : MonoBehaviour
{

    public GameObject head;
    public GameObject rod;
    public GameObject bottom;
    private const int dim = 256; // Size of square in pixels
    private const float unityDim = dim / 100f;
    private const float headScale = .2f;
    private const float speed = unityDim / 20;
    private const float scaleSpeed = unityDim / speed / 100 / 4; // "uD / speed" is percent to scale by.
                                                                 // ".. / 100" turns into a decimal
    private const float headMin = unityDim / 2 + unityDim * headScale / 2;
	
	// Update is called once per frame
	void Update ()
    {
        float posDelta = 0;
        if (Input.GetKey(KeyCode.RightBracket))
            posDelta = speed;
        else if (Input.GetKey(KeyCode.LeftBracket) && head.transform.localPosition.y > headMin)
            posDelta = -speed;

        float scaleDelta = 0;
        if (Input.GetKey(KeyCode.RightBracket))
            scaleDelta = scaleSpeed;
        else if (Input.GetKey(KeyCode.LeftBracket) && rod.transform.localScale.y > 0)
            scaleDelta = -scaleSpeed;

        if (posDelta != 0)
        {
            Vector3 headPos = head.transform.localPosition;
            Vector3 rodPos = rod.transform.localPosition;
            headPos.y += posDelta;
            rodPos.y += posDelta / 2; // Only move half as much, scaling adds to both sides
            head.transform.localPosition = headPos;
            rod.transform.localPosition = rodPos;
            
            Vector3 rodScale = rod.transform.localScale;
            rodScale.y += scaleDelta;
            rod.transform.localScale = rodScale;
        }
	}
}