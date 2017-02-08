using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour {

    // Every level will have one of these.
    // offs.x or offs.y are offsets to the alternate world equivalent location
    // i.e. origin.x + offs.x == alternateOrigin.x
    public WorldOffsets offs;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    // This should resize the portal to have the top left corner at corner
    // with width and height from size
    public void resize(Vector2 corner, Vector2 size)
    {


    }
}
