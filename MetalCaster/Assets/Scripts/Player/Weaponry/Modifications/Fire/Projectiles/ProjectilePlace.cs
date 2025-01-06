using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Fire/Projectile Place")]
public class ProjectilePlace : Modification.Queue
{
    [Header("Item")]
    [SerializeField] private Placeable placeablePrefab;
    [SerializeField] private LayerMask hittableLayers;

    public override void OnFirstHit(ref RaycastHit hit, ref Bullet bullet)
    {
        if (hittableLayers != (hittableLayers | (1 << hit.collider.gameObject.layer))) return;

        AddTurret(hit.point, hit.normal, ref bullet);
    }

    public override void OnFirstProjectileHit(Collider collider, ref Bullet bullet)
    {
        if (hittableLayers != (hittableLayers | (1 << collider.gameObject.layer))) return;

        AddTurret(bullet.position, bullet.normal, ref bullet);
    }

    private void AddTurret(Vector3 pos, Vector3 normal, ref Bullet bullet)
    {
        if (bullet.hitObjects == null) bullet.hitObjects = new();

        Placeable placeable    = Instantiate(placeablePrefab, pos, Quaternion.identity);
        placeable.transform.up = normal;
        placeable.ProvideBullet(ref bullet, Player);
    }
}
