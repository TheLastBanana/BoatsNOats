using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class titlescript : MonoBehaviour {


    float currentTime = 0f;
    float timeToMove = 3f;
    Color imageAlpha;
    // Use this for initialization
    void Start () {
        imageAlpha = GetComponent<Image>().color;
    }
    
    // Update is called once per frame
    void Update () {
        if (currentTime <= timeToMove)
        {
            currentTime += Time.deltaTime;
            imageAlpha.a = Mathf.Lerp(1,0, currentTime / timeToMove);
            GetComponent<Image>().color = imageAlpha;

        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
