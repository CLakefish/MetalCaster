using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Fire/Chain Lightning")]
public class ChainLightning : Modification.Queue
{
    [Header("Parameters")]
    [SerializeField] private int ricochetAddition;
    [SerializeField] private float colliderRadius;
    [SerializeField] private LayerMask collidableLayer;


    [Header("Display")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float destroyTime;

    public override void Modify(Weapon context) {
        context.WeaponData.ricochetCount += ricochetAddition;
        activeBullets = 0;
    }

    public override void OnFirstHit(ref RaycastHit hit, ref Bullet bullet)
    {
        bullet.hitObjects = new() { hit.collider };

        if (!hit.collider.TryGetComponent(out Health _)) return;

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

            Vector3 pos          = bullet.hitObjects.Count == 0 ? bullet.position : bullet.hitObjects[^1].transform.position;
            Collider[] colliders = Physics.OverlapSphere(pos, colliderRadius, collidableLayer);

            Collider closest = null;
            float dist = Mathf.Infinity;

            foreach (var collider in colliders)
            {
                if (bullet.hitObjects.Contains(collider)) continue;

                float newDist = Vector3.Distance(collider.transform.position, pos);
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

            InstantiateLine(pos, closest.gameObject.transform.position);

            bullet.normal = closest.transform.forward;

            bullet.ricochetCount--;
            bullet.hitObjects.Add(closest);

            if (closest.TryGetComponent(out Health hp)) hp.Damage(bullet.damage);

            --activeBullets;
            Player.PlayerWeapon.BulletManager.QueueBullet(ref bullet);
        }
    }

    private void InstantiateLine(Vector3 p0, Vector3 p1)
    {
        LineRenderer line = Instantiate(lineRenderer, Vector3.zero, Quaternion.identity);
        line.SetPosition(0, p0);
        line.SetPosition(1, p1);
        Destroy(line.gameObject, destroyTime);
    }
}
