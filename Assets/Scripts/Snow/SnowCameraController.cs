using UnityEngine;

public class SnowCameraController : MonoBehaviour
{
    // _____________________________________________________________________
    // Exposed properties
    [SerializeField]
    [Range(10f, 200f)]
    [Tooltip("The horizontal size, in meters, of the deformation volume captured by the camera.")]
    private float m_deformationArea = 100f;

    [SerializeField]
    [Range(10f, 200f)]
    [Tooltip("The vertical size, in meters, of the deformation volume captured by the camera")]
    private float m_deformationHeight = 50f;


    // _____________________________________________________________________
    // Internal members
    private Camera m_snowCamera;


    // _____________________________________________________________________
    // Component lifecycle
    void Start()
    {
        // Get Camera component from the GameObject
        m_snowCamera = GetComponent<Camera>();

        Debug.Assert(m_snowCamera != null,
            "SnowCameraController script must be attached to a GameObject with a Camera component.");
    }

    private void Update()
    {
        // Override camera projection settings
        m_snowCamera.orthographic = true;
        m_snowCamera.nearClipPlane = -0.5f * m_deformationHeight;
        m_snowCamera.farClipPlane = 0.5f * m_deformationHeight;
        m_snowCamera.orthographicSize = 0.5f * m_deformationArea;

        // Set '_PrevSnowDeformationOrigin' shader variable
        Vector3 previousCamOrigin = transform.position;
        Shader.SetGlobalVector("_PrevSnowDeformationOrigin", previousCamOrigin);

        // Update the camera position based on the main camera view state.
        transform.position = CameraManager.Instance.GetViewCenteredPositionOnPath();

        // Set '_CurSnowDeformationOrigin' shader variable
        Vector3 currentCamOrigin = transform.position;
        Shader.SetGlobalVector("_CurSnowDeformationOrigin", currentCamOrigin);

        // Set '_SnowDeformationAreaMeters' shader variable
        Shader.SetGlobalFloat("_SnowDeformationAreaMeters", m_deformationArea);

        // Set '_SnowDeformationOriginOffset' shader variable
        Vector3 originOffset = (currentCamOrigin - previousCamOrigin) / m_deformationArea;
        Shader.SetGlobalVector("_SnowDeformationOriginOffset", originOffset);
    }

}