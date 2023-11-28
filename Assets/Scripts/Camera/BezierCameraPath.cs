using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BezierCameraPath : MonoBehaviour
{
    [SerializeField]
    [Range(0.0001f, 1f)]
    private float debugCurveResolution = 0.01f;

    void OnDrawGizmos()
    {
        if (transform.childCount < 2)
        {
            return;
        }

        float t = 0f;
        Vector3 previousPoint = Interpolate(t);

        // Draw bezier curve
        Gizmos.color = Color.blue;

        while (t <= 1f)
        {
            t += GetStepSize(t, debugCurveResolution);
            Vector3 point = Interpolate(t);

            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        // Draw control points
        Gizmos.color = Color.green;

        for (int i = 0; i < transform.childCount - 1; i++)
        {
            Gizmos.DrawLine(transform.GetChild(i).position, transform.GetChild(i+1).position);
        }
    }

    public Vector3 Interpolate(float t)
    {
        int numPoints = transform.childCount;
        if (numPoints < 2)
        {
            Debug.LogError("Bezier curve requires at least two control points.");
            return Vector3.zero;
        }

        float u = 1 - t;
        float[] basis = new float[numPoints];

        // Calculate basis functions
        for (int i = 0; i < numPoints; i++)
        {
            basis[i] = BinomialCoefficient(numPoints - 1, i) * Mathf.Pow(u, numPoints - 1 - i) * Mathf.Pow(t, i);
        }

        // Calculate the final point on the curve
        Vector3 position = Vector3.zero;

        for (int i = 0; i < numPoints; i++)
        {
            position += basis[i] * transform.GetChild(i).position;
        }

        return position;
    }

    // Binomial coefficient function
    int BinomialCoefficient(int n, int k)
    {
        if (k < 0 || k > n)
        {
            return 0;
        }

        int result = 1;

        for (int i = 1; i <= k; ++i)
        {
            result *= n--;
            result /= i;
        }

        return result;
    }

    // Get dynamic step size based on the curve resolution
    float GetStepSize(float t, float resolution)
    {
        float curveLength = Vector3.Distance(Interpolate(t), Interpolate(t + 0.001f)); // Small step for distance calculation
        return resolution / curveLength;
    }
}
