using UnityEngine;

public class WeightedClip : MonoBehaviour
{
    public AudioClip clip;

    public float volume = 1f;

    // How likely is this to play?
    public float weight = 1f;

    // Subtract this from the clip length, allowing other clips to play over it
    public float overplay = 0f;
}
