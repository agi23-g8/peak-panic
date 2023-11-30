using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostTemporary : MonoBehaviour
{
    public float boostForce = 20f;
    public float boostDuration = 3f;
    public float cooldownDuration = 1f;

    private bool canBoost = true;
    private float cooldownEndTime = 0f;
    private Renderer triggerRenderer;

    private void Start()
    {
        triggerRenderer = GetComponent<Renderer>();

        if (triggerRenderer)
        {
            // All temp boosts are visible at the beginning
            triggerRenderer.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider _other)
    {
        if (canBoost && _other.CompareTag("Player") && Time.time >= cooldownEndTime)
        {
            Transform playerTransform = _other.GetComponent<Transform>();
            Rigidbody playerRigidBody = _other.GetComponent<Rigidbody>();
            StartCoroutine(ActivateBoost(playerTransform, playerRigidBody));

            cooldownEndTime = Time.time + cooldownDuration;
            canBoost = false;

            if (triggerRenderer)
            {
                // The boost disappears after it is used
                triggerRenderer.enabled = false;
            }
        }
    }

    IEnumerator ActivateBoost(Transform _transform, Rigidbody _rigidBody)
    {
        // Temporary boost doesn't alter the player's direction, only its speed
        _rigidBody.AddForce(_transform.forward * boostForce, ForceMode.VelocityChange);

        // We wait for the boost duration to end
        yield return new WaitForSeconds(boostDuration);
        canBoost = true;

        if (triggerRenderer)
        {
            // The boost reappears after the cooldown duration
            triggerRenderer.enabled = true; 
        }
    }
}

