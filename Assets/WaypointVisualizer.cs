using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// This is just a visualizer class to help me see the waypoints in the editor.
[ExecuteInEditMode]
public class WaypointVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] private float waypointRadius = 0.5f;
    [SerializeField] private float thresholdRadius = 10f; // Match the threshold from PlayerObject.cs
    [SerializeField] private Color waypointColor = Color.yellow;
    [SerializeField] private Color thresholdColor = new Color(1f, 0.5f, 0f, 0.3f); // Orange with transparency
    [SerializeField] private bool showWaypointNumbers = true;
    [SerializeField] private bool showThresholdAreas = true;

    private void OnDrawGizmos()
    {
        if (!TrackManager.ins) return;

        var waypoints = TrackManager.ins.waypoints;
        if (waypoints == null || waypoints.Count == 0) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform waypoint = waypoints[i];
            if (waypoint == null) continue;

            // Draw waypoint sphere
            Gizmos.color = waypointColor;
            Gizmos.DrawSphere(waypoint.position, waypointRadius);

            // Draw threshold area
            if (showThresholdAreas)
            {
                Gizmos.color = thresholdColor;
                Gizmos.DrawSphere(waypoint.position, thresholdRadius);
            }

            // Draw waypoint number
            if (showWaypointNumbers)
            {
#if UNITY_EDITOR
                Handles.Label(waypoint.position + Vector3.up * (waypointRadius + 0.2f), i.ToString());
#endif
            }

            // Draw line to next waypoint
            int nextIndex = (i + 1) % waypoints.Count;
            if (waypoints[nextIndex] != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(waypoint.position, waypoints[nextIndex].position);
            }
        }
    }
}