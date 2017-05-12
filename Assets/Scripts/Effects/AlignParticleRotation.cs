using UnityEngine;

public class AlignParticleRotation : MonoBehaviour {
    ParticleSystem system;
    ParticleSystem.MainModule sysMain;
    public Transform align;

    void Awake()
    {
        system = GetComponent<ParticleSystem>();
        sysMain = system.main;
    }
    
    void Update()
    {
        sysMain.startRotationZMultiplier = (-align.localRotation.eulerAngles.z) * Mathf.Deg2Rad;
    }
}
