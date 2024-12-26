using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotPayload {
    public int ricochetTotal;
    public bool firstShot;
    public HashSet<GameObject> hashSet;
    public Vector3 prevHitPoint;
}

[System.Serializable]
public class Weapon : MonoBehaviour
{
    private class FireData
    {
        public Vector3 pos;
        public Vector3 dir;
        public WeaponModificationData payload;

        public System.Func<Vector3, Vector3, WeaponModificationData, WeaponModificationData> action;
    }

    [Header("Modifications (Debugging)")]
    [SerializeField] public List<WeaponModification> modifications;
    [SerializeField] public List<WeaponModification> permanentModifications;

    [Header("Data")]
    [SerializeField] private PlayerWeaponData baseData;
    [SerializeField] private string weaponName;

    [Header("Viewmodel Shenanigans")]
    [SerializeField] private Transform muzzlePosition;
    [SerializeField] private Transform modificationPos;
    [SerializeField] private Transform menuPos;
    [SerializeField] private Transform menuHolder;

    public PlayerWeaponData WeaponData { get; set; }
    public PlayerWeapon PlayerWeapon { get; private set; }

    public Transform MuzzlePos       => muzzlePosition;
    public Transform MenuPos         => menuPos;
    public Transform ModificationPos => modificationPos;
    public Transform MenuHolder      => menuHolder;

    public string WeaponName => weaponName;

    private readonly Queue<FireData> bullets = new();
    private Coroutine reloadCoroutine        = null;
    private bool reloading                   = false;

    public void OnEnable() => ReloadModifications();

    private void Update()
    {
        while (bullets.Count > 0)
        {
            var data = bullets.Dequeue();
            data.payload = data.action.Invoke(data.pos, data.dir, data.payload);
        }
    }

    public void Equip(PlayerWeapon context) 
    {
        PlayerWeapon = context;
    }

    public void UnEquip() {
        bullets.Clear();
        Destroy(gameObject);
    }

    public void AddModification(WeaponModification data)
    {
        if (modifications.Count >= WeaponData.modificationSlots) return;

        modifications.Add(data);
        ReloadModifications();

        WeaponData.shotCount = WeaponData.magazineSize;
    }

    public void RemoveModification(WeaponModification data)
    {
        if (!modifications.Contains(data)) return;

        modifications.Remove(data);
        ReloadModifications();

        WeaponData.shotCount = WeaponData.magazineSize;
    }

    private void ReloadModifications()
    {
        if (WeaponData != null) Destroy(WeaponData);

        WeaponData = ScriptableObject.CreateInstance<PlayerWeaponData>();
        WeaponData.Set(baseData);

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

        WeaponData.prevFireTime = Time.time;

        context.GetCamera().Recoil(WeaponData.recoil);
        context.GetCamera().ViewmodelRecoil();

        for (int i = 0; i < WeaponData.bulletsPerShot; ++i)
        {
            WeaponData.shotCount--;

            if (WeaponData.shotCount < 0) break;

            WeaponModificationData payload = new();
            payload.Set(WeaponData.ricochetCount, "ricochet");
            payload.Set(true, "firstShot");

            FireImmediate(context.GetCamera().CameraTransform.position, context.GetCamera().CameraForward, ref payload);
        }

        PlayerWeapon.Fire?.Invoke();
    }

    public void AltFire(PlayerWeapon context)
    {
        Vector3 pos = context.GetCamera().CameraTransform.position;
        Vector3 dir = context.GetCamera().CameraForward;

        foreach (var mod in permanentModifications) mod.AltFire(this, dir);
        foreach (var mod in modifications)          mod.AltFire(this, dir);
    }

    public void FireImmediate(Vector3 position, Vector3 direction, ref WeaponModificationData payload)
    {
        FireData data = new()
        {
            pos     = position,
            dir     = direction,
            payload = payload,
            action  = (Vector3 actPos, Vector3 actDir, WeaponModificationData actPayload) =>
            {
                foreach (var mod in permanentModifications) mod.OnFire(this);
                foreach (var mod in modifications)          mod.OnFire(this);

                if (WeaponData.type != PlayerWeaponData.ProjectileType.Raycast)
                {
                    actPayload.Set(false, "firstShot");
                    return actPayload;
                }

                Vector3 deviatedDir = actDir + (Random.insideUnitSphere * WeaponData.shotDeviation);

                if (Physics.Raycast(actPos, deviatedDir, out RaycastHit hit, Mathf.Infinity, PlayerWeapon.CollisionLayers))
                {
                    if (hit.collider.TryGetComponent(out Health hp))
                    {
                        if (actPayload.Contains("hashSet"))
                        {
                            HashSet<Health> h = actPayload.Get<HashSet<Health>>("hashSet");
                            if (!h.Contains(hp)) hp.Damage(WeaponData.damage);
                        }
                        else
                        {
                            hp.Damage(WeaponData.damage);
                        }
                    }

                    foreach (var mod in permanentModifications) mod.OnHit(this, hit, ref actPayload);
                    foreach (var mod in modifications)          mod.OnHit(this, hit, ref actPayload);
                }
                else
                {
                    foreach (var mod in permanentModifications) mod.OnMiss(this, deviatedDir, ref actPayload);
                    foreach (var mod in modifications)          mod.OnMiss(this, deviatedDir, ref actPayload);
                }

                actPayload.Set(false, "firstShot");
                return actPayload;
            }
        };

        bullets.Enqueue(data);
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

        PlayerWeapon.ReloadStart?.Invoke();

        yield return new WaitForSeconds(WeaponData.reloadTime);

        WeaponData.shotCount = WeaponData.magazineSize;

        reloading = false;
        PlayerWeapon.ReloadFinished?.Invoke();
    }
}
