using UnityEngine;


[CreateAssetMenu(menuName = "Weapons/Modifications/Fire/All Bullets")]
public class AllBullets : Modification.AlwaysEmpty
{
    public override void Modify(Weapon context) {
        context.WeaponData.bulletsPerShot = context.WeaponData.magazineSize;
    }
}
