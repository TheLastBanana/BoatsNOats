using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour {
    public GameObject Source;

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y-0.2f, transform.position.z);
        Source.GetComponent<PowerSource>().isOn = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.2f, transform.position.z);
        Source.GetComponent<PowerSource>().isOn = false;
    }

}
