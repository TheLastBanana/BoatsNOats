using UnityEngine;
using System.Collections;
using Prime31;


public class PlayerController : MonoBehaviour
{
	// movement config
	public float gravity = -25f;
	public float runSpeed = 8f;
	public float groundDamping = 20f; // how fast do we change direction? higher means faster
	public float inAirDamping = 5f;
	public float jumpHeight = 3.5f;

    [HideInInspector]
	public float normalizedHorizontalSpeed = 0;

	private CharacterController2D _controller;
	private Animator _animator;
    private Rigidbody2D _rigidbody;
    private PlayerEffects _sound;
	private RaycastHit2D _lastControllerColliderHit;
	private Vector3 _velocity;
    public GameControls controls;

    public bool inputDisabled;
    private bool portalSelecting;
    private Vector2 storedVelocity;
    private float storedGravity;


    private bool isWalking = false;
    Coroutine currentWalk;

    void Start()
    {
        portalSelecting = false;
    }

    void Awake()
	{
        _sound = GetComponent<PlayerEffects>();
		_animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
		_controller = GetComponent<CharacterController2D>();

		// listen to some events for illustration purposes
		_controller.onControllerCollidedEvent += onControllerCollider;
		_controller.onTriggerEnterEvent += onTriggerEnterEvent;
		_controller.onTriggerExitEvent += onTriggerExitEvent;
	}


	#region Event Listeners

	void onControllerCollider( RaycastHit2D hit )
	{
		// bail out on plain old ground hits cause they arent very interesting
		if( hit.normal.y == 1f )
			return;

		// logs any collider hits if uncommented. it gets noisy so it is commented out for the demo
		//Debug.Log( "flags: " + _controller.collisionState + ", hit.normal: " + hit.normal );
	}


	void onTriggerEnterEvent( Collider2D col )
	{
		//Debug.Log( "onTriggerEnterEvent: " + col.gameObject.name );
	}


	void onTriggerExitEvent( Collider2D col )
	{
		//Debug.Log( "onTriggerExitEvent: " + col.gameObject.name );
	}

	#endregion


	// the Update loop contains a very simple example of moving the character around and controlling the animation
	void Update()
	{
        // Completely freeze Gemma when selecting with a portal
        if (portalSelecting)
            return;

        if (_controller.isGrounded)
        {
            _velocity.y = 0;
        }

        if (controls.GemmaRight() && !inputDisabled)
		{
			normalizedHorizontalSpeed = 1;
            if (transform.localScale.x > 0f)
                FlipGemma();
		}
		else if (controls.GemmaLeft() && !inputDisabled)
		{
			normalizedHorizontalSpeed = -1;
			if( transform.localScale.x < 0f )
                FlipGemma();
        }
        else if(!isWalking)
		{
			normalizedHorizontalSpeed = 0;
		}


        // we can only jump whilst grounded
        if (_controller.isGrounded && controls.GemmaJump() && !inputDisabled)
		{
			_velocity.y = Mathf.Sqrt( 2f * jumpHeight * -gravity );
            _sound.PlayJumpEffect();
        }


        // apply horizontal speed smoothing it. dont really do this with Lerp. Use SmoothDamp or something that provides more control
        var smoothedMovementFactor = _controller.isGrounded ? groundDamping : inAirDamping; // how fast do we change direction?
		_velocity.x = Mathf.Lerp( _velocity.x, normalizedHorizontalSpeed * runSpeed, Time.deltaTime * smoothedMovementFactor );

		// apply gravity before moving
		_velocity.y += gravity * Time.deltaTime;

        _animator.SetFloat("Speed", Mathf.Abs(_velocity.x));
		_controller.move( _velocity * Time.deltaTime );

		// grab our current _velocity to use as a base for all calculations
		_velocity = _controller.velocity;




        _animator.SetBool("Ground", _controller.isGrounded);
    }
    public bool DoneWalking()
    {
        return (currentWalk == null);
    }
    public void FlyToPosition(Vector3 target)
    {
        currentWalk = StartCoroutine(walkToPosition(target));
    }

    IEnumerator walkToPosition(Vector3 target)
    {
        isWalking = true;
        Vector3 start = transform.position;

        if (target.x > start.x)
        {
            if (transform.localScale.x > 0f)
                FlipGemma();
            while (transform.position.x < target.x)
            {
                normalizedHorizontalSpeed = 1;
                yield return null;
            }
        }
        else
        {
            if (transform.localScale.x < 0f)
                FlipGemma();
            while (transform.position.x > target.x)
            {
                normalizedHorizontalSpeed = -1;
                yield return null;
            }
                
        }
        isWalking = false;
        normalizedHorizontalSpeed = 0;
        currentWalk = null;
    }
    public void FlipGemma()
    {
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    public void StopForCutscene()
    {
        inputDisabled = true;
        _animator.SetFloat("Speed", 0f);
        _rigidbody.velocity = Vector2.zero;
    }

    public void ResumeAfterCutscene()
    {
        inputDisabled = false;
    }

    public void StopForPortal()
    {
        inputDisabled = true;
        portalSelecting = true;

        _animator.SetFloat("Speed", 0f);
        storedVelocity = _rigidbody.velocity;
        _rigidbody.velocity = Vector2.zero;
        storedGravity = gravity;
        gravity = 0f;
    }

    public void ResumeAfterPortal()
    {
        inputDisabled = false;
        portalSelecting = false;

        _rigidbody.velocity = storedVelocity;
        gravity = storedGravity;
    }
}
