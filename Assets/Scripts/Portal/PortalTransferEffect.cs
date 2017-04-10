using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalTransferEffect : MonoBehaviour
{
    public float planeSize = 10.0f;
    public float endScaleMultiplier = 5.0f;
    public float expandTime = 0.5f;
    public float lifeTime = 1.0f;
    public float curveStrength = 1.0f;
    float startTime;
    Color startColor;

    Vector3 _startScale = new Vector3(1f, 1f, 1f);
    public Vector3 startScale
    {
        get
        {
            return _startScale;
        }

        set
        {
            _startScale = new Vector3(value.x, value.z, value.y);
            transform.localScale = _startScale / planeSize;
        }
    }

    void Awake()
    {
        startColor = GetComponent<Renderer>().material.color;
    }

    void Start()
    {
        startTime = Time.time;
    }

    void Update()
    {
        var timeDiff = Time.time - startTime;

        // Apply a curve to get a nicer effect
        var lerpVal = Mathf.Pow(timeDiff / expandTime, curveStrength);

        transform.localScale = Vector3.Lerp(
            startScale / planeSize,
            startScale * endScaleMultiplier / planeSize,
            lerpVal
        );

        var color = startColor;
        startColor.a = 1f - lerpVal;
        GetComponent<Renderer>().material.color = color;

        // Destroy when life time is up
        if (timeDiff > lifeTime)
            Destroy(gameObject);
    }
}
