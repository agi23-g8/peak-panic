using UnityEngine;

public class GameCameraController : Singleton<GameCameraController>
{
    [SerializeField]
    [Tooltip("The transform that the camera will follow.")]
    private Transform m_objectToFollow;

    [SerializeField]
    [Tooltip("Camera offset position from the followed object, expressed in meters.")]
    private Vector3 m_camOffset = new Vector3(0, 3, -7);

    [SerializeField]
    [Range(0f, 20f)]
    [Tooltip("Factor controlling the blending of camera movement.")]
    private float m_blendFactor = 5f;


    [SerializeField]
    [Range(0f, 20f)]
    [Tooltip("Minimal height above the terrain.")]
    private float m_minHeightFromTerrain = 5f;

    private int m_terrainLayer;

    private GameObject[] players;

    private void Start()
    {
        m_terrainLayer = 1 << LayerMask.NameToLayer("Terrain");
    }

    private void Update()
    {
        if (players != null && players.Length > 0)
        {
            m_objectToFollow = FindCurrentLeader();
        }

        // Compute target transform to follow the object
        Vector3 targetPosition = m_objectToFollow.position + m_camOffset;
        targetPosition.x = GetAveragePosition().x;
        Quaternion targetRotation = Quaternion.LookRotation(m_objectToFollow.position - transform.position);

        // Raycast down to find the terrain height at the target position
        RaycastHit hit;
        if (Physics.Raycast(targetPosition + Vector3.up * m_minHeightFromTerrain, Vector3.down, out hit, 2f * m_minHeightFromTerrain, m_terrainLayer))
        {
            // Adjust the height to stay above the terrain
            targetPosition.y = Mathf.Max(targetPosition.y, hit.point.y + m_minHeightFromTerrain);
        }

        // Smoothly move the camera towards the target transform
        float blendWeight = Mathf.Clamp(m_blendFactor * Time.deltaTime, 0f, 1f);
        transform.position = Vector3.Lerp(transform.position, targetPosition, blendWeight);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blendWeight);

        // Prevent camera from yaw and roll
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, 0f, 0f);
    }

    private Transform FindCurrentLeader()
    {
        // which player is in the lead?
        // which player has the largest z value?
        // return that player's transform

        float maxZ = -Mathf.Infinity;
        Transform leader = players[0].transform;
        foreach (GameObject player in players)
        {
            if (player.transform.position.z > maxZ)
            {
                maxZ = player.transform.position.z;
                leader = player.transform;
            }
        }
        return leader;
    }

    private Vector3 GetAveragePosition()
    {
        Vector3 averagePosition = Vector3.zero;
        foreach (GameObject player in players)
        {
            averagePosition += player.transform.position;

        }
        averagePosition /= players.Length;

        return averagePosition;
    }

    public void UpdatePlayerList()
    {
        players = GameObject.FindGameObjectsWithTag("Player");
        Debug.Log("Number of players: " + players.Length);
    }

}