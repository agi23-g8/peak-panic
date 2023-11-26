using UnityEngine;
using Unity.Collections;

public class PhysicsSkierController : MonoBehaviour
{
    // _____________________________________________________
    // Global settings

    [Header("Global Settings")]

    [SerializeField]
    [Range(0f, 100f)]
    [Tooltip("Sets the speed at which the skier moves.")]
    private float m_moveSpeed = 50f;

    [SerializeField]
    [Range(0f, 10f)]
    [Tooltip("Sets the speed at which the skier makes turns.")]
    private float m_turnSpeed = 4f;

    [SerializeField]
    [Range(1f, 200f)]
    [Tooltip("Controls the inertia applied to the player's movements.")]
    private float m_inertiaFactor = 100f;

    [SerializeField]
    [Range(0f, 300f)]
    [Tooltip("Additional gravity applied when not grounded.")]
    private float m_additionalGravity = 100f;

    [SerializeField]
    [Range(1f, 100f)]
    [Tooltip("Controls how fast the skier is dragged when skidding sideways.")]
    private float m_sidewaysDrag = 50f;

    [SerializeField]
    [Tooltip("The game object responsible for retrieving and pre-processing the inputs from the accelerometer.")]
    private MobileAccelerometer m_accelerometer;

    [SerializeField]
    private NetworkPlayer m_networkPlayer;


    // _____________________________________________________
    // Carving settings

    [Header("Carving Settings")]

    [SerializeField]
    [Range(0f, 0.5f)]
    [Tooltip("Threshold applied to the accelerometer roll input to trigger a carving action.")]
    private float m_carvingDetectionThreshold = 0.05f;

    [SerializeField]
    [Range(0f, 10f)]
    [Tooltip("Controls how much carving influences the movement direction.")]
    private float m_carvingDirectionInfluence = 4f;

    [SerializeField]
    [Range(0f, 20f)]
    [Tooltip("Controls how much carving scales up the player's speed.")]
    private float m_carvingSpeedInfluence = 10f;

    [SerializeField]
    [Range(0f, 300f)]
    [Tooltip("Sets the speed boost gained when successfully starting to carve.")]
    private float m_carvingStartBoost = 100f;


    // _____________________________________________________
    // Jump settings

    [Header("Jump Settings")]

    [SerializeField]
    [Range(0f, 2f)]
    [Tooltip("Threshold applied to the accelerometer pitch input to trigger a jump action.")]
    private float m_jumpDetectionThreshold = 0.8f;

    [SerializeField]
    [Range(0f, 50f)]
    [Tooltip("Vertical force applied to the player when successfully initiating a jump.")]
    private float m_jumpPower = 35f;

    [SerializeField]
    [Range(0f, 300f)]
    [Tooltip("Sets the boost gain when landing after a successfully jump.")]
    private float m_jumpLandingBoost = 100f;

    [SerializeField]
    [Range(0f, 1000f)]
    [Tooltip("Controls the angular speed of the flip when jumping from a ramp.")]
    private float m_jumpFlipSpeed = 650f;


    // _____________________________________________________
    // Internal members

    private Camera m_mainCamera;
    private Rigidbody m_rigidBody;
    private float m_skierHeight;
    private int m_terrainLayer;
    private bool m_onJumpRamp = false;
    private bool m_isGrounded = true;
    private bool m_isCarving = false;
    private float m_jumpLastInput = 0f;
    private float m_jumpLastTime;
    private float m_startCarvingLastTime;


    // _____________________________________________________
    // Script lifecycle

    private void Start()
    {
        m_mainCamera = Camera.main;
        m_terrainLayer = 1 << LayerMask.NameToLayer("Terrain");

        m_jumpLastTime = Time.time;
        m_startCarvingLastTime = Time.time;

        m_rigidBody = GetComponent<Rigidbody>();
        // m_rigidBody.freezeRotation = true;
        Freeze();

        Collider collider = GetComponent<Collider>();
        m_skierHeight = collider.bounds.size.y;
    }

