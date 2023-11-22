using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ServerManager : MonoBehaviour
{

    [SerializeField]
    private GameObject playerPrefab;

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
            Logger.Instance.LogInfo("Server started...");
        else
            Logger.Instance.LogInfo("Unable to start server...");


        // Handle client connection
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    void OnClientConnected(ulong clientID)
    {
        // find the NetworkPlayer object
        GameObject networkPlayer = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.gameObject;
        // Instantiate the Player object
        GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        // Keep track of the player
        playerMap.Add(networkPlayer, player);
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
        // foreach (GameObject player in playerMap.Values)
        //      player.GetComponent<Player>().SetNetworkPlayer(networkPlayer.GetComponent<NetworkPlayer>());
    }

    // Update is called once per frame
    void Update()
    {

    }
}
