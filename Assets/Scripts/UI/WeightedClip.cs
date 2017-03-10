using UnityEngine;

[RequireComponent (typeof (AudioSource))]
public class WeightedClip : MonoBehaviour
{
    // How likely is this to play?
    public float weight = 1f;

    // Subtract this from the clip length, allowing other clips to play over it
    public float overplay = 0f;
}
