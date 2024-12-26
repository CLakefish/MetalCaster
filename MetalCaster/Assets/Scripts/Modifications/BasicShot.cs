using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Basic Shot")]
public class BasicShot : WeaponModification
{
    [SerializeField] private GameObject hitSpawn;
    [SerializeField] private int ricochetTotal;
    [SerializeField] private float screenShakeDuration, screenShakeMagnitude;

    public override void Modify(Weapon context) {
        context.WeaponData.ricochetCount += ricochetTotal;
    }

    public override void OnFire(Weapon context) {
        context.PlayerWeapon.GetCamera().Screenshake(screenShakeDuration, screenShakeMagnitude);
    }

    public override void OnHit(Weapon context, RaycastHit hit, ref ShotPayload payload)
    {
        if (payload.ricochetTotal <= 0 && context.WeaponData.ricochetCount != 0) return;
        payload.ricochetTotal--;

        Vector3 start       = context.PlayerWeapon.GetCamera().CameraTransform.position;
        Vector3 end         = hit.point;
        Vector3 toPlayer    = -(start - end).normalized;
        Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(toPlayer, hit.normal), hit.normal);

        if (Vector3.Angle(Vector3.up, hit.normal) == 90f) {
            rotation = Quaternion.Euler(-90, rotation.eulerAngles.y, rotation.eulerAngles.z);
        }

        Instantiate(hitSpawn, hit.point + (hit.normal / 10.0f), rotation);

        Vector3 reflected = Vector3.Reflect((hit.point - context.StartPos).normalized, hit.normal);

        context.FireImmediate(hit.point, reflected, ref payload);
    }
}
