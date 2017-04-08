﻿using UnityEngine;
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
    private PlayerEffects _sound;
	private RaycastHit2D _lastControllerColliderHit;
	private Vector3 _velocity;
    private Transform canvasTransform;
    public GameControls controls;

    public bool inputDisabled;
    private bool portalSelecting;
    private Vector2 storedVelocity;
    private float storedGravity;


    void Start()
    {
        canvasTransform = GetComponentInChildren<Canvas>().transform;
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
        else
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

    private void FlipGemma()
    {
        // Get canvas' x position
        float canvasX = canvasTransform.position.x;

        // Flip Gemma
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);

        // Flip canvas back
        canvasTransform.localScale = new Vector3(-canvasTransform.localScale.x, canvasTransform.localScale.y, canvasTransform.localScale.z);

        // Reset canvas' x position
        canvasTransform.position = new Vector3(canvasX, canvasTransform.position.y, canvasTransform.position.z);
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
