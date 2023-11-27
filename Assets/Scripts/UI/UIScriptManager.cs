using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UIScriptManager : Singleton<UIScriptManager>
{
    [Header("UI Screens")]
    public GameObject[] uiScreenElements;

    [Header("UI Elements")]
    public TMP_InputField nameInputField;
    public TMP_InputField gameCodeInputField;
    public TMP_Text joinButtonText;
    public TMP_Text playmodeInfoText;
    public TMP_Text playmodeName;

    public GameObject messagePopupPrefab;
    public float messagePopupDuration = 2.5f;

    private UIMessagePopup messagePopup;

    private void Start()
    {
        foreach (GameObject uiElement in uiScreenElements)
        {
            uiElement.SetActive(false);
        }

        uiScreenElements[0].SetActive(true);

        messagePopup = messagePopupPrefab.GetComponent<UIMessagePopup>();
        messagePopupPrefab.SetActive(false);
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

        // make sure name is alphanumeric
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9]+$"))
        {
            messagePopup.Show("Invalid name, letters and numbers only", messagePopupDuration);
            Debug.Log("Name must be alphanumeric!");
            return;
        }

        Debug.Log($"{name} is attempting to connect to game with code {code}...");

        joinButtonText.text = "Connecting...";

        Connect(code);

        playmodeName.text = name;
        // StartCoroutine(SetNetworkPlayerName(name));
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
        }
        else
        {
            messagePopup.Show("Failed to connect to server", messagePopupDuration);

            Debug.Log("Failed to connect");
            joinButtonText.text = "Join";
        }
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
