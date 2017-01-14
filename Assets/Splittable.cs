using System.Collections.Generic;
using UnityEngine;

// An object which can be split up by the portal.
public class Splittable : MonoBehaviour {
    void Start () {
        // Get sprite info (if there is one)
        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        Texture tex = sr.sprite.texture;
        Material mat = sr.material;
        sr.enabled = false;

        // Create mesh, copying the bounds of the sprite
        Mesh mesh = new Mesh();

        Vector3 min = transform.InverseTransformPoint(sr.bounds.min);
        Vector3 max = transform.InverseTransformPoint(sr.bounds.max);

        Vector3[] vertices =
        {
            min,
            new Vector3(max.x, min.y, 0),
            max,
            new Vector3(min.x, max.y, 0)
        };

        Vector2[] uv =
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        int[] triangles =
        {
            3, 1, 0,
            3, 2, 1 
        };

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        // Remove sprite renderer
        DestroyImmediate(sr);

        // Create mesh filter
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        // Create mesh renderer
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = mat;
        mr.material.mainTexture = tex;



        // DEBUG
        SplitOnPlane(new Vector3(0.5f, 0, 0), new Vector2(1, 0));
    }

    // Split the object along a plane defined by anchor and dir
    public void SplitOnPlane(Vector2 anchor, Vector2 dir)
    {
        // Make a second object
        GameObject rightObj = Instantiate(gameObject);

        SplitMesh(gameObject, rightObj, anchor, dir);
        SplitCollider(gameObject, rightObj, anchor, dir);
    }

    // Split leftObj's mesh into two (leftObj and rightObj) along a plane defined by anchor and dir
    static private void SplitMesh(GameObject leftObj, GameObject rightObj, Vector2 anchor, Vector2 dir)
    {
        Mesh rightMesh = rightObj.GetComponent<MeshFilter>().mesh;
        Mesh leftMesh = leftObj.GetComponent<MeshFilter>().mesh;

        List<Vector3> leftVerts = new List<Vector3>(leftMesh.vertices);
        List<Vector3> rightVerts = new List<Vector3>(leftMesh.vertices);

        List<Vector2> leftUV = new List<Vector2>(leftMesh.uv);
        List<Vector2> rightUV = new List<Vector2>(rightMesh.uv);

        // Polygons on each side
        List<int> leftTris = new List<int>();
        List<int> rightTris = new List<int>();

        // Sort triangles to sides
        for (int i = 0; i < leftMesh.triangles.Length; i += 3)
        {
            List<int> left = new List<int>();
            List<int> right = new List<int>();

            for (int j = i; j < i + 3; ++j)
            {
                int vi = leftMesh.triangles[j];
                if (leftMesh.vertices[vi].x < anchor.x) left.Add(vi);
                else right.Add(vi);
            }

            // Triangle is all left
            if (left.Count == 3) leftTris.AddRange(left);
            // Triangle is all right
            else if (right.Count == 3) rightTris.AddRange(right);
            // Triangle intersects the plane
            else
            {
                Vector2 pointA, pointB, pointC;
                Vector2 uvA, uvB, uvC;

                // Two cases: one point right, two points left; or one point left, two points right
                // Either way, the logic is similar. Let point C be the point that is alone on its side
                if (left.Count == 2)
                {
                    pointA = leftMesh.vertices[left[0]];
                    pointB = leftMesh.vertices[left[1]];
                    pointC = leftMesh.vertices[right[0]];

                    uvA = leftMesh.uv[left[0]];
                    uvB = leftMesh.uv[left[1]];
                    uvC = leftMesh.uv[right[0]];
                }
                else
                {
                    pointA = leftMesh.vertices[right[0]];
                    pointB = leftMesh.vertices[right[1]];
                    pointC = leftMesh.vertices[left[0]];

                    uvA = leftMesh.uv[right[0]];
                    uvB = leftMesh.uv[right[1]];
                    uvC = leftMesh.uv[left[0]];
                }

                // x and y increase based on slope of the edge, so we find where the intersection
                // lies on the axis perpendicular to the plane
                float ratioA = (anchor.x - pointA.x) / (pointC.x - pointA.x);
                float ratioB = (anchor.x - pointB.x) / (pointC.x - pointB.x);

                // Now use the ratio to calculate intersection points
                Vector2 interA = Vector2.Lerp(pointA, pointC, ratioA);
                Vector2 interB = Vector2.Lerp(pointB, pointC, ratioB);

                // Add them to the vertex lists
                leftVerts.Add(interA);
                leftVerts.Add(interB);
                rightVerts.Add(interA);
                rightVerts.Add(interB);

                // Add corresponding UV coords
                Vector2 interUVA = Vector2.Lerp(uvA, uvC, ratioA);
                Vector2 interUVB = Vector2.Lerp(uvB, uvC, ratioB);

                leftUV.Add(interUVA);
                leftUV.Add(interUVB);
                rightUV.Add(interUVA);
                rightUV.Add(interUVB);

                if (left.Count == 2)
                {
                    // Add right triangle
                    rightTris.Add(right[0]);
                    rightTris.Add(rightVerts.Count - 2);
                    rightTris.Add(rightVerts.Count - 1);

                    // Add left triangles
                    leftTris.Add(left[0]);
                    leftTris.Add(left[1]);
                    leftTris.Add(leftVerts.Count - 2);

                    leftTris.Add(left[1]);
                    leftTris.Add(leftVerts.Count - 1);
                    leftTris.Add(leftVerts.Count - 2);
                }
                else
                {
                    // Add left triangle
                    leftTris.Add(left[0]);
                    leftTris.Add(leftVerts.Count - 2);
                    leftTris.Add(leftVerts.Count - 1);

                    // Add right triangles
                    rightTris.Add(right[0]);
                    rightTris.Add(right[1]);
                    rightTris.Add(rightVerts.Count - 2);

                    rightTris.Add(right[1]);
                    rightTris.Add(rightVerts.Count - 1);
                    rightTris.Add(rightVerts.Count - 2);
                }
            }
        }

        leftMesh.vertices = leftVerts.ToArray();
        leftMesh.triangles = leftTris.ToArray();
        leftMesh.uv = leftUV.ToArray();

        rightMesh.vertices = rightVerts.ToArray();
        rightMesh.triangles = rightTris.ToArray();
        rightMesh.uv = rightUV.ToArray();
    }

