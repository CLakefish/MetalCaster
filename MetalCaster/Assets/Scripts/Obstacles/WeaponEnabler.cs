using UnityEngine;

public class WeaponEnabler : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other.transform.parent.TryGetComponent<PlayerWeapon>(out PlayerWeapon p)) p.enabled = true;
        Destroy(gameObject);
    }
}
