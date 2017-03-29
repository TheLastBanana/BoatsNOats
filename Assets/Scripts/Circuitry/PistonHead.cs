using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistonHead : MonoBehaviour {

    public GameObject parent;
    private Piston piston;

	// Use this for initialization
	void Start () {
        // If the parent object is null we've been split, we can just destroy this
        if (parent == null)
        {
            Destroy(this);
            return;
        }
        piston = parent.GetComponent<Piston>();
	}
	
    void OnCollisionStay2D(Collision2D coll)
    {
        // If the piston object is null we've been split, we can just destroy this
        if (piston == null)
        {
            Destroy(this);
            return;
        }
        
        if (coll.gameObject.tag == "Player" && piston.IsMovingUp())
        {
            Transform t = coll.gameObject.transform;
            // This is the rotation of the piston
            Quaternion pistRot = piston.transform.rotation;

            // This is the speed to push Gemma at if we were going straight up
            Vector3 speed = new Vector3(0, piston.GetSpeed(), 0);

            // This is the speed rotated to push Gemma in the correct direction
            Vector3 rotSpeed = pistRot * speed;

            Debug.Log(piston.transform.localScale);

            // Add to Gemma's position
            t.position += rotSpeed;
        }
    }
}
