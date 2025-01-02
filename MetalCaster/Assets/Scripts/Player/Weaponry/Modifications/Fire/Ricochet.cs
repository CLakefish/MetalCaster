using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Fire/Ricochet")]
public class Ricochet : Modification.Queue
{
    [Header("Parameters")]
    [SerializeField] private int ricochetAddition;
    [SerializeField] private LayerMask ricochetLayers;

    public override void Modify(Weapon context) {
        context.WeaponData.ricochetCount += ricochetAddition;
        activeBullets = 0;
    }

    public override void OnHit(ref RaycastHit hit, ref Bullet bullet)
    {
        bullet.direction = (hit.point - bullet.position).normalized;
        bullet.position  = hit.point;
        bullet.normal    = hit.normal;

        ProvideBullet(ref bullet);
    }

    public override void OnUpdate()
    {
        while (bulletQueue.Count > 0)
        {
            var bullet = Next();

            if (bullet.ricochetCount <= 0) {
                activeBullets--;
                continue;
            }

            Vector3 reflected = Vector3.Reflect(bullet.direction, bullet.normal);

            if (Physics.Raycast(bullet.position, reflected, out RaycastHit hit, Mathf.Infinity, ricochetLayers))
            {
                if (hit.collider.TryGetComponent(out Health hp)) hp.Damage(bullet.damage);

                bullet.ricochetCount--;

                activeBullets--;

                Player.PlayerWeapon.BulletManager.QueueBullet((hit.point - bullet.position).normalized, hit.point, ref bullet);
                continue;
            }

            activeBullets--;
        }
    }
}
