using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotAI : MonoBehaviour {

    public float speed = 7.0f;
    public float checkDist = 0.1f;
    public float circleCastRadius = 0.1f;
    public float groundCheckOffset = 0.11f;
    private Rigidbody2D rb;
    private Vector2 direction;
    Vector2 downdir;
    Rect edges;
    float width;
    float height;
    private bool grounded;

    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody2D>();
        direction = Vector2.right;
        downdir = transform.TransformDirection(Vector2.down);

        // Get collision rectangle relative to the transform
        var bounds = transform.GetChild(0).GetComponent<Collider2D>().bounds;
        edges = new Rect(bounds.min - transform.position, bounds.size);

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
        // Break when this is split
        var splittable = GetComponent<Splittable>();
        if (splittable != null && splittable.isSplit)
        {
            return;
        }

        // Current bounding box edges
        var curEdges = new Rect(edges.min + (Vector2)transform.position, edges.size);

        // Try casting a circle down on either side
        var leftOrigin = new Vector2(curEdges.xMin - groundCheckOffset, curEdges.yMin + checkDist / 2f);
        var rightOrigin = new Vector2(curEdges.xMax + groundCheckOffset, curEdges.yMin + checkDist / 2f);

        print(leftOrigin);

        var leftCast = Physics2D.CircleCast(leftOrigin, circleCastRadius, downdir, checkDist, 1 << LayerMask.NameToLayer("Default"));
        var rightCast = Physics2D.CircleCast(rightOrigin, circleCastRadius, downdir, checkDist, 1 << LayerMask.NameToLayer("Default"));

        // Use Circle Cast to make the detection less sensitive
        if (grounded)
        {
            // Cast the ray
            // Ignore casts against any layer except Default
            if ((direction.x > 0 && !rightCast) || (direction.x < 0 && !leftCast))
            {
                direction.x *= -1;
            }
        }

        // Grounded if either side found ground
        grounded = leftCast || rightCast;

        if (grounded)
            rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
    }

}
