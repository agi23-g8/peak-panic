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

    [Header("Debug")]

    public Vector3 d_accel;
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
            Debug.LogError("No accelerometer found! If using Unity Remote debugging this is fine");

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
            {
                return;
            }

            Debug.Log("Jump!");
            jumps++;
            jumpText.text = $"Jump counter: {jumps}";
            lastJumpTime = Time.time;
        }

        d_accel = acc;
        d_sqrMagnitude = acc.sqrMagnitude;

        xText.text = "X: " + acc.x.ToString("F6");
        yText.text = "Y: " + acc.y.ToString("F6");
        zText.text = "Z: " + acc.z.ToString("F6");
    }

    Vector3 LowPassFilterAccelerometer(Vector3 prevValue, Vector3 acc)
    {
        Vector3 newValue = Vector3.Lerp(prevValue, acc, lowPassFilterFactor);
        return newValue;
    }
}
