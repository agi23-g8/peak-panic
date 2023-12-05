using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class ServerManager : Singleton<ServerManager>
{
    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private GameObject menuScreen;

    [SerializeField]
    private GameObject joinCode;

    [SerializeField]
    private Button startGameButton;

    [SerializeField]
    private UICountdown countdown;

    [SerializeField]
    private int countdownTime = 5;

    // A map from NetworkPlayer to Player 
    private Dictionary<GameObject, GameObject> playerMap = new Dictionary<GameObject, GameObject>();

    // A map from player ID to Player
    private Dictionary<ulong, GameObject> playerIdMap = new Dictionary<ulong, GameObject>();

    // List of Players
    public List<GameObject> players = new List<GameObject>();

    public bool gameStarted = false;

    private async void Start()
    {
        countdown.ResetCountdown();

        // START SERVER
        startGameButton?.onClick.AddListener(() =>
        {
            StartGame();
        });

        RelayHostData hostData;
        if (RelayManager.Instance.IsRelayEnabled)
        {
            hostData = await RelayManager.Instance.SetupRelay();
        }
        else
        {
            throw new Exception("Relay could not be enabled!");
        }

        if (NetworkManager.Singleton.StartServer())
            Debug.Log("Server started successfully!");
        else
            Debug.Log("Server failed to start!");

        joinCode.GetComponentInChildren<TMP_Text>().text = hostData.JoinCode;

        // Handle client connection and disconnection
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        else
        {
            Debug.Log("Something went wrong! This is not a server!");
        }

        StartCoroutine(SetPlayerNames());
    }

    private void Update()
    {
        if (gameStarted)
        {
            CullPlayers();
        }
    }

    private void OnClientConnected(ulong clientID)
    {
        Debug.Log("Client connected: " + clientID);
        Debug.Log("Number of clients connected: " + NetworkManager.Singleton.ConnectedClientsList.Count);

        // find the NetworkPlayer object
        GameObject networkPlayer = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.gameObject;
        // Instantiate the Player object


        Transform spawnPoint = SpawnPointManager.Instance.GetSpawnPoint();
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        // Keep track of the player
        playerMap.Add(player, networkPlayer);
        players.Add(player);
        playerIdMap.Add(clientID, player);

        PhysicsSkierController skierController = player.GetComponent<PhysicsSkierController>();
        skierController.SetNetworkPlayer(networkPlayer.GetComponent<NetworkPlayer>());

    }

    private void OnClientDisconnected(ulong clientID)
    {
        Debug.Log("Client disconnected: " + clientID);

        GameObject temp = playerIdMap[clientID];

        // removes the player object from scene
        Destroy(playerIdMap[clientID]);

        // update the map
        playerIdMap.Remove(clientID);

        // update the list
        players.Remove(temp);
    }


    private void StartGame()
    {
        // Hide menu UI
        startGameButton.gameObject.SetActive(false);
        menuScreen.SetActive(false);

        // Start countdown
        countdown.NewCountDown(countdownTime, () => {
            Debug.Log("Game started!");

            foreach (GameObject player in players)
            {
                PhysicsSkierController skierController = player.GetComponent<PhysicsSkierController>();
                skierController.Unfreeze();
            }
            gameStarted = true;
        });
    }

    // Every second, set the player names
    private IEnumerator SetPlayerNames()
    {
        while (true)
        {
            foreach (GameObject player in players)
            {
                GameObject networkPlayer = playerMap[player];
                TMP_Text text = player.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = networkPlayer.GetComponent<NetworkPlayer>().GetPlayerName();
            }
            yield return new WaitForSeconds(1);
        }
    }

    private void CullPlayers()
    {
        foreach (GameObject player in players)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(player.transform.position);
            if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
            {
                // player is off screen
                players.Remove(player);
                Destroy(player);
            }
        }
    }

    /// <summary>
    /// Ends the gameplay session, and returns to the "main menu".
    /// </summary>
    public void EndGame()
    {
        Debug.Log("TODO: reset the game and players");
    }

    /// <summary>
    /// Returns the current number of players which is playing (not eliminated yet).
    /// </summary>
    /// <returns></returns>
    public int GetActivePlayers()
    {
        return players.Count;
    }
}
