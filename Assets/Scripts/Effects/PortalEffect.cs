using UnityEngine;
using System.Collections.Generic;

public class PortalEffect : MonoBehaviour
{
    public GameObject edgePrefab;
    public GameObject middlePrefab;
    public Rect portalShape;
    public float sizeSpeedCurve = 0.8f;
    public float particleIntensity = 1f;
    public float minBorderThickness = 0.1f;
    public float maxBorderThickness = 0.2f;
    public float waveThickness = 0.2f;
    public float changeRate = 0.1f;
    public float wavinessAreaThreshold = 1.0f;

    public Material defaultMaterial;
    public Material blockedMaterial;

    ParticleSystem middle;
    float middleBaseRate;

    Transform[] transforms = new Transform[4];
    ParticleSystem[][] edgeSystems = new ParticleSystem[4][];
    ParticleSystem.MainModule[][] mains = new ParticleSystem.MainModule[4][];
    ParticleSystem.EmissionModule[][] emissions = new ParticleSystem.EmissionModule[4][];
    ParticleSystem.ShapeModule[][] shapes = new ParticleSystem.ShapeModule[4][];
    float[] edgeBaseRates;
    float[] edgeBaseSpeeds;

    const int numBorderVerts = 20;
    const int numBorderTris = 16;

    bool _blocked;
    public bool blocked
    {
        get
        {
            return _blocked;
        }

        set
        {
            _blocked = value;

            var renderer = GetComponent<MeshRenderer>();
            if (_blocked) renderer.material = blockedMaterial;
            else renderer.material = defaultMaterial;
        }
    }

    void Awake()
    {
        SetUpParticles();
        SetUpMesh();

        Disable();
    }

    void Update()
    {
        UpdateParticles();
        UpdateMesh();
    }

    // Clear all particle systems
    public void Clear()
    {
        middle.Clear();
        foreach (var systemList in edgeSystems)
            foreach (var system in systemList)
                system.Clear();
    }

    // Enable emissions and border
    public void Enable()
    {
        EnableDisable(true);
    }

    // Disable emissions and border
    public void Disable()
    {
        EnableDisable(false);
    }

    // Enable/disable the effect
    void EnableDisable(bool enable)
    {
        Clear();
        GetComponent<MeshRenderer>().enabled = enable;

        var middleEmission = middle.emission;
        middleEmission.enabled = enable;

        for (int i = 0; i < 4; ++i)
            for (int j = 0; j < edgeBaseRates.Length; ++j)
                emissions[i][j].enabled = enable;
    }

    // Set up the particle systems
    void SetUpParticles()
    {
        for (int i = 0; i < 4; ++i)
        {
            var newObj = transforms[i] = Instantiate(edgePrefab, transform).transform;
            transforms[i].eulerAngles = new Vector3(-90 * i, 90, 0);

            var systems = edgeSystems[i] = newObj.GetComponentsInChildren<ParticleSystem>();
            shapes[i] = new ParticleSystem.ShapeModule[systems.Length];
            emissions[i] = new ParticleSystem.EmissionModule[systems.Length];
            mains[i] = new ParticleSystem.MainModule[systems.Length];

            for (int j = 0; j < systems.Length; ++j)
            {
                var system = systems[j];

                shapes[i][j] = system.shape;
                emissions[i][j] = system.emission;
                mains[i][j] = system.main;
            }
        }

        // Determine base spawn rates for edge systems
        edgeBaseRates = new float[emissions[0].Length];
        edgeBaseSpeeds = new float[emissions[0].Length];
        for (int i = 0; i < edgeBaseRates.Length; ++i)
        {
            edgeBaseRates[i] = emissions[0][i].rateOverTimeMultiplier;
            edgeBaseSpeeds[i] = mains[0][i].startSpeedMultiplier;
        }

        middle = Instantiate(middlePrefab, transform).GetComponent<ParticleSystem>();
        middle.transform.localPosition = new Vector3();
        middleBaseRate = middle.emission.rateOverTimeMultiplier;
    }

    // Update particle systems based on portal size
    void UpdateParticles()
    {
        float w = Mathf.Abs(portalShape.width);
        float h = Mathf.Abs(portalShape.height);

        // Update edge sizes
        for (int i = 0; i < edgeBaseRates.Length; ++i)
        {
            shapes[0][i].radius = shapes[2][i].radius = h / 2;
            shapes[1][i].radius = shapes[3][i].radius = w / 2;
        }

        var middleShape = middle.shape;
        middleShape.box = new Vector3(w, h);

        // Move to encompass rectangle
        transforms[0].localPosition = new Vector2(w / 2, 0);
        transforms[1].localPosition = new Vector2(0, h / 2);
        transforms[2].localPosition = new Vector2(-w / 2, 0);
        transforms[3].localPosition = new Vector2(0, -h / 2);

        // Scale emission rate and speed to size, so we get the same relative coverage
        var minRadius = Mathf.Min(shapes[0][0].radius, shapes[1][0].radius);
        for (int i = 0; i < 4; ++i)
            for (int j = 0; j < edgeBaseRates.Length; ++j)
            {
                emissions[i][j].rateOverTimeMultiplier =
                    shapes[i][j].radius == 0f ? 0f : particleIntensity * shapes[i][j].radius * edgeBaseRates[j];
                
                // In this case, we want to scale to the minimum radius to avoid firing particles right out of thin/wide portals
                mains[i][j].startSpeedMultiplier =
                    shapes[i][j].radius == 0f ? 0f : particleIntensity * Mathf.Pow(minRadius, sizeSpeedCurve) * edgeBaseSpeeds[j];
            }

        // Middle is a square, so use area to scale emission rate
        var middleEmission = middle.emission;
        middleEmission.rateOverTime = middleBaseRate * w * h * particleIntensity;

        // Move this into position
        transform.position = portalShape.center;
    }

