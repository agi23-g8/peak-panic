using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class ServerManager : MonoBehaviour
{

    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private Button startGameButton;

    // A map from NetworkPlayer to Player 
    private Dictionary<GameObject, GameObject> playerMap = new Dictionary<GameObject, GameObject>();

    private List<GameObject> players = new List<GameObject>();

    // Start is called before the first frame update
    async void Start()
    {
        // START SERVER
        startGameButton?.onClick.AddListener(() =>
        {
            StartGame();
        });

        if (RelayManager.Instance.IsRelayEnabled)
            await RelayManager.Instance.SetupRelay();

        if (NetworkManager.Singleton.StartServer())
            Debug.Log("Server started successfully!");
        else
            Debug.Log("Server failed to start!");


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
    }

    // private void OnClientConnected(ulong clientId) => Debug.Log($"=> OnClientConnected({clientId})");


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
        playerMap.Add(networkPlayer, player);
        players.Add(player);

        PhysicsSkierController skierController = player.GetComponent<PhysicsSkierController>();
        skierController.SetNetworkPlayer(networkPlayer.GetComponent<NetworkPlayer>());

    }

    void OnClientDisconnected(ulong clientID)
    {
        // find the NetworkPlayer object
        GameObject networkPlayer = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.gameObject;
        // Destroy the Player object
        Destroy(playerMap[networkPlayer]);
        // Remove the player from the map
        playerMap.Remove(networkPlayer);
        players.Remove(networkPlayer);
    }


    public void StartGame()
    {
        // START GAME when all players have joined
        foreach (GameObject player in players)
        {
            PhysicsSkierController skierController = player.GetComponent<PhysicsSkierController>();
            skierController.Unfreeze();
        }
        GameCameraController.Instance.UpdatePlayerList();
    }

    // Update is called once per frame
    void Update()
    {
        if (NetworkManager.Singleton.IsServer)
            Debug.Log("Number of clients connected: " + NetworkManager.Singleton.ConnectedClientsList.Count);
    }
}
