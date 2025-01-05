using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Fire/Ricochet")]
public class Ricochet : Modification.Queue
{
    [Header("Parameters")]
    [SerializeField] private int ricochetAddition;
    [SerializeField] private LayerMask ricochetLayers;

    [Header("Display")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float destroyTime;

    public override void Modify(Weapon context) {
        context.WeaponData.ricochetCount += ricochetAddition;
        activeBullets = 0;
    }

    public override void OnFirstHit(ref RaycastHit hit, ref Bullet bullet)
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

            if (bullet.ricochetCount <= 0 || bullet.hitObjects.Count > 0) {
                activeBullets--;
                continue;
            }

            Vector3 reflected = Vector3.Reflect(bullet.direction, bullet.normal);

            if (Physics.Raycast(bullet.position, reflected, out RaycastHit hit, Mathf.Infinity, ricochetLayers))
            {
                InstantiateLine(bullet.position, hit.point);
                bullet.normal   = hit.normal;
                bullet.position = hit.point;
                bullet.ricochetCount--;

                if (hit.collider.TryGetComponent(out Health hp))
                {
                    if (!bullet.hitObjects.Contains(hit.collider)) bullet.hitObjects.Add(hit.collider);

                    hp.Damage(bullet.damage);
                }

                activeBullets--;

                Player.PlayerWeapon.BulletManager.QueueBullet(ref bullet);
                continue;
            }

            activeBullets--;
            if (reflected != Vector3.zero) InstantiateLine(bullet.position, bullet.position + (reflected * 1000));
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
