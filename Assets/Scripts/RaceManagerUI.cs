using TMPro;
using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Collections;
// For UI to show the local player, and his/her stats
public class RaceManagerUI : MonoBehaviour
{
    public TextMeshProUGUI positionText;
    public Slider progressBar;
    private PlayerObject localPlayer;

    [SerializeField] private float sliderSmoothingSpeed = 5f;
    private float targetProgressValue = 0f;
    private float previousProgressValue = 0f;

    [Header("Lap Timing")]
    [SerializeField] private TextMeshProUGUI currentLapText;
    [SerializeField] private TextMeshProUGUI lastLapTimeText;
    [SerializeField] private TextMeshProUGUI bestLapTimeText;
    [SerializeField] private TextMeshProUGUI currentLapTimeText;

    [Header("Race Completion")]
    [SerializeField] private GameObject raceCompletionPanel;
    [SerializeField] private TextMeshProUGUI completionPositionText;
    [SerializeField] private TextMeshProUGUI completionTotalTimeText;
    [SerializeField] private TextMeshProUGUI completionBestLapText;
    [SerializeField] private Button returnToLobbyButton;

    private bool raceCompleted = false;

    void Start()
    {
        // hide the race completion panel at start
        if (raceCompletionPanel != null)
            raceCompletionPanel.SetActive(false);

        //setup return to lobby button if it exists
        if (returnToLobbyButton != null)
            returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);

        //wait for local player to spawn
        InvokeRepeating(nameof(TryFindLocalPlayer), 0.5f, 0.5f);

