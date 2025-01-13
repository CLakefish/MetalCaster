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

        if (Physics.Raycast(bullet.position - bullet.direction.normalized, bullet.direction.normalized, out RaycastHit hit, 3.0f, hittableLayers))
        {
            AddTurret(hit.point, hit.normal, ref bullet);
            return;
        }

        AddTurret(bullet.position, bullet.normal, ref bullet);
    }

    private void AddTurret(Vector3 pos, Vector3 normal, ref Bullet bullet)
    {
        if (bullet.hitObjects == null) bullet.hitObjects = new();

        Placeable placeable = Instantiate(placeablePrefab, pos, Quaternion.identity);
        Vector3   bound     = placeable.GetComponent<MeshRenderer>().bounds.size;
        placeable.transform.up        = normal;
        placeable.transform.position += normal * (bound.y * 0.5f);
        placeable.ProvideBullet(ref bullet, Player);
    }
}
