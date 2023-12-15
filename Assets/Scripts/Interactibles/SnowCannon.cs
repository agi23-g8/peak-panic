using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowCanon : MonoBehaviour
{
    public GameObject snowballPrefab; // Référence vers le prefab de la boule de neige
    public float launchInterval = 3f; // Intervalle de lancement en secondes
    public float minInitialSpeed = 20f; // Vitesse minimale initiale
    public float maxInitialSpeed = 30f; // Vitesse maximale initiale

    private float timer = 0f;

    void Update()
    {
        // Incrémenter le timer
        timer += Time.deltaTime;

        // Vérifier si le temps de lancement est atteint
        if (timer >= launchInterval)
        {
            // Lancer une boule de neige
            LaunchSnowball();
            
            // Réinitialiser le timer
            timer = 0f;
        }
    }

    void LaunchSnowball()
    {
        // Générer une vitesse aléatoire pour la boule de neige
        float initialSpeed = Random.Range(minInitialSpeed, maxInitialSpeed);

        // Instancier une nouvelle boule de neige au niveau du canon
        GameObject newSnowball = Instantiate(snowballPrefab, transform.position, Quaternion.identity);

        // Récupérer le composant Rigidbody de la boule de neige
        Rigidbody snowballRigidbody = newSnowball.GetComponent<Rigidbody>();

        if (snowballRigidbody != null)
        {
            // Appliquer une vélocité à la boule de neige dans la direction du canon
            snowballRigidbody.velocity = transform.up * initialSpeed;
        }
        else
        {
            Debug.LogError("Rigidbody component not found on snowball prefab!");
        }
    }
}

