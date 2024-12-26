using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Advertisement")]
public class Advertisement : WeaponModification
{
    [SerializeField] private Material advertisementMaterial;

    public override void OnHit(RaycastHit hit, ref Weapon.FireData payload)
    {
        if (hit.collider.TryGetComponent<MeshRenderer>(out MeshRenderer mesh)) mesh.material = advertisementMaterial;
    }
}
