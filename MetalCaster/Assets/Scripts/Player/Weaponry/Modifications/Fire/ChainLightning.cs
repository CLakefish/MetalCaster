using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Fire/Chain Lightning")]
public class ChainLightning : Modification.Queue
{
    [Header("Parameters")]
    [SerializeField] private int ricochetAddition;
    [SerializeField] private float colliderRadius;
    [SerializeField] private LayerMask collidableLayer;

    public override void Modify(Weapon context) {
        context.WeaponData.ricochetCount += ricochetAddition;
        activeBullets = 0;
    }

    public override void OnHit(ref RaycastHit hit, ref Bullet bullet)
    {
        bullet.hitObjects = new() { hit.collider };
        if (!hit.collider.TryGetComponent(out Health _)) return;
        ProvideBullet(ref bullet);
    }

    public override void OnUpdate()
    {
        Debug.Log(activeBullets);

        while (bulletQueue.Count > 0)
        {
            var bullet = Next();

            if (bullet.ricochetCount <= 0) {
                activeBullets--;
                continue;
            }

            Collider[] colliders = Physics.OverlapSphere(bullet.position, colliderRadius, collidableLayer);

            Collider closest = null;
            float dist = Mathf.Infinity;

            foreach (var collider in colliders)
            {
                if (bullet.hitObjects.Contains(collider)) continue;

                float newDist = Vector3.Distance(collider.transform.position, bullet.position);
                if (newDist < dist)
                {
                    closest = collider;
                    dist = newDist;
                }
            }

            if (closest == null) {
                activeBullets--;
                continue;
            }

            bullet.hitObjects.Add(closest);

            if (closest.TryGetComponent(out Health hp)) hp.Damage(bullet.damage);

            bullet.ricochetCount--;

            --activeBullets;
            Player.PlayerWeapon.BulletManager.QueueBullet(closest.transform.position, Vector3.zero, ref bullet);
        }
    }
}