    private void FixedUpdate()
    {
        // Detect grounded / aerial state
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, -transform.up, out hit, m_skierHeight, m_terrainLayer))
        {
            // Apply an additional gravity force to speed up landing
            AddGravity(m_additionalGravity);

            m_isGrounded = false;
            return;
        }

        // Force adherence to slope
        Vector3 slopeNormal = hit.normal;
        float slopeOffset = hit.distance;
        AdhereToSlope(slopeNormal, slopeOffset);

        // Process phone inputs
        float jumpInput = 0f;
        float carvingInput = 0f;
        float jumpLandingBoost = 0f;
        float carvingStartBoost = 0f;

        if (m_networkPlayer == null)
        {
            return;
        }

        // JUMP
        // Trigger jump action based on the change rate of the pitch angle (tilting) of the phone.
        // The pitch angle represents the rotation around the device's side-to-side axis.
        // A detection threshold is used to smooth the movement and limit false positives.

        // TODO: Get the delta Y from the network player
        float deltaY = 0;

        if (deltaY > m_jumpDetectionThreshold && Time.time > m_jumpLastTime + 1f)
        {
            jumpInput = deltaY;
            m_jumpLastTime = Time.time;
            m_jumpLastInput = jumpInput;
        }

        // Request boost on landing
        if (!m_isGrounded && m_jumpLastInput > 0f)
        {
            jumpLandingBoost = m_jumpLastInput;
            m_jumpLastInput = 0f;
        }
        m_isGrounded = true;

        // CARVING
        // Start carving action based on the current rolling angle of the phone.
        // The rolling angle refers to the rotation around the forward axis of the device.
        // A detection threshold is used to smooth the movement and limit false positives.
        float inputX = m_networkPlayer.GetX();
        if (Mathf.Abs(inputX) > m_carvingDetectionThreshold)
        {
            carvingInput = inputX;

            // Request boost when starting to carve
            if (!m_isCarving && Time.time > m_startCarvingLastTime + 1f)
            {
                m_startCarvingLastTime = Time.time;
                carvingStartBoost = 1f;//m_accelerometer.GetDeltaX();
            }
            m_isCarving = true;
        }
        else
        {
            m_isCarving = false;
        }


        // Base the movement direction on camera / slope / carving inputs
        Vector3 moveDirection = new Vector3(m_carvingDirectionInfluence * carvingInput, 0f, 1f);
        moveDirection = ProjectOnCameraSpace(moveDirection, m_mainCamera);
        moveDirection = ProjectOnSlope(moveDirection, slopeNormal);

        // Make the player ski faster when carving
        float moveSpeed = m_moveSpeed * (1f + m_carvingSpeedInfluence * Mathf.Abs(carvingInput));
        Advance(moveDirection, moveSpeed, m_turnSpeed, m_inertiaFactor);

        if (jumpInput > 0f)
        {
            // Impulse a vertical jump
            Jump(jumpInput * m_jumpPower);

            if (m_onJumpRamp)
            {
                // Make the player do a backflip when jumping from a ramp
                Flip(-jumpInput * m_jumpFlipSpeed);
            }
        }

        else if (jumpLandingBoost > 0f)
        {
            // Apply boost on landing
            Boost(jumpLandingBoost * m_jumpLandingBoost);
        }

        else if (carvingStartBoost > 0f)
        {
            // Apply boost when start carving
            Boost(carvingStartBoost * m_carvingStartBoost);
        }

        else
        {
            // Drag the player when skidding sideways
            float lateralSpeed = transform.InverseTransformDirection(m_rigidBody.velocity).x;
            SkidBrake(lateralSpeed * m_sidewaysDrag);
        }
    }

    //_________________________________________________________________________________
    // Vector Helpers

    Vector3 ProjectOnCameraSpace(Vector3 _direction, Camera _camera)
    {
        Vector3 cameraForward = _camera.transform.forward;
        Vector3 cameraRight = _camera.transform.right;

        // Disregard Y component to prevent unwanted vertical movement
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        // Normalize vectors to ensure consistent speed in all directions
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Deduce the direction in camera space
        Vector3 viewDirection = cameraRight * _direction.x + cameraForward * _direction.z;
        return viewDirection.normalized;
    }

    Vector3 ProjectOnSlope(Vector3 _direction, Vector3 _slopeNormal)
    {
        return Vector3.ProjectOnPlane(_direction, _slopeNormal);
    }


    //_________________________________________________________________________________
    // Rigid Body Helpers

    private void AdhereToSlope(Vector3 _slopeNormal, float _slopeOffset)
    {
        // Rotate the player to align with the slope orientation
        Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, _slopeNormal);
        m_rigidBody.MoveRotation(slopeRotation * transform.rotation);

        // Move the player down where the slope is to prevent bouncing
        float slopeDeltaHeight = Mathf.Max(0f, _slopeOffset - m_skierHeight);
        m_rigidBody.MovePosition(transform.position - transform.up * slopeDeltaHeight);
    }

    private void Advance(Vector3 _direction, float _moveSpeed, float _turnSpeed, float _inertia)
    {
        // Turn the player to face the given direction
        Quaternion turnRotation = Quaternion.LookRotation(_direction, Vector3.up);
        turnRotation = Quaternion.Slerp(transform.rotation, turnRotation, _turnSpeed * Time.fixedDeltaTime);
        m_rigidBody.MoveRotation(turnRotation);

        // Advance the player towards the given direction
        Vector3 moveForce = (_moveSpeed * _direction - m_rigidBody.velocity) / (_inertia * Time.fixedDeltaTime);
        m_rigidBody.AddForce(moveForce, ForceMode.Force);
    }

    private void Jump(float _intensity)
    {
        // Jump is simulated using a vertical impulse
        Vector3 jumpImpulse = _intensity * Vector3.up;
        m_rigidBody.AddForce(jumpImpulse, ForceMode.Impulse);
    }

    private void Flip(float _intensity)
    {
        // Flip is simulated using a torque around the current right vector.
        // This results in a front or back flip depending on the sign of _intensity.
        Vector3 backflipTorque = _intensity * transform.right;
        m_rigidBody.AddTorque(backflipTorque, ForceMode.Acceleration);
    }

    private void Boost(float _intensity)
    {
        // Boost is simulated using a forward impulse
        Vector3 boostImpulse = _intensity * transform.forward;
        m_rigidBody.AddForce(boostImpulse, ForceMode.Impulse);
    }

    private void SkidBrake(float _intensity)
    {
        // Skid-braking is simulated using a force opposed to the current right vector
        Vector3 skidForce = -_intensity * transform.right;
        m_rigidBody.AddForce(skidForce, ForceMode.Force);
    }

    private void AddGravity(float _intensity)
    {
        // Gravity is obtained using a simple vertical down force
        Vector3 gravity = -_intensity * Vector3.up;
        m_rigidBody.AddForce(gravity, ForceMode.Force);
    }


    //_________________________________________________________________________________
    // Triggers

    private void OnTriggerEnter(Collider _other)
    {
        if (_other.CompareTag("Jump"))
        {
            m_onJumpRamp = true;
        }
    }

    private void OnTriggerExit(Collider _other)
    {
        if (_other.CompareTag("Jump"))
        {
            m_onJumpRamp = false;
        }
    }

    public void SetNetworkPlayer(NetworkPlayer networkPlayer)
    {
        m_networkPlayer = networkPlayer;
    }

    public void Freeze()
    {
        m_rigidBody.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void Unfreeze()
    {
        m_rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
    }


}
