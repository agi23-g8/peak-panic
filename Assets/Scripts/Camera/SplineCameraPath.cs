using UnityEngine;

public class SplineCameraPath : MonoBehaviour
{
    [SerializeField]
    [Range(0.0001f, 0.01f)]
    private float debugSplineStep = 0.001f;

    private Transform[] controlPoints;

    void Start()
    {
        FetchControlPoints();
    }

    void OnDrawGizmos()
    {
        FetchControlPoints();
        Gizmos.color = Color.green;

        float t = 0f;
        Quaternion unused;

        Vector3 previousPoint;
        Interpolate(t, out previousPoint, out unused);

        while (t <= 1f)
        {
            t += debugSplineStep;

            Vector3 point;
            Interpolate(t, out point, out unused);

            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
    }

    void FetchControlPoints()
    {
        controlPoints = new Transform[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            controlPoints[i] = transform.GetChild(i);
        }
    }

    public void Interpolate(float t, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (controlPoints == null || controlPoints.Length < 4)
        {
            Debug.LogError("Catmull-Rom spline requires at least 4 control points.");
            return;
        }

        float numSections = controlPoints.Length - 3;
        int currentIndex = Mathf.Min(Mathf.FloorToInt(t * numSections), controlPoints.Length - 4);

        float u = t * numSections - currentIndex;

        Vector3 a = controlPoints[currentIndex].position;
        Vector3 b = controlPoints[currentIndex + 1].position;
        Vector3 c = controlPoints[currentIndex + 2].position;
        Vector3 d = controlPoints[currentIndex + 3].position;

        position = 0.5f * (
            (-a + 3f * b - 3f * c + d) * (u * u * u)
            + (2f * a - 5f * b + 4f * c - d) * (u * u)
            + (-a + c) * u
            + 2f * b
        );

        // Interpolate rotation using Slerp
        Quaternion q0 = controlPoints[currentIndex].rotation;
        Quaternion q1 = controlPoints[currentIndex + 1].rotation;
        Quaternion q2 = controlPoints[currentIndex + 2].rotation;
        Quaternion q3 = controlPoints[currentIndex + 3].rotation;

        rotation = Quaternion.Slerp(Quaternion.Slerp(q0, q1, 0.5f), Quaternion.Slerp(q2, q3, 0.5f), u);
    }
}
