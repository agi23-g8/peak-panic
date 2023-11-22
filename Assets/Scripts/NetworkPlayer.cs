using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayer : NetworkBehaviour
{
    public NetworkVariable<Vector3> accelerometer = new NetworkVariable<Vector3>(
        Vector3.zero,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<string> playerName = new NetworkVariable<string>(
        string.Empty,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner
    );

    private Vector3 prevAccelerometerInput;

    [Header("Low Pass filter Settings")]

    [Tooltip("How many times per second to update the accelerometer.")]
    public float accelerometerUpdateFrequency = 100.0f;

    [Tooltip("The greater the value of LowPassKernelWidthInSeconds, the slower the filtered value will converge towards current input sample (and vice versa).")]
    public float lowPassKernelWidthInSeconds = 1.0f;

    private float lowPassFilterFactor;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Logger.Instance.LogInfo("I am the owner");

            accelerometer.Value = Vector3.zero;

            if (Accelerometer.current == null)
            {
                Logger.Instance.LogInfo("Accelerometer not found, make sure you are runnig on a mobile device");
            }
            else
            {
                SetupAccelerometer();
            }

        }

        if (IsServer)
        {
            Logger.Instance.LogInfo("I am the server");
            accelerometer.OnValueChanged += OnAccelerometerChanged;
        }
    }

    public override void OnDestroy()
    {
        if (IsServer)
        {
            accelerometer.OnValueChanged -= OnAccelerometerChanged;
        }
    }

    public void OnAccelerometerChanged(Vector3 prevValue, Vector3 newValue)
    {
        Logger.Instance.LogInfo("Accelerometer changed from " + prevValue + " to " + newValue);
    }

    void SetupAccelerometer()
    {
        InputSystem.EnableDevice(Accelerometer.current);
        prevAccelerometerInput = Accelerometer.current.acceleration.ReadValue();
        lowPassFilterFactor = accelerometerUpdateFrequency * lowPassKernelWidthInSeconds;
    }

    void Update()
    {
        if (IsOwner)
        {
            Vector3 accelerometerInput = Accelerometer.current.acceleration.ReadValue();
            accelerometerInput = LowPassFilterAccelerometer(prevAccelerometerInput, accelerometerInput);
            accelerometer.Value = accelerometerInput;
            prevAccelerometerInput = accelerometerInput;
        }
    }

    Vector3 LowPassFilterAccelerometer(Vector3 prevValue, Vector3 newValue)
    {
        return Vector3.Lerp(prevValue, newValue, lowPassFilterFactor);
    }

    public void SetPlayerName(string name)
    {
        playerName.Value = name;
    }

}
