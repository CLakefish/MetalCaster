using UnityEngine;

public class WeaponEnabler : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private bool remove;

    private void OnTriggerStay(Collider other)
    {
        if (!other.transform.parent.TryGetComponent<PlayerWeapon>(out PlayerWeapon p)) return;

        if (remove) {
            p.RemoveWeapon(weapon);
        }
        else {
            p.AddWeapon(weapon, true);
        }

        Destroy(gameObject);
    }
}
