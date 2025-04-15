using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TrackManager : MonoBehaviour
{
    public static TrackManager ins;
    public List<Transform> waypoints = new List<Transform>(); // to store our waypoints.

    // tried auto gen, works but needs more refinement, for which i shouldnt prioritize atm, if later time allows ill get back to this.
    // A prefab, mostly an empty GameObject, to represent a waypoint.
    public Transform waypointPrefab;
    // The number of waypoints we want around the track.
    public int desiredWaypointCount = 20; // the number is the amount of waypoints to generate
    private void Awake()
    {
        ins = this;
        //GenerateAutomaticWaypoints();
        UsePredefinedWaypoints();
    }
    public void UsePredefinedWaypoints()
    {
        waypoints.Clear(); // Clear any existing waypoints

        // Find all child transforms and add them to the waypoints list
        foreach (Transform child in transform)
        {
            waypoints.Add(child);
        }

        Debug.Log($"Predefined Waypoints Count: {waypoints.Count}");
    }
    #region Automatic Waypoints Testing
    // Automatic Gen code below, tried using navmesh to generate..
    public void GenerateAutomaticWaypoints()
    {
        // Calculate the NavMesh triangulation.
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        // Get the convex hull of the triangulated vertices.
        List<Vector3> hullPoints = ComputeConvexHull(triangulation.vertices);

        if (hullPoints.Count == 0)
        {
            Debug.LogWarning("No hull points found. Check your NavMesh boundaries.");
            return;
        }

        // Compute the centroid of the hull. (NEW)
        Vector3 centroid = ComputeCentroid(hullPoints);

        // Evenly sample points along the hull boundary.
        List<Vector3> samplePoints = SamplePointsOnHull(hullPoints, desiredWaypointCount);

        // Reverse the sample points so they follow the proper directional order.
        samplePoints.Reverse();

        // For each sample point, offset it toward the centroid.
        // Adjust factor 0.0 means no offset..
        float offsetFactor = 0.0f; // 15% toward the center, make it 0, if you just want the waypoints to be in the edge of the navmesh surface.
        for (int i = 0; i < samplePoints.Count; i++)
        {
            samplePoints[i] = Vector3.Lerp(samplePoints[i], centroid, offsetFactor);
        }

        // 5. Instantiate waypoint objects at the sampled positions and parent them to this TrackManager.
        foreach (Vector3 pos in samplePoints)
        {
            Transform wp = Instantiate(waypointPrefab, pos, Quaternion.identity, transform);
            waypoints.Add(wp);
        }
    }
    Vector3 ComputeCentroid(List<Vector3> points)
    {
        Vector3 centroid = Vector3.zero;
        foreach (Vector3 p in points)
        {
            centroid += p;
        }
        centroid /= points.Count;
        return centroid;
    }
    // Computes the convex hull of a set of points (ignoring Y differences) using the Jarvis March algorithm.
    List<Vector3> ComputeConvexHull(Vector3[] points)
    {
        List<Vector3> hull = new List<Vector3>();
        if (points.Length < 3)
            return new List<Vector3>(points);

        // Find the leftmost point (lowest X).
        int leftMost = 0;
        for (int i = 1; i < points.Length; i++)
        {
            if (points[i].x < points[leftMost].x)
                leftMost = i;
        }

        int p = leftMost;
        do
        {
            hull.Add(points[p]);
            int q = (p + 1) % points.Length;
            // Find the point that makes the most counter-clockwise turn from p.
            for (int r = 0; r < points.Length; r++)
            {
                if (Cross(points[p], points[q], points[r]) < 0)
                    q = r;
            }
            p = q;
        } while (p != leftMost);

        return hull;
    }

    // Helper: returns the cross product (2D, using X and Z coordinates) to determine orientation.
   
    float Cross(Vector3 p, Vector3 q, Vector3 r)
    {
        // Create two vectors from point p.
        Vector2 a = new Vector2(q.x - p.x, q.z - p.z);
        Vector2 b = new Vector2(r.x - p.x, r.z - p.z);
        // Cross product value.
        return a.x * b.y - a.y * b.x;
    }
    // Evenly samples points along the boundary defined by the convex hull.
    List<Vector3> SamplePointsOnHull(List<Vector3> hull, int count)
    {
        List<Vector3> samples = new List<Vector3>();
        if (hull.Count == 0)
            return samples;

        // Calculate the perimeter length.
        float totalLength = 0f;
        List<float> segmentLengths = new List<float>();
        for (int i = 0; i < hull.Count; i++)
        {
            int next = (i + 1) % hull.Count;
            float seg = Vector3.Distance(hull[i], hull[next]);
            segmentLengths.Add(seg);
            totalLength += seg;
        }

        // Spacing between each waypoint.
        float spacing = totalLength / count;
        float distanceAccum = 0f;
        int currentSegment = 0;

        // Walk along the hull, sampling points at intervals of 'spacing'.
        while (samples.Count < count)
        {
            // Advance current segment until we find where the next sample falls.
            while (currentSegment < segmentLengths.Count && distanceAccum > segmentLengths[currentSegment])
            {
                distanceAccum -= segmentLengths[currentSegment];
                currentSegment = (currentSegment + 1) % hull.Count;
            }
            int nextIndex = (currentSegment + 1) % hull.Count;
            float t = (segmentLengths[currentSegment] > 0f) ? distanceAccum / segmentLengths[currentSegment] : 0f;
            Vector3 samplePoint = Vector3.Lerp(hull[currentSegment], hull[nextIndex], t);
            samples.Add(samplePoint);

            // Move along the hull by spacing.
            distanceAccum += spacing;
        }

        return samples;
    }
    #endregion
}
