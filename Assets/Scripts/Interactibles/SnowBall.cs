using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowBall : MonoBehaviour
{
    public float maximalSize = 10f; // Maximal size of the snowball
    public float initialsize; // Initial size of the snowball

    public float growingFactor = 0.001f; // Growing factor which depends on the speed

    private bool onSnow = false;

    public float minimalSpeed = 1;
    private Rigidbody rb;

    public float slowForce = 30f;
    public float slowDuration = 3f;

    private Renderer triggerRenderer;
    private float timer = 0f;

    public float lifeTime = 20f;

    private void Start()
    { 
        initialsize = transform.localScale.x;
        rb = gameObject.GetComponent<Rigidbody>();

        triggerRenderer = GetComponent<Renderer>();
        if (triggerRenderer)
        {
            triggerRenderer.enabled = true;
        }

    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            destroyBall();
        }

        if (onSnow)
        {
            RaycastHit hit;
            Vector3 rayDir = Vector3.down;
            Vector3 currentNormal = transform.up;
            if (Physics.Raycast(transform.position, rayDir, out hit))
            {
                currentNormal = hit.normal;
            }

            float speed = rb.velocity.magnitude;

            // Now change the size of the ball based on the velocity only if the speed is above a certain threshold
            if (speed > minimalSpeed & transform.localScale.x < maximalSize)
            {   
                // Change the size of the ball
                float growingFactorSize = growingFactor * speed;
                transform.localScale += new Vector3(growingFactorSize, growingFactorSize, growingFactorSize);

                // Now compensate for the fact that the ball is growing in size by moving it back towards the center of the cube
                Vector3 compensation = currentNormal * growingFactorSize;
                transform.position += compensation;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        onSnow = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        onSnow = false;
    }

    private void OnTriggerEnter(Collider _other)
    {
        if (_other.CompareTag("Player"))
        {
            Transform playerTransform = _other.GetComponent<Transform>();
            Rigidbody playerRigidBody = _other.GetComponent<Rigidbody>();
            ActivateSlow(playerTransform, playerRigidBody);

            destroyBall();
        }
    }

    private void destroyBall()
    {
        Destroy(gameObject);
    }

    private void ActivateSlow(Transform _transform, Rigidbody _rigidBody)
    {
        float speedFactor = (transform.localScale.x - initialsize) / (maximalSize - initialsize); // Speed factor which depends on the size of the ball
        // Temporary slow doesn't alter the player's direction, only its speed
        _rigidBody.AddForce(- _transform.forward * slowForce * speedFactor, ForceMode.VelocityChange);
    }
}