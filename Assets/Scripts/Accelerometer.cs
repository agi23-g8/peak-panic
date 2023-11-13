using TMPro;
using UnityEngine;

// With much help from https://docs.unity3d.com/2022.3/Documentation/Manual/MobileInput.html
public class Accelerometer : MonoBehaviour
{
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
    
    public TextMeshProUGUI xText;
    public TextMeshProUGUI yText;
    public TextMeshProUGUI zText;

    private void Start()
    {
        float accelerometerUpdateInterval = 1 / accelerometerUpdateIntervalHz;

        lowPassFilterFactor = accelerometerUpdateInterval / lowPassKernelWidthInSeconds;
        lowPassValue = Input.acceleration;

        lastJumpTime = Time.time;
    }

    private void Update()
    {
        Vector3 accData = GetAccelerometerValue();

        // low pass filter
        accData = LowPassFilterAccelerometer(lowPassValue, accData);

        Vector3 acc = Vector3.zero;

        // we assume that the device is held parallel to the ground and the home button
        // is closest to the player. Example: lay the phone flat on a table in front of you
        // and you should be able to read on it normally. (not rotated)
        acc.x = accData.y;
        acc.y = -accData.z;
        acc.z = -accData.x;

        // demo of jump event!
        if (acc.y > triggerJumpEventThreshold)
        {
            // only jump once per second
            if (lastJumpTime + 1f > Time.time)
                return;

            Debug.Log("Jump!");
            lastJumpTime = Time.time;
        }

        d_accel = acc;
        d_sqrMagnitude = acc.sqrMagnitude;

        xText.text = "X: " + acc.x.ToString("F6");
        yText.text = "Y: " + acc.y.ToString("F6");
        zText.text = "Z: " + acc.z.ToString("F6");
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
