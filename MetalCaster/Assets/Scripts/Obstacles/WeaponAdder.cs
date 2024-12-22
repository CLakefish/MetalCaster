using UnityEngine;

public class WeaponAdder : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private bool remove;

    private void OnTriggerStay(Collider other)
    {
        if (!other.transform.parent.TryGetComponent<PlayerWeapon>(out PlayerWeapon p)) return;

        if (remove) {
            Debug.Log("Removed: " + p.RemoveWeapon(weapon));
        }
        else {
            p.AddWeapon(weapon, true);
        }

        //Destroy(gameObject);
    }
}
