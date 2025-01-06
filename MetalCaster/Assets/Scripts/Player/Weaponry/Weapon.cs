using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public PlayerWeaponData BaseData => baseData;
    public PlayerWeaponData AlteredData { get; set; }

    public Player Player                { get; private set; }

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

    public void AddModification(Modification data, int index) {
        if (index < 0 || index >= BaseData.modificationSlots) return;

        modifications[index] = data;
        ReloadData();

        GameDataManager.Instance.ActiveSave.SaveWeapon(this);

        AlteredData.shotCount = AlteredData.magazineSize;
    }

    public void RemoveModification(Modification data) {
        if (!modifications.Contains(data)) return;

        modifications[modifications.IndexOf(data)] = null;
        ReloadData();

        GameDataManager.Instance.ActiveSave.SaveWeapon(this);

        AlteredData.shotCount = AlteredData.magazineSize;
    }

    private void ReloadData()
    {
        if (AlteredData != null) Destroy(AlteredData);

        AlteredData = ScriptableObject.CreateInstance<PlayerWeaponData>();
        AlteredData.Set(baseData);

        mods.Clear();
        mods.AddRange(modifications.Where(x => x != null));
        mods.AddRange(permanentModifications);

        foreach (var mod in mods) mod.Modify(this);

        Player.PlayerWeapon.ModificationManager.AddModifications(mods);
    }

    public void Fire()
    {
        if (Time.time < AlteredData.fireTime + AlteredData.prevFireTime || AlteredData.shotCount <= 0) return;

        if (reloading) {
            if (AlteredData.shotCount <= 0) return;
            else StopCoroutine(reloadCoroutine);
        }

        AlteredData.prevFireTime = Time.time;

        OnFire?.Invoke();

        Player.PlayerCamera.Recoil(AlteredData.recoil);
        Player.PlayerCamera.ViewmodelRecoil();

        for (int i = 0; i < AlteredData.bulletsPerShot; ++i)
        {
            AlteredData.shotCount--;

            if (AlteredData.shotCount < 0)
            {
                Reload();
                return;
            }

            Vector3 pos = Player.PlayerCamera.CameraTransform.position;
            Vector3 dir = Player.PlayerCamera.CameraForward.normalized;

            Bullet bullet    = Player.PlayerWeapon.BulletManager.AddBullet(AlteredData, mods);
            bullet.position  = pos;
            bullet.direction = dir;

            switch (AlteredData.type) {
                case PlayerWeaponData.ProjectileType.Raycast:

                    if (Physics.Raycast(pos, dir, out RaycastHit hit, Mathf.Infinity, Player.hittableLayer)) {
                        if (hit.collider.TryGetComponent(out Health hp)) hp.Damage(AlteredData.damage);

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
        if (AlteredData.shotCount <= 0) Reload();
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

        yield return new WaitForSeconds(AlteredData.reloadTime);

        AlteredData.shotCount = AlteredData.magazineSize;
        reloading            = false;
        OnReloadEnd?.Invoke();
    }
}
