using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainbowSphere : MonoBehaviour
{
    public float colorChangeSpeed = 1.0f; // Vitesse de changement de couleur

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
        // Vérifie si le matériau de la sphère est bien configuré
        if (material != null)
        {
            // Incrémente la valeur de teinte (Hue) pour changer la couleur
            hueValue = (hueValue + colorChangeSpeed * Time.deltaTime) % 1.0f;

            // Applique la nouvelle couleur avec une valeur de teinte différente
            material.color = Color.HSVToRGB(hueValue, 1f, 1f);
        }
    }
}
