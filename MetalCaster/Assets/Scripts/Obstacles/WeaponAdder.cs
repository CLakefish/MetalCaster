using UnityEngine;

public class WeaponAdder : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private bool remove;

    private void OnTriggerStay(Collider other)
    {
        if (remove) {
            Debug.Log("Removed: " + Player.Instance.PlayerWeapon.RemoveWeapon(weapon));
        }
        else {
            Player.Instance.PlayerWeapon.AddWeapon(weapon, true);
        }

        //Destroy(gameObject);
    }
}
