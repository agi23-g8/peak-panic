using UnityEngine;
using Unity.Collections;
using UnityEngine.InputSystem;

// References :
//   - https://docs.unity3d.com/2022.3/Documentation/Manual/MobileInput.html
//   - https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/api/UnityEngine.InputSystem.Accelerometer.html

public class MobileAccelerometer : MonoBehaviour
{
    [ReadOnly]
    [Tooltip("Current state of the phone's accelerometer.")]
    public bool m_isReady = false;

    [ReadOnly]
    [Tooltip("Current value retrieved from the phone's accelerometer and pre-processed.")]
    public Vector3 m_value = Vector3.zero;

    [SerializeField]
    [Tooltip("Refresh rate of the accelerometer, expressed in hertz.")]
    private float m_accelerometerRefreshRate = 100f;

    [SerializeField]
    [Tooltip("Width of the low pass kernel used to filter the raw input sample, expressed in seconds.")]
    private float m_lowPassKernelWidth = 1f;

    [SerializeField]
    [Tooltip("Multiplier pre-applied to the phone inputs")]
    private float m_inputMultiplier = 25f;

    // internal variables
    private float m_lowPassFilterFactor;
    private Vector3 m_lowPassFilterValue;


    // ---- Script lifecycle -----------------------------------------------------

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

        // Read accelerometer value
        Vector3 accValue = Accelerometer.current.acceleration.ReadValue();

        // Apply low pass filter
        accValue = Vector3.Lerp(m_lowPassFilterValue, accValue, m_lowPassFilterFactor);

        // Remap coordinates
        m_value.x = accValue.x;
        m_value.y = -accValue.z;
        m_value.z = accValue.y;

        m_value.Normalize();
        m_value *= m_inputMultiplier * Time.deltaTime;
    }


    // ---- Public Getters -------------------------------------------------------
    
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
}
