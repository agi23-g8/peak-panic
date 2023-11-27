using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostPermanent : MonoBehaviour
{
    public float boostForce = 20f;
    public float boostDuration = 3f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(ActivateBoost(other.GetComponent<Rigidbody>()));
        }
    }

    IEnumerator ActivateBoost(Rigidbody rb)
    {
        rb.AddForce(transform.forward * boostForce, ForceMode.VelocityChange);
        yield return new WaitForSeconds(boostDuration);
        // We wait for the boost duration to end
    }
}

