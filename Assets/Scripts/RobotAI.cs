using System.Collections;
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
    public float checkDist = 0.1f;
    private bool grounded;

    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody2D>();
        direction = Vector2.right;
        downdir = transform.TransformDirection(Vector2.down);
        rt = transform.GetChild(0).GetComponent<Collider2D>().bounds.extents;
        width = rt.x;
        height = rt.y;
        grounded = true;
    }
	
	// Update is called once per frame
	void Update ()
    {
        // Break when this is split
        var splittable = GetComponent<Splittable>();
        if (splittable != null && splittable.isSplit)
        {
            Destroy(this);
            return;
        }

        if (rb.velocity.x == 0 || Mathf.Abs(rb.velocity.x) < 2)
        {
            direction.x *= -1;
        }
    }

    void FixedUpdate()
    {
        //Use Circle Cast to make the detection less sensitive
        //Check if its a box if it is ignore the cast against it
        if (grounded)
        {
            if (direction.x > 0)
            {
                if (!Physics2D.CircleCast((Vector2)transform.position + new Vector2(width + 0.11f, -height + checkDist / 2f), 0.10f, downdir, checkDist))
                {
                    direction.x *= -1;
                }

                transform.localScale = new Vector3(-.55f, .55f, .55f);
            }
            else
            {
                if (!Physics2D.CircleCast((Vector2)transform.position + new Vector2(-width - 0.11f, -height + checkDist / 2f), 0.10f, downdir, checkDist))
                {
                    direction.x *= -1;
                }

                transform.localScale = new Vector3(.55f, .55f, .55f);
            }
        }
        if(!Physics2D.CircleCast((Vector2)transform.position + new Vector2(width + 0.11f, -height + checkDist / 2f), 0.10f, downdir, checkDist) && !Physics2D.CircleCast((Vector2)transform.position + new Vector2(-width - 0.11f, -height + checkDist / 2f), 0.10f, downdir, checkDist))
        {
            grounded = false;
        }
        else
        {
            grounded = true;
        }
        if(grounded)
        rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
    }

}
