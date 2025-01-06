using System.Collections.Generic;
using UnityEngine;

public class Modification : ScriptableObject
{
    public class AlwaysEmpty : Modification {
        public override bool IsEmpty() {
            return true;
        }
    }

    public class Queue : Modification
    {
        protected readonly Queue<Bullet> bulletQueue = new();

        protected Bullet Next() {
            return bulletQueue.Dequeue();
        }

        public override void ProvideBullet(ref Bullet bullet) {
            bulletQueue.Enqueue(bullet);
            activeBullets++;
        }
    }

    [Header("Data and Display")]
    [SerializeField] private string modificationName;
    [SerializeField] private string description;
    [SerializeField] private Sprite modificationSprite;

    public string ModificationName   => modificationName;
    public string Description        => description;
    public Sprite ModificationSprite => modificationSprite;

    protected Player Player    { get; private set; }
    public bool MarkedToRemove { get; set; }
    protected int activeBullets = 0;

    public virtual bool IsEmpty() {
        return activeBullets <= 0;
    }

    public void SetPlayer(Player player) {
        this.Player = player;
    }

    public virtual void ProvideBullet(ref Bullet bullet) { }
    public virtual void Modify(Weapon context)           { }

    public virtual void OnFirstShot(Vector3 pos, Vector3 dir, ref Bullet bullet)          { }
    public virtual void OnFirstProjectileHit(Collider collider, ref Bullet bullet) { }


    public virtual void OnFirstHit(ref RaycastHit hit, ref Bullet bullet)        { }
    public virtual void OnFirstMiss(Vector3 pos, Vector3 dir)                    { }

    public virtual void OnReload() { }
    public virtual void OnUpdate() { }
    public virtual void AltFire() { }
}
