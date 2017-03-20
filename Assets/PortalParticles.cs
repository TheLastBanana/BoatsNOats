using UnityEngine;
using System.Collections.Generic;

public class PortalParticles : MonoBehaviour
{
    public GameObject edgePrefab;
    public GameObject middlePrefab;
    public Rect portalShape;
    public float intensity = 1f;

    ParticleSystem middle;
    float middleBaseRate;

    Transform[] transforms = new Transform[4];
    ParticleSystem[][] edgeSystems = new ParticleSystem[4][];
    ParticleSystem.EmissionModule[][] emissions = new ParticleSystem.EmissionModule[4][];
    ParticleSystem.ShapeModule[][] shapes = new ParticleSystem.ShapeModule[4][];
    float[] edgeBaseRates;

    void Awake()
    {
        // Set up particle systems
        for (int i = 0; i < 4; ++i)
        {
            var newObj = transforms[i] = Instantiate(edgePrefab, transform).transform;
            transforms[i].eulerAngles = new Vector3(-90 * i, 90, 0);

            var systems = edgeSystems[i] = newObj.GetComponentsInChildren<ParticleSystem>();
            shapes[i] = new ParticleSystem.ShapeModule[systems.Length];
            emissions[i] = new ParticleSystem.EmissionModule[systems.Length];

            for (int j = 0; j < systems.Length; ++j)
            {
                var system = systems[j];

                shapes[i][j] = system.shape;
                emissions[i][j] = system.emission;
            }
        }


        // Determine base spawn rates for edge systems
        edgeBaseRates = new float[emissions[0].Length];
        for (int i = 0; i < edgeBaseRates.Length; ++i)
            edgeBaseRates[i] = emissions[0][i].rateOverTimeMultiplier;

        middle = Instantiate(middlePrefab, transform).GetComponent<ParticleSystem>();
        middle.transform.localPosition = new Vector3();
        middleBaseRate = middle.emission.rateOverTimeMultiplier;

        Disable();
    }

    void Update()
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

        // Scale emission rate to size, so we get the same relative coverage
        for (int i = 0; i < 4; ++i)
            for (int j = 0; j < edgeBaseRates.Length; ++j)
            {
                emissions[i][j].rateOverTimeMultiplier =
                    shapes[i][j].radius == 0f ? 0f : edgeBaseRates[j] * intensity * shapes[i][j].radius;
            }

        // Middle is a square, so use area to scale emission rate
        var middleEmission = middle.emission;
        middleEmission.rateOverTime = middleBaseRate * w * h * intensity;

        // Move this into position
        transform.position = portalShape.center;
    }

    // Clear all particle systems
    public void Clear()
    {
        middle.Clear();
        foreach (var systemList in edgeSystems)
            foreach (var system in systemList)
                system.Clear();
    }

    // Enable emissions
    public void Enable()
    {
        Clear();
        var middleEmission = middle.emission;
        middleEmission.enabled = true;

        for (int i = 0; i < 4; ++i)
            for (int j = 0; j < edgeBaseRates.Length; ++j)
            emissions[i][j].enabled = true;
    }

    // Disable emissions
    public void Disable()
    {
        var middleEmission = middle.emission;
        middleEmission.enabled = false;

        for (int i = 0; i < 4; ++i)
            for (int j = 0; j < edgeBaseRates.Length; ++j)
                emissions[i][j].enabled = false;
    }
}
