using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerObject : NetworkBehaviour
{
    // for player position tracking.
    [Networked] public int currentWaypointIndex { get; set; }
    [Networked] public float progress { get; set; }
    [Networked] public int lapsCompleted { get; set; }
    public float distanceToNext { get; private set; }

    // Lap timing data
    [Networked] public float currentLapStartTime { get; set; }
    [Networked] public float lastLapTime { get; set; }
    [Networked] public float bestLapTime { get; set; }

    // Total race time accumulator
    [Networked] public float totalRaceTime { get; set; }

    // Track previous progress to detect changes manually
    private float previousProgress;

    // For smoother progress calculation
    private float previousDistanceToNext;
    private Vector3 lastPosition;

    public override void Spawned()
    {
        // Register all players, not just the one with input authority
        RaceManager.ins.RegisterPlayer(this);
        previousProgress = progress;
        lastPosition = transform.position;

        // Initialize lap tracking
        lapsCompleted = 0;
        currentLapStartTime = (float)Runner.SimulationTime;
        lastLapTime = 0;
        bestLapTime = float.MaxValue;
        totalRaceTime = 0;  //initialize totalRaceTime to zero at the start
    }

    public override void FixedUpdateNetwork()
    {
        // Only process for the local player
        if (!Object.HasInputAuthority) return;

        var waypoints = TrackManager.ins.waypoints;
        int nextIndex = (currentWaypointIndex + 1) % waypoints.Count;

        float dist = Vector3.Distance(transform.position, waypoints[nextIndex].position);

        // Check if we've passed a waypoint
        if (dist < 10f) // proximity threshold to "hit" waypoint
        {
            currentWaypointIndex = nextIndex;
            nextIndex = (currentWaypointIndex + 1) % waypoints.Count;
            dist = Vector3.Distance(transform.position, waypoints[nextIndex].position);

            // Check if we completed a lap (crossing start/finish line)
            if (nextIndex == 0)
            {
                // Calculate lap time
                float currentTime = (float)Runner.SimulationTime;
                float lapTime = currentTime - currentLapStartTime;

                // Store lap time
                lastLapTime = lapTime;

                // Update best lap time if this lap was faster
                if (lapTime < bestLapTime)
                {
                    bestLapTime = lapTime;
                }

                // ACCUMULATE the lap time into totalRaceTime.
                totalRaceTime += lapTime;    //This records the lap time exactly when the lap finishes

                // Reset lap timer
                currentLapStartTime = currentTime;

                // Increment lap counter
                lapsCompleted++;

                Debug.Log($"Player {Object.Id} completed lap {lapsCompleted} in {lapTime:F2}s (Best: {bestLapTime:F2}s) - Total: {totalRaceTime:F2}s");
            }
        }

        distanceToNext = dist;

        // Rest of your existing progress calculation code
        float totalWaypoints = waypoints.Count;
        float waypointProgress = (float)currentWaypointIndex / totalWaypoints;

        int prevIndex = (currentWaypointIndex + waypoints.Count - 1) % waypoints.Count;
        float segmentLength = Vector3.Distance(waypoints[prevIndex].position, waypoints[nextIndex].position);

        Vector3 prevToNext = waypoints[nextIndex].position - waypoints[prevIndex].position;
        Vector3 prevToPlayer = transform.position - waypoints[prevIndex].position;

        float projectionDistance = Vector3.Dot(prevToPlayer, prevToNext.normalized);
        float segmentProgress = Mathf.Clamp01(projectionDistance / segmentLength);

        float segmentContribution = 1.0f / totalWaypoints;
        float distanceProgress = segmentProgress * segmentContribution;

        float newProgress = waypointProgress + distanceProgress;

        Vector3 movementSinceLastUpdate = transform.position - lastPosition;
        float forwardMovement = Vector3.Dot(movementSinceLastUpdate, transform.forward);
        float movementBonus = Mathf.Max(0, forwardMovement * 0.001f);

        newProgress += movementBonus;
        newProgress = Mathf.Clamp01(newProgress);
        lastPosition = transform.position;

        // Always update progress
        progress = newProgress;

        // Mark that we've changed the progress value
        if (Mathf.Abs(previousProgress - newProgress) > 0.0001f)
        {
            previousProgress = newProgress;
            NotifyProgressChanged();
        }
    }

    // This will be called on all clients after network sync in their respective Render updates
    public override void Render()
    {
        // Check if the progress has changed from network updates
        if (Mathf.Abs(previousProgress - progress) > 0.0001f) // Smaller threshold for change detection, I would do some more test runs and change this to more efficient but in this case going with this value.
        {
            previousProgress = progress;
            NotifyProgressChanged();
        }
    }

    // Notify the RaceManager that progress has changed
    private void NotifyProgressChanged()
    {
        if (RaceManager.ins != null)
        {
            RaceManager.ins.OnPlayerProgressUpdated();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        RaceManager.ins.UnregisterPlayer(this);
    }
}
