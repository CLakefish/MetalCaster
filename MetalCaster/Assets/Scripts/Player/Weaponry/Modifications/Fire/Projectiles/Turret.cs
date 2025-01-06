using UnityEngine;

public class Turret : Placeable
{
    private void LateUpdate()
    {
        /*
        Collider[] colliders = Physics.OverlapSphere(transform.position, 10, player.enemyLayer);
        Collider closest     = null;
        float dist           = Mathf.Infinity;

        foreach (var collider in colliders)
        {
            float checkDist = Vector3.Distance(collider.transform.position, transform.position);
            if (checkDist < dist)
            {
                closest = collider;
                dist    = checkDist;
            }
        }

        if (closest == null) return;

        // ADD IN RAYCAST SO MODS WORK TOO!

        Bullet newBullet        = new(bullet);
        newBullet.position      = transform.position;
        newBullet.direction     = (closest.transform.position - transform.position).normalized;

        Debug.DrawRay(transform.position, newBullet.direction * 100, Color.red, 10);

        transform.LookAt(closest.transform.position, transform.up);
        player.PlayerWeapon.BulletManager.QueueBullet(ref newBullet);*/
    }
}
