using UnityEngine;
using UnityEngine.InputSystem;

public class EnableAccelerometer : MonoBehaviour
{
    private void Start()
    {
        InputSystem.EnableDevice(Accelerometer.current);
    }
}
