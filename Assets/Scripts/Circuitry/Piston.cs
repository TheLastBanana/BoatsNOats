using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piston : MonoBehaviour
{

    public GameObject head;
    public GameObject rod;
    public GameObject bottom;
    public float maxHeight;
    public float hackAmount = 1.5f;
    private const int dim = 256; // Size of square in pixels
    private const float unityDim = dim / 100f;
    private const float headScale = .1f;
    private const float speed = unityDim / 20;
    private const float scaleSpeed = unityDim / speed / 100 / 4; // "uD / speed" is percent to scale by.
                                                                 // ".. / 100" turns into a decimal
    private const float headMin = unityDim / 2 + unityDim * headScale / 2;

	// Update is called once per frame
	void Update ()
    {
        float posDelta = 0;

        //Reset box collider offset
        head.transform.GetChild(0).GetComponent<PolygonCollider2D>().offset = new Vector2(0, 0);
        
        //Use base.GetComponent<Circuit>().powered to use the power from a circuit
        //if (Input.GetKey(KeyCode.RightBracket) && rod.transform.localScale.y < maxHeight)
        if (bottom.GetComponent<Circuit>().powered && rod.transform.localScale.y < maxHeight)
        {
            posDelta = speed;
            //if head is going up account for character feet sinking by offsetting box collider
            head.transform.GetChild(0).GetComponent<PolygonCollider2D>().offset = new Vector2(0,2);
        }
            
        else if (!bottom.GetComponent<Circuit>().powered && head.transform.localPosition.y > headMin)
            posDelta = -speed;

        float scaleDelta = 0;
        //if (Input.GetKey(KeyCode.RightBracket) && rod.transform.localScale.y < maxHeight)
        if (bottom.GetComponent<Circuit>().powered && rod.transform.localScale.y < maxHeight)
            scaleDelta = scaleSpeed;
        //else if (Input.GetKey(KeyCode.LeftBracket) && rod.transform.localScale.y > 0)
        else if (!bottom.GetComponent<Circuit>().powered && rod.transform.localScale.y > 0)
            scaleDelta = -scaleSpeed;

        if (posDelta != 0)
        {
            Vector3 headPos = head.transform.localPosition;
            Vector3 rodPos = rod.transform.localPosition;
            headPos.y += posDelta * hackAmount;
            rodPos.y += posDelta / 2; // Only move half as much, scaling adds to both sides
            head.transform.localPosition = headPos;
            rod.transform.localPosition = rodPos;
            
            Vector3 rodScale = rod.transform.localScale;
            rodScale.y += scaleDelta;
            rod.transform.localScale = rodScale;
        }
	}
}
