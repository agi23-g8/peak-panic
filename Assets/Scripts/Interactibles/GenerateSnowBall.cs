using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateSnowballs : MonoBehaviour
{
    public GameObject snowballPrefab; // Reference of the prefab to generate
    public float generationInterval = 2f; // Time between each generation
    public float spawnAreaRadius = 20f; // Radius of the area where the snowballs will be generated

    private float timer = 0f;

    void Update()
    {
        // Incremente the timer
        timer += Time.deltaTime;

        // If the timer is greater than the generation interval
        if (timer >= generationInterval)
        {
            GenerateSnowball();
            timer = 0f;
        }
    }

    void GenerateSnowball()
    {
        // Generate a random position inside a sphere of radius spawnAreaRadius
        Vector3 randomPosition = Random.insideUnitSphere * spawnAreaRadius;
        // Adjust the position to be relative to the position of the camera
        randomPosition.x = transform.position.x + randomPosition.x;
        randomPosition.z = transform.position.z + randomPosition.z;
        randomPosition.y = transform.position.y + 20f; // Adjust the height of the snowball
        
        // Instanciate the prefab at the random position
        GameObject newSnowball = Instantiate(snowballPrefab, randomPosition, Quaternion.identity);
    }
}