        //start subscribing to RaceManager events
        StartCoroutine(WaitForRaceManagerAndSubscribe());
    }

    // to find our local player in the scene, could also use Tag one with Player to increase efficiency.
    void TryFindLocalPlayer()
    {
        var players = FindObjectsOfType<PlayerObject>();
        foreach (var player in players)
        {
            if (player.Object.HasInputAuthority)
            {
                localPlayer = player;
                CancelInvoke(nameof(TryFindLocalPlayer));

                // Once found, update UI immediately
                UpdateUI();
                break;
            }
        }
    }

    void Update()
    {
        // update lap timers
        UpdateLapTimers();

        //check for race completion based on laps
        if (!raceCompleted && localPlayer != null && RaceManager.ins != null)
        {
            if (RaceManager.ins.HasPlayerFinishedRace(localPlayer))
            {
                OnRaceCompleted();
            }
        }

        //smoothly update the progress bar value
        if (progressBar != null && progressBar.value != targetProgressValue)
        {
            //smooth interpolation towards the target value
            progressBar.value = Mathf.Lerp(progressBar.value, targetProgressValue, Time.deltaTime * sliderSmoothingSpeed);

            //if we're very close to the target, snap to it
            if (Mathf.Abs(progressBar.value - targetProgressValue) < 0.001f)
            {
                progressBar.value = targetProgressValue;
            }
        }
    }

    void UpdateLapTimers()
    {
        if (localPlayer == null || RaceManager.ins == null) return;

        //update current lap counter
        if (currentLapText != null)
        {
            int totalLaps = RaceManager.ins.TotalLapsForRace;
            int currentLap = Mathf.Min(localPlayer.lapsCompleted + 1, totalLaps);
            currentLapText.text = $"Lap: {currentLap}/{totalLaps}";
        }

        //update last lap time
        if (lastLapTimeText != null)
        {
            if (localPlayer.lastLapTime > 0)
            {
                lastLapTimeText.text = $"Last Lap: {FormatTime(localPlayer.lastLapTime)}";
            }
            else
            {
                lastLapTimeText.text = "Last Lap: --:--:--";
            }
        }

        //update best lap time
        if (bestLapTimeText != null)
        {
            if (localPlayer.bestLapTime < float.MaxValue)
            {
                bestLapTimeText.text = $"Best Lap: {FormatTime(localPlayer.bestLapTime)}";
            }
            else
            {
                bestLapTimeText.text = "Best Lap: --:--:--";
            }
        }

        //update current lap time (still running)
        if (currentLapTimeText != null && !raceCompleted)
        {
            float currentTime = (float)localPlayer.Runner.SimulationTime;
            float lapTimeElapsed = currentTime - localPlayer.currentLapStartTime;
            currentLapTimeText.text = $"Current: {FormatTime(lapTimeElapsed)}";
        }
    }

    System.Collections.IEnumerator WaitForRaceManagerAndSubscribe()
    {
        //wait until RaceManager is initialized
        while (RaceManager.ins == null)
        {
            yield return null;
        }

        //subscribe to ranking changes
        RaceManager.ins.OnRankingsChanged += UpdateUI;
    }

    private void OnDestroy()
    {
        //unsubscribe when this object is destroyed
        if (RaceManager.ins != null)
        {
            RaceManager.ins.OnRankingsChanged -= UpdateUI;
        }
    }

    void UpdateUI()
    {
        if (localPlayer == null) return;
        if (RaceManager.ins == null) return;

        int position = RaceManager.ins.GetPositionOf(localPlayer);
        int total = RaceManager.ins.players.Count;

        positionText.text = $"You are in: {Ordinal(position)} place / {total}";

        //to set the target progress value, actual slider will smoothly interpolate in Update()
        if (progressBar != null)
        {
            float progress = RaceManager.ins.GetProgressOf(localPlayer);
            targetProgressValue = progress;
        }

        // Update lap timing displays
        UpdateLapTimers();
    }

    void OnRaceCompleted()
    {
        if (raceCompleted) return; // Only trigger once

        raceCompleted = true;

        // Get final position
        int position = RaceManager.ins.GetPositionOf(localPlayer);
        int total = RaceManager.ins.players.Count;

        // Calculate total race time
        // float totalRaceTime = 0;

        // // To calculate total time including first lap, even if its slightly incomplete, but to be fair, you can remove the first one too.. but this is a simple test so wth, i've kept it.
        // if (localPlayer.lapsCompleted > 0)
        // {
        //     float currentTime = (float)localPlayer.Runner.SimulationTime;
        //     totalRaceTime = currentTime - localPlayer.currentLapStartTime + (localPlayer.lapsCompleted * localPlayer.lastLapTime);
        // }
        //we do most of the calculation in PlayerObject and then pass it in below line.
        float totalRaceTime = localPlayer.totalRaceTime;
        // Update completion UI text
        if (completionPositionText != null)
        {
            completionPositionText.text = $"You finished in {Ordinal(position)} place out of {total}!";
        }

        if (completionTotalTimeText != null)
        {
            completionTotalTimeText.text = $"Total Time: {FormatTime(totalRaceTime)}";
        }

        if (completionBestLapText != null && localPlayer.bestLapTime < float.MaxValue)
        {
            completionBestLapText.text = $"Best Lap: {FormatTime(localPlayer.bestLapTime)}";
        }

        // Show the completion panel
        if (raceCompletionPanel != null)
        {
            raceCompletionPanel.SetActive(true);
        }

        // Disable the player's movement component so that no further input is processed.
        PlayerMovement playerMovement = localPlayer.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        Debug.Log($"Race completed! Position: {position}/{total}, Time: {FormatTime(totalRaceTime)}, Best Lap: {FormatTime(localPlayer.bestLapTime)}");
    }

    // to make time look good and in time format, and not just a big number in seconds
    string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000) % 1000);
        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }

    void OnReturnToLobbyClicked()
    {
        NetworkManager.ReturnToLobby();
    }

    // To show the position and the in n'th or n'st or n'nd or n'rd style
    string Ordinal(int number)
    {
        if (number <= 0) return number.ToString();
        if (number % 100 >= 11 && number % 100 <= 13) return number + "th";

        switch (number % 10)
        {
            case 1: return number + "st";
            case 2: return number + "nd";
            case 3: return number + "rd";
            default: return number + "th";
        }
    }
}