using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// With much help from https://docs.unity3d.com/2022.3/Documentation/Manual/MobileInput.html
// and this: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/api/UnityEngine.InputSystem.Accelerometer.html
public class MobileAccelerometer : MonoBehaviour
{
    public float triggerJumpEventThreshold = 0.1f;
    private float lastJumpTime = 0.0f;

    [Header("Low Pass filter Settings")]

    public float accelerometerUpdateIntervalHz = 100.0f;

    [Tooltip("The greater the value of LowPassKernelWidthInSeconds, the slower the filtered value will converge towards current input sample (and vice versa).")]
    public float lowPassKernelWidthInSeconds = 1.0f;

    private float lowPassFilterFactor;
    private Vector3 lowPassValue = Vector3.zero;

    [Header("Accelerometer vector")]

    [Tooltip("The current acceleration of the device. Send this to the server")]
    public Vector3 acceleration;

    [Header("Debug")]

    public float d_sqrMagnitude;
    
    public TextMeshProUGUI xText;
    public TextMeshProUGUI yText;
    public TextMeshProUGUI zText;
    public TextMeshProUGUI jumpText;

    private bool ready = false;
    private int jumps = 0;

    private void Start()
    {
        if (Accelerometer.current == null)
        {
            Debug.LogError("No accelerometer found! If you are using Unity Remote debugging this is fine. If you don't want to test the Accelerometer: disable this Game Object.");

#if UNITY_EDITOR
            Debug.Log("You can ignore these errors stating NullReference - ugly I know");
            Debug.Log("It's because the Unity Remote App is loading and the editor is running as well.");
            ready = true;
            Setup();
#endif
            return;
        }
        else
        {
            InputSystem.EnableDevice(Accelerometer.current);
            Setup();
            ready = true;
        }
    }

    private void Setup()
    {
        float accelerometerUpdateInterval = 1 / accelerometerUpdateIntervalHz;

        lowPassFilterFactor = accelerometerUpdateInterval / lowPassKernelWidthInSeconds;

        lowPassValue = Accelerometer.current.acceleration.ReadValue();

        lastJumpTime = Time.time;
    }

    private void Update()
    {
        if (!ready)
            return;

        Vector3 accData = Accelerometer.current.acceleration.ReadValue();

        // low pass filter
        accData = LowPassFilterAccelerometer(lowPassValue, accData);

        acceleration = Vector3.zero;

        // we assume that the device is held parallel to the ground and the home button
        // is closest to the player. Example: lay the phone flat on a table in front of you
        // and you should be able to read on it normally. Portrait mode.
        acceleration.x = accData.y;
        acceleration.y = -accData.z;
        acceleration.z = -accData.x;

        // demo of jump event!
        if (acceleration.y > triggerJumpEventThreshold)
        {
            // only jump once per second
            if (lastJumpTime + 1f > Time.time)
            {
                return;
            }

            // here you could trigger a networked jump event

            Debug.Log("Jump!");
            jumps++;
            lastJumpTime = Time.time;
        }

        d_sqrMagnitude = acceleration.sqrMagnitude;

        if (xText == null || yText == null || zText == null || jumpText == null)
            return;

        xText.text = "X: " + acceleration.x.ToString("F6");
        yText.text = "Y: " + acceleration.y.ToString("F6");
        zText.text = "Z: " + acceleration.z.ToString("F6");
        jumpText.text = $"Jump counter: {jumps}";
    }

    Vector3 LowPassFilterAccelerometer(Vector3 prevValue, Vector3 acc)
    {
        Vector3 newValue = Vector3.Lerp(prevValue, acc, lowPassFilterFactor);
        return newValue;
    }

    /// <summary>
    /// Get the current acceleration of the device's sensor.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetAcceleration()
    {
        return acceleration;
    }
}
