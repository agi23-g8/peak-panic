using UnityEngine;

public class PhysicsSkierController : MonoBehaviour
{
    [SerializeField]
    [Range(0f, 100f)]
    [Tooltip("Sets the desired speed at which the skier moves on the snow-covered terrain.")]
    private float m_moveSpeed = 50f;

    [SerializeField]
    [Range(0f, 20f)]
    [Tooltip("Sets the desired speed at which the skier rotates on the snow-covered terrain.")]
    private float m_rotateSpeed = 5f;

    [SerializeField]
    [Range(1f, 200f)]
    [Tooltip("Controls with how much inertia the player moves.")]
    private float m_inertiaFactor = 100f;

    [SerializeField]
    [Range(1f, 100f)]
    [Tooltip("Controls how fast the skier is dragged when skidding sideways.")]
    private float m_sidewaysDrag = 50f;

    [SerializeField]
    [Tooltip("The game object responsible for retrieving and pre-processing the inputs from the phone.")]
    private PhoneInputs m_phoneInputs;

    private Camera m_mainCamera;
    private Rigidbody m_rigidBody;
    private float m_skierHeight;
    private int m_terrainLayer;

    void Start()
    {
        m_mainCamera = Camera.main;
        m_terrainLayer = 1 << LayerMask.NameToLayer("Terrain");

        m_rigidBody = GetComponent<Rigidbody>();
        m_rigidBody.freezeRotation = true;

        Collider collider = GetComponent<Collider>();
        m_skierHeight = collider.bounds.size.y;
    }

    void FixedUpdate()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, m_skierHeight, m_terrainLayer))
        {
            Vector3 slopeNormal = hit.normal;

            // Rotate the player to align with the slope orientation
            Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, slopeNormal);
            m_rigidBody.MoveRotation(slopeRotation * transform.rotation);

            // Ensure the player adheres to the slope to prevent bouncing
            float slopeDeltaHeight = Mathf.Max(0f, hit.distance - m_skierHeight);
            m_rigidBody.MovePosition(transform.position - transform.up * slopeDeltaHeight);

            float verticalInput = 0f;
            float horizontalInput = 0f;

            if (m_phoneInputs != null && m_phoneInputs.IsReady())
            {
                // Get current phone inputs
                horizontalInput = m_phoneInputs.GetX();
                verticalInput = m_phoneInputs.GetY();
            }
            else
            {
                // Get current keyboard / gamepad inputs
                horizontalInput = Input.GetAxis("Horizontal");
                verticalInput = Input.GetAxis("Vertical");
            }

            // Movement is calculated relative to the camera's view space
            Vector3 cameraForward = m_mainCamera.transform.forward;
            Vector3 cameraRight = m_mainCamera.transform.right;

            // Disregard the y component to prevent unwanted vertical movement
            cameraForward.y = 0f;
            cameraRight.y = 0f;

            // Normalize vectors to ensure consistent speed in all directions
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Calculate the movement direction based on camera orientation
            Vector3 inputMovement = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;

            // Project the movement onto the slope
            inputMovement = Vector3.ProjectOnPlane(inputMovement, slopeNormal);

            if (inputMovement.magnitude > 0f)
            {
                // Rotate the player to face the direction of movement
                Quaternion inputRotation = Quaternion.LookRotation(inputMovement, Vector3.up);
                inputRotation = Quaternion.Slerp(transform.rotation, inputRotation, m_rotateSpeed * Time.fixedDeltaTime);
                m_rigidBody.MoveRotation(inputRotation);

                // Move the player based on the processed inputs
                Vector3 inputForce = (m_moveSpeed * inputMovement - m_rigidBody.velocity) / (m_inertiaFactor * Time.fixedDeltaTime);
                m_rigidBody.AddForce(inputForce, ForceMode.Force);
            }

            if (m_rigidBody.velocity.magnitude > 0f)
            {
                // Drag the player when skidding sideways
                float lateralSpeed = transform.InverseTransformDirection(m_rigidBody.velocity).x;
                Vector3 skidForce = -transform.right * lateralSpeed * m_sidewaysDrag;
                m_rigidBody.AddForce(skidForce, ForceMode.Force);
            }
        }
    }
}
