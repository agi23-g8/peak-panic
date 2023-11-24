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
    private Transform spawnPoint;

    [SerializeField]
    private Button startGameButton;

    // A map from NetworkPlayer to Player 
    private Dictionary<GameObject, GameObject> playerMap = new Dictionary<GameObject, GameObject>();

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
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        // Keep track of the player
        playerMap.Add(networkPlayer, player);

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
    }


    public void StartGame()
    {
        // START GAME when all players have joined

        // TODO: Update the actual player object with the network player
        // Something like:


        // Then, in Player.cs, you can do:
        // void SetNetworkPlayer(NetworkPlayer networkPlayer)
        // {
        //     networkPlayer.accelerometer.OnValueChanged += OnAccelerometerChanged;
        //     networkPlayer.playerName.OnValueChanged += OnPlayerNameChanged;
        // }
    }

    // Update is called once per frame
    void Update()
    {
        if (NetworkManager.Singleton.IsServer)
            Debug.Log("Number of clients connected: " + NetworkManager.Singleton.ConnectedClientsList.Count);
    }
}
