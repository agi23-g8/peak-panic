using UnityEngine;
using Unity.Collections;
using UnityEngine.InputSystem;

// References :
//   - https://docs.unity3d.com/2022.3/Documentation/Manual/MobileInput.html
//   - https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/api/UnityEngine.InputSystem.Accelerometer.html

public class MobileAccelerometer : MonoBehaviour
{
    [ReadOnly]
    [Tooltip("Current state of the accelerometer.")]
    public bool m_isReady = false;

    [ReadOnly]
    [Tooltip("Current value retrieved from the accelerometer and pre-processed.")]
    public Vector3 m_value = Vector3.zero;

    [ReadOnly]
    [Tooltip("Last computed change rate of the inputs retrieve from the accelerometer.")]
    public Vector3 m_changeRate = Vector3.zero;


    // _____________________________________________________
    // low pass filter

    [SerializeField]
    [Tooltip("Refresh rate of the accelerometer, expressed in hertz.")]
    private float m_accelerometerRefreshRate = 100f;

    [SerializeField]
    [Tooltip("Width of the low pass kernel used to filter the raw input sample, expressed in seconds.")]
    private float m_lowPassKernelWidth = 1f;

    [SerializeField]
    [Tooltip("Global multiplier pre-applied to the phone inputs.")]
    private float m_inputMultiplier = 25f;

    private float m_lowPassFilterFactor;
    private Vector3 m_lowPassFilterValue;


    // _____________________________________________________
    // sliding average for Y component

    [SerializeField]
    [Range(1, 60)]
    [Tooltip("Number of frames to consider when averaging the accelerometer's Y input.")]
    private int m_numberOfFrames = 20;

    private float[] m_valuesY; // Circular buffer to store Y values
    private int m_currentIndex = 0; // Index to keep track of the current position in the buffer
    private float m_sumY = 0f; // Sum of the Y values in the circular buffer


    // _____________________________________________________
    // Value tracked to compute movement speed
    private Vector3 m_previousValue = Vector3.zero;


    // _____________________________________________________
    // Script lifecycle

    private void Start()
    {
        if (Accelerometer.current != null)
        {
            InputSystem.EnableDevice(Accelerometer.current);
            m_lowPassFilterValue = Accelerometer.current.acceleration.ReadValue();
            m_lowPassFilterFactor = 1f / (m_accelerometerRefreshRate * m_lowPassKernelWidth);
            m_isReady = true;
        }
        else
        {
            Debug.LogWarning("Accelerometer missing. If available, Unity Remote will be used instead.");
            m_isReady = false;
        }

        m_valuesY = new float[m_numberOfFrames];
        m_currentIndex = 0;
    }

    private void Update()
    {
    #if UNITY_EDITOR
        if (!m_isReady && UnityEditor.EditorApplication.isRemoteConnected)
        {
            m_lowPassFilterFactor = 1f / (m_accelerometerRefreshRate * m_lowPassKernelWidth);
            m_lowPassFilterValue = Vector3.zero;
            m_isReady = true;
        }
    #endif

        if (!m_isReady)
        {
            return;
        }

        // Read accelerometer value and apply a low pass filter
        Vector3 accValue = Accelerometer.current.acceleration.ReadValue();
        accValue = Vector3.Lerp(m_lowPassFilterValue, accValue, m_lowPassFilterFactor);

        // Remap coordinates
        Vector3 remappedValue;
        remappedValue.x = accValue.x;
        remappedValue.y = -accValue.z;
        remappedValue.z = accValue.y;
        remappedValue.Normalize();

        // Pre-multiply with user-defined input scale
        m_value = remappedValue * m_inputMultiplier;

        // The Y component seems to be more unstable, so we
        // perform an additional sliding average on it
        if (m_valuesY.Length != m_numberOfFrames)
        {
            System.Array.Resize(ref m_valuesY, m_numberOfFrames);
            m_currentIndex = 0;
        }

        // Replace current value
        m_sumY -= m_valuesY[m_currentIndex];
        m_valuesY[m_currentIndex] = m_value.y;
        m_sumY += m_valuesY[m_currentIndex];

        // Move to the next position in the circular buffer
        m_currentIndex = (m_currentIndex + 1) % m_numberOfFrames;

        // Calculate the average
        m_value.y = m_sumY / m_numberOfFrames;

        // Update change rate
        m_changeRate = (m_value - m_previousValue) / Time.deltaTime;

        // Save inputs for last for next frame
        m_previousValue = m_value;
    }


    // _____________________________________________________
    // Public getters
    
    public bool IsReady()
    {
        return m_isReady;
    }

    public float GetX()
    {
        return m_value.x;
    }

    public float GetY()
    {
        return m_value.y;
    }

    public float GetZ()
    {
        return m_value.z;
    }

    public float GetDeltaX()
    {
        return m_changeRate.x;
    }

    public float GetDeltaY()
    {
        return m_changeRate.y;
    }

    public float GetDeltaZ()
    {
        return m_changeRate.z;
    }
}
