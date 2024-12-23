using System.Collections;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Weapons/Weapon Data")]
public class PlayerWeaponData : ScriptableObject
{
    public enum ProjectileType
    {
        Raycast,
        GameObject,
        Other
    }

    [Header("Weapon Data")]
    [SerializeField] public int damage;
    [SerializeField] public int magazineSize;
    [SerializeField] public float shotDeviation;
    [SerializeField] public int bulletsPerShot = 1;
    [SerializeField] public ProjectileType type;

    [Header("Modifications")]
    [SerializeField] public int modificationSlots;

    [Header("Timings")]
    [SerializeField] public float fireTime;
    [SerializeField] public float reloadTime;

    [Header("Recoil")]
    [SerializeField] public Vector3 recoil;

    [Header("Debugging")]
    [HideInInspector] public int shotCount = 0;
    [HideInInspector] public int ricochetCount = 0;
    [HideInInspector] public float prevFireTime = 0;

    public void Set(PlayerWeaponData other)
    {
        if (other == this || other == null) return;

        // Could use reflection, although this is faster. If you really wanted to, use typeof().GetFields() and iterate :)

        this.damage            = other.damage;
        this.magazineSize      = other.magazineSize;
        this.shotCount         = other.shotCount;
        this.fireTime          = other.fireTime;
        this.reloadTime        = other.reloadTime;
        this.type              = other.type;
        this.bulletsPerShot    = other.bulletsPerShot;
        this.recoil            = other.recoil;
        this.ricochetCount     = other.ricochetCount;
        this.shotDeviation     = other.shotDeviation;
        this.modificationSlots = other.modificationSlots;
    }
}
