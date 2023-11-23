using UnityEngine;
using Unity.Collections;

public class PhysicsSkierController : MonoBehaviour
{
    public enum ActionState
    {
        Idle,
        Carving,
        Jump,
        Airborne,
    }

    public bool m_respawn = false;
    private Vector3 m_initialPos;
    private Quaternion m_initialRot;
    private float m_jumpBoost = 0f;


    // _____________________________________________________
    // Global controls

    [Header("Global Controls")]

    [SerializeField]
    [Range(0f, 100f)]
    [Tooltip("Sets the global speed at which the skier moves on the snow-covered terrain.")]
    private float m_globalMoveSpeed = 50f;

    [SerializeField]
    [Range(0f, 20f)]
    [Tooltip("Sets the global speed at which the skier rotates on the snow-covered terrain.")]
    private float m_globalRotateSpeed = 5f;

    [SerializeField]
    [Range(1f, 200f)]
    [Tooltip("Controls the inertia applied to the player's movements.")]
    private float m_inertiaFactor = 100f;

    [SerializeField]
    [Range(1f, 100f)]
    [Tooltip("Controls how fast the skier is dragged when skidding sideways.")]
    private float m_sidewaysDrag = 50f;


    // _____________________________________________________
    // Mobile controls

    [Header("Mobile Controls")]

    [SerializeField]
    [Tooltip("The game object responsible for retrieving and pre-processing the inputs from the accelerometer.")]
    private MobileAccelerometer m_accelerometer;

    [ReadOnly]
    public ActionState m_state;

    [SerializeField]
    [Range(0f, 10f)]
    [Tooltip("Base forward speed applied to the player.")]
    private float m_baseForwardSpeed = 1f;

    [SerializeField]
    [Range(0f, 0.5f)]
    [Tooltip("Threshold applied to the accelerometer roll input to trigger a carving action.")]
    private float m_carvingDetectionThreshold = 0.05f;

    [SerializeField]
    [Range(1f, 10f)]
    [Tooltip("Multiplier applied to the carving input (triggered when rolling the phone).")]
    private float m_carvingIntensity = 4f;

    [SerializeField]
    [Range(1f, 100f)]
    [Tooltip("Sets the boost gain when successfully carving.")]
    private float m_carvingBoost = 2f;

    [SerializeField]
    [Range(0f, 2f)]
    [Tooltip("Threshold applied to the accelerometer pitch input to trigger a jump action.")]
    private float m_jumpDetectionThreshold = 0.8f;

    [SerializeField]
    [Range(1f, 200f)]
    [Tooltip("Multiplier applied to the jump input (triggered when tilting the phone).")]
    private float m_jumpIntensity = 50f;

    [SerializeField]
    [Range(1f, 100f)]
    [Tooltip("Sets the boost gain when landing after a successfully jump.")]
    private float m_jumpLandingBoost = 2f;

    [SerializeField]
    [Range(0f, 50f)]
    [Tooltip("Speed from which a jump becomes a back flip.")]
    private float m_backflipSpeedThreshold = 20f;

    [SerializeField]
    [Range(1f, 50f)]
    [Tooltip("Additional multiplier applied to the back flip torque.")]
    private float m_backflipIntensity = 15f;


    // _____________________________________________________
    // Internal members
    private Camera m_mainCamera;
    private Rigidbody m_rigidBody;
    private float m_skierHeight;
    private int m_terrainLayer;
    private float m_lastJumpTime;
    private float m_lastBoostTime;


    // _____________________________________________________
    // Script lifecycle

    private void Start()
    {
        m_mainCamera = Camera.main;
        m_terrainLayer = 1 << LayerMask.NameToLayer("Terrain");

        m_lastJumpTime = Time.time;
        m_lastBoostTime = Time.time;

        m_rigidBody = GetComponent<Rigidbody>();
        m_rigidBody.freezeRotation = true;

        Collider collider = GetComponent<Collider>();
        m_skierHeight = collider.bounds.size.y;

        m_initialPos = transform.position;
        m_initialRot = transform.rotation;
        m_respawn = false;
    }

