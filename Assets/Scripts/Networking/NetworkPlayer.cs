using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayer : NetworkBehaviour
{
    [DllImport("__Internal")]
    private static extern void Vibrate(int ms);

    public NetworkVariable<Vector3> accelerometer = new NetworkVariable<Vector3>(
        Vector3.zero,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        string.Empty,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<Color> skinColor = new NetworkVariable<Color>(
        Color.black,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    private Vector3 prevAccelerometerInput;

    [Header("Low Pass filter Settings")]

    [Tooltip("How many times per second to update the accelerometer.")]
    public float accelerometerUpdateFrequency = 100.0f;

    [Tooltip("The greater the value of LowPassKernelWidthInSeconds, the slower the filtered value will converge towards current input sample (and vice versa).")]
    public float lowPassKernelWidthInSeconds = 1.0f;

    private float lowPassFilterFactor;

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            Logger.Instance.LogInfo("NetworkPlayer despawned on client");
            Debug.Log("NetworkPlayer despawned on client");

            // this is only called on the webgl client from the NetworkedPlayer
            // notify ui manager that we have despawned
            ClientUIManager.Instance.OnNetworkDespawn();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            Logger.Instance.LogInfo("NetworkPlayer spawned on client");
        }


        if (IsOwner)
        {
            Vibrate(1000);

            accelerometer.Value = Vector3.zero;

            playerName.Value = ClientUIManager.Instance.nameInputField.text;
            skinColor.OnValueChanged += (prevValue, newValue) =>
            {
                ClientUIManager.Instance.backgroundColor.color = newValue;
            };

            if (Accelerometer.current == null)
            {
                Logger.Instance.LogInfo("Accelerometer not found, make sure you are runnig on a mobile device");
            }
            else
            {
                SetupAccelerometer();
            }

        }

        // if (IsServer)
        // {
        //     accelerometer.OnValueChanged += OnAccelerometerChanged;
        // }
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

    [ClientRpc]
    public void VibrateClientRpc(int ms, ClientRpcParams clientRpcParams = default)
    {
        if (IsClient)
        {
            Logger.Instance.LogInfo("Vibrating for " + ms + "ms");
            Vibrate(ms);
        }
    }


    Vector3 LowPassFilterAccelerometer(Vector3 prevValue, Vector3 newValue)
    {
        return Vector3.Lerp(prevValue, newValue, lowPassFilterFactor);
    }

    // public void SetPlayerName(string name)
    // {
    //     playerName.Value = name;
    // }

    public float GetX()
    {
        return accelerometer.Value.x;
    }

    public float GetY()
    {
        return accelerometer.Value.y;
    }

    public float GetZ()
    {
        return accelerometer.Value.z;
    }

    public string GetPlayerName()
    {
        return playerName.Value.ToString();
    }

}
