using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowCameraController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The target transform that the camera will follow.")]
    private Transform m_target;

    private Camera m_snowCamera;
    private Vector3 m_previousCamOrigin;
    private Vector3 m_currentCamOrigin;

    void Start()
    {
        // Get the Camera component from the GameObject
        m_snowCamera = GetComponent<Camera>();
        Debug.Assert(m_snowCamera != null, "SnowCameraController script must be attached to a GameObject with a Camera component.");
    }

    private void LateUpdate()
    {
        m_previousCamOrigin = transform.position;
        Shader.SetGlobalVector("_PrevSnowDeformationOrigin", m_previousCamOrigin);

        if (m_target != null)
        {
            // update camera position
            transform.position = m_target.position;
        }

        m_currentCamOrigin = transform.position;
        Shader.SetGlobalVector("_CurSnowDeformationOrigin", m_currentCamOrigin);

        float deformationAreaMeters = 2 * m_snowCamera.orthographicSize;
        Shader.SetGlobalFloat("_SnowDeformationAreaMeters", deformationAreaMeters);

        Vector3 originOffset = (m_currentCamOrigin - m_previousCamOrigin) / deformationAreaMeters;
        Shader.SetGlobalVector("_SnowDeformationOriginOffset", originOffset);
    }
}