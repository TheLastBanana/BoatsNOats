using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

// Custom version of the Platformer2DUserControl script
[RequireComponent(typeof(PlayerController))]
public class PlayerController : MonoBehaviour
{
    private PlayerMovement m_Character;
    private bool m_Jump;

    private void Awake()
    {
        m_Character = GetComponent<PlayerMovement>();
    }


    private void Update()
    {
        if (!m_Jump)
        {
            // Read the jump input in Update so button presses aren't missed.
            m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
        }
    }


    private void FixedUpdate()
    {
        // Read the inputs.
        bool crouch = Input.GetKey(KeyCode.LeftControl);
        float h = CrossPlatformInputManager.GetAxis("Horizontal");
        if (true)
        {
            // Pass all parameters to the character control script.
            m_Character.Move(h, crouch, m_Jump);
        }
        m_Jump = false;
    }
}