    // Set up the portal border mesh
    void SetUpMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = new Vector3[numBorderVerts];
        mesh.normals = new Vector3[numBorderVerts];
        mesh.triangles = new int[numBorderTris * 3]
        {
            0, 1, 2,
            1, 3, 2,
            2, 4, 8,
            4, 6, 8,
            5, 3, 7,
            3, 9, 7,
            8, 9, 10,
            9, 11, 10,

            12, 13, 0,
            13, 1, 0,
            14, 0, 10,
            14, 10, 15,
            1, 16, 17,
            1, 17, 11,
            10, 11, 19,
            10, 19, 18
        };

        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Update the portal border mesh based on portal size
    void UpdateMesh()
    {
        float animTime = Time.time * changeRate;

        // If we're below this threshold, be less wavy
        float waviness = Mathf.Clamp01(Mathf.Abs(portalShape.width * portalShape.height / wavinessAreaThreshold));

        // Modulate the border thickness. Match it up with the cycle of the edge points
        // (sin(x) + 1) / 2 ranges between 0 and 1
        float thicknessMod = (Mathf.Sin(animTime * Mathf.PI * 4) * waviness + 1f) / 2f;
        float borderThickness = Mathf.Lerp(minBorderThickness, maxBorderThickness, thicknessMod);

        // Inner and outer rectangles of the rectangular border
        Rect inner = new Rect(
            -Mathf.Abs(portalShape.width) * 0.5f, -Mathf.Abs(portalShape.height) * 0.5f,
            Mathf.Abs(portalShape.width), Mathf.Abs(portalShape.height)
        );
        Rect outer = new Rect(inner);
        outer.x -= borderThickness;
        outer.y -= borderThickness;
        outer.width += 2 * borderThickness;
        outer.height += 2 * borderThickness;

        // This effect makes the portal appear to be spinning by moving the two outer points
        // of each edge (vertices 12 to 19). The vertices can't change distance too much or
        // the triangles will cross over each other, so one is always on one half of the edge
        // and the other always on the other half.

        // One wave is offset by 180 degrees so the vertices are always halfway apart from each other.
        // Flip the times so that triangles never cross over.
        float animTime1, animTime2;

        if (animTime % 1f < 0.5f)
        {
            animTime1 = animTime % 1f;
            animTime2 = (animTime + 0.5f) % 1f;
        }
        else
        {
            animTime1 = (animTime + 0.5f) % 1f;
            animTime2 = animTime % 1f;
        }
        
        // sin(x) ranges between 0 and 1 when 0 < x < pi
        var edge1Height = Mathf.Sin(animTime1 * Mathf.PI) * waveThickness * waviness;
        var edge1Offset = animTime1;
        
        var edge2Height = Mathf.Sin(animTime2 * Mathf.PI) * waveThickness * waviness;
        var edge2Offset = animTime2;

        var mesh = GetComponent<MeshFilter>().mesh;
        mesh.vertices = new Vector3[numBorderVerts]
        {
            new Vector3(outer.xMin, outer.yMax),
            new Vector3(outer.xMax, outer.yMax),
            new Vector3(outer.xMin, inner.yMax),
            new Vector3(outer.xMax, inner.yMax),

            new Vector3(inner.xMin, inner.yMax),
            new Vector3(inner.xMax, inner.yMax),
            new Vector3(inner.xMin, inner.yMin),
            new Vector3(inner.xMax, inner.yMin),

            new Vector3(outer.xMin, inner.yMin),
            new Vector3(outer.xMax, inner.yMin),
            new Vector3(outer.xMin, outer.yMin),
            new Vector3(outer.xMax, outer.yMin),

            new Vector3(Mathf.Lerp(inner.xMin, inner.xMax, edge1Offset), outer.yMax + edge1Height),
            new Vector3(Mathf.Lerp(inner.xMin, inner.xMax, edge2Offset), outer.yMax + edge2Height),

            new Vector3(outer.xMin - edge2Height, Mathf.Lerp(inner.yMin, inner.yMax, edge2Offset)),
            new Vector3(outer.xMin - edge1Height, Mathf.Lerp(inner.yMin, inner.yMax, edge1Offset)),

            new Vector3(outer.xMax + edge1Height, Mathf.Lerp(inner.yMax, inner.yMin, edge1Offset)),
            new Vector3(outer.xMax + edge2Height, Mathf.Lerp(inner.yMax, inner.yMin, edge2Offset)),

            new Vector3(Mathf.Lerp(inner.xMax, inner.xMin, edge2Offset), outer.yMin - edge2Height),
            new Vector3(Mathf.Lerp(inner.xMax, inner.xMin, edge1Offset), outer.yMin - edge1Height),
        };

        mesh.RecalculateBounds();
    }
}
