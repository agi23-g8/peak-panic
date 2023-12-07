using System.Collections;
using TMPro;
using UnityEngine;

public class ColorLerp : MonoBehaviour
{
    public float duration = 1.0f;
    public float baseAlpha = 0.5f;

    public TextMeshProUGUI text;

    private void Update()
    {
        float t = 0.5f * Mathf.Sin(2 * (Time.time / duration)) + 0.5f;
        float a = Mathf.Min(1, t + baseAlpha);

        text.color = new Color(text.color.r, text.color.g, text.color.b, a);
    }
}