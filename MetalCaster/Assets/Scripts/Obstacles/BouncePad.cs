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

        Vector3 vel = other.attachedRigidbody.linearVelocity;
        if (vel.x < force.x) vel.x = force.x;
        if (vel.z < force.z) vel.z = force.z;
        vel.y = force.y;

        other.attachedRigidbody.linearVelocity = vel;

        PlayerController controller = other.gameObject.GetComponentInParent<PlayerController>();
        if (controller != null) controller.Launch();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, force);
    }
}
