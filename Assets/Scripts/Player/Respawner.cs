using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawner : MonoBehaviour
{

    [SerializeField]
    private GameObject m_objectToRespawn;


    private Transform m_objectTransform;
    private Rigidbody m_objectRigidBody;


    private Vector3 m_initialPos;
    private Quaternion m_initialRot;
    private Vector3 m_initialVel;

    void Start()
    {
        m_objectTransform = m_objectToRespawn.GetComponent<Transform>();
        m_objectRigidBody = m_objectToRespawn.GetComponent<Rigidbody>();

        // save initial state
        m_initialPos = m_objectTransform.position;
        m_initialRot = m_objectTransform.rotation;
        m_initialVel = m_objectRigidBody.velocity;
    }

    void Update()
    {
        const float kFallHeight = 20f;
        float heightLimit = transform.position.y - kFallHeight;

        // Respawn when object is falling 
        if (m_objectTransform.position.y < heightLimit)
        {
            Respawn();
        }
    }

    private void OnTriggerEnter(Collider _other)
    {
        // Respawn when object enters in the trigger volume
        if (_other.gameObject == m_objectToRespawn)
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        m_objectTransform.position = m_initialPos;
        m_objectTransform.rotation = m_initialRot;
        m_objectRigidBody.velocity = m_initialVel;
    }
}
