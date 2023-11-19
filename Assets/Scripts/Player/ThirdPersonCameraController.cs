using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The target transform that the camera will follow.")]
    private Transform m_target;

    [SerializeField]
    [Range(1f, 100f)]
    [Tooltip("Sensitivity of camera movement.")]
    private float m_sensitivity = 50f;

    [SerializeField]
    [Range(0f, 20f)]
    [Tooltip("Factor controlling the blending of camera movement.")]
    private float m_blendFactor = 5f;

    [SerializeField]
    [Range(0.1f, 40f)]
    [Tooltip("Distance from the camera to the target, expressed in meters.")]
    private float m_distanceToTarget = 5f;

    private Vector3 m_targetPosition;
    private Quaternion m_targetRotation;

    private void Start()
    {
        m_targetPosition = transform.position;
        m_targetRotation = transform.rotation;
    }

    private void Update()
    {
        // Get input from the right joystick of the gamepad
        float horizontalRotate = Input.GetAxis("RightStickHorizontal");
        float verticalRotate = Input.GetAxis("RightStickVertical");

        // Calculate the rotation angles based on the right joystick input
        float rotationX = verticalRotate * m_sensitivity;
        float rotationY = horizontalRotate * m_sensitivity;

        // Update the orientation to rotation around the target
        UpdateRotation(rotationX, rotationY);

        // Update the position to follow the target
        UpdatePosition(m_target.position);

        // Apply temporal blending to smoothly move the camera
        SmoothBlend();
    }

    private void UpdateRotation(float _xInput, float _yInput)
    {
        // Update the target rotation based on input
        m_targetRotation = Quaternion.Euler(
            Mathf.Clamp(transform.rotation.eulerAngles.x + _xInput, 0f, 89f),
            transform.rotation.eulerAngles.y + _yInput, 
            0f);
    }

    private void UpdatePosition(Vector3 _focusPosition)
    {
        // Update the target position based on input
        m_targetPosition = _focusPosition - transform.forward * m_distanceToTarget;
    }

    private void SmoothBlend()
    {
        float blendWeight = Mathf.Clamp(m_blendFactor * Time.deltaTime, 0f, 1f);

        // Smoothly rotate the camera towards the target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, m_targetRotation, blendWeight);

        // Smoothly move the camera towards the target position
        transform.position = Vector3.Lerp(transform.position, m_targetPosition, blendWeight);
    }
}