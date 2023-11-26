using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICameraPath
{
    void Interpolate(float _t, out Vector3 _position, out Quaternion _rotation);
}
