using UnityEngine;

[RequireComponent (typeof (Circuit))]
public class CircuitSpriteChanger : MonoBehaviour
{
    Circuit circuit;
    bool wasPowered;

    void Awake()
    {
        circuit = GetComponent<Circuit>();
    }

    void Update()
    {
        if (wasPowered == circuit.powered) return;

        for (int i = 0; i < transform.childCount; ++i)
        {
            var child = transform.GetChild(i);
            if (child.tag == "Powered On") child.gameObject.SetActive(circuit.powered);
            if (child.tag == "Powered Off") child.gameObject.SetActive(!circuit.powered);
        }

        wasPowered = circuit.powered;
    }
}
