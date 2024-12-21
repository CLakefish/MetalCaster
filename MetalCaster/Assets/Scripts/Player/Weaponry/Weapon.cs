using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotPayload {
    public int ricochetTotal;
    public bool firstShot;
    public HashSet<GameObject> hashSet;
}

public class Weapon : MonoBehaviour
{
    private class FireData
    {
        public Vector3 pos;
        public Vector3 dir;
        public ShotPayload payload;

        public System.Func<Vector3, Vector3, ShotPayload, ShotPayload> action;
    }

    [Header("Modifications (Debugging)")]
    [SerializeField] public List<WeaponModification> modifications;
    [SerializeField] public List<WeaponModification> permanentModifications;

    [Header("Data")]
    [SerializeField] private PlayerWeaponData baseData;

    public PlayerWeaponData WeaponData { get; set; }
    public PlayerWeapon PlayerWeapon   { get; private set; }
    public Vector3 StartPos            { get; private set; }


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
            data.payload.firstShot = false;
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

        WeaponData.prevFireTime = Time.time;
        StartPos                 = context.GetCamera().CameraTransform.position;

        context.GetCamera().Recoil(WeaponData.recoil);
        context.GetCamera().ViewmodelRecoil();

        for (int i = 0; i < WeaponData.bulletsPerShot; ++i)
        {
            WeaponData.shotCount--;

            if (WeaponData.shotCount < 0) break;

            ShotPayload payload = new()
            {
                ricochetTotal = WeaponData.ricochetCount,
                hashSet       = new(),
                firstShot     = true
            };

            FireImmediate(context.GetCamera().CameraTransform.position, context.GetCamera().CameraForward, ref payload);
        }

        PlayerWeapon.Fire?.Invoke();
    }

    public void AltFire(PlayerWeapon context)
    {
        Vector3 pos = context.GetCamera().CameraTransform.position;
        Vector3 dir = context.GetCamera().CameraForward;

        StartPos = pos;

        foreach (var mod in permanentModifications) mod.AltFire(this, dir);
        foreach (var mod in modifications)          mod.AltFire(this, dir);
    }

    public void FireImmediate(Vector3 position, Vector3 direction, ref ShotPayload payload)
    {
        FireData data = new()
        {
            pos     = position,
            dir     = direction,
            payload = payload,
            action  = (Vector3 actPos, Vector3 actDir, ShotPayload actPayload) =>
            {
                foreach (var mod in permanentModifications) mod.OnFire(this);
                foreach (var mod in modifications)          mod.OnFire(this);

                if (WeaponData.type == PlayerWeaponData.ProjectileType.Raycast)
                {
                    Vector3 deviatedDir = actDir + (Random.insideUnitSphere * WeaponData.shotDeviation);

                    if (Physics.Raycast(actPos, deviatedDir, out RaycastHit hit, Mathf.Infinity, PlayerWeapon.CollisionLayers))
                    {
                        if (hit.collider.TryGetComponent(out Health hp) && !actPayload.hashSet.Contains(hit.collider.gameObject)) {
                            hp.Damage(WeaponData.damage);
                        }

                        foreach (var mod in permanentModifications) mod.OnHit(this, hit, ref actPayload);
                        foreach (var mod in modifications)          mod.OnHit(this, hit, ref actPayload);

                        StartPos = hit.point;
                    }
                    else
                    {
                        foreach (var mod in permanentModifications) mod.OnMiss(this, deviatedDir, ref actPayload);
                        foreach (var mod in modifications)          mod.OnMiss(this, deviatedDir, ref actPayload);
                    }
                }

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
