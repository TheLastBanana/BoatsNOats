using UnityEngine;

public class PortalParticles : MonoBehaviour
{
    public GameObject edgePrefab;
    public GameObject middlePrefab;
    public Rect portalShape;
    public float intensity = 1f;

    ParticleSystem middle;
    float middleBaseRate;

    ParticleSystem[] edgeSystems = new ParticleSystem[4];
    Transform[] transforms = new Transform[4];
    ParticleSystem.EmissionModule[] emissions = new ParticleSystem.EmissionModule[4];
    ParticleSystem.ShapeModule[] shapes = new ParticleSystem.ShapeModule[4];
    float edgeBaseRate;

    void Awake()
    {
        // Set up particle systems
        for (int i = 0; i < 4; ++i)
        {
            edgeSystems[i] = Instantiate(edgePrefab, transform).GetComponent<ParticleSystem>();
            shapes[i] = edgeSystems[i].shape;
            emissions[i] = edgeSystems[i].emission;
            transforms[i] = edgeSystems[i].transform;
            transforms[i].eulerAngles = new Vector3(-90 * i, 90, 0);
        }

        middle = Instantiate(middlePrefab, transform).GetComponent<ParticleSystem>();
        middle.transform.localPosition = new Vector3();

        edgeBaseRate = emissions[0].rateOverTimeMultiplier;
        middleBaseRate = middle.emission.rateOverTimeMultiplier;

        Disable();
    }

    void Update()
    {
        float w = Mathf.Abs(portalShape.width);
        float h = Mathf.Abs(portalShape.height);

        // Update edge sizes
        shapes[0].radius = shapes[2].radius = h / 2;
        shapes[1].radius = shapes[3].radius = w / 2;
        var middleShape = middle.shape;
        middleShape.box = new Vector3(w, h);

        // Move to encompass rectangle
        transforms[0].localPosition = new Vector2(w / 2, 0);
        transforms[1].localPosition = new Vector2(0, h / 2);
        transforms[2].localPosition = new Vector2(-w / 2, 0);
        transforms[3].localPosition = new Vector2(0, -h / 2);

        // Scale emission rate to size, so we get the same relative coverage
        for (int i = 0; i < 4; ++i)
            emissions[i].rateOverTimeMultiplier =
                shapes[i].radius == 0f ? 0f : edgeBaseRate * intensity * shapes[i].radius;

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
        foreach (var system in edgeSystems)
            system.Clear();
    }

    // Enable emissions
    public void Enable()
    {
        Clear();
        var middleEmission = middle.emission;
        middleEmission.enabled = true;

        for (int i = 0; i < 4; ++i)
            emissions[i].enabled = true;
    }

    // Disable emissions
    public void Disable()
    {
        var middleEmission = middle.emission;
        middleEmission.enabled = false;

        for (int i = 0; i < 4; ++i)
            emissions[i].enabled = false;
    }
}
