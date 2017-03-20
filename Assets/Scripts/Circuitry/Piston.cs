using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piston : MonoBehaviour
{

    public GameObject head;
    public GameObject rod;
    public GameObject bottom;
    public float maxHeight;

    private Vector3 botSize;
    private Vector3 rodSize;
    private Vector3 headSize;
    private float headMin;
    private float speed;

    private void Awake()
    {
        // Get sprite sizes
        botSize = bottom.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size;
        rodSize = rod.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size;
        headSize = head.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size;

        // Head minimum
        headMin = botSize.y / 2 + headSize.y / 2;

        // Speed is arbitrarily a fifth of the head size I guess
        speed = headSize.y / 5;
    }

    // Update is called once per frame
    void Update ()
    {
        float posDelta = 0;

        //Reset box collider offset
        head.transform.GetChild(0).GetComponent<BoxCollider2D>().offset = new Vector2(0, 0);

        //Use base.GetComponent<Circuit>().powered to use the power from a circuit
        //if (Input.GetKey(KeyCode.RightBracket) && rod.transform.localScale.y < maxHeight)
        if (bottom.GetComponent<Circuit>().powered && rod.transform.localScale.y < maxHeight)
        {
            posDelta = speed;
            //if head is going up account for character feet sinking by offsetting box collider
            head.transform.GetChild(0).GetComponent<BoxCollider2D>().offset = new Vector2(0,2);
        }
        else if (!bottom.GetComponent<Circuit>().powered && head.transform.localPosition.y > headMin)
            posDelta = -speed;

        if (posDelta != 0)
        {
            Vector3 headPos = head.transform.localPosition;
            headPos.y += posDelta;
            head.transform.localPosition = headPos;

            Debug.Log(headPos.y + " " + headMin + " " + (headPos.y - headMin) + " " + rodSize.y + " " + (headPos.y - headMin) / rodSize.y);
            Vector3 rodScale = rod.transform.localScale;
            rodScale.y = (headPos.y - headMin) / rodSize.y;
            rod.transform.localScale = rodScale;
        }
	}
}
