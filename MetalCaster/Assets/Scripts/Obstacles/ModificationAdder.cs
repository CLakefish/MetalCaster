using UnityEngine;

public class ModificationAdder : MonoBehaviour
{
    [SerializeField] private WeaponModification modification;

    private void OnTriggerStay(Collider other)
    {
        if (!other.transform.parent.TryGetComponent<PlayerWeapon>(out PlayerWeapon p)) return;

        GameDataManager.Instance.ActiveSave.UnlockModification(modification);
    }
}
