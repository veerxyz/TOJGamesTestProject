using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
// this one acts as a central network manager from creating rooms/sessions to holding them up in session.
// Implemented and derived methods by and from INetworkRunnerCallbacks, are self explanatory, if you need me to explain you something in detail feel free to ask.
public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkRunner runnerInstance; // to store our runner for that instance, the names are going to be pretty self explanatory to be honest.
    public string lobbyName = "default";
    public TMPro.TMP_InputField roomNumberInputField;
    public string playSceneName;
    public string lobbySceneName;
    public GameObject playerPrefab;

    //public Transform sessionListContentParent;
    //public GameObject sessionListEntryPrefab;
    //public Dictionary<string, GameObject> sessionListUiDictionary = new Dictionary<string, GameObject>();

    private void Awake()
    {

        runnerInstance = gameObject.GetComponent<NetworkRunner>();
        if (runnerInstance == null) // a fail safe incase i forget to add NetworkRunner
        {
            runnerInstance = gameObject.AddComponent<NetworkRunner>();
        }
    }
    public void CreateRandomSession()
    {
        // int randomInt = UnityEngine.Random.Range(1000, 9999);
        int roomNumber = 0; // default fallback value

        // We Check if the input field is empty. If not, try parsing its content.
        if (!string.IsNullOrEmpty(roomNumberInputField.text))
        {
            if (!int.TryParse(roomNumberInputField.text, out roomNumber))
            {
                Debug.LogError("Invalid room number input!");
                return;
            }

        }
        else //field was empty/null
        {
            Debug.Log("Field was empty, defaulted to Room Number = 0");
        }

        int randomInt = roomNumber; // now we input it from UI itself, so use same room number to join our/your friends.

        string randomSessionName = "Room : " + randomInt.ToString();
        NetworkManager.runnerInstance.StartGame(new StartGameArgs()
        {
            Scene = SceneRef.FromIndex(GetSceneIndex(playSceneName)),
            SessionName = randomSessionName,
            GameMode = GameMode.Shared,
        }); ;
    }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // we need only for local player so we do this
        if (player == runner.LocalPlayer)
        {

            //NetworkObject playerNetworkObject = runner.Spawn(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity, player);
            //runner.SetPlayerObject(player, playerNetworkObject);

            // to spawn in a grid fashion, like a starting race.
            // Using the player’s ordering (for example, the Raw value) to compute the spawn grid.
            int index = player.PlayerId; //  player.PlayerId.
            int columnIndex = index % 2;  // Two columns
            int rowIndex = index / 2;     // Calculate row number

            // Define grid parameters:
            Vector3 spawnOrigin = new Vector3(5.0f, 0, 0); // Start position and from here on will be calculated for the rest
            float columnSpacing = 1.5f; // Spacing as needed (for left/right positioning)
            float rowSpacing = 3f;    // Spacing as needed (for front/back positioning)

            // Determine offsets:
            float xOffset = (columnIndex == 0) ? -columnSpacing : columnSpacing;
            float zOffset = rowIndex * rowSpacing;

            // Calculate final spawn position for this player
            Vector3 spawnPosition = spawnOrigin + new Vector3(xOffset, 0, zOffset);

            // Spawn the player prefab at the computed position with no rotation
            NetworkObject playerNetworkObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

            // Set the player object reference for this player
            runner.SetPlayerObject(player, playerNetworkObject);
        }

    }

    public int GetSceneIndex(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            // We get the scene path
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            // Extracting the name of the scene from the path
            string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            // Checking if the names match
            if (name == sceneName)
            {
                // If matched, return
                return i;
            }
        }
        // If the scene name is not found, return -1 (error code)
        return -1;
    }
    private void Start()
    {
        runnerInstance.JoinSessionLobby(SessionLobby.Shared, lobbyName);

    }
    public static void ReturnToLobby() // we call from ReturnToLobby.cs thats triggerd by the ReturnToLobby button.
    {
        NetworkManager.runnerInstance.Despawn(runnerInstance.GetPlayerObject(runnerInstance.LocalPlayer)); // Usually it should despawn when we shutdown but just doing it for our local player still.
        NetworkManager.runnerInstance.Shutdown(true, ShutdownReason.Ok);

    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        SceneManager.LoadScene(lobbySceneName);
    }
    //public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    //{
    //    //Debug.Log("Session List Updated");

    //    // Iterate through the Session List and check to see if there are sessions that are no longert in use and delete/remove them.
    //    DeleteOldSessionsFromUI(sessionList);


    //    /*Check out SessionListEntry list to see if we already have an entry object for THAT session.
    //    If we do, then update the values for it, if we dont, then we create one.*/
    //    CompareLists(sessionList);

    //}
    //private void CompareLists(List<SessionInfo> sessionList)
    //{
    //    foreach (SessionInfo session in sessionList)
    //    {
    //        if (sessionListUiDictionary.ContainsKey(session.Name))
    //        {
    //            UpdateEntryUI(session);
    //        }
    //        else
    //        {
    //            CreateEntryUI(session);
    //        }
    //    }
    //}
    //private void CreateEntryUI(SessionInfo session)
    //{
    //    GameObject newEntry = GameObject.Instantiate(sessionListEntryPrefab);
    //    newEntry.transform.parent = sessionListContentParent;
    //    SessionListEntry entryScript = newEntry.GetComponent<SessionListEntry>();
    //    sessionListUiDictionary.Add(session.Name, newEntry);

    //    entryScript.roomName.text = session.Name;
    //    entryScript.playerCount.text = session.PlayerCount.ToString() + "/" + session.MaxPlayers.ToString();
    //    entryScript.joinButton.interactable = session.IsOpen;

    //    newEntry.SetActive(session.IsVisible);
    //}

    //private void UpdateEntryUI(SessionInfo session)
    //{

    //    sessionListUiDictionary.TryGetValue(session.Name, out GameObject newEntry);

    //    SessionListEntry entryScript = newEntry.GetComponent<SessionListEntry>();

    //    entryScript.roomName.text = session.Name;
    //    entryScript.playerCount.text = session.PlayerCount.ToString() + "/" + session.MaxPlayers.ToString();
    //    entryScript.joinButton.interactable = session.IsOpen;

    //    newEntry.SetActive(session.IsVisible);
    //}

    //private void DeleteOldSessionsFromUI(List<SessionInfo> sessionList)
    //{
    //    bool isContained = false;
    //    GameObject uiToDelete = null;

    //    foreach (KeyValuePair<string, GameObject> kvp in sessionListUiDictionary)
    //    {
    //        string sessionKey = kvp.Key;

    //        foreach (SessionInfo sessionInfo in sessionList)
    //        {
    //            if (sessionInfo.Name == sessionKey)
    //            {
    //                isContained = true;
    //                break;

    //            }
    //        }

    //        if (!isContained)
    //        {
    //            uiToDelete = kvp.Value;
    //            sessionListUiDictionary.Remove(sessionKey);
    //            Destroy(uiToDelete);

    //        }
    //    }
    //}
    public void OnConnectedToServer(NetworkRunner runner)
    {
       
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
       
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
       
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
       
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
       
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
       
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
       
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
       
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
       
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
       
    }


    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
       
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
       
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
       
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
       
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
       
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
       
    }

   

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
       
    }

}
