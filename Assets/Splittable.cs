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
        SplitOnPlane(new Vector3(0.5f, 0.0f, 0), new Vector2(0, 1));
    }

    // Split the object along a plane defined by anchor and dir.
    // Return the other half, or null if the plane did not intersect.
    public GameObject SplitOnPlane(Vector2 anchor, Vector2 dir)
    {
        // Make a second object
        GameObject rightObj = Instantiate(gameObject);

        // If false, plane doesn't intersect collider, so don't split
        if (!SplitCollider(gameObject, rightObj, anchor, dir))
        {
            DestroyImmediate(rightObj);
            return null;
        }
        SplitMesh(gameObject, rightObj, anchor, dir);
        Center(gameObject);
        Center(rightObj);

        return rightObj;
    }

    // Split leftObj's mesh into two (leftObj and rightObj) along a plane defined by anchor and dir
    static private void SplitMesh(GameObject leftObj, GameObject rightObj, Vector2 anchor, Vector2 dir)
    {
        Mesh rightMesh = rightObj.GetComponent<MeshFilter>().mesh;
        Mesh leftMesh = leftObj.GetComponent<MeshFilter>().mesh;

        List<Vector3> leftVerts = new List<Vector3>();
        List<Vector3> rightVerts = new List<Vector3>();

        // Transform vertices to be relative to the plane
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Matrix4x4 rotate = Matrix4x4.TRS(
            anchor,
            Quaternion.AngleAxis(-angle - 90.0f, Vector3.forward), // Subtract 90 so we get "left" and "right" rather than "up" and "down"
            Vector3.one
        );

        foreach (Vector3 vert in leftMesh.vertices)
        {
            Vector4 point = new Vector4(vert.x, vert.y, vert.z, 1);
            Vector3 transformed = rotate * point;
            leftVerts.Add(transformed);
            rightVerts.Add(transformed);
        }

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
                if (leftVerts[vi].x < 0) left.Add(vi);
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
                    pointA = leftVerts[left[0]];
                    pointB = leftVerts[left[1]];
                    pointC = leftVerts[right[0]];

                    uvA = leftMesh.uv[left[0]];
                    uvB = leftMesh.uv[left[1]];
                    uvC = leftMesh.uv[right[0]];
                }
                else
                {
                    pointA = leftVerts[right[0]];
                    pointB = leftVerts[right[1]];
                    pointC = leftVerts[left[0]];

                    uvA = leftMesh.uv[right[0]];
                    uvB = leftMesh.uv[right[1]];
                    uvC = leftMesh.uv[left[0]];
                }

                // x and y increase based on slope of the edge, so we find where the intersection
                // lies on the axis perpendicular to the plane
                float ratioA = (-pointA.x) / (pointC.x - pointA.x);
                float ratioB = (-pointB.x) / (pointC.x - pointB.x);

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

        // Transform back
        for (int i = 0; i < leftVerts.Count; ++i)
        {
            Vector4 point = new Vector4(leftVerts[i].x, leftVerts[i].y, leftVerts[i].z, 1);
            leftVerts[i] = rotate.inverse * point;
        }
        for (int i = 0; i < rightVerts.Count; ++i)
        {
            Vector4 point = new Vector4(rightVerts[i].x, rightVerts[i].y, rightVerts[i].z, 1);
            rightVerts[i] = rotate.inverse * point;
        }

        leftMesh.vertices = leftVerts.ToArray();
        leftMesh.triangles = leftTris.ToArray();
        leftMesh.uv = leftUV.ToArray();

        rightMesh.vertices = rightVerts.ToArray();
        rightMesh.triangles = rightTris.ToArray();
        rightMesh.uv = rightUV.ToArray();
    }

    // Split leftObj's PolygonCollider2d into two (leftObj and rightObj) along a plane defined by anchor and dir
    static private bool SplitCollider(GameObject leftObj, GameObject rightObj, Vector2 anchor, Vector2 dir)
    {
        PolygonCollider2D leftColl = leftObj.GetComponent<PolygonCollider2D>();
        PolygonCollider2D rightColl = rightObj.GetComponent<PolygonCollider2D>();

        List<Vector2> oldPoints = new List<Vector2>();
        List<Vector2> leftPoints = new List<Vector2>();
        List<Vector2> rightPoints = new List<Vector2>();

        // Transform vertices to be relative to the plane
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Matrix4x4 rotate = Matrix4x4.TRS(
            anchor,
            Quaternion.AngleAxis(-angle - 90.0f, Vector3.forward),
            Vector3.one
        );

        foreach (Vector3 point in leftColl.points)
        {
            Vector4 point4 = new Vector4(point.x, point.y, point.z, 1);
            Vector3 transformed = rotate * point4;
            oldPoints.Add(transformed);
        }

        // Whether the last point was on the left
        bool wasLeft = oldPoints[0].x < 0;
        bool switched = false;

        for (int i = 0; i < oldPoints.Count + 1; ++i)
        {
            Vector2 point = oldPoints[i % oldPoints.Count];
            bool isLeft = point.x < 0;

            // Check if this edge is split (i.e. we've gone from left to right)
            if (wasLeft != isLeft)
            {
                // Add a new point between the two using the slope calculation (see comments in SplitMesh)
                Vector2 lastPoint = oldPoints[i - 1];
                float ratio = (-lastPoint.x) / (point.x - lastPoint.x);
                Vector2 inter = Vector2.Lerp(lastPoint, point, ratio);

                leftPoints.Add(inter);
                rightPoints.Add(inter);

                switched = true;
            }
            
            // Don't duplicate the last point if we're not interpolating it
            if (i < oldPoints.Count)
            {
                if (isLeft) leftPoints.Add(point);
                else rightPoints.Add(point);
            }

            wasLeft = isLeft;
        }

        // Everything is on one side
        if (!switched) return false;

        // Transform back
        for (int i = 0; i < leftPoints.Count; ++i)
        {
            Vector4 point = new Vector4(leftPoints[i].x, leftPoints[i].y, 0, 1);
            leftPoints[i] = rotate.inverse * point;
        }
        for (int i = 0; i < rightPoints.Count; ++i)
        {
            Vector4 point = new Vector4(rightPoints[i].x, rightPoints[i].y, 0, 1);
            rightPoints[i] = rotate.inverse * point;
        }

        leftColl.points = leftPoints.ToArray();
        rightColl.points = rightPoints.ToArray();

        return true;
    }

    // Center an object's mesh and collider (by center of mass)
    static private void Center(GameObject obj)
    {
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        PolygonCollider2D coll = obj.GetComponent<PolygonCollider2D>();

        // Find the average point
        Vector2 sum = new Vector2();
        foreach (Vector2 p in coll.points) sum += p;
        Vector3 center = sum / coll.points.Length;

        // Offset vertices
        Vector3[] newVertices = mesh.vertices;
        for (int i = 0; i < newVertices.Length; ++i)
        {
            newVertices[i] -= center;
        }
        mesh.vertices = newVertices;

        // Offset collider
        Vector2[] newPoints = coll.points;
        for (int i = 0; i < newPoints.Length; ++i)
        {
            newPoints[i] -= new Vector2(center.x, center.y);
        }
        coll.points = newPoints;

        // Finally, move the transform to accommodate
        obj.transform.position += center;
    }
}
