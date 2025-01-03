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

        bullet.position  = rb.position;
        bullet.normal    = Vector3.up;
        bullet.direction = rb.linearVelocity.normalized;

        OnHit?.Invoke();
        Player.PlayerWeapon.BulletManager.QueueBullet(ref bullet);
        OnTrigger();
    }
}
