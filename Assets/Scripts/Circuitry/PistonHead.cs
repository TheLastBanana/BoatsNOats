using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistonHead : MonoBehaviour {

    public GameObject parent;
    private Piston piston;

	// Use this for initialization
	void Start () {
        piston = parent.GetComponent<Piston>();
	}
	
    void OnCollisionStay2D(Collision2D coll)
    {
        if (coll.gameObject.tag == "Player" && piston.IsMovingUp())
        {
            Transform t = coll.gameObject.transform;
            t.position = new Vector3(t.position.x, t.position.y + piston.GetSpeed(), t.position.z);
        }
    }
}
