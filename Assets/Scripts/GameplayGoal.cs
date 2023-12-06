using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameplayGoal : Singleton<GameplayGoal>
{
    // stack with players that has reached the goal
    private readonly Stack<GameObject> finished = new Stack<GameObject>();

    [SerializeField]
    private GameObject finishedScreen;

    [SerializeField]
    private GameObject positionsContainer;

    [SerializeField]
    private GameObject UIPlayerPositionPrefab;

    [SerializeField]
    private Button goAgainButton;

    private void Start()
    {
        ResetGoal();

        goAgainButton.onClick.AddListener(() =>
        {
            ServerManager.Instance.EndGame();
        });
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // make sure player has not already reached the goal
            if (finished.Contains(other.gameObject))
            {
                return;
            }

            finishedScreen.SetActive(true);

            Debug.Log("Player has reached the goal!");

            // add player to stack
            finished.Push(other.gameObject);

            string name = ServerManager.Instance.GetPlayerDisplayName(other.gameObject);
            AddPlayerPositionUI(finished.Count, name);

            // disable player movement
            ServerManager.Instance.SetPlayerSkiControllerActive(other.gameObject, false);

            bool hasAllFinished = GetActivePlayers();

            if (hasAllFinished)
            {
                goAgainButton.gameObject.SetActive(true);
            }
        }
    }

    private bool GetActivePlayers()
    {
        int activePlayers = ServerManager.Instance.GetActivePlayers();
        if (finished.Count == activePlayers)
        {
            Debug.Log("All players have reached the goal!");
            return true;
        }
        return false;
    }

    private void AddPlayerPositionUI(int position, string name)
    {
        // instantiate UI element under the container
        GameObject uiElement = Instantiate(UIPlayerPositionPrefab, positionsContainer.transform);
        UIPlayerPositionElement ui = uiElement.GetComponent<UIPlayerPositionElement>();

        // TODO: get actual player name
        ui.SetPlayerName(name);
        ui.SetPlayerPosition(position);
    }

    public void ResetGoal()
    {
        // reset stack
        finished.Clear();

        // reset UI
        foreach (Transform child in positionsContainer.transform)
        {
            Destroy(child.gameObject);
        }

        goAgainButton.gameObject.SetActive(false);
        finishedScreen.SetActive(false);
    }
}
