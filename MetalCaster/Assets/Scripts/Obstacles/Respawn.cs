using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Respawn))]
public class RespawnEditor : Editor
{
    private void OnSceneGUI()
    {
        Respawn respawn = (Respawn)target;

        respawn.spawnPos = Handles.DoPositionHandle(respawn.spawnPos, Quaternion.identity);
        Handles.Label(respawn.spawnPos, "Spawn Position");
    }
}

#endif

public class Respawn : MonoBehaviour
{
    [SerializeField] public Vector3 spawnPos;

    private void OnTriggerEnter(Collider other)
    {
        other.transform.position = spawnPos;
        if (other.attachedRigidbody != null) other.attachedRigidbody.velocity = new Vector3(other.attachedRigidbody.velocity.x, 0, other.attachedRigidbody.velocity.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(spawnPos, 1f);
    }
}
