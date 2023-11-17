using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    public float speed = 5f;
    private Camera mainCamera;
    private Vector3 currentVelocity;

    void Start()
    {
        mainCamera = Camera.main;
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

        // Use SmoothDamp to simulate inertia
        Vector3 targetVelocity = new Vector3(movement.x * speed, 0f, movement.z * speed);
        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref currentVelocity, 0.1f);

        // Move the player only on the X and Z axes
        transform.Translate(currentVelocity * Time.deltaTime, Space.World);

        // Rotate the player model to face the direction of movement
        if (movement.magnitude > 0)
        {
            Quaternion toRotation = Quaternion.LookRotation(new Vector3(movement.x, 0f, movement.z), Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, 0.1f);
        }
    }
}
