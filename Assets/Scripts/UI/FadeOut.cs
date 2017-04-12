using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeOut : MonoBehaviour
{
    public GameObject fadeBox;
    private SpriteRenderer boxRen;
    private Camera parentCamera;

    private bool animate = false;
    private bool wasAnimating = false;
    private bool fadeIn = false;
    private float curFrame = 0;
    private float frameCount = 256;


    // Use this for initialization
    void Awake()
    {
        parentCamera = GetComponent<Camera>();
        boxRen = fadeBox.GetComponent<SpriteRenderer>();
        
        // Rescale
        float camHeight = 2f * parentCamera.orthographicSize;
        float camWidth = camHeight / Screen.height * Screen.width;
        Vector3 extents = boxRen.bounds.extents;
        float xScale = camWidth / extents.x / 2;
        float yScale = camHeight / extents.y / 2;

        // Make slightly larger to account for sliding when gemma pauses
        xScale *= 1.1f;
        yScale *= 1.1f;

        // Apply scale
        fadeBox.transform.localScale = new Vector3(xScale, yScale, 1);
    }

    void Update()
    {
        if (animate)
        {
            if (!wasAnimating)
            {
                // Enable the renderer
                boxRen.enabled = true;
                wasAnimating = true;
            }

            // Make new position
            Vector3 newPos = parentCamera.transform.position;
            newPos.z = fadeBox.transform.position.z;
            fadeBox.transform.position = newPos;

            // Fade
            Color boxColor = boxRen.color;
            if (fadeIn)
                boxColor.a = Mathf.Lerp(1, 0, curFrame / frameCount);
            else
                boxColor.a = Mathf.Lerp(0, 1, curFrame / frameCount);
            boxRen.color = boxColor;
            
            // Reset
            if (curFrame >= frameCount)
            {
                // If it was fading in we probably don't want the box to
                // render anymore
                if (fadeIn)
                    boxRen.enabled = false;
                wasAnimating = false;
                animate = false;
            }

            // Increment frame
            ++curFrame;
        }
    }

    // THIS IS THE PUBLIC INTERFACE
    public void StartFade(Vector2 pos, float count, bool fadeIn)
    {
        frameCount = count;
        this.fadeIn = fadeIn;
        animate = true;
        curFrame = 0;
    }

    public bool isAnimating()
    {
        return animate;
    }
}
