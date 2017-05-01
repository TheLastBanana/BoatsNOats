using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour {
    public GameObject Source;
    private bool isOn;

    public AudioSource onSound;
    public AudioSource offSound;

    // Use this for initialization
    void Start() {
        isOn = false;
    }

    // Update is called once per frame
    void Update() {
        // Break when this is split
        var splittable = transform.parent.GetComponent<Splittable>();
        if (splittable != null && splittable.IsSplit)
        {
            Source.GetComponent<PowerSource>().IsOn = false;
            Destroy(this);
            return;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOn)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y - 0.2f, transform.position.z);
            Source.GetComponent<PowerSource>().IsOn = true;
            onSound.Play();
        }
        isOn = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isOn)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.2f, transform.position.z);
            Source.GetComponent<PowerSource>().IsOn = false;
            offSound.Play();
        }
        isOn = false;
    }

}
