using UnityEngine;

public class GameCameraController : MonoBehaviour
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

    private void Start()
    {
        m_terrainLayer = 1 << LayerMask.NameToLayer("Terrain");
    }

    private void Update()
    {
        // Compute target transform to follow the object
        Vector3 targetPosition = m_objectToFollow.position + m_camOffset;
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
    }
}