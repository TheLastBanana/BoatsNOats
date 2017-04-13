using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ScrollingBackground : MonoBehaviour
{
    new public Camera camera;
    public string sortingLayer;

    public float backgroundScale = 1f;
    public Vector2 offset = new Vector2(0f, 0f);
    public Vector2 scrollRate = new Vector2(0f, 0f);

    new MeshRenderer renderer;
    Material material;
    Vector2 textureSize;

    void Awake()
    {
        // Mesh code from http://answers.unity3d.com/answers/51015/view.html
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[]
        {
             new Vector3( 1,  1),
             new Vector3( 1, -1),
             new Vector3(-1,  1),
             new Vector3(-1, -1),
        };

        Vector2[] uv = new Vector2[]
        {
             new Vector2(1, 1),
             new Vector2(1, 0),
             new Vector2(0, 1),
             new Vector2(0, 0),
        };

        int[] triangles = new int[]
        {
             0, 1, 2,
             2, 1, 3,
        };

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        
        var filter = GetComponent<MeshFilter>();
        filter.mesh = mesh;

        renderer = GetComponent<MeshRenderer>();
        material = renderer.material;

        var texture = material.mainTexture;
        textureSize = new Vector2(texture.width, texture.height);

        renderer.sortingLayerName = sortingLayer;
        renderer.sortingLayerID = SortingLayer.NameToID(sortingLayer);

        UpdateTexture();
    }

    void OnWillRenderObject()
    {
        UpdateTexture();
    }

    void UpdateTexture()
    {
        // Scale the quad to screen size
        float camHeight = camera.orthographicSize;
        float camWidth = camHeight / Screen.height * Screen.width;
        transform.localScale = new Vector3(camWidth, camHeight, 1);

        // Scale the texture to screen size
        material.mainTextureScale = new Vector2(
            (Screen.width / textureSize.x) * (textureSize.y / Screen.height), 1
        ) / backgroundScale;

        // Offset the texture
        var camPos = camera.transform.position;
        material.mainTextureOffset = new Vector2(
            camPos.x * scrollRate.x,
            camPos.y * scrollRate.y
        ) + offset;
    }
}
