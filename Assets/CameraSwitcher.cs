using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour {

    public Camera cMain;
    public Camera cAlt;
    bool switched = false;

    bool isSelecting = false;
    Vector3 mousePosition1;
    Rect rect;

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
        {
            var v1 = Camera.main.ScreenToWorldPoint(mousePosition1);
            var v2 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var min = Vector3.Min(v1, v2);
            var max = Vector3.Max(v1, v2);
            min.z = 0;
            max.z = 0;
            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            
            //Iterate through the splittable objects
            foreach (var selectableObject in FindObjectsOfType<Splittable>())
            {
                //If the object and the selection box bounds touch figure out where they do for cutting purposes
                if (bounds.Intersects(selectableObject.GetComponent<PolygonCollider2D>().bounds))
                {
                    cutObject(selectableObject, bounds);
                }  
                
            }
            isSelecting = false;
        }
            

    }

    //Figure out the 4 corners of the bounds for both the selection box and the object and do AABB to figure out where they overlap
    void cutObject(Splittable selectableObject, Bounds selectbounds)
    {

        //Check which edge intersects the object or if the selection box is all around the object
        Vector2 selectbotleft = new Vector2(selectbounds.center.x - selectbounds.extents.x, selectbounds.center.y - selectbounds.extents.y);
        Vector2 selecttopleft = new Vector2(selectbounds.center.x - selectbounds.extents.x, selectbounds.center.y + selectbounds.extents.y);
        Vector2 selectbotright = new Vector2(selectbounds.center.x + selectbounds.extents.x, selectbounds.center.y - selectbounds.extents.y);
        Vector2 selecttopright = new Vector2(selectbounds.center.x + selectbounds.extents.x, selectbounds.center.y + selectbounds.extents.y);

        Bounds objbounds = selectableObject.GetComponent<PolygonCollider2D>().bounds;
        Vector2 objbotleft = new Vector2(objbounds.center.x - objbounds.extents.x, objbounds.center.y - objbounds.extents.y);
        Vector2 objtopleft = new Vector2(objbounds.center.x - objbounds.extents.x, objbounds.center.y + objbounds.extents.y);
        Vector2 objbotright = new Vector2(objbounds.center.x + objbounds.extents.x, objbounds.center.y - objbounds.extents.y);
        Vector2 objtopright = new Vector2(objbounds.center.x + objbounds.extents.x, objbounds.center.y + objbounds.extents.y);

        List<List<GameObject>> verticalPieces = new List<List<GameObject>>();
        verticalPieces.Add(null);
        verticalPieces.Add(null);
        List<List<GameObject>> horizontalPieces= new List<List<GameObject>>();
        horizontalPieces.Add(null);
        horizontalPieces.Add(null);

        if (selecttopright.x > objtopleft.x && selecttopright.x < objtopright.x)
        {
            //Right of selection is greater than left of object
            verticalPieces[0] = selectableObject.SplitOnPlane(selectbotright, selecttopright - selectbotright);
        }

        if (selecttopleft.x < objtopright.x && selecttopleft.x > objtopleft.x)
        {
            //Left of selection is less than right of object
            verticalPieces[1] = selectableObject.SplitOnPlane(selecttopleft, selectbotleft - selecttopleft);
            
        }

        if (selectbotleft.y < objtopleft.y && selectbotleft.y > objbotleft.y)
        {
            //Bottom of selection is less than top of Object
            horizontalPieces[0] = selectableObject.SplitOnPlane(selectbotleft, selectbotright - selectbotleft);
        }

        if (selecttopright.y > objbotleft.y && selecttopright.y < objtopleft.y)
        {
            //Top of selection is greater than bottom of Object
            horizontalPieces[1] = selectableObject.SplitOnPlane(selecttopright, selecttopleft - selecttopright);
        }


        //Index 0 of the pieces list is the "original" object. Index 1 is the created "Clone"
        if ((horizontalPieces[0] != null || horizontalPieces[1] !=null) && (verticalPieces[0] != null || verticalPieces[1] != null))
        {
            GameObject myGameObject = new GameObject("GluedObj");
            myGameObject.AddComponent<Rigidbody2D>();
            for (int i = 0; i<2; i++)
            {
                if (horizontalPieces[i] != null)
                {
                    Destroy(horizontalPieces[i][1].GetComponent<Rigidbody2D>());
                    horizontalPieces[i][1].transform.parent = myGameObject.transform;
                }
                if (verticalPieces[i] != null)
                {
                    Destroy(verticalPieces[i][1].GetComponent<Rigidbody2D>());
                    verticalPieces[i][1].transform.parent = myGameObject.transform;
                }
            }
        }
        
        

    }
    void OnGUI()
    {
        if (isSelecting)
        {
            // Create a rect from both mouse positions
            rect = Selectionbox.GetScreenRect(mousePosition1, Input.mousePosition);
            Selectionbox.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
            Selectionbox.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));
        }

    }



}
