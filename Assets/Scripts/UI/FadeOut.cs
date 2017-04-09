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
    private float curFrame = 0;
    private const float frameCount = 256;


    // Use this for initialization
    void Awake()
    {
        parentCamera = GetComponent<Camera>();
        boxRen = fadeBox.GetComponent<SpriteRenderer>();
        
        // Rescale
        float camHeight = 2f * parentCamera.orthographicSize;
        float camWidth = camHeight * parentCamera.aspect;
        Vector3 extents = boxRen.bounds.extents;
        float xScale = camWidth / extents.x / 2;
        float yScale = camHeight / extents.y / 2;
        fadeBox.transform.localScale = new Vector3(xScale, yScale, 1);
    }

    void Update()
    {
        if (animate)
        {
            if (!wasAnimating)
            {
                // Make new position
                Vector3 newPos = parentCamera.transform.position;
                newPos.z = fadeBox.transform.position.z;
                fadeBox.transform.position = newPos;

                // Enable the renderer
                boxRen.enabled = true;
                wasAnimating = true;
            }

            // Fade
            Color boxColor = boxRen.color;
            boxColor.a = Mathf.Lerp(0, 1, curFrame / frameCount);
            boxRen.color = boxColor;
            
            // Reset
            if (curFrame == frameCount)
            {
                boxRen.enabled = false;
                wasAnimating = false;
                animate = false;
            }

            // Increment frame
            ++curFrame;
        }
    }

    // THIS IS THE PUBLIC INTERFACE
    public void StartFadeOut(Vector2 pos)
    {
        animate = true;
    }

    public bool isAnimating()
    {
        return animate;
    }
}
