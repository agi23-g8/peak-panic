using TMPro;
using UnityEngine;

public class UIScriptManager : MonoBehaviour
{
    [Header("UI Screens")]
    public GameObject[] uiScreenElements;

    [Header("UI Elements")]
    public TMP_InputField nameInputField;
    public TMP_InputField gameCodeInputField;
    public TMP_Text joinButtonText;
    public TMP_Text playmodeInfoText;

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

        Connect(name, code);
    }

    private void Connect(string name, string code)
    {
        // TODO: replace with actual connection status
        bool status = false;
        
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

    public void SetBGColor()
    {
        // TODO: get color from server
    }

    public void PlayerEliminated()
    {
        // TODO: get game over message from server

        playmodeInfoText.text = "Game Over!";
    }
}
