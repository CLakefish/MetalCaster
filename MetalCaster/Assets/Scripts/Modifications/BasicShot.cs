using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Basic Shot")]
public class BasicShot : WeaponModification
{
    [SerializeField] private GameObject hitSpawn;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float lineTime;
    [SerializeField] private int ricochetTotal;
    [SerializeField] private float screenShakeDuration, screenShakeMagnitude;

    public override void Modify(Weapon context) {
        context.WeaponData.ricochetCount += ricochetTotal;
    }

    public override void OnFire(ref Weapon.FireData data) {
        //data.PlayerWeapon.GetCamera().Screenshake(screenShakeDuration, screenShakeMagnitude);
    }

    public override void OnMiss(ref Weapon.FireData data)
    {
        if (data.payload.Contains("prevHit")) {
            Vector3 prevHitPoint = data.payload.Get<Vector3>("prevHit");
            InstantiateLine(prevHitPoint, data.direction.normalized * 1000);
        }
    }

    public override void OnHit(RaycastHit hit, ref Weapon.FireData data)
    {
        int ricochetCount = data.payload.Get<int>("ricochet");
        if (ricochetCount <= 0 && data.weaponData.ricochetCount != 0) return;

        bool firstShot = data. payload.Get<bool>("firstShot");
        if (!firstShot) {
            ricochetCount--;
            data.payload.Set(ricochetCount, "ricochet");
        }

        if (data.payload.Get<Vector3>("prevHit") == Vector3.zero) {
            data.payload.Set(data.context.MuzzlePos.position, "prevHit");
        }

        Vector3 start       = data.context.PlayerWeapon.GetCamera().CameraTransform.position;
        Vector3 end         = hit.point;
        Vector3 toPlayer    = -(start - end).normalized;
        Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(toPlayer, hit.normal), hit.normal);

        if (Vector3.Angle(Vector3.up, hit.normal) == 90f) {
            rotation = Quaternion.Euler(-90, rotation.eulerAngles.y, rotation.eulerAngles.z);
        }

        Instantiate(hitSpawn, hit.point + (hit.normal / 10.0f), rotation);

        Vector3 prevHit = data.payload.Get<Vector3>("prevHit");
        Vector3 reflected = Vector3.Reflect((hit.point - prevHit).normalized, hit.normal);

        data.context.FireImmediate(hit.point, reflected, ref data.payload);
        InstantiateLine(prevHit, hit.point);
        data.payload.Set(hit.point, "prevHit");
    }

    private void InstantiateLine(Vector3 p0, Vector3 p1)
    {
        LineRenderer line = Instantiate(lineRenderer, Vector3.zero, Quaternion.identity);
        line.SetPosition(0, p0);
        line.SetPosition(1, p1);
        Destroy(line.gameObject, lineTime);
    }
}
