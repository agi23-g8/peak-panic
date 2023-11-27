using UnityEngine;

public class SnowCameraController : Singleton<SnowCameraController>
{
    [SerializeField]
    [Tooltip("The target transform that the camera will follow.")]
    private Transform m_target;

    [SerializeField]
    [Range(1f, 50f)]
    [Tooltip("The height, in meters, from which the camera captures the terrain deformation.")]
    private float m_heightOffset = 10f;

    [SerializeField]
    [Range(10f, 100f)]
    [Tooltip("The length, in meters, of the deformation area captured by the camera.")]
    private float m_deformationArea = 60f;

    private Camera m_snowCamera;
    private int m_terrainLayer;
    private GameObject[] players;

    void Start()
    {
        // Get Camera component from the GameObject
        m_snowCamera = GetComponent<Camera>();
        Debug.Assert(m_snowCamera != null, "SnowCameraController script must be attached to a GameObject with a Camera component.");

        // Fetch terrain layer index
        m_terrainLayer = 1 << LayerMask.NameToLayer("Terrain");
    }

    private void Update()
    {
        // Override camera projection settings
        m_snowCamera.orthographic = true;
        m_snowCamera.nearClipPlane = 0f;
        m_snowCamera.farClipPlane = m_heightOffset * 2f;
        m_snowCamera.orthographicSize = m_deformationArea * 0.5f;

        // Set '_PrevSnowDeformationOrigin' shader variable
        Vector3 previousCamOrigin = transform.position;
        Shader.SetGlobalVector("_PrevSnowDeformationOrigin", previousCamOrigin);

        // Makes the camera follow the current leader at a constant height above the terrain.
        if (players != null && players.Length > 0)
        {
            m_target = FindCurrentLeader();
            Vector3 camPosition = m_target.position;

            Vector3 avgPosition = GetAveragePosition();
            camPosition.x = avgPosition.x;

            RaycastHit hit;
            if (Physics.Raycast(camPosition, Vector3.down, out hit, 50f, m_terrainLayer))
            {
                camPosition.y = hit.point.y + m_heightOffset;
            }

            transform.position = camPosition;
        }

        // Set '_CurSnowDeformationOrigin' shader variable
        Vector3 currentCamOrigin = transform.position;
        Shader.SetGlobalVector("_CurSnowDeformationOrigin", currentCamOrigin);

        // Set '_SnowDeformationAreaMeters' shader variable
        Shader.SetGlobalFloat("_SnowDeformationAreaMeters", m_deformationArea);

        // Set '_SnowDeformationOriginOffset' shader variable
        Vector3 originOffset = (currentCamOrigin - previousCamOrigin) / m_deformationArea;
        Shader.SetGlobalVector("_SnowDeformationOriginOffset", originOffset);
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