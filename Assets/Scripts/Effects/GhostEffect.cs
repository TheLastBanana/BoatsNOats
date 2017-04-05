using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostEffect : MonoBehaviour
{
    public Shader customShader;

    public void Awake()
    {
        GetComponent<Camera>().SetReplacementShader(customShader, null);
    }
}
