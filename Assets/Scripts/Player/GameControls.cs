using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * All the controls the player will use are here, for easy reference and changing should we want to change at any time.
 * Only the mouse for portal selection is excluded, because that has to be the mouse.
 */
public class GameControls : MonoBehaviour {

    public PortalManager portalManager;
    public bool DisablePortalThisLevel = false;

    private float skipDialogueLast = 0f;

    void Start()
    {
        if (portalManager != null)
            portalManager.DisablePortalForLevel(DisablePortalThisLevel);
    }

    public bool GemmaLeft()
    {
        return Input.GetKey(KeyCode.A);
    }

    public bool GemmaRight()
    {
        return Input.GetKey(KeyCode.D);
    }

    public bool GemmaJump()
    {
        return (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W));
    }

    public bool CheckOtherWorld(bool pressed)
    {
        if (DisablePortalThisLevel)
            return false;

        KeyCode key = KeyCode.Tab;
        if (pressed)
            return Input.GetKeyDown(key);
        else
            return Input.GetKeyUp(key);
    }

    public bool SkipDialogue()
    {
        // We don't want multiple presses from one physical press, this is also why GetKeyDown is used instead of GetKey
        if (Time.time - skipDialogueLast > 0.1f)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
            {
                skipDialogueLast = Time.time;
                return true;
            }
        }
        return false;
    }

    public bool RestartLevel()
    {
        return Input.GetKey(KeyCode.R);
    }

    public bool QuitGame()
    {
        return Input.GetKey(KeyCode.Escape);
    }
}
