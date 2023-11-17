using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public Transform player;
    public float sensitivity = 50f;
    public float blendFactor = 5f;
    public float distance = 5f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private void Start()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    private void Update()
    {
        // Get input from the right joystick of the gamepad
        float horizontalRotate = Input.GetAxis("RightStickHorizontal");
        float verticalRotate = Input.GetAxis("RightStickVertical");

        // Calculate the rotation angles based on the right joystick input
        float rotationX = verticalRotate * sensitivity;
        float rotationY = horizontalRotate * sensitivity;

        // Update the target rotation around the player
        UpdateRotation(rotationX, rotationY);

        // Update the target position to follow the player
        UpdatePosition(player.position);

        // Apply temporal blending to move each frame towards the target
        SmoothBlend();
    }

    private void UpdateRotation(float _xInput, float _yInput)
    {
        // Update the target rotation based on input
        targetRotation = Quaternion.Euler(
            Mathf.Clamp(transform.rotation.eulerAngles.x + _xInput, 0f, 89f),
            transform.rotation.eulerAngles.y + _yInput, 
            0f);
    }

    private void UpdatePosition(Vector3 _focusPosition)
    {
        // Update the target position based on input
        targetPosition = _focusPosition - transform.forward * distance;
    }

    private void SmoothBlend()
    {
        float blendWeight = Mathf.Clamp(blendFactor * Time.deltaTime, 0f, 1f);

        // Smoothly rotate the camera towards the target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blendWeight);

        // Smoothly move the camera towards the target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, blendWeight);
    }
}



