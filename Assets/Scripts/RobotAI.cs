using System.Collections;
using UnityEngine;

public class RobotAI : MonoBehaviour {

    public float speed = 7.0f;
    public float checkDist = 0.1f;
    public float circleCastRadius = 0.1f;
    public float groundCheckOffset = 0.11f;
    public float pauseTime = 0.5f;
    public float turnTime = 0.2f;

    Rigidbody2D rb;
    Animator animator;
    Vector2 direction;
    Vector2 downdir;
    Rect edges;
    float width;
    float height;
    bool grounded;
    bool paused = false;
    bool wasPaused = false;

    // Use this for initialization
    void Awake()
    {
        direction = Vector2.right;
        downdir = transform.TransformDirection(Vector2.down);

        rb = GetComponent<Rigidbody2D>();
        rb.velocity = direction * speed;

        // Get collision rectangle relative to the transform
        var collider = transform.GetChild(0).GetComponent<Collider2D>();
        if (collider == null) return;

        var bounds = collider.bounds;
        edges = new Rect(bounds.min - transform.position, bounds.size);

        animator = GetComponent<Animator>();

        grounded = true;
    }
	
	// Update is called once per frame
	void Update()
    {
        // Break when this is split
        var splittable = GetComponent<Splittable>();
        if (splittable != null && splittable.isSplit)
        {
            Destroy(animator);
            Destroy(this);
            return;
        }

        animator.SetFloat("Speed", paused ? 0f : direction.x * speed);
        animator.SetBool("Facing Right", direction.x > 0);
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

        // Cast the circles
        // Ignore casts against any layer except Default
        // Use Circle Cast to make the detection less sensitive
        var leftCast = Physics2D.CircleCast(leftOrigin, circleCastRadius, downdir, checkDist, 1 << LayerMask.NameToLayer("Default"));
        var rightCast = Physics2D.CircleCast(rightOrigin, circleCastRadius, downdir, checkDist, 1 << LayerMask.NameToLayer("Default"));

        // Change direction when grounded
        if (grounded && !paused)
        {
            if ((direction.x > 0 && !rightCast) || (direction.x < 0 && !leftCast))
            {
                TurnAround();
            }

            if (!wasPaused && Mathf.Abs(rb.velocity.x) < 2)
            {
                TurnAround();
            }
        }

        // Grounded if either side found ground
        grounded = leftCast || rightCast;

        if (grounded)
            if (paused)
                rb.velocity = new Vector2(0, rb.velocity.y);
            else
                rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);

        if (!paused) wasPaused = false;
    }

    void TurnAround()
    {
        wasPaused = paused = true;

        StartCoroutine(TurnCoroutine());
    }

    IEnumerator TurnCoroutine()
    {
        yield return new WaitForSeconds(pauseTime - turnTime);

        direction.x *= -1;

        yield return new WaitForSeconds(turnTime);

        paused = false;
    }
}
