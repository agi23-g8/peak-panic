using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float slipperiness = 0.1f;
    public float maxRaycastDistance = 1.5f;

    private Camera mainCamera;
    private Rigidbody rigidBody;
    private Vector3 currentVelocity;
    private Vector3 currentSlopeNormal;
    private int terrainLayer;

    void Start()
    {
        mainCamera = Camera.main;
        terrainLayer = 1 << LayerMask.NameToLayer("Terrain");

        currentVelocity = Vector3.zero;
        currentSlopeNormal = Vector3.up;

        rigidBody = GetComponent<Rigidbody>();
        rigidBody.freezeRotation = true;
    }

    void Update()
    {
        // Get input from arrow keys
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Calculate movement direction relative to the camera's view space
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        // Flatten the vectors to disregard the y component (prevents unwanted vertical movement)
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        // Normalize vectors to ensure consistent speed in all directions
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate the movement direction based on camera orientation
        Vector3 movement = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;

        // Evaluate the normal of the terrain slope
        RaycastHit hit;
        if (Physics.Raycast(transform.position,  Vector3.down, out hit, maxRaycastDistance, terrainLayer))
        {
            currentSlopeNormal = hit.normal;
        }

        // Project the movement onto the current slope
        movement = Vector3.ProjectOnPlane(movement, currentSlopeNormal);

        // Move the player using SmoothDamp() to simulate the slipperiness of snow
        currentVelocity = Vector3.SmoothDamp(currentVelocity, speed * movement, ref currentVelocity, slipperiness);
        transform.Translate(currentVelocity * Time.deltaTime, Space.World);

        // Rotate the player to align with the slope
        Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, currentSlopeNormal) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, 0.1f);

        // Rotate the player to face the direction of movement
        if (movement.magnitude > 0)
        {
            Quaternion inputRotation = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, inputRotation, 0.1f);
        }

    }
}