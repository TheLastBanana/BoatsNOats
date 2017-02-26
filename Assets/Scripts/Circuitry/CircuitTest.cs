using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitTest : MonoBehaviour
{
    void Update()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            var child = transform.GetChild(i);

            // Don't recolor circuitry
            if (child.gameObject.layer == LayerMask.NameToLayer("Circuitry")) continue;

            Material mat = child.GetComponent<Renderer>().material;
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
}
