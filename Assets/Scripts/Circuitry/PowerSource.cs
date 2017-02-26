using UnityEngine;

public class PowerSource : MonoBehaviour
{
    public bool startsOn = false;
    public int groupId = 0;

    bool _isOn = false;
    public bool isOn
    {
        get
        {
            return _isOn;
        }

        set
        {
            _isOn = value;
            CircuitManager.RecalculatePower(groupId);
        }
    }

    void Awake()
    {
        _isOn = startsOn;
    }

    void Update()
    {
        // The power source breaks if it's split
        if (GetComponent<Splittable>().isSplit)
        {
            isOn = false;
            Destroy(this);
        }
    }
}
