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
	private float normalizedHorizontalSpeed = 0;

	private CharacterController2D _controller;
	private Animator _animator;
    private Rigidbody2D _rigidbody;
    private PlayerSound _sound;
	private RaycastHit2D _lastControllerColliderHit;
	private Vector3 _velocity;

    private bool inputDisabled;
    private Vector2 storedVelocity;
    private float storedGravity;

    void Start()
    {
        inputDisabled = false;
    }

    void Awake()
	{
        _sound = GetComponent<PlayerSound>();
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
		Debug.Log( "onTriggerEnterEvent: " + col.gameObject.name );
	}


	void onTriggerExitEvent( Collider2D col )
	{
		Debug.Log( "onTriggerExitEvent: " + col.gameObject.name );
	}

	#endregion


	// the Update loop contains a very simple example of moving the character around and controlling the animation
	void Update()
	{
		if( _controller.isGrounded )
        {
            _velocity.y = 0;
            _animator.SetBool("Ground", true);
        }

        if ( Input.GetKey( KeyCode.D ) && !inputDisabled)
		{
			normalizedHorizontalSpeed = 1;
			if( transform.localScale.x > 0f )
				transform.localScale = new Vector3( -transform.localScale.x, transform.localScale.y, transform.localScale.z );
		}
		else if( Input.GetKey( KeyCode.A ) && !inputDisabled)
		{
			normalizedHorizontalSpeed = -1;
			if( transform.localScale.x < 0f )
				transform.localScale = new Vector3( -transform.localScale.x, transform.localScale.y, transform.localScale.z );
		}
		else
		{
			normalizedHorizontalSpeed = 0;
		}


		// we can only jump whilst grounded
		if( _controller.isGrounded && (Input.GetKeyDown( KeyCode.Space ) || Input.GetKeyDown( KeyCode.W )) && !inputDisabled)
		{
			_velocity.y = Mathf.Sqrt( 2f * jumpHeight * -gravity );
            _sound.PlayJumpSound();
            _animator.SetBool("Ground", false);
        }


        // apply horizontal speed smoothing it. dont really do this with Lerp. Use SmoothDamp or something that provides more control
        var smoothedMovementFactor = _controller.isGrounded ? groundDamping : inAirDamping; // how fast do we change direction?
		_velocity.x = Mathf.Lerp( _velocity.x, normalizedHorizontalSpeed * runSpeed, Time.deltaTime * smoothedMovementFactor );

		// apply gravity before moving
		_velocity.y += gravity * Time.deltaTime;

		// if holding down bump up our movement amount and turn off one way platform detection for a frame.
		// this lets us jump down through one way platforms
		if( _controller.isGrounded && Input.GetKey( KeyCode.S ) && !inputDisabled)
		{
			_velocity.y *= 3f;
			_controller.ignoreOneWayPlatformsThisFrame = true;
		}

        _animator.SetFloat("Speed", Mathf.Abs(_velocity.x));
		_controller.move( _velocity * Time.deltaTime );

		// grab our current _velocity to use as a base for all calculations
		_velocity = _controller.velocity;
	}

    public void StopForCutscene()
    {
        DisableInput(true);
        _animator.SetFloat("Speed", 0f);
        _rigidbody.velocity = Vector2.zero;
    }

    public void ResumeAfterCutscene()
    {
        DisableInput(false);
    }

    public void StopForPortal()
    {
        DisableInput(true);
        storedVelocity = _rigidbody.velocity;
        _rigidbody.velocity = Vector2.zero;
        storedGravity = gravity;
//        gravity = 0f;
    }

    public void ResumeAfterPortal()
    {
        DisableInput(false);
        _rigidbody.velocity = storedVelocity;
        gravity = storedGravity;
    }

    private void DisableInput(bool disable)
    {
        inputDisabled = disable;
    }
}
