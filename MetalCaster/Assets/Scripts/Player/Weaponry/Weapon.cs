using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon : MonoBehaviour
{
    [Header("Modifications (Debugging)")]
    [SerializeField] public List<Modification> modifications;
    [SerializeField] public List<Modification> permanentModifications;

    [Header("Data")]
    [SerializeField] private PlayerWeaponData baseData;
    [SerializeField] private string weaponName;

    [Header("Viewmodel Shenanigans")]
    [SerializeField] private Transform muzzlePosition;
    [SerializeField] private Transform modificationPos;
    [SerializeField] private Transform menuPos;
    [SerializeField] private Transform menuHolder;

    public PlayerWeaponData WeaponData { get; set; }
    public Player Player               { get; private set; }

    public Transform MuzzlePos       => muzzlePosition;
    public Transform MenuPos         => menuPos;
    public Transform ModificationPos => modificationPos;
    public Transform MenuHolder      => menuHolder;
    public string WeaponName         => weaponName;

    public event System.Action OnFire;
    public event System.Action OnAltFire;
    public event System.Action OnReloadStart;
    public event System.Action OnReloadEnd;

    private readonly List<Modification> mods = new();
    private Coroutine reloadCoroutine = null;
    private bool reloading  = false;

    public void Equip(Player player) { 
        Player = player;
        ReloadData();
    }

    public void UnEquip() {
        GameDataManager.Instance.ActiveSave.SaveWeapon(this);
        Destroy(gameObject);
    }

    public void AddModification(Modification data) {
        if (modifications.Count >= WeaponData.modificationSlots) return;

        modifications.Add(data);
        ReloadData();

        GameDataManager.Instance.ActiveSave.SaveWeapon(this);

        WeaponData.shotCount = WeaponData.magazineSize;
    }

    public void RemoveModification(Modification data) {
        if (!modifications.Contains(data)) return;

        modifications.Remove(data);
        ReloadData();

        GameDataManager.Instance.ActiveSave.SaveWeapon(this);

        WeaponData.shotCount = WeaponData.magazineSize;
    }

    private void ReloadData()
    {
        if (WeaponData != null) Destroy(WeaponData);

        WeaponData = ScriptableObject.CreateInstance<PlayerWeaponData>();
        WeaponData.Set(baseData);

        mods.Clear();
        mods.AddRange(modifications);
        mods.AddRange(permanentModifications);

        foreach (var mod in mods) mod.Modify(this);

        Player.PlayerWeapon.ModificationManager.AddModifications(mods);
    }

    public void Fire()
    {
        if (Time.time < WeaponData.fireTime + WeaponData.prevFireTime || WeaponData.shotCount <= 0) return;

        if (reloading) {
            if (WeaponData.shotCount <= 0) return;
            else StopCoroutine(reloadCoroutine);
        }

        WeaponData.prevFireTime = Time.time;

        OnFire?.Invoke();

        Player.PlayerCamera.Recoil(WeaponData.recoil);
        Player.PlayerCamera.ViewmodelRecoil();

        for (int i = 0; i < WeaponData.bulletsPerShot; ++i)
        {
            WeaponData.shotCount--;

            if (WeaponData.shotCount < 0)
            {
                Reload();
                return;
            }

            Vector3 pos = Player.PlayerCamera.CameraTransform.position;
            Vector3 dir = Player.PlayerCamera.CameraForward.normalized;

            Bullet bullet    = Player.PlayerWeapon.BulletManager.AddBullet(WeaponData, mods);
            bullet.position  = pos;
            bullet.direction = dir;

            switch (WeaponData.type) {
                case PlayerWeaponData.ProjectileType.Raycast:

                    if (Physics.Raycast(pos, dir, out RaycastHit hit, Mathf.Infinity, Player.hittableLayer)) {
                        if (hit.collider.TryGetComponent(out Health hp)) hp.Damage(WeaponData.damage);

                        foreach (var mod in mods) mod.OnFirstHit(ref hit, ref bullet);
                    }
                    else {
                        foreach (var mod in mods) mod.OnFirstMiss(pos, dir);
                    }

                    break;

                case PlayerWeaponData.ProjectileType.GameObject:
                    foreach (var mod in mods) mod.OnFirstShot(pos, dir, ref bullet);
                    break;
            }
        }
    }

    public void AltFire()
    {
        OnAltFire?.Invoke();

        foreach (var mod in mods) mod.AltFire();
    }

    public void CheckReload() {
        if (WeaponData.shotCount <= 0) Reload();
    }

    private void Reload() {
        if (reloading) return;

        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);

        reloading       = true;
        reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        OnReloadStart?.Invoke();

        foreach (var mod in mods) mod.OnReload();

        yield return new WaitForSeconds(WeaponData.reloadTime);

        WeaponData.shotCount = WeaponData.magazineSize;
        reloading            = false;
        OnReloadEnd?.Invoke();
    }
}
