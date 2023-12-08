using UnityEngine;
using Cinemachine;
using System.Collections.Generic;

public class CameraManager : Singleton<CameraManager>
{
    // _____________________________________________________________________
    // Internal members
    private CinemachineVirtualCamera m_virtualCamera;
    private List<GameObject> m_players => ServerManager.Instance.players;
    private Vector3 m_averagePlayerPos;
    private Transform m_leaderPlayer;


    // _____________________________________________________________________
    // Exposed read-only members
    public Vector3 AveragePlayerPosition
    {
        get { return m_averagePlayerPos; }
    }

    public Vector3 LeaderPlayerPosition
    {
        get { return m_leaderPlayer.position; }
    }


    // _____________________________________________________________________
    // Lifecycle
    private void Start()
    {
        // Gets CinemachineVirtualCamera component from the GameObject
        m_virtualCamera = GetComponent<CinemachineVirtualCamera>();

        Debug.Assert(m_virtualCamera != null,
            "CameraManager must be attached to a GameObject with a CinemachineVirtualCamera component.");

        m_averagePlayerPos = m_virtualCamera.Follow.position;
        m_leaderPlayer = m_virtualCamera.Follow;
    }

    private void Update()
    {
        // Early discards if the race has not started yet
        if (m_players == null || m_players.Count == 0 || !ServerManager.Instance.gameStarted)
        {
            return;
        }

        // Detects leader player based on altitude
        {
            float maxY = Mathf.Infinity;
            Transform leader = m_players[0].transform;

            foreach (GameObject player in m_players)
            {
                if (player.transform.position.y < maxY)
                {
                    maxY = player.transform.position.y;
                    leader = player.transform;
                }
            }

            m_leaderPlayer = leader;
        }

        // Recomputes average player position
        {
            Vector3 averagePosition = Vector3.zero;
            foreach (GameObject player in m_players)
            {
                averagePosition += player.transform.position;

            }
            averagePosition /= m_players.Count;
            m_averagePlayerPos = averagePosition;
        }

        // Updates Cinemachine virtual camera target
        if (m_virtualCamera != null)
        {
            m_virtualCamera.Follow = m_leaderPlayer;
            m_virtualCamera.LookAt = m_leaderPlayer;
        }
    }
}
