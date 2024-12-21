using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Burst")]
public class BigBurst : WeaponModification
{
    [SerializeField] private float reloadIncrease;

    public override void Modify(Weapon context)
    {
        context.WeaponData.bulletsPerShot = context.WeaponData.magazineSize;
        context.WeaponData.reloadTime    *= reloadIncrease;
    }
}
