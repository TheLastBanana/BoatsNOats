using UnityEngine;

[RequireComponent (typeof (Circuit))]
public class CircuitSpriteChanger : MonoBehaviour
{
    public GameObject offSprite;
    public GameObject onSprite;

    Circuit circuit;
    bool wasPowered;

    void Awake()
    {
        circuit = GetComponent<Circuit>();
    }

    void Update()
    {
        if (wasPowered == circuit.powered) return;

        if (onSprite != null) onSprite.SetActive(circuit.powered);
        if (offSprite != null) offSprite.SetActive(!circuit.powered);

        wasPowered = circuit.powered;
    }
}
