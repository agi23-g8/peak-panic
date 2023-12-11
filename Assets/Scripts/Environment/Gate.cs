
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Gate : MonoBehaviour
{

    private List<GameObject> pillars;

    [SerializeField]
    private float target_height = -46.0f;
    [SerializeField]
    private float speed = 0.1f;
    [SerializeField]
    private float shakeAmount = 0.1f;
    [SerializeField]
    private float shakeSpeed = 0.1f;

    void Start()
    {
        pillars = new List<GameObject>();
        foreach (Transform child in transform)
        {
            pillars.Add(child.gameObject);
        }
    }

    public void LowerGate()
    {
        foreach (GameObject pillar in pillars)
        {
            StartCoroutine(LowerGateCoroutine(pillar));
        }
    }

    IEnumerator LowerGateCoroutine(GameObject pillar)
    {
        while (pillar.transform.localPosition.y > target_height)
        {
            Debug.Log("Lowering gate");

            Vector3 lower = Vector3.down * speed;
            pillar.transform.position += lower;
            yield return new WaitForFixedUpdate();
            Vector3 shake = Vector3.zero;
            shake.x = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            shake.y = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            shake.z = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            pillar.transform.position += shake;
            yield return new WaitForFixedUpdate();
            pillar.transform.position -= shake;
            yield return new WaitForFixedUpdate();
        }
    }
}