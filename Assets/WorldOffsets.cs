using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldOffsets : MonoBehaviour {
    public int x, y; // Offset to alt world

    public Vector3 vec3
    {
        get { return new Vector3(x, y, 0); }
    }
}
