using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingEffect : MonoBehaviour
{
    public float bounceHeight = 0.5f; // Hauteur du rebond
    public float bounceSpeed = 4.0f; // Vitesse du rebond

    private Vector3 originalPosition;

    private void Start()
    {
        originalPosition = transform.position;
    }

    private void Update()
    {
        // Calcul du mouvement vertical utilisant une fonction sinusoidale pour créer un effet de rebond
        float yOffset = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
        transform.position = originalPosition + new Vector3(0f, yOffset, 0f);
    }
}
