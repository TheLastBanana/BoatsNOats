﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotAI : MonoBehaviour {

    public float speed = 7.0f;
    private Rigidbody2D rb;
    private Vector2 direction;
    Vector2 downdir;
    Vector3 rt;
    float width;
    float height;

    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody2D>();
        direction = Vector2.right;
        downdir = transform.TransformDirection(Vector2.down);
        rt = transform.GetChild(0).GetComponent<Collider2D>().bounds.size;
        width = rt.x;
        height = rt.y;
        
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
        if (direction.x > 0)
        {
            if (!Physics2D.Raycast((Vector2)transform.position + new Vector2(width / 2 + 0.01f, -(height/2)), downdir, height))
            {
                rb.velocity = new Vector2(0, 0);
                direction.x *= -1;
                rb.velocity = direction * speed;
            }
        }
        else
        {
            if (!Physics2D.Raycast((Vector2)transform.position - new Vector2(width / 2 + 0.01f, (height/2)), downdir, height))
            {
                rb.velocity = new Vector2(0, 0);
                direction.x *= -1;
                rb.velocity = direction * speed;

            }
        }
    }

}
