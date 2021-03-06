﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

// An object which can be split up by the portal.
public class Splittable : MonoBehaviour
{
    // A split object with a size below this threshold will be deleted
    static private float minSize = 0.00001f;
    private Vector3 startPoint;

    enum Side
    {
        Neither,
        Left,
        Right,
        Both
    }

    private bool _isSplit = false;
    public bool IsSplit
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
    public Bounds TotalBounds
    {
        get
        {
            // In case we can't find any relevant children
            var defaultBounds = new Bounds(transform.position, new Vector3());

            if (transform.childCount == 0) return defaultBounds;

            Bounds? total = null;
            for (int i = 0; i < transform.childCount; ++i)
            {
                var child = transform.GetChild(i);

                // Only count mesh renderers in the total bounds
                var renderer = child.GetComponent<MeshRenderer>();
                if (!renderer) continue;

                // If we haven't set the total yet, just use this one
                if (total == null)
                    total = renderer.bounds;
                // Otherwise, encapsulate the new bounds
                else
                {
                    var newValue = total.Value;
                    newValue.Encapsulate(renderer.bounds);

                    total = newValue;
                }
            }

            if (total == null) return defaultBounds;

            // Set z to 0
            var result = total.Value;
            result.center = new Vector3(result.center.x, result.center.y, 0);
            result.extents = new Vector3(result.extents.x, result.extents.y, 0);
            return result;
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

        // transform.InverseTransformDirection() is unaffected by scale, but we need to take it into account.
        // If the x-axis is flipped, then what we call "left" of the direction is also flipped, but we have
        // the same vertical direction, so negate the y direction to account for this. The opposite applies
        // for the x-axis.
        if (transform.localScale.x < 0)
            dir.y = -dir.y;

        if (transform.localScale.y < 0)
            dir.x = -dir.x;

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

            // This is specifically tagged to not copy, so don't copy it over
            if (leftChild.tag == "No Copy on Split")
            {
                DestroyImmediate(rightChild.gameObject);
                continue;
            }

            // Ignore children that are neither visible nor collidable
            if (leftChild.GetComponent<MeshRenderer>() == null && leftChild.GetComponent<PolygonCollider2D>() == null)
                continue;

            // Take the object's local transform into account
            var localMatrix = matrix * Matrix4x4.TRS(
                leftChild.transform.localPosition,
                leftChild.transform.localRotation,
                leftChild.transform.localScale
            );

            // This is where we actually split the object
            Side meshSide = SplitMesh(leftChild.gameObject, rightChild.gameObject, localMatrix);
            Side collSide = SplitCollider(leftChild.gameObject, rightChild.gameObject, localMatrix);

            // Calculate sizes of new renderers to check if they're practically invisible
            var rightRenderer = rightChild.GetComponent<Renderer>();
            var leftRenderer = leftChild.GetComponent<Renderer>();
            bool rightTooSmall = false;
            bool leftTooSmall = false;

            if (rightRenderer)
            {
                var rightRendererSize = rightRenderer.bounds.size;
                rightTooSmall = Mathf.Min(rightRendererSize.x, rightRendererSize.y) < minSize;
            }

            if (leftRenderer) {
                var leftRendererSize = leftRenderer.bounds.size;
                leftTooSmall = Mathf.Min(leftRendererSize.x, leftRendererSize.y) < minSize;
            }

            // Check if a mesh/collider is entirely on one side, on no side at all, or is too small to bother with
            bool meshAllLeft = (meshSide == Side.Left || meshSide == Side.Neither || rightTooSmall);
            bool meshAllRight = (meshSide == Side.Right || meshSide == Side.Neither || leftTooSmall);
            bool collAllLeft = (collSide == Side.Left || collSide == Side.Neither);
            bool collAllRight = (collSide == Side.Right || collSide == Side.Neither);

            // On left; destroy right copy
            if (meshAllLeft && collAllLeft)
            {
                rightChild.parent = null;
                Destroy(rightChild.gameObject);
                rightChild = null;
            }

            // On right; destroy left copy
            if (meshAllRight && collAllRight)
            {
                leftChild.parent = null;
                Destroy(leftChild.gameObject);
                leftChild = null;
            }
        }
        
        // Copy over physics info
        Rigidbody2D leftPhys = GetComponent<Rigidbody2D>();
        Rigidbody2D rightPhys = rightParent.GetComponent<Rigidbody2D>();
        if (leftPhys != null)
        {
            rightPhys.velocity = leftPhys.velocity;
            rightPhys.angularVelocity = leftPhys.angularVelocity;

            float leftArea = CalculateArea(transform);
            float rightArea = CalculateArea(rightParent.transform);
            float totalArea = leftArea + rightArea;

            var totalMass = leftPhys.mass;
            leftPhys.mass = totalMass * leftArea / totalArea;
            rightPhys.mass = totalMass * rightArea / totalArea;
        }

        // Remove animations
        Animator leftAnim = GetComponent<Animator>();
        if (leftAnim)
        {
            Destroy(leftAnim);
            Destroy(rightParent.GetComponent<Animator>());
        }

        // Clean up parents with no children
        if (rightParent.transform.childCount == 0)
        {
            Destroy(rightParent);
            rightParent = null;
        }

        var leftParent = gameObject;
        if (transform.childCount == 0)
        {
            Destroy(leftParent);
            leftParent = null;
        }

        // Mark that both sides have been split
        _isSplit = true;
        if (rightParent != null)
        {
            rightParent.GetComponent<Splittable>()._isSplit = true;
        }

        List<GameObject> gameObjectList = new List<GameObject>
        {
            leftParent,
            rightParent
        };
        return gameObjectList;
    }

