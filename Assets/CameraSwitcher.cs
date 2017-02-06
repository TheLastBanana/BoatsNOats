using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera cMain;
    public Camera cAlt;
    bool switched = false;

	// Update is called once per frame
	void Update () {
		if (!switched && Input.GetKeyDown(KeyCode.Tab))
        {
            cMain.enabled = false;
            cAlt.enabled = true;
            switched = true;
        }
        else if (switched && Input.GetKeyUp(KeyCode.Tab))
        {
            cMain.enabled = true;
            cAlt.enabled = false;
            switched = false;
        }
    }
}
