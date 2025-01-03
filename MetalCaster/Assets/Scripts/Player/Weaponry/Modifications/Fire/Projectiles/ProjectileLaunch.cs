using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Fire/Projectile Launch")]
public class ProjectileLaunch : Modification
{
    [Header("Item")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float force;

    public override void Modify(Weapon context)
    {
        context.WeaponData.type = PlayerWeaponData.ProjectileType.GameObject;
    }

    public override void OnFirstShot(Vector3 pos, Vector3 dir, ref Bullet bullet) {
        bullet.hitObjects = new();

        Vector3 muzzlePos = Player.PlayerWeapon.Selected.Weapon.MuzzlePos.position;
        Projectile proj   = Instantiate(projectilePrefab, muzzlePos, Quaternion.identity);

        proj.Launch(dir.normalized * force, ref bullet, Player);
    }
}