    // Calculate the area of a splittable object
    static private float CalculateArea(Transform obj)
    {
        float area = 0.0f;
        for (var i = 0; i < obj.childCount; ++i)
        {
            var child = obj.GetChild(i);
            var filter = child.GetComponent<MeshFilter>();

            if (filter == null) continue;

            var tris = filter.mesh.triangles;
            var verts = filter.mesh.vertices;

            // Add each triangle's area to the sum
            for (int ti = 0; ti < verts.Length; ti += 3)
            {
                // http://answers.unity3d.com/answers/291985/view.html
                var v1 = verts[tris[ti]];
                var v2 = verts[tris[ti + 1]];
                var v3 = verts[tris[ti + 2]];

                var cross = Vector3.Cross(v1 - v2, v1 - v3);
                area += Mathf.Abs(cross.z) * 0.5f;
            }
        }

        return area;
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
        Mesh mesh = new Mesh()
        {
            vertices = Array.ConvertAll(sr.sprite.vertices, item => (Vector3)item),
            uv = sr.sprite.uv,
            triangles = Array.ConvertAll(sr.sprite.triangles, item => (int)item)
        };

        // Remove sprite renderer
        var sortingLayerID = sr.sortingLayerID;
        var sortingLayerName = sr.sortingLayerName;
        var sortingOrder = sr.sortingOrder;
        DestroyImmediate(sr);

        // Create mesh filter
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        // Create mesh renderer
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = mat;
        mr.material.mainTexture = tex;
        mr.sortingLayerID = sortingLayerID;
        mr.sortingLayerName = sortingLayerName;
        mr.sortingOrder = sortingOrder;

        mesh.RecalculateBounds();
    }

