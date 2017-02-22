using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitTest : MonoBehaviour
{
    void Update()
    {
        Material mat = transform.GetChild(0).GetComponent<Renderer>().material;
        if (GetComponent<Circuit>().powered)
        {
            mat.color = new Color(0, 1, 0);
        }
        else
        {
            mat.color = new Color(1, 0, 0);
        }
    }
}