    private void FixedUpdate()
    {
        // HACK HACK HACK
        if (m_respawn)
        {
            transform.position = m_initialPos;
            transform.rotation = m_initialRot;
            m_rigidBody.velocity = Vector3.zero;
            m_respawn = false;
        }

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

            float forwardInput = m_baseForwardSpeed;
            float carvingInput = 0f;
            float jumpInput = 0f;
            float speedBoost = 0f;

            // Process phone inputs
            if (m_accelerometer != null && m_accelerometer.IsReady())
            {
                // Jump Landing Boost
                if (m_state == ActionState.Airborne)
                {
                    Debug.Log($"[Jump] boost !");
                    speedBoost += m_jumpBoost;
                    //m_state = ActionState.Idle;
                }

                // JUMP
                // Detect jump action based on the change rate of the pitch angle (tilting) of the phone.
                // The pitch angle represents the rotation around the device's side-to-side axis.
                // A detection threshold is used to smooth the movement and limit false positives.
                float deltaY = -m_accelerometer.GetDeltaY();

                // Jump trigger
                if (deltaY > m_jumpDetectionThreshold && Time.time > m_lastJumpTime + 2f)
                {
                    jumpInput = m_jumpIntensity * deltaY;
                    m_lastJumpTime = Time.time;
                    m_state = ActionState.Jump;

                    // saved for landing boost
                    m_jumpBoost = m_jumpLandingBoost * deltaY;
                }

                // Jump Landing Boost
                // else if (m_state == ActionState.Jump)
                // {
                //     Debug.Log($"[Jump] boost !");
                //     speedBoost += m_jumpLandingBoost;
                //     m_state = ActionState.Idle;
                // }

                // CARVING
                // Detect carving action based on the current rolling angle of the phone.
                // The rolling angle refers to the rotation around the forward axis of the device.
                // A detection threshold is used to smooth the movement and limit false positives.
                float inputX = m_accelerometer.GetX();

                // Carving trigger
                if (Mathf.Abs(inputX) > m_carvingDetectionThreshold)
                {
                    carvingInput = m_carvingIntensity * inputX;

                    // Carving Boost
                    if (m_state == ActionState.Idle && Time.time > m_lastBoostTime + 1f)
                    {
                        Debug.Log($"[Carving] boost !");
                        speedBoost += m_carvingBoost;
                        m_lastBoostTime = Time.time;
                    }

                    m_state = ActionState.Carving;
                }
                else
                {
                    m_state = ActionState.Idle;
                }
            }
        #if UNITY_EDITOR
            else
            {
                // Get current keyboard / gamepad inputs, only for debug purpose
                carvingInput = Input.GetAxis("Horizontal");
                forwardInput = Input.GetAxis("Vertical");
            }
        #endif

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
            Vector3 inputMovement = (cameraForward * forwardInput + cameraRight * carvingInput).normalized;

            // Project the movement onto the slope
            inputMovement = Vector3.ProjectOnPlane(inputMovement, slopeNormal);

            if (inputMovement.magnitude > 0f)
            {
                // Rotate the player to face the direction of movement
                Quaternion inputRotation = Quaternion.LookRotation(inputMovement, Vector3.up);
                inputRotation = Quaternion.Slerp(transform.rotation, inputRotation, m_globalRotateSpeed * Time.fixedDeltaTime);
                m_rigidBody.MoveRotation(inputRotation);

                // Move the player based on the processed inputs
                Vector3 inputForce = (m_globalMoveSpeed * inputMovement - m_rigidBody.velocity) / (m_inertiaFactor * Time.fixedDeltaTime);
                m_rigidBody.AddForce(inputForce, ForceMode.Force);
            }

            if (m_rigidBody.velocity.magnitude > 0f)
            {
                // Drag the player when skidding sideways
                float lateralSpeed = transform.InverseTransformDirection(m_rigidBody.velocity).x;
                Vector3 skidForce = -transform.right * lateralSpeed * m_sidewaysDrag;
                m_rigidBody.AddForce(skidForce, ForceMode.Force);
            }

            if (jumpInput > 0f)
            {
                // Add vertical impulse force to simulate jump
                Vector3 jumpForce = jumpInput * Vector3.up;
                m_rigidBody.AddForce(jumpForce, ForceMode.Impulse);

                float forwardSpeed = Vector3.Dot(m_rigidBody.velocity, transform.forward);
                if (forwardSpeed >= m_backflipSpeedThreshold)
                {
                    // Apply torque to simulate backflip
                    Vector3 backflipTorque = -jumpInput * m_backflipIntensity * transform.right;
                    m_rigidBody.AddTorque(backflipTorque, ForceMode.Acceleration);
                }
            }

            if (speedBoost > 0f)
            {
                // Add forward impulse to simulate a speed boost
                Vector3 boostForce = speedBoost * transform.forward;
                m_rigidBody.AddForce(boostForce, ForceMode.Impulse);
            }
        }
        else
        {
            m_state = ActionState.Airborne;
        }
    }
}