    // Split leftObj's PolygonCollider2d into two (leftObj and rightObj) along a plane defined by anchor and dir
    static private void SplitCollider(GameObject leftObj, GameObject rightObj, Vector2 anchor, Vector2 dir)
    {
        PolygonCollider2D leftColl = leftObj.GetComponent<PolygonCollider2D>();
        PolygonCollider2D rightColl = rightObj.GetComponent<PolygonCollider2D>();

        List<Vector2> leftPoints = new List<Vector2>();
        List<Vector2> rightPoints = new List<Vector2>();

        // Whether the last point was on the left
        bool wasLeft = leftColl.points[0].x < anchor.x;

        for (int i = 0; i < leftColl.points.Length + 1; ++i)
        {
            Vector2 point = leftColl.points[i % leftColl.points.Length];
            bool isLeft = point.x < anchor.x;

            // Check if this edge is split (i.e. we've gone from left to right)
            if (wasLeft != isLeft)
            {
                // Add a new point between the two using the slope calculation (see comments in SplitMesh)
                Vector2 lastPoint = leftColl.points[i - 1];
                float ratio = (anchor.x - lastPoint.x) / (point.x - lastPoint.x);
                Vector2 inter = Vector2.Lerp(lastPoint, point, ratio);

                leftPoints.Add(inter);
                rightPoints.Add(inter);
            }
            
            // Don't duplicate the last point if we're not interpolating it
            if (i < leftColl.points.Length)
            {
                if (isLeft) leftPoints.Add(point);
                else rightPoints.Add(point);
            }

            wasLeft = isLeft;
        }

        leftColl.points = leftPoints.ToArray();
        rightColl.points = rightPoints.ToArray();
    }
}
