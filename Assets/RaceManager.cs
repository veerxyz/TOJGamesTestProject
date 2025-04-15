using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class RaceManager : MonoBehaviour
{
    public static RaceManager ins;
    public List<PlayerObject> players = new();

    [Header("Race Settings")]
    [SerializeField] private int totalLapsForRace = 1; // Change in Inspector

    [Header("Race Timing")]
    private float raceStartTime;
    private bool raceActive = false;

    // Event for UI components to subscribe to
    public System.Action OnRankingsChanged;
    public System.Action OnRaceStarted;
    public System.Action OnRaceEnded;

    // Cache the sorted list to avoid resorting every frame
    private List<PlayerObject> cachedSortedPlayers = new();
    private bool isDirty = true;

    public int TotalLapsForRace => totalLapsForRace;
    public float RaceElapsedTime => raceActive ? Time.time - raceStartTime : 0;

    void Awake()
    {
        ins = this;
        StartRace();
    }

    public void StartRace()
    {
        raceStartTime = Time.time;
        raceActive = true;
        OnRaceStarted?.Invoke();
    }

    public void EndRace()
    {
        raceActive = false;
        OnRaceEnded?.Invoke();
    }

    // Called when any player's progress is updated (from PlayerObject)
    public void OnPlayerProgressUpdated()
    {
        // Mark the sorted cache as dirty, needing re-sorting
        isDirty = true;

        // Notify UI components that rankings have changed
        OnRankingsChanged?.Invoke();
    }

    // to register player to a list to show it in the ui
    public void RegisterPlayer(PlayerObject player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            isDirty = true;
            OnRankingsChanged?.Invoke();
        }
    }

    // to de-register player and remove from the players list
    public void UnregisterPlayer(PlayerObject player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
            isDirty = true;
            OnRankingsChanged?.Invoke();
        }
    }

    // Get a sorted list of all players by their progress
    public List<PlayerObject> GetSortedPlayers()
    {
        // Only resort if something has changed
        if (isDirty)
        {
            // First sort by laps completed, then by progress within the current lap
            cachedSortedPlayers = players.OrderByDescending(p => p.lapsCompleted)
                                         .ThenByDescending(p => p.progress)
                                         .ToList();
            isDirty = false;
        }
        return cachedSortedPlayers;
    }

    public int GetPositionOf(PlayerObject target)
    {
        if (players.Count == 0) return 0;

        // Use GetSortedPlayers for consistency
        var sorted = GetSortedPlayers();
        return sorted.IndexOf(target) + 1;
    }

    public float GetProgressOf(PlayerObject target)
    {
        if (target == null) return 0f;
        return target.progress;
    }

    public bool HasPlayerFinishedRace(PlayerObject player)
    {
        return player != null && player.lapsCompleted >= totalLapsForRace;
    }

    public string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000) % 1000);
        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }
}