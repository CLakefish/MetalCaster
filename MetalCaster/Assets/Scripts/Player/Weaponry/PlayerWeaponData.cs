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

    [Header("Unlocked")]
    [SerializeField] private bool unlocked;

    [Header("Weapon Data")]
    [SerializeField] public int damage;
    [SerializeField] public int magazineSize;
    [SerializeField] public int bulletsPerShot = 1;
    [SerializeField] public int bounceCount    = 0;
    [SerializeField] public ProjectileType type;

    [Header("Timings")]
    [SerializeField] public float fireTime;
    [SerializeField] public float reloadTime;

    [Header("Recoil")]
    [SerializeField] public Vector3 recoil;

    [Header("Debugging")]
    [HideInInspector] public int shotCount = 0;
    [HideInInspector] public float prevFireTime = 0;
    [HideInInspector] public float currentBounce = 0;

    public bool Unlocked => unlocked;

    public void Set(PlayerWeaponData other)
    {
        if (other == this) return;

        this.damage         = other.damage;
        this.magazineSize   = other.magazineSize;
        this.shotCount      = other.shotCount;
        this.fireTime       = other.fireTime;
        this.reloadTime     = other.reloadTime;
        this.type           = other.type;
        this.bulletsPerShot = other.bulletsPerShot;
        this.recoil         = other.recoil;
        this.bounceCount    = other.bounceCount;
    }
}
