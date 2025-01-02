using System.Collections.Generic;
using UnityEngine;

public class Bullet
{
    public Vector3 position;
    public Vector3 direction;
    public Vector3 normal;

    public HashSet<string> modifications;

    public int damage;
    public int ricochetCount;

    public HashSet<Collider> hitObjects;
}

public class BulletManager : PlayerWeapon.PlayerWeaponSystem
{
    private readonly Queue<Bullet> bulletQueue = new();

    public Bullet AddBullet(PlayerWeaponData weaponData, List<Modification> modifications) {
        HashSet<string> modIDs = new();

        foreach (var mod in modifications) modIDs.Add(mod.ModificationName);

        Bullet bullet = new() {
            damage        = weaponData.damage,
            ricochetCount = weaponData.ricochetCount,
            modifications = modIDs,
        };

        return bullet;
    }

    public void QueueBullet(Vector3 pos, Vector3 dir, ref Bullet bullet) {
        bullet.direction = dir;
        bullet.position  = pos;
        bulletQueue.Enqueue(bullet);
    }

    public void OnUpdate() {
        HashSet<Bullet> bullets = new();

        int queueTotal = bulletQueue.Count;

        for (int i = 0; i < queueTotal; i++)
        {
            Bullet bullet = bulletQueue.Dequeue();
            if (bullets.Contains(bullet)) continue;
            bullets.Add(bullet);
            PlayerWeapon.ModificationManager.ApplyAllModifications(ref bullet);
        }
    }
}
