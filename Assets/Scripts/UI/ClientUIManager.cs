using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ClientUIManager : Singleton<ClientUIManager>
{
    [Header("UI Screens")]
    public GameObject[] uiScreenElements;

    [Header("UI Elements")]
    public TMP_InputField nameInputField;
    public TMP_InputField gameCodeInputField;
    public TMP_Text joinButtonText;
    public TMP_Text playmodeInfoText;
    public TMP_Text playmodeName;
    public Image backgroundColor;
    public Button disconnectButton;

    [Header("UI Popups")]

    public GameObject messagePopupPrefab;
    public GameObject actionConfirmPopupPrefab;

    public float messagePopupDuration = 2.5f;
    public float actionConfirmPopupDuration = 4.0f;

    private UIMessagePopup messagePopup;
    private UIMessagePopup actionConfirmPopup;

    private void Start()
    {
        foreach (GameObject uiElement in uiScreenElements)
        {
            uiElement.SetActive(false);
        }

        uiScreenElements[0].SetActive(true);

        messagePopup = messagePopupPrefab.GetComponent<UIMessagePopup>();
        messagePopupPrefab.SetActive(false);

        actionConfirmPopup = actionConfirmPopupPrefab.GetComponent<UIMessagePopup>();
        actionConfirmPopupPrefab.SetActive(false);
    }

    /// <summary>
    /// Go to the next UI screen in the UI Screen Elements list. 
    /// If last, it will loop back to first.
    /// </summary>
    public void Next()
    {
        for (int i = 0; i < uiScreenElements.Length; i++)
        {
            if (uiScreenElements[i].activeSelf)
            {
                uiScreenElements[i].SetActive(false);
                uiScreenElements[(i + 1) % uiScreenElements.Length].SetActive(true);
                return;
            }
        }
    }

    /// <summary>
    /// Called when user presses "Join" button.
    /// </summary>
    public void JoinGame()
    {
        Logger.Instance.LogInfo("Joining game...");

        string name = nameInputField.text;
        string code = gameCodeInputField.text;

        if (name == "")
        {
            messagePopup.Show("Enter a name", messagePopupDuration);
            Debug.Log("Name cannot be empty!");
            return;
        }

        if (code == "")
        {
            messagePopup.Show("Enter a game code", messagePopupDuration);
            Debug.Log("Game code cannot be empty!");
            return;
        }

        if (name.Length > 12)
        {
            messagePopup.Show("Name must be 12 characters or less", messagePopupDuration);
            Debug.Log("Name must be 12 characters or less!");
            return;
        }

        Debug.Log($"{name} is attempting to connect to game with code {code}...");

        joinButtonText.text = "Connecting...";

        Connect(code);

        playmodeName.text = name;
        // StartCoroutine(SetNetworkPlayerName(name));
    }

    /// <summary>
    /// Called when user presses the arrow button in the top left on the Play Mode screen.
    /// </summary>
    public void DisconnectButton()
    {
        actionConfirmPopup.Show("Do you want to leave the game?", actionConfirmPopupDuration);
    }

    /// <summary>
    /// Called from pressing Yes on the Disconnect confirmation popup.
    /// </summary>
    public void DisconnectConfirm()
    {
        // user wants to disconnect
        actionConfirmPopup.HideEarly();

        // disconnect from server
        // this will trigger OnNetworkDespawn on the NetworkPlayer
        // which in turn will make the UI go back to the first screen
        NetworkManager.Singleton.Shutdown();
    }

    /// <summary>
    /// Called from pressing No on the Disconnect confirmation popup.
    /// </summary>
    public void DisconnectCancel()
    {
        actionConfirmPopup.HideEarly();
    }

    private async void Connect(string code)
    {
        if (RelayManager.Instance.IsRelayEnabled && !string.IsNullOrEmpty(code))
        {
            await RelayManager.Instance.JoinRelay(code);
        }

        bool status = NetworkManager.Singleton.StartClient();

        if (status)
        {
            Debug.Log("Connected to server!");
            Logger.Instance.LogInfo("Connected to server!");
            Next();
            return;
        }

        // if no return, we failed.
        messagePopup.Show("Failed to connect to server", messagePopupDuration);
        Debug.Log("Failed to connect");
        joinButtonText.text = "Join";
    }

    /// <summary>
    /// Triggered from NetworkPlayer when it is despawned. Lost connection to server
    /// </summary>
    public void OnNetworkDespawn()
    {
        Next();
        joinButtonText.text = "Join";
        messagePopup.Show("Lost connection to server", messagePopupDuration + 2f);
    }

    // IEnumerator SetNetworkPlayerName(string name)
    // {
    //     // when the network player prefab is spawned, set the name

    //     // search for a game object with the NetworkPlayer component
    //     GameObject networkPlayer = null;
    //     while (networkPlayer == null)
    //     {
    //         networkPlayer = GameObject.FindObjectOfType<NetworkPlayer>()?.gameObject;
    //         yield return new WaitForSeconds(0.5f);
    //     }

    //     // set the name
    //     networkPlayer.GetComponent<NetworkPlayer>().SetPlayerName(name);


    // }


}
