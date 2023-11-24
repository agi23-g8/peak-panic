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
            triggerRenderer.enabled = true; // Verify that the renderer is enabled
    }

    private void OnTriggerEnter(Collider other)
    {
        if (canBoost && Time.time >= cooldownEndTime && other.CompareTag("Player"))
        {
            StartCoroutine(ActivateBoost(other.GetComponent<Rigidbody>()));
            canBoost = false;
            cooldownEndTime = Time.time + cooldownDuration;
            if (triggerRenderer)
                triggerRenderer.enabled = false;
        }
    }

    IEnumerator ActivateBoost(Rigidbody rb)
    {
        rb.AddForce(transform.forward * boostForce, ForceMode.VelocityChange);
        yield return new WaitForSeconds(boostDuration);
        // The boost reappears after the cooldown duration
        canBoost = true;
        if (triggerRenderer)
            triggerRenderer.enabled = true; 
    }
}

