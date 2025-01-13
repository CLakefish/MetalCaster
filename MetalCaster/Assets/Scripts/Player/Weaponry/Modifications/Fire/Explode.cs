using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Fire/Explode")]
public class Explode : Modification
{
    [Header("Collisions")]
    [SerializeField] private LayerMask explodableLayers;
    [SerializeField] private ParticleSystem explosion;
    [SerializeField] private float radius;
    [SerializeField] private float explosiveForce;
    [SerializeField] private int damage;

    public override void OnFirstHit(ref RaycastHit hit, ref Bullet bullet)
    {
        CheckExplosion(hit.point);
        base.OnFirstHit(ref hit, ref bullet);
    }

    public override void ProvideBullet(ref Bullet bullet)
    {
        CheckExplosion(bullet.position);
        base.ProvideBullet(ref bullet);
    }

    private void CheckExplosion(Vector3 pos)
    {
        Instantiate(explosion, pos, Quaternion.identity);

        RaycastHit[] colliders = Physics.SphereCastAll(pos, radius, Vector3.up, radius, explodableLayers);

        for (int i = 0; i < colliders.Length; i++)
        {
            var col = colliders[i];

            if (col.transform.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                Vector3 dir = (col.transform.position - pos).normalized;
                rb.linearVelocity += dir * explosiveForce;

                if (col.collider.CompareTag("Player")) {
                    Player.Instance.PlayerMovement.Launch();
                }
            }

            if (col.transform.TryGetComponent(out Health health)) health.Damage(damage);
        }
    }
}
