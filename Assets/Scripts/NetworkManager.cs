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

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
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

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
       
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
       
    }

}
