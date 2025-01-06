using UnityEngine;
using UnityEngine.Events;

public class Projectile : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private UnityAction OnHit;
    [SerializeField] private LayerMask hittableLayer;

    protected Bullet bullet;
    protected Player Player;
    protected virtual void OnTrigger() { }

    public void Launch(Vector3 force, ref Bullet bullet, Player player) {
        rb.AddForce(force, ForceMode.VelocityChange);
        this.bullet = bullet;
        this.Player = player;
    }

    private void OnTriggerEnter(Collider other) {
        if ((hittableLayer & (1 << other.gameObject.layer)) == 0) return;

        if (other.transform.TryGetComponent(out Health hp)) {
            hp.Damage(bullet.damage);
            bullet.hitObjects.Add(other);
        }

        if (Physics.SphereCast(rb.transform.position, 0.5f, (other.transform.position - rb.transform.position).normalized, out RaycastHit hit, 1, hittableLayer)) {
            bullet.normal = hit.normal;
        }
        else {
            bullet.normal = Vector3.up;
        }

        bullet.direction = rb.linearVelocity;
        bullet.position  = rb.transform.position;

        foreach (var modification in Player.PlayerWeapon.ModificationManager.ActiveModifications) {
            modification.OnFirstProjectileHit(other, ref bullet);
        }

        OnHit?.Invoke();
        Player.PlayerWeapon.BulletManager.QueueBullet(ref bullet);
        OnTrigger();
    }
}
