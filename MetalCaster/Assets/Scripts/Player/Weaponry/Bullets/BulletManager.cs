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

    public List<Collider> hitObjects;

    public Bullet() { }

    public Bullet(Bullet other)
    {
        position      = other.position;
        direction     = other.direction;
        normal        = other.normal;
        modifications = other.modifications;
        damage        = other.damage;
        ricochetCount = other.ricochetCount;
        hitObjects    = other.hitObjects;
    }
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

    public void QueueBullet(ref Bullet bullet) {
        bulletQueue.Enqueue(bullet);
    }

    public void OnUpdate() {
        int queueTotal = bulletQueue.Count;

        for (int i = 0; i < queueTotal; i++)
        {
            Bullet bullet = bulletQueue.Dequeue();
            PlayerWeapon.ModificationManager.ApplyAllModifications(ref bullet);
        }
    }
}
