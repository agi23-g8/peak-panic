using UnityEngine;

public class CameraFollowPath : MonoBehaviour
{
    // Reference to the CameraPatth script
    public BezierCameraPath cameraPath;
    public float rotationStep = 0.01f;

    public float duration = 5f;

    private float currentTime = 0f;

    void Update()
    {
        currentTime += Time.deltaTime;

        if (currentTime > duration)
        {
            currentTime = duration;
        }

        float t = currentTime / duration;

        // Calculate position on Catmull-Rom spline
        Vector3 position = cameraPath.Interpolate(t);

        // Move the camera to the calculated transform
        transform.position = position;

        // Rotate the camera to look at the next point on the curve
        if (t < 1f)
        {
            Vector3 nextPosition = cameraPath.Interpolate(t + 0.01f);
            transform.LookAt(nextPosition);
        }
    }
}
