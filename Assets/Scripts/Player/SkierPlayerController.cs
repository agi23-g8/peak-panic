using UnityEngine;

public class SkierPlayerController : MonoBehaviour
{
    [SerializeField]
    [Range(1f, 20f)]
    [Tooltip("Sets the desired m_speed at which the skier moves on the snow-covered terrain.")]
    private float m_speed = 5f;

    [SerializeField]
    [Range(0f, 0.5f)]
    [Tooltip("Adjusts the glide effect on snow, controlling how smoothly the skier slides.")]
    private float m_slipperiness = 0.1f;

    [SerializeField]
    [Range(0f, 3f)]
    [Tooltip("Determines how much above the terrain surface the player should be positioned.")]
    private float m_adherenceOffset = 1.0f;

    [SerializeField]
    [Range(0f, 3f)]
    [Tooltip("Sets the maximum raycast distance under which player adherence is ensured to prevent bouncing on slopes.")]
    private float m_adherenceThreshold = 1.5f;

    private Camera m_mainCamera;
    private Rigidbody m_rigidBody;
    private Vector3 m_currentVelocity;
    private Vector3 m_currentSlopeNormal;
    private int m_terrainLayer;

    void Start()
    {
        m_mainCamera = Camera.main;
        m_terrainLayer = 1 << LayerMask.NameToLayer("Terrain");

        m_currentVelocity = Vector3.zero;
        m_currentSlopeNormal = Vector3.up;

        m_rigidBody = GetComponent<Rigidbody>();
        m_rigidBody.freezeRotation = true;
    }

    void Update()
    {
        // Get input from arrow keys
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Calculate movement direction relative to the camera's view space
        Vector3 cameraForward = m_mainCamera.transform.forward;
        Vector3 cameraRight = m_mainCamera.transform.right;

        // Flatten the vectors to disregard the y component (prevents unwanted vertical movement)
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        // Normalize vectors to ensure consistent m_speed in all directions
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate the movement direction based on camera orientation
        Vector3 movement = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;

        // Evaluate the normal and distance to the terrain slope
        RaycastHit hit;
        if (Physics.Raycast(transform.position,  Vector3.down, out hit, m_adherenceThreshold, m_terrainLayer))
        {
            m_currentSlopeNormal = hit.normal;

            // Ensure the skier adheres to the piste to prevent bouncing on inclined slopes
            transform.position += Vector3.down * Mathf.Max(0f, hit.distance - m_adherenceOffset);
        }

        // Project the movement onto the slope
        movement = Vector3.ProjectOnPlane(movement, m_currentSlopeNormal);

        // Move the player using SmoothDamp() to simulate the slipperiness of snow
        m_currentVelocity = Vector3.SmoothDamp(m_currentVelocity, m_speed * movement, ref m_currentVelocity, m_slipperiness);
        transform.Translate(m_currentVelocity * Time.deltaTime, Space.World);

        // Rotate the player to align with the slope
        Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, m_currentSlopeNormal) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, 0.1f);

        // Rotate the player to face the direction of movement
        if (movement.magnitude > 0)
        {
            Quaternion inputRotation = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, inputRotation, 0.1f);
        }

    }
}