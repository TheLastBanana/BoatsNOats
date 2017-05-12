using System.Collections;
using UnityEngine;

public class RobotAI : MonoBehaviour {

    public float speed = 7.0f;
    public float checkDist = 0.1f;
    public float groundedVelocityThreshold = 0.1f;
    public float circleCastRadius = 0.1f;
    public float groundCheckOffset = 0.11f;
    public float edgePauseTime = 0.5f;
    public float bumpPauseTime = 0.7f;
    public float turnTime = 0.2f;
    public Vector2 bumpVelocity = new Vector2(1.5f, 1.5f);
    public ParticleSystem bumpEffect;
    public ParticleSystem[] rollEffects;

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
    Vector3 bumpEffectPos;

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

        if (bumpEffect)
            bumpEffectPos = bumpEffect.transform.localPosition;
    }
    
    // Update is called once per frame
    void Update()
    {
        // Break when this is split
        var splittable = GetComponent<Splittable>();
        if (splittable != null && splittable.IsSplit)
        {
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
        if (splittable != null && splittable.IsSplit)
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
                TurnAround(edgePauseTime);
            }

            if (!wasPaused && Mathf.Abs(rb.velocity.x) < 2)
            {
                TurnAround(bumpPauseTime);
                rb.velocity = new Vector2(-direction.x * bumpVelocity.x, bumpVelocity.y);
                if (bumpEffect)
                {
                    bumpEffect.Play();

                    var newBumpPos = bumpEffectPos;
                    newBumpPos.x *= direction.x;
                    bumpEffect.transform.localPosition = newBumpPos;

                    // Rotate bump effect to face the right way
                    var bumpAngles = bumpEffect.transform.localEulerAngles;
                    bumpAngles.y = -90 * direction.x;
                    bumpEffect.transform.localEulerAngles = bumpAngles;
                }
            }
        }

        // Grounded if either side found ground
        grounded = leftCast || rightCast;

        if (grounded && Mathf.Abs(rb.velocity.y) < groundedVelocityThreshold)
            if (paused)
                rb.velocity = new Vector2(0, rb.velocity.y);
            else
                rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);

        if (!paused) wasPaused = false;
    }

    void OnDisable()
    {
        DisableRollEffects();
    }

    void OnEnable()
    {
        EnableRollEffects();
    }

    void TurnAround(float time)
    {
        wasPaused = paused = true;

        StartCoroutine(TurnCoroutine(time));
    }

    void DisableRollEffects()
    {
        foreach (var effect in rollEffects)
            effect.Stop();
    }

    void EnableRollEffects()
    {
        foreach (var effect in rollEffects)
            effect.Play();
    }

    IEnumerator TurnCoroutine(float time)
    {
        DisableRollEffects();

        yield return WaitWhileEnabled(time - turnTime);

        direction.x *= -1;

        yield return WaitWhileEnabled(turnTime);

        paused = false;

        EnableRollEffects();
    }

    // In contrast to WaitForSeconds, this pauses while the robot is disabled
    IEnumerator WaitWhileEnabled(float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            if (enabled) elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
