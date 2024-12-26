using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Advertisement")]
public class Advertisement : WeaponModification
{
    [SerializeField] private Material advertisementMaterial;

    public override void OnHit(Weapon context, RaycastHit hit, ref WeaponModificationData payload)
    {
        if (hit.collider.TryGetComponent<MeshRenderer>(out MeshRenderer mesh)) mesh.material = advertisementMaterial;
    }
}
