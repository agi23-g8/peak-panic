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
    [Tooltip("Factor controlling the blending of camera translation.")]
    private float m_translationBlend = 3f;

    [SerializeField]
    [Range(0f, 20f)]
    [Tooltip("Factor controlling the blending of camera rotation.")]
    private float m_rotationBlend = 1f;

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
        if (players == null || players.Length == 0)
        {
            return;
        }

        // Compute target transform to follow the object
        m_objectToFollow = FindCurrentLeader();
        Vector3 targetPosition = m_objectToFollow.position;
        targetPosition.x = GetAveragePosition().x;

        // Offset the camera to see the player from behind
        Vector3 offset = m_camOffset;
        offset = m_objectToFollow.right * offset.x + m_objectToFollow.up * offset.y + m_objectToFollow.forward * offset.z;
        targetPosition += offset;

        Quaternion targetRotation = Quaternion.LookRotation(m_objectToFollow.position - transform.position);

        // Raycast down to find the terrain height at the target position
        RaycastHit hit;
        if (Physics.Raycast(targetPosition + Vector3.up * m_minHeightFromTerrain, Vector3.down, out hit, 2f * m_minHeightFromTerrain, m_terrainLayer))
        {
            // Adjust the height to stay above the terrain
            targetPosition.y = Mathf.Max(targetPosition.y, hit.point.y + m_minHeightFromTerrain);
        }

        // Smoothly move the camera towards the target transform
        float translationBlendWeight = Mathf.Clamp(m_translationBlend * Time.deltaTime, 0f, 1f);
        transform.position = Vector3.Lerp(transform.position, targetPosition, translationBlendWeight);

        float rotationBlendWeight = Mathf.Clamp(m_rotationBlend * Time.deltaTime, 0f, 1f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationBlendWeight);

        // Prevent camera from yaw and roll
        //transform.rotation = Quaternion.Euler(transform.eulerAngles.x, 0f, 0f);
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
        if (players == null || players.Length == 0)
        {
            return Vector3.zero;
        }

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