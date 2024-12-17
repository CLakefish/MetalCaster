using UnityEngine;

public class WinObject : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        Respawn spawn = FindFirstObjectByType<Respawn>();
        spawn.Spawn(other);
    }
}
