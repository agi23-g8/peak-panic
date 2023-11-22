using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UIScriptManager : MonoBehaviour
{
    public GameObject[] uiScreenElements;

    public TMP_InputField nameInputField;
    public TMP_InputField gameCodeInputField;
    public TMP_Text joinButtonText;

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
        string name = nameInputField.text;
        string code = gameCodeInputField.text;

        if (name == "")
        {
            messagePopup.Show("Invalid name", messagePopupDuration);
            Debug.Log("Name cannot be empty!");
            return;
        }

        if (code == "")
        {
            messagePopup.Show("Enter a game code", messagePopupDuration);
            Debug.Log("Game code cannot be empty!");
            return;
        }

        Debug.Log($"{name} is attempting to connect to game with code {code}...");

        joinButtonText.text = "Connecting...";

        Connect(name, code);
    }

    private async void Connect(string name, string code)
    {

        if (RelayManager.Instance.IsRelayEnabled && !string.IsNullOrEmpty(code))
        {
            await RelayManager.Instance.JoinRelay(code);
        }

        bool status = NetworkManager.Singleton.StartClient();

        if (status)
        {
            Debug.Log("Connected to server!");
            Next();
        }
        else
        {
            messagePopup.Show("Failed to connect to server", messagePopupDuration);

            Debug.Log("Failed to connect");
            joinButtonText.text = "Join";
        }
    }
}
