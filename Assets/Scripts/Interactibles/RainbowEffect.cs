using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainbowEffect : MonoBehaviour
{
    public float colorChangeSpeed = 1.0f;

    private Material material;
    private float hueValue = 0f;

    private void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            material = renderer.material;
        }
    }

    private void Update()
    {
        if (material != null)
        {
            hueValue = (hueValue + colorChangeSpeed * Time.deltaTime) % 1.0f;
            material.color = Color.HSVToRGB(hueValue, 1f, 1f);
        }
    }
}
