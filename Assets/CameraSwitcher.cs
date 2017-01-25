using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour {

    public Camera cMain;
    public Camera cAlt;
    bool switched = false;

    bool isSelecting = false;
    Vector3 mousePosition1;


    // Use this for initialization
    void Start () {
		
	}
	
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

        // If we press the right mouse button, save mouse location and begin selection box
        if (Input.GetMouseButtonDown(1))
        {
            isSelecting = true;
            mousePosition1 = Input.mousePosition;
        }
        // If we let go of the right mouse button, end selection
        if (Input.GetMouseButtonUp(1))
            isSelecting = false;
    }

    void OnGUI()
    {
        if (isSelecting)
        {
            // Create a rect from both mouse positions
            var rect = Selectionbox.GetScreenRect(mousePosition1, Input.mousePosition);
            Selectionbox.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
            Selectionbox.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));
        }

    }

}
