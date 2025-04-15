using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using Fusion;
// To show the different player ranking/postions/stats across the session in sync for all players in the session.
public class PlayerRankingUI : MonoBehaviour
{
    [Header("References")]
    public GameObject playerRankEntryPrefab; // create a prefab and assign to this to make it show on the ranking panel
    public Transform entriesContainer;

    [Header("Design")]
    public Color localPlayerColor = Color.yellow; // to help better see the local player
    public Color otherPlayerColor = Color.white;
    [SerializeField] private bool showDecimalPlaces = true; //to show more precise percentage values

    private List<TextMeshProUGUI> rankEntries = new List<TextMeshProUGUI>();
    private Dictionary<PlayerObject, float> displayProgress = new Dictionary<PlayerObject, float>();
    private float smoothingSpeed = 3f; //speed of percentage value smoothing

    private void Start()
    {
        //subscribes to ranking changes when RaceManager is ready
        StartCoroutine(WaitForRaceManagerAndSubscribe());
    }

    private void Update()
    {
        // smoothly update displayed progress values
        if (RaceManager.ins != null)
        {
            bool needsUpdate = false;
            var players = RaceManager.ins.players;

            foreach (var player in players)
            {
                //initialize if needed
                if (!displayProgress.ContainsKey(player))
                {
                    displayProgress[player] = player.progress;
                }

                //smoothly interpolate to actual progress
                float targetProgress = player.progress;
                float currentDisplayProgress = displayProgress[player];

                if (Mathf.Abs(currentDisplayProgress - targetProgress) > 0.0001f)
                {
                    displayProgress[player] = Mathf.Lerp(currentDisplayProgress, targetProgress,
                                                     Time.deltaTime * smoothingSpeed);
                    needsUpdate = true;
                }
            }

            //clean up old players
            var keysToRemove = displayProgress.Keys.Where(p => !players.Contains(p)).ToList();
            foreach (var key in keysToRemove)
            {
                displayProgress.Remove(key);
            }

            // Update the UI if values changed
            if (needsUpdate)
            {
                UpdateRankingText();
            }
        }
    }

    private System.Collections.IEnumerator WaitForRaceManagerAndSubscribe()
    {
        // we wait until RaceManager is initialized
        while (RaceManager.ins == null)
        {
            yield return null;
        }

        // Subscribe to ranking changes
        RaceManager.ins.OnRankingsChanged += UpdateRankings;

        // initial update
        UpdateRankings();
    }

    private void OnDestroy()
    {
        // unsubscribe when this object is destroyed
        if (RaceManager.ins != null)
        {
            RaceManager.ins.OnRankingsChanged -= UpdateRankings;
        }
    }

    private void UpdateRankings()
    {
        //if RaceManager is not initialized yet, skip this update
        if (RaceManager.ins == null) return;

        //get sorted players from RaceManager - this now uses networked progress values
        var sortedPlayers = RaceManager.ins.GetSortedPlayers();

        //create or update rank entries
        EnsureEnoughRankEntries(sortedPlayers.Count);

        //update entry texts
        UpdateRankingText();
    }

    private void UpdateRankingText()
    {
        if (RaceManager.ins == null) return;

        var sortedPlayers = RaceManager.ins.GetSortedPlayers();

        //update each entry
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            PlayerObject player = sortedPlayers[i];
            TextMeshProUGUI entry = rankEntries[i];

            //get the interpolated display progress for smoother UI
            float displayValue = displayProgress.ContainsKey(player) ?
                displayProgress[player] : player.progress;

            //format percentage based on preference (with or without decimal places)
            string progressFormat = showDecimalPlaces ? "{0:F1}%" : "{0:F0}%";
            string progressText = string.Format($" ({progressFormat})", displayValue * 100);

            //show player ID and progress percentage
            string playerName = player.Object.HasInputAuthority ? "You" : $"Player {player.Object.Id}";

            entry.text = $"{i + 1}. {playerName}{progressText}";

            //highlight local player's entry [colored]
            entry.color = player.Object.HasInputAuthority ? localPlayerColor : otherPlayerColor;
        }

        // Hide unused entries
        for (int i = sortedPlayers.Count; i < rankEntries.Count; i++)
        {
            rankEntries[i].gameObject.SetActive(false);
        }
    }

    private void EnsureEnoughRankEntries(int count)
    {
        //create new entries if needed
        while (rankEntries.Count < count)
        {
            GameObject entryObj = Instantiate(playerRankEntryPrefab, entriesContainer);
            TextMeshProUGUI entryText = entryObj.GetComponent<TextMeshProUGUI>();
            rankEntries.Add(entryText);
        }

        //make sure all required entries are active
        for (int i = 0; i < count; i++)
        {
            rankEntries[i].gameObject.SetActive(true);
        }
    }
}