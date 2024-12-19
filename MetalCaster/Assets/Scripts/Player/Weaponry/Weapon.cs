using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Modifications (Debugging)")]
    [SerializeField] public List<WeaponModification> modifications;
    [SerializeField] public List<WeaponModification> permanentModifications;

    [Header("Data")]
    [SerializeField] private PlayerWeaponData baseData;

    public PlayerWeaponData WeaponData { get; set; }
    public PlayerWeapon PlayerWeapon   { get; private set; }
    public Vector3 PrevHit             { get; private set; }

    private Coroutine reloadCoroutine = null;
    private bool reloading            = false;


    public void OnEnable() => ReloadModifications();

    public void Equip(PlayerWeapon context) 
    {
        PlayerWeapon = context;
        Debug.Log("Equipped");
    }

    public void UnEquip() 
    {
        Debug.Log("Unequipped");
    }

    public void ReloadModifications()
    {
        WeaponData = ScriptableObject.CreateInstance<PlayerWeaponData>();
        WeaponData.Set(baseData);

        WeaponData.shotCount = WeaponData.magazineSize;

        foreach (var mod in permanentModifications) mod.Modify(this);
        foreach (var mod in modifications)          mod.Modify(this);
    }

    public void Fire(PlayerWeapon context)
    {
        if (Time.time < WeaponData.fireTime + WeaponData.prevFireTime || WeaponData.shotCount <= 0) return;

        if (reloading)
        {
            if (WeaponData.shotCount <= 0) return;
            else StopReload();
        }

        PrevHit = context.GetCamera().CameraTransform.position;
        WeaponData.prevFireTime = Time.time;

        context.GetCamera().Recoil(WeaponData.recoil);
        context.GetCamera().ViewmodelRecoil();

        for (int i = 0; i < WeaponData.bulletsPerShot; ++i)
        {
            WeaponData.shotCount--;

            if (WeaponData.shotCount < 0) break;

            FireImmediate(PrevHit, context.GetCamera().CameraForward, WeaponData.bounceCount);
        }
    }

    public void FireImmediate(Vector3 pos, Vector3 dir, int bounceCount)
    {
        if (bounceCount < 0) return;
        bounceCount--;

        foreach (var mod in permanentModifications) mod.OnFire(this);
        foreach (var mod in modifications)          mod.OnFire(this);

        if (WeaponData.type == PlayerWeaponData.ProjectileType.Raycast)
        {
            if (Physics.Raycast(pos, dir + (Random.insideUnitSphere * WeaponData.shotDeviation), out RaycastHit hit, Mathf.Infinity, PlayerWeapon.CollisionLayers))
            {
                if (hit.collider.TryGetComponent<Health>(out Health hp)) hp.Damage(WeaponData.damage);

                foreach (var mod in permanentModifications) mod.OnHit(this, hit, bounceCount);
                foreach (var mod in modifications)          mod.OnHit(this, hit, bounceCount);

                PrevHit = hit.point;
            }
            else
            {
                foreach (var mod in permanentModifications) mod.OnMiss(this);
                foreach (var mod in modifications)          mod.OnMiss(this);
            }
        }
    }

    public void UpdateWeapon()
    {
        foreach (var mod in permanentModifications) mod.OnUpdate(this);
        foreach (var mod in modifications)          mod.OnUpdate(this);

        if (WeaponData.shotCount <= 0) Reload();
    }

    private void Reload()
    {
        if (reloading) return;

        reloading = true;
        StopReload();
        reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    private void StopReload()
    {
        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
    }

    private IEnumerator ReloadCoroutine()
    {
        foreach (var mod in permanentModifications) mod.OnReload(this);
        foreach (var mod in modifications)          mod.OnReload(this);

        yield return new WaitForSeconds(WeaponData.reloadTime);

        WeaponData.shotCount = WeaponData.magazineSize;

        reloading = false;
    }
}
