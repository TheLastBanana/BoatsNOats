using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * All the controls the player will use are here, for easy reference and changing should we want to change at any time.
 * Only the mouse for portal selection is excluded, because that has to be the mouse.
 */
public class GameControls : MonoBehaviour {

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

    // For one way platforms, which are in the Player Controller, but aren't used in our game
    public bool GemmaMoveDown()
    {
        return Input.GetKey(KeyCode.S);
    }

    public bool CheckOtherWorld(bool pressed)
    {
        KeyCode key = KeyCode.Tab;
        if (pressed)
            return Input.GetKeyDown(key);
        else
            return Input.GetKeyUp(key);
    }

    public bool SkipDialogue()
    {
        return (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.E));
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
