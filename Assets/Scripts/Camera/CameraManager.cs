using UnityEngine;
using Cinemachine;
using System.Collections.Generic;

public class CameraManager : Singleton<CameraManager>
{
    // _____________________________________________________________________
    // Internal members
    private CinemachineVirtualCamera m_virtualCamera;
    private CinemachineTrackedDolly m_trackedDolly;
    private List<GameObject> m_players => ServerManager.Instance.players;
    private bool m_raceStarted => ServerManager.Instance.gameStarted;


    // _____________________________________________________________________
    // Component lifecycle
    private void Start()
    {
        // Get CinemachineVirtualCamera component from GameObject
        m_virtualCamera = GetComponent<CinemachineVirtualCamera>();

        Debug.Assert(m_virtualCamera != null,
            "CameraManager must be attached to a GameObject with a CinemachineVirtualCamera component.");

        // Get CinemachineTrackedDolly component from virtual camera
        m_trackedDolly = m_virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
    }

    private void Update()
    {
        // Early discards if the race has not started yet
        if (m_players == null || m_players.Count == 0 || !m_raceStarted)
        {
            m_trackedDolly.m_XDamping = 1f;
            m_trackedDolly.m_YDamping = 1f;
            m_trackedDolly.m_ZDamping = 1f;
            m_trackedDolly.m_PathOffset = new Vector3(0f, 10f, 0f);
            return;
        }
        else
        {
            m_trackedDolly.m_XDamping = 5.0f;
            m_trackedDolly.m_YDamping = 5.0f;
            m_trackedDolly.m_ZDamping = 5.0f;
            m_trackedDolly.m_PathOffset = new Vector3(0f, 4f, 0f);
        }

        // Make the dolly camera follow the leader player
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

        m_virtualCamera.Follow = leader;
    }


    // _____________________________________________________________________
    // Exposed methods

    // @brief Returns the position halfway between the camera and the 
    // follow target (leader player) projected along the camera path.
    public Vector3 GetViewCenteredPositionOnPath()
    {
        if (m_trackedDolly == null)
        {
            return Vector3.zero;
        }

        // Apply half position offset along the path
        float pathPosition = m_trackedDolly.m_PathPosition - 0.5f * m_trackedDolly.m_AutoDolly.m_PositionOffset;

        // Retrieve corresponding transform on path
        Vector3 worldPosition = m_trackedDolly.m_Path.EvaluatePositionAtUnit(pathPosition, m_trackedDolly.m_PositionUnits);
        Quaternion worldOrientation = m_trackedDolly.m_Path.EvaluateOrientationAtUnit(pathPosition, m_trackedDolly.m_PositionUnits);

        // Compute horizontal offset from path
        Vector3 offsetX = worldOrientation * Vector3.right;
        Vector3 offsetZ = worldOrientation * Vector3.forward;

        // Apply half horizontal offset from path
        worldPosition += 0.5f * m_trackedDolly.m_PathOffset.x * offsetX;
        worldPosition += 0.5f * m_trackedDolly.m_PathOffset.z * offsetZ;

        return worldPosition;
    }
}
