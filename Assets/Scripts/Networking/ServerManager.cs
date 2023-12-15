using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.SceneManagement;

public class ServerManager : Singleton<ServerManager>
{
    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private ColorPool skinPresets;

    [SerializeField]
    private GameObject menuScreen;

    [SerializeField]
    private GameObject joinCode;

    [SerializeField]
    private Button startGameButton;

    [SerializeField]
    private Button resetGameButton;

    [SerializeField]
    private UICountdown countdown;

    [SerializeField]
    private int countdownTime = 5;

    // A map from Player to NetworkPlayer 
    private Dictionary<GameObject, GameObject> playerMap = new Dictionary<GameObject, GameObject>();

    // A map from player ID to Player
    private Dictionary<ulong, GameObject> playerIdMap = new Dictionary<ulong, GameObject>();

    // List of Players
    public List<GameObject> players = new List<GameObject>();

    // Map with active players, this list is cleared when the game ends and populated when the game starts
    private Dictionary<GameObject, bool> activePlayers = new Dictionary<GameObject, bool>();

    public bool gameStarted = false;

    private async void Start()
    {
        countdown.ResetCountdown();

        // START SERVER
        startGameButton?.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count == 0)
            {
                return;
            }

            StartGame();
        });

        resetGameButton?.onClick.AddListener(() =>
        {
            EndGame();
        });
        resetGameButton.gameObject.SetActive(false);

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
            NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        }
        else
        {
            Debug.Log("Something went wrong! This is not a server!");
        }

        if (skinPresets != null)
        {
            // Reset the skin color preset pool
            skinPresets.ShuffleColors();
            skinPresets.ResetPool();
        }

        StartCoroutine(SetPlayerNames());
    }

    private void OnServerStopped(bool obj)
    {
        Debug.LogWarning("Server stopped! Going back to title screen");

        // make sure to destroy the network manager before reload scene
        Destroy(gameObject);

        SceneManager.LoadScene("title-screen");
    }

    private void Update()
    {
        if (gameStarted)
        {
            CullPlayers();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (gameStarted)
            {
                EndGame();
            }
            else
            {
                RestartServer();
            }
        }
    }

    private void RestartServer()
    {
        NetworkManager.Singleton.Shutdown();
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

        // Link the accelerometer to the player controller
        PhysicsSkierController skierController = player.GetComponent<PhysicsSkierController>();
        skierController.SetNetworkPlayer(networkPlayer.GetComponent<NetworkPlayer>());

        if (skinPresets != null)
        {
            // Update the player skin color
            Color skinColor = skinPresets.PullColor();
            Renderer playerRenderer = player.GetComponent<Renderer>();
            playerRenderer.material.SetColor("_SkinColor", skinColor);
            networkPlayer.GetComponent<NetworkPlayer>().skinColor.Value = skinColor;
        }
    }


    private void OnClientDisconnected(ulong clientID)
    {
        Debug.Log("Client disconnected: " + clientID);

        GameObject temp = playerIdMap[clientID];

        // removes the player object from scene
        Destroy(playerIdMap[clientID]);
        playerIdMap.Remove(clientID);

        // remove the networked player
        Destroy(playerMap[temp]);
        playerMap.Remove(temp);

        // remove from player list
        players.Remove(temp);

        // if all players have disconnected and game is going, end the game
        if (players.Count == 0 && gameStarted)
        {
            resetGameButton.gameObject.SetActive(true);
        }
    }

    private void StartGame()
    {
        // Hide menu UI
        startGameButton.gameObject.SetActive(false);
        menuScreen.SetActive(false);

        // Start countdown
        countdown.NewCountDown(countdownTime, () =>
        {
            Debug.Log("Game started!");

            foreach (GameObject player in players)
            {
                PhysicsSkierController skierController = player.GetComponent<PhysicsSkierController>();
                skierController.Unfreeze();
            }

            // set all players to active
            foreach (GameObject player in players)
            {
                activePlayers.Add(player, true);
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
                activePlayers[player] = false;
                player.SetActive(false);

                if (GetActivePlayers() == 0)
                {
                    resetGameButton.gameObject.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// Ends the gameplay session, and returns to the "main menu".
    /// </summary>
    public void EndGame()
    {
        GameplayGoal.Instance.ResetGoal();

        resetGameButton.gameObject.SetActive(false);

        activePlayers.Clear();

        gameStarted = false;

        SpawnPointManager.Instance.ResetSpawnPoints();

        // Show menu UI
        startGameButton.gameObject.SetActive(true);
        menuScreen.SetActive(true);

        // Reset players
        foreach (GameObject player in players)
        {
            player.SetActive(true);
            Transform spawnPoint = SpawnPointManager.Instance.GetSpawnPoint();
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;

            SetPlayerSkiControllerActive(player, true);

            PhysicsSkierController skierController = player.GetComponent<PhysicsSkierController>();
            skierController.Freeze();

            Debug.Log("Resetting player: " + GetPlayerDisplayName(player));
        }
    }

    /// <summary>
    /// Returns the current number of players which is playing (not eliminated yet).
    /// </summary>
    /// <returns></returns>
    public int GetActivePlayers()
    {
        // go through active players and count them
        int count = 0;
        foreach (KeyValuePair<GameObject, bool> player in activePlayers)
        {
            if (player.Value)
            {
                count++;
            }
        }
        return count;
    }

    public string GetPlayerDisplayName(GameObject player)
    {
        return playerMap[player].GetComponent<NetworkPlayer>().GetPlayerName();
    }

    public void SetPlayerSkiControllerActive(GameObject gameObject, bool active)
    {
        gameObject.GetComponent<PhysicsSkierController>().enabled = active;
    }
}