    // Split leftObj's mesh into two (leftObj and rightObj) along the Y axis after transformation by matrix
    static private Side SplitMesh(GameObject leftObj, GameObject rightObj, Matrix4x4 matrix)
    {
        Matrix4x4 matInverse = matrix.inverse;

        var rightFilter = rightObj.GetComponent<MeshFilter>();
        var leftFilter = leftObj.GetComponent<MeshFilter>();

        // If either side is missing a mesh filter, then there's no point in splitting the mesh
        if (rightFilter == null || leftFilter == null) return Side.Neither;

        var rightMesh = rightFilter.mesh;
        var leftMesh = leftFilter.mesh;

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

        // All on the right; destroy the left side
        if (leftCount == 0)
        {
            Destroy(leftFilter);
            Destroy(leftObj.GetComponent<MeshRenderer>());
            return Side.Right;
        }
        // All on the left; destroy the right side
        else if (leftCount == leftVerts.Count)
        {
            Destroy(rightFilter);
            Destroy(rightObj.GetComponent<MeshRenderer>());
            return Side.Left;
        }

        // UV coordinates on each side
        List<Vector2> leftUV = new List<Vector2>(leftMesh.uv);
        List<Vector2> rightUV = new List<Vector2>(rightMesh.uv);

        // Triangles on each side
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

            // For each vertex in the triangle, determine whether it's left or right of the Y axis
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
                // so the edge tuple is consistent for lookup in the *Inters dicts
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

                // No matter what, we have two vertices on one side and one vertex on the other. The side with one vertex will
                // produce three edges, i.e. a single triangle, while the side with two vertices will result in four edges, i.e.
                // two triangles.
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

        // Transform back to local coordinates
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
        
        // Update the actual meshes
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

        return Side.Both; // Note this is currently also triggered if the mesh is empty
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

        // Any vertices whose IDs are in the triangle array are in use
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

        // As we remove vertices, we'll be decrementing the IDs of any higher vertices. We reverse the list of
        // vertices to be removed here so that we don't affect the IDs of other vertices that will be removed
        // later (i.e. if we remove vertex 1, then vertex 2 is now vertex 1, so our "removed" list would now be
        // wrong; instead, we remove vertex 2 first, then remove vertex 1).
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

    // Split leftObj's PolygonCollider2d into two (leftObj and rightObj) along the Y axis after transformation by matrix
    static private Side SplitCollider(GameObject leftObj, GameObject rightObj, Matrix4x4 matrix)
    {
        Matrix4x4 matInverse = matrix.inverse;

        PolygonCollider2D leftColl = leftObj.GetComponent<PolygonCollider2D>();
        PolygonCollider2D rightColl = rightObj.GetComponent<PolygonCollider2D>();

        if (leftColl == null || rightColl == null) return Side.Neither;

        bool anyLeft = false;
        bool anyRight = false;

        for (int pathIndex = 0; pathIndex < leftColl.pathCount; ++pathIndex)
        {
            Vector2[] path = leftColl.GetPath(pathIndex);

            // New collider points
            List<Vector2> leftPoints = new List<Vector2>();
            List<Vector2> rightPoints = new List<Vector2>();

            // List of the left object's pre-split points
            List<Vector2> oldPoints = new List<Vector2>();

            // Transform points to be relative to cutting plane (Y axis)
            foreach (Vector3 point in path)
            {
                Vector3 transformed = matrix.MultiplyPoint3x4(point);
                oldPoints.Add(transformed);
            }

            // Whether the previous point was on the left
            bool wasLeft = oldPoints[0].x < 0;

            for (int i = 0; i < oldPoints.Count + 1; ++i)
            {
                // Each path in the collider is closed, so our "next" point is the first point if we're at the end of the list
                Vector2 point = oldPoints[i % oldPoints.Count];
                bool isLeft = point.x < 0;

                // Check if this edge is split (i.e. we've gone from a point on the left to a point on the right)
                if (wasLeft != isLeft)
                {
                    // Add a new point between the two using the slope calculation (see comments in IntersectEdge)
                    Vector2 lastPoint = oldPoints[i - 1];
                    float ratio = (-lastPoint.x) / (point.x - lastPoint.x);
                    Vector2 inter = Vector2.Lerp(lastPoint, point, ratio);

                    leftPoints.Add(inter);
                    rightPoints.Add(inter);
                }

                // If we're at the last point, then point is already in the list, so don't add it
                if (i < oldPoints.Count)
                {
                    if (isLeft) leftPoints.Add(point);
                    else rightPoints.Add(point);
                }

                wasLeft = isLeft;
            }

            // Transform back to local coordinates
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

            // Update paths if valid
            if (leftPoints.Count > 0)
            {
                anyLeft = true;
                leftColl.SetPath(pathIndex, leftPoints.ToArray());
            }

            if (rightPoints.Count > 0)
            {
                anyRight = true;
                rightColl.SetPath(pathIndex, rightPoints.ToArray());
            }
        }

        if (!anyLeft) Destroy(leftColl);
        if (!anyRight) Destroy(rightColl);

        if (!anyLeft && !anyRight) return Side.Neither;
        if (!anyLeft) return Side.Left;
        if (!anyRight) return Side.Left;

        return Side.Both;
    }
}
