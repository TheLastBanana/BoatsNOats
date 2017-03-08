using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotAI : MonoBehaviour {

    public float speed = 7.0f;
    private Rigidbody2D rb;
    private Vector2 direction;
    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody2D>();
        direction = Vector2.right;
	}
	
	// Update is called once per frame
	void Update () {
        if(rb.velocity.x == 0 || Mathf.Abs(rb.velocity.x) < 2)
        {
            direction.x *= -1;            
        }       
    }
    void FixedUpdate()
    {     
            rb.velocity = direction * speed;   
    }

}
