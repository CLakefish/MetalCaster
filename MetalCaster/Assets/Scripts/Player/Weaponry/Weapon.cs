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
    public PlayerWeaponData weaponData;

    private Coroutine reloadCoroutine;
    private bool reloading = false;

    public PlayerWeapon context;
    public Vector3 PrevHit { get; private set; }

    public void OnEnable() => ReloadModifications();

    public void Equip() {
        Debug.Log("Equipped");
    }

    public void UnEquip() {
        Debug.Log("Unequipped");
    }

    public void ReloadModifications()
    {
        weaponData = ScriptableObject.CreateInstance<PlayerWeaponData>();
        weaponData.Set(baseData);

        weaponData.shotCount = weaponData.magazineSize;

        foreach (var mod in permanentModifications) mod.Modify(this);
        foreach (var mod in modifications)          mod.Modify(this);
    }

    public void Fire(PlayerWeapon context)
    {
        this.context = context;

        if (Time.time < weaponData.fireTime + weaponData.prevFireTime) return;

        if (reloading)
        {
            if (weaponData.shotCount <= 0) return;
            else StopReload();
        }

        PrevHit = context.GetCamera().CameraTransform.position;
        weaponData.prevFireTime = Time.time;

        context.GetCamera().Recoil(weaponData.recoil);
        context.GetCamera().ViewmodelRecoil();

        for (int i = 0; i < weaponData.bulletsPerShot; ++i)
        {
            weaponData.shotCount--;

            if (weaponData.shotCount <= 0) break;

            FireImmediate(PrevHit, context.GetCamera().CameraForward, weaponData.bounceCount);
        }
    }

    public void FireImmediate(Vector3 pos, Vector3 dir, int bounceCount)
    {
        if (bounceCount < 0) return;
        bounceCount--;

        foreach (var mod in permanentModifications) mod.OnFire(this);
        foreach (var mod in modifications)          mod.OnFire(this);

        if (weaponData.type == PlayerWeaponData.ProjectileType.Raycast)
        {
            if (Physics.Raycast(pos, dir + (Random.insideUnitSphere * weaponData.shotDeviation), out RaycastHit hit, Mathf.Infinity, context.CollisionLayers))
            {
                foreach (var mod in permanentModifications) mod.OnHit(this, hit, bounceCount);
                foreach (var mod in modifications)          mod.OnHit(this, hit, bounceCount);

                PrevHit = hit.point;
            }
        }
    }

    public void UpdateWeapon()
    {
        foreach (var mod in permanentModifications) mod.OnUpdate(this);
        foreach (var mod in modifications)          mod.OnUpdate(this);

        if (weaponData.shotCount <= 0) Reload();
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

        yield return new WaitForSeconds(weaponData.reloadTime);

        weaponData.shotCount = weaponData.magazineSize;

        reloading = false;
    }
}
