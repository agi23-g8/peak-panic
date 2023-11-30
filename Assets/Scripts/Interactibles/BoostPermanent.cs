using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostPermanent : MonoBehaviour
{
    public float boostForce = 20f;
    public float boostDuration = 3f;

    private void OnTriggerEnter(Collider _other)
    {
        if (_other.CompareTag("Player"))
        {
            Rigidbody rigidBody = _other.GetComponent<Rigidbody>();
            StartCoroutine(ActivateBoost(rigidBody));
        }
    }

    IEnumerator ActivateBoost(Rigidbody _rigidBody)
    {
        // Permanent boost overrides the player's direction
        _rigidBody.AddForce(transform.forward * boostForce, ForceMode.VelocityChange);

        // We wait for the boost duration to end
        yield return new WaitForSeconds(boostDuration);
    }
}

