using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is the same as Accelerometer.cs but with added control features
// Good to use with Unity Remote app to control a game object
public class DebugAccelerometer : MonoBehaviour
{
    [Header("The object we want to move")]
    public Transform objectOfInterest;

    public Rigidbody rb;

    [Header("Movement Settings")]

    public float horizontalSpeed = 250.0f;
    public float verticalSpeed = 2.50f;
    public float triggerJumpEventThreshold = 0.1f;

    private float lastJumpTime = 0.0f;

    [Header("Low Pass filter Settings")]

    public float accelerometerUpdateIntervalHz = 100.0f;

    [Tooltip("The greater the value of LowPassKernelWidthInSeconds, the slower the filtered value will converge towards current input sample (and vice versa).")]
    public float lowPassKernelWidthInSeconds = 1.0f;

    private float lowPassFilterFactor;
    private Vector3 lowPassValue = Vector3.zero;

    [Header("Debug")]

    public Vector3 d_accel;
    public float d_sqrMagnitude;

    public bool lockX = false;
    public bool lockY = true;
    public bool lockZ = false;

    private void Start()
    {
        float accelerometerUpdateInterval = 1 / accelerometerUpdateIntervalHz;

        lowPassFilterFactor = accelerometerUpdateInterval / lowPassKernelWidthInSeconds;
        lowPassValue = Input.acceleration;

        lastJumpTime = Time.time;
    }

    private void Update()
    {
        Vector3 acc = GetAccelerometerValue();

        // low pass filter
        acc = LowPassFilterAccelerometer(lowPassValue, acc);

        d_accel = acc;
        d_sqrMagnitude = acc.sqrMagnitude;

        MoveObject(acc);
    }

    private void MoveObject(Vector3 acc)
    {
        Vector3 dir = Vector3.zero;

        // we assume that the device is held parallel to the ground and the home button
        // is closest to the player. Example: lay the phone flat on a table in front of you
        // and you should be able to read on it normally. (not rotated)

        dir.x = acc.x;
        dir.y = -acc.z;
        dir.z = acc.y;

        // clamp acceleration vector to the unit sphere
        if (dir.sqrMagnitude > 1)
            dir.Normalize();

        // draw ray before deltaTime
        Debug.DrawRay(objectOfInterest.position, Vector3.right * dir.x * horizontalSpeed, Color.red);
        Debug.DrawRay(objectOfInterest.position, Vector3.up * dir.y * verticalSpeed, Color.green);
        Debug.DrawRay(objectOfInterest.position, Vector3.forward * dir.z * horizontalSpeed, Color.blue);

        // demo of jump event!
        if (dir.y > triggerJumpEventThreshold)
        {
            // only jump once per second
            if (lastJumpTime + 1f > Time.time)
                return;

            Debug.Log("Jump!");
            rb.AddForce(Vector3.up * verticalSpeed, ForceMode.Impulse);
            lastJumpTime = Time.time;
        }

        // make it move in m/s instead of m/frame
        dir *= Time.deltaTime;

        // scale by speed
        dir.x *= horizontalSpeed;
        dir.z *= horizontalSpeed;

        // lock axis
        if (lockX) dir.x = 0;
        if (lockY) dir.y = 0;
        if (lockZ) dir.z = 0;

        objectOfInterest.Translate(dir);
    }

    Vector3 GetAccelerometerValue()
    {
        Vector3 acc = Vector3.zero;
        float period = 0.0f;

        foreach (AccelerationEvent evnt in Input.accelerationEvents)
        {
            acc += evnt.acceleration * evnt.deltaTime;
            period += evnt.deltaTime;
        }
        if (period > 0)
        {
            acc *= 1.0f / period;
        }
        return acc;
    }

    Vector3 LowPassFilterAccelerometer(Vector3 prevValue, Vector3 acc)
    {
        Vector3 newValue = Vector3.Lerp(prevValue, acc, lowPassFilterFactor);
        return newValue;
    }
}
