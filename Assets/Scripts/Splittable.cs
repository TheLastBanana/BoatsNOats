using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

// An object which can be split up by the portal.
public class Splittable : MonoBehaviour
{
    static private float minSizeMagnitude = 0.00001f;
    private Vector3 startPoint;

    private bool _isSplit = false;
    public bool isSplit
    {
        get
        {
            return _isSplit;
        }
    }

    // Represents an edge between two vertices
    private struct Edge
    {
        public Edge(int a, int b)
        {
            this.a = a;
            this.b = b;
        }

        public int a, b;
    }

    // Encapsulated bounds of all children
    public Bounds totalBounds
    {
        get
        {
            if (transform.childCount == 0) return new Bounds();

            Bounds totalBounds = transform.GetChild(0).GetComponent<Renderer>().bounds;

            for (int i = 1; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i).transform;
                var childBounds = child.GetComponent<Renderer>().bounds;

                totalBounds.Encapsulate(childBounds);
            }

            totalBounds.center = new Vector3(totalBounds.center.x, totalBounds.center.y, 0);
            totalBounds.extents = new Vector3(totalBounds.extents.x, totalBounds.extents.y, 0);
            return totalBounds;
        }
    }

    void Start()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            ConvertToMesh(transform.GetChild(i).gameObject);
        }
    }

    // Split the object along a plane defined by anchor and dir.
    // Return a list. The first entry is the object on the left; the second entry is the object on the right
    public List<GameObject> SplitOnPlane(Vector2 anchor, Vector2 dir)
    {
        anchor = transform.InverseTransformPoint(anchor);
        dir = transform.InverseTransformDirection(dir);

        // Matrix to transform vertices so they're relative to the plane
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // We want to move points so the anchor is the origin, then rotate. Matrix operations are in reverse order,
        // so build a rotation matrix, then multiply by a translation matrix.
        Matrix4x4 matrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.AngleAxis(-angle + 90.0f, Vector3.forward), // Add 90 so we get "left" and "right" rather than "up" and "down"
            Vector3.one
        );
        matrix *= Matrix4x4.TRS(
            -anchor,
            Quaternion.identity,
            Vector3.one
        );

        GameObject rightParent = Instantiate(gameObject, transform.parent);

        // Copy over physics info
        Rigidbody2D leftPhys = GetComponent<Rigidbody2D>();
        if (leftPhys != null)
        {
            Rigidbody2D rightPhys = rightParent.GetComponent<Rigidbody2D>();
            rightPhys.velocity = leftPhys.velocity;
            rightPhys.angularVelocity = leftPhys.angularVelocity;
        }

        // Get children so we can remove them as we go
        int childCount = transform.childCount;
        List<Transform> leftChildren = new List<Transform>();
        List<Transform> rightChildren = new List<Transform>();

        for (int i = 0; i < childCount; ++i)
        {
            leftChildren.Add(transform.GetChild(i));
            rightChildren.Add(rightParent.transform.GetChild(i));
        }

        // Split children
        for (int i = 0; i < childCount; ++i)
        {
            Transform leftChild = leftChildren[i];
            Transform rightChild = rightChildren[i];

            int splitResult = SplitMesh(leftChild.gameObject, rightChild.gameObject, matrix);

            bool intersected = true;
            // On left; destroy right copy
            if (splitResult == -1 || rightChild.GetComponent<Renderer>().bounds.extents.sqrMagnitude < minSizeMagnitude)
            {
                rightChild.parent = null;
                Destroy(rightChild.gameObject);
                intersected = false;
            }

            // On right; destroy left copy
            if (splitResult == 1 || leftChild.GetComponent<Renderer>().bounds.extents.sqrMagnitude < minSizeMagnitude)
            {
                leftChild.parent = null;
                Destroy(leftChild.gameObject);
                intersected = false;
            }

            // Split intersected, so split collider
            if (intersected) SplitCollider(leftChild.gameObject, rightChild.gameObject, matrix);
        }

        bool anySplits = false;

        // Clean up parents
        if (rightParent.transform.childCount == 0) Destroy(rightParent);
        else anySplits = true;

        if (transform.childCount == 0) Destroy(gameObject);
        else anySplits = true;

        if (!anySplits) return null;

        _isSplit = true;
        if (rightParent) rightParent.GetComponent<Splittable>()._isSplit = true;

        List<GameObject> gameObjectList = new List<GameObject>();
        gameObjectList.Add(gameObject);
        gameObjectList.Add(rightParent);
        return gameObjectList;
    }

    // Convert the SpriteRenderer to a mesh
    static private void ConvertToMesh(GameObject gameObject)
    {
        // Get sprite info (if there is one)
        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        Texture tex = sr.sprite.texture;
        Material mat = sr.material;
        sr.enabled = false;

        // Create mesh, copying the mesh data of the sprite
        Mesh mesh = new Mesh();
        mesh.vertices = Array.ConvertAll(sr.sprite.vertices, item => (Vector3)item);
        mesh.uv = sr.sprite.uv;
        mesh.triangles = Array.ConvertAll(sr.sprite.triangles, item => (int)item);

        // Remove sprite renderer
        DestroyImmediate(sr);

        // Create mesh filter
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        // Create mesh renderer
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = mat;
        mr.material.mainTexture = tex;

        mesh.RecalculateBounds();
    }

    // Split leftObj's mesh into two (leftObj and rightObj) along a plane defined by anchor and dir
    // Returns 0 if intersected, -1 if entirely on left, 1 if entirely on right
    static private int SplitMesh(GameObject leftObj, GameObject rightObj, Matrix4x4 matrix)
    {
        Matrix4x4 matInverse = matrix.inverse;

        Mesh rightMesh = rightObj.GetComponent<MeshFilter>().mesh;
        Mesh leftMesh = leftObj.GetComponent<MeshFilter>().mesh;

        List<Vector3> leftVerts = new List<Vector3>();
        List<Vector3> rightVerts = new List<Vector3>();

        var meshVertices = leftMesh.vertices;
        var meshTriangles = leftMesh.triangles;
        var meshUVs = leftMesh.uv;

        // Transform points to be relative to cutting plane
        foreach (Vector3 vert in meshVertices)
        {
            Vector3 transformed = matrix.MultiplyPoint3x4(vert);
            leftVerts.Add(transformed);
            rightVerts.Add(transformed);
        }

        // Early exit if everything's on one side
        int leftCount = 0;
        foreach (var vert in leftVerts)
            if (vert.x < 0) leftCount += 1;

        // All on the right
        if (leftCount == 0) return 1;

        // All on the left
        else if (leftCount == leftVerts.Count) return -1;

         List<Vector2> leftUV = new List<Vector2>(leftMesh.uv);
        List<Vector2> rightUV = new List<Vector2>(rightMesh.uv);

        // Polygons on each side
        List<int> leftTris = new List<int>();
        List<int> rightTris = new List<int>();

        // Cache of newly-created vertices for intersecting edges
        Dictionary<Edge, int> leftInters = new Dictionary<Edge, int>();
        Dictionary<Edge, int> rightInters = new Dictionary<Edge, int>();

        // Sort triangles to sides
        var numMeshTris = meshTriangles.Length;
        for (int i = 0; i < numMeshTris; i += 3)
        {
            List<int> left = new List<int>();
            List<int> right = new List<int>();

            for (int j = i; j < i + 3; ++j)
            {
                int vi = meshTriangles[j];
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
                int vertA, vertB, vertC;
                Vector2 pointA, pointB, pointC;
                Vector2 uvA, uvB, uvC;

                // Two cases: one point right, two points left; or one point left, two points right
                // Either way, the logic is similar. Let point C be the point that is alone on its side
                if (left.Count == 2)
                {
                    vertA = left[0];
                    vertB = left[1];
                    vertC = right[0];
                }
                else
                {
                    vertA = right[0];
                    vertB = right[1];
                    vertC = left[0];
                }

                pointA = leftVerts[vertA];
                pointB = leftVerts[vertB];
                pointC = leftVerts[vertC];

                uvA = meshUVs[vertA];
                uvB = meshUVs[vertB];
                uvC = meshUVs[vertC];

                int leftInterIdA, rightInterIdA, leftInterIdB, rightInterIdB;

                // Determine edges we're looking at. Always put the smallest vert id first
                // so the edge tuple is consistent.
                Edge edgeA = new Edge(Math.Min(vertA, vertC), Math.Max(vertA, vertC));
                Edge edgeB = new Edge(Math.Min(vertB, vertC), Math.Max(vertB, vertC));

                // If the point isn't cached, pass it into this horrible cluster of arguments to calculate the intersection
                if (!leftInters.ContainsKey(edgeA))
                    IntersectEdge(edgeA, pointA, pointC, uvA, uvC, leftVerts, rightVerts, leftUV, rightUV, leftInters, rightInters);

                leftInterIdA = leftInters[edgeA];
                rightInterIdA = rightInters[edgeA];

                // Do the same thing for edge B
                if (!leftInters.ContainsKey(edgeB))
                    IntersectEdge(edgeB, pointB, pointC, uvB, uvC, leftVerts, rightVerts, leftUV, rightUV, leftInters, rightInters);

                leftInterIdB = leftInters[edgeB];
                rightInterIdB = rightInters[edgeB];

                if (left.Count == 2)
                {
                    // Add right triangle
                    rightTris.Add(right[0]);
                    rightTris.Add(rightInterIdB);
                    rightTris.Add(rightInterIdA);

                    // Add left triangles
                    leftTris.Add(left[0]);
                    leftTris.Add(left[1]);
                    leftTris.Add(leftInterIdA);

                    leftTris.Add(left[1]);
                    leftTris.Add(leftInterIdB);
                    leftTris.Add(leftInterIdA);
                }
                else
                {
                    // Add left triangle
                    leftTris.Add(left[0]);
                    leftTris.Add(leftInterIdB);
                    leftTris.Add(leftInterIdA);

                    // Add right triangles
                    rightTris.Add(right[0]);
                    rightTris.Add(right[1]);
                    rightTris.Add(rightInterIdA);

                    rightTris.Add(right[1]);
                    rightTris.Add(rightInterIdB);
                    rightTris.Add(rightInterIdA);
                }
            }
        }

        // Transform back
        for (int i = 0; i < leftVerts.Count; ++i)
        {
            Vector4 point = new Vector4(leftVerts[i].x, leftVerts[i].y, leftVerts[i].z, 1);
            leftVerts[i] = matInverse * point;
        }
        for (int i = 0; i < rightVerts.Count; ++i)
        {
            Vector4 point = new Vector4(rightVerts[i].x, rightVerts[i].y, rightVerts[i].z, 1);
            rightVerts[i] = matInverse * point;
        }

        leftMesh.vertices = leftVerts.ToArray();
        leftMesh.triangles = leftTris.ToArray();
        leftMesh.uv = leftUV.ToArray();

        rightMesh.vertices = rightVerts.ToArray();
        rightMesh.triangles = rightTris.ToArray();
        rightMesh.uv = rightUV.ToArray();

        // Clean up any leftover vertices
        CleanUpVertices(leftMesh);
        CleanUpVertices(rightMesh);

        // Recalculate bounds for renderer
        leftMesh.RecalculateBounds();
        rightMesh.RecalculateBounds();

        return 0; // Note this is also triggered if the mesh is empty
    }

    // Used within SplitMesh to find the intersection point between the plane and a mesh edge
    static private void IntersectEdge(Edge edge, Vector2 start, Vector2 end, Vector2 startUV, Vector2 endUV,
        List<Vector3> leftVerts, List<Vector3> rightVerts, List<Vector2> leftUV, List<Vector2> rightUV,
        Dictionary<Edge, int> leftInters, Dictionary<Edge, int> rightInters)
    {
        // x and y increase based on slope of the edge, so we find where the intersection
        // lies on the axis perpendicular to the plane
        float ratio = (-start.x) / (end.x - start.x);

        // Now use the ratio to calculate intersection points
        Vector2 inter = Vector2.Lerp(start, end, ratio);

        // Add them to the vertex lists
        leftVerts.Add(inter);
        rightVerts.Add(inter);

        // Add corresponding UV coords
        Vector2 interUVA = Vector2.Lerp(startUV, endUV, ratio);
        leftUV.Add(interUVA);
        rightUV.Add(interUVA);

        // Cache this index
        leftInters[edge] = leftVerts.Count - 1;
        rightInters[edge] = rightVerts.Count - 1;
    }

    // Remove any unused vertices
    static private void CleanUpVertices(Mesh mesh)
    {
        List<Vector3> tempVerts = new List<Vector3>();
        List<Vector2> tempUV = new List<Vector2>();
        List<int> removed = new List<int>();
        HashSet<int> usedVertices = new HashSet<int>();

        var meshTriangles = mesh.triangles;
        var meshVertices = mesh.vertices;
        var meshUVs = mesh.uv;

        var numMeshTris = meshTriangles.Length;
        var numMeshVerts = meshVertices.Length;

        for (int i = 0; i < numMeshTris; ++i)
            usedVertices.Add(meshTriangles[i]);

        // Rebuild mesh list
        for (int i = 0; i < numMeshVerts; ++i)
        {
            // No triangle uses this index, so mark it to be removed
            if (!usedVertices.Contains(i))
            {
                removed.Add(i);
                continue;
            }

            tempVerts.Add(meshVertices[i]);
            tempUV.Add(meshUVs[i]);
        }

        // Reverse the list so we don't also have to decrement higher removed indices
        removed.Reverse();

        int[] tempTriangles = meshTriangles.Clone() as int[];
        foreach (int index in removed)
        {
            // Decrement any higher indices to reflect the vertex's removal
            for (int j = 0; j < tempTriangles.Length; ++j)
            {
                if (tempTriangles[j] >= index) --tempTriangles[j];
            }
        }

        mesh.triangles = tempTriangles;
        mesh.vertices = tempVerts.ToArray();
        mesh.uv = tempUV.ToArray();
    }

    // Split leftObj's PolygonCollider2d into two (leftObj and rightObj) along a plane defined by anchor and dir
    static private void SplitCollider(GameObject leftObj, GameObject rightObj, Matrix4x4 matrix)
    {
        Matrix4x4 matInverse = matrix.inverse;

        PolygonCollider2D leftColl = leftObj.GetComponent<PolygonCollider2D>();
        PolygonCollider2D rightColl = rightObj.GetComponent<PolygonCollider2D>();

        if (leftColl == null || rightColl == null) return;

        for (int pathIndex = 0; pathIndex < leftColl.pathCount; ++pathIndex)
        {
            Vector2[] path = leftColl.GetPath(pathIndex);

            List<Vector2> oldPoints = new List<Vector2>();
            List<Vector2> leftPoints = new List<Vector2>();
            List<Vector2> rightPoints = new List<Vector2>();

            // Transform points to be relative to cutting plane
            foreach (Vector3 point in path)
            {
                Vector3 transformed = matrix.MultiplyPoint3x4(point);
                oldPoints.Add(transformed);
            }

            // Whether the last point was on the left
            bool wasLeft = oldPoints[0].x < 0;

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
                }

                // Don't duplicate the last point if we're not interpolating it
                if (i < oldPoints.Count)
                {
                    if (isLeft) leftPoints.Add(point);
                    else rightPoints.Add(point);
                }

                wasLeft = isLeft;
            }

            // Transform back
            for (int i = 0; i < leftPoints.Count; ++i)
            {
                Vector4 point = new Vector4(leftPoints[i].x, leftPoints[i].y, 0, 1);
                leftPoints[i] = matInverse * point;
            }
            for (int i = 0; i < rightPoints.Count; ++i)
            {
                Vector4 point = new Vector4(rightPoints[i].x, rightPoints[i].y, 0, 1);
                rightPoints[i] = matInverse * point;
            }

            if (leftPoints.Count == 0) Destroy(leftColl);
            else leftColl.SetPath(pathIndex, leftPoints.ToArray());

            if (rightPoints.Count == 0) Destroy(rightColl);
            else rightColl.SetPath(pathIndex, rightPoints.ToArray());
        }
    }
}
