using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon : MonoBehaviour
{
    public class FireData
    {
        public Vector3 position;
        public Vector3 direction;

        public PlayerWeaponData weaponData;
        public Weapon context;

        public WeaponModification[] permanentMods;
        public WeaponModification[] mods;

        public WeaponModificationData payload;
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

            foreach (var mod in data.permanentMods) mod.OnFire(ref data);
            foreach (var mod in data.mods)          mod.OnFire(ref data);

            if (data.weaponData.type != PlayerWeaponData.ProjectileType.Raycast) continue;

            if (Physics.Raycast(data.position, data.direction, out RaycastHit hit, Mathf.Infinity, PlayerWeapon.CollisionLayers))
            {
                if (hit.collider.TryGetComponent(out Health health))
                {
                    if (!data.payload.Contains("hashSet")) health.Damage(data.weaponData.damage);
                }


                foreach (var mod in data.permanentMods) mod.OnHit(hit, ref data);
                foreach (var mod in data.mods)          mod.OnHit(hit, ref data);
            }
            else
            {
                foreach (var mod in data.permanentMods) mod.OnMiss(ref data);
                foreach (var mod in data.mods)          mod.OnMiss(ref data);
            }

            data.payload.Set(false, "firstShot");
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
        Vector3 dir = context.GetCamera().CameraForward;

        foreach (var mod in permanentModifications) mod.AltFire(dir);
        foreach (var mod in modifications)          mod.AltFire(dir);
    }

    public void FireImmediate(Vector3 position, Vector3 direction, ref WeaponModificationData payload)
    {
        var bulletData = new FireData()
        {
            position      = position,
            direction     = direction + (Random.insideUnitSphere * WeaponData.shotDeviation),
            weaponData    = WeaponData,
            permanentMods = permanentModifications.ToArray(),
            mods          = modifications.ToArray(),
            payload       = payload,
            context       = this,
        };

        bullets.Enqueue(bulletData);
    }

    public void UpdateWeapon()
    {
        foreach (var mod in permanentModifications) mod.OnUpdate();
        foreach (var mod in modifications)          mod.OnUpdate();

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
        foreach (var mod in permanentModifications) mod.OnReload();
        foreach (var mod in modifications)          mod.OnReload();

        PlayerWeapon.ReloadStart?.Invoke();

        yield return new WaitForSeconds(WeaponData.reloadTime);

        WeaponData.shotCount = WeaponData.magazineSize;

        reloading = false;
        PlayerWeapon.ReloadFinished?.Invoke();
    }
}
