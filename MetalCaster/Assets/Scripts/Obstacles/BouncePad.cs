using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [SerializeField] private Vector3 force;

    private void OnTriggerEnter(Collider other) => Launch(other);
    private void OnTriggerStay(Collider other)  => Launch(other);

    private void Launch(Collider other)
    {
        if (other.attachedRigidbody == null) return;

        Vector3 vel = other.attachedRigidbody.linearVelocity + force;
        vel.y = force.y;

        other.attachedRigidbody.linearVelocity = vel;

        if (other.CompareTag("Player"))
        {
            Player.Instance.PlayerMovement.Launch();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, force);
    }
}
