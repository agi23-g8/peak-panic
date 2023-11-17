using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowCameraController : MonoBehaviour
{
    [SerializeField]
    Transform cameraTarget;

    private Camera snowCamera;
    private Vector3 previousCamOrigin;
    private Vector3 currentCamOrigin;

    void Start()
    {
        // Get the Camera component from the GameObject
        snowCamera = GetComponent<Camera>();
        Debug.Assert(snowCamera != null, "SnowCameraController script must be attached to a GameObject with a Camera component.");
    }

    private void LateUpdate()
    {
        previousCamOrigin = transform.position;
        Shader.SetGlobalVector("_PrevSnowDeformationOrigin", previousCamOrigin);

        if (cameraTarget != null)
        {
            // update camera position
            transform.position = new Vector3(cameraTarget.position.x, 0f, cameraTarget.position.z);
        }

        currentCamOrigin = transform.position;
        Shader.SetGlobalVector("_CurSnowDeformationOrigin", currentCamOrigin);

        float deformationAreaMeters = 2 * snowCamera.orthographicSize;
        Shader.SetGlobalFloat("_SnowDeformationAreaMeters", deformationAreaMeters);

        Vector3 originOffset = (currentCamOrigin - previousCamOrigin) / deformationAreaMeters;
        Shader.SetGlobalVector("_SnowDeformationOriginOffset", originOffset);
    }

}