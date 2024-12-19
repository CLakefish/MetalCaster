using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Basic Shot")]
public class BasicShot : WeaponModification
{
    [SerializeField] private GameObject hitSpawn;

    public override void OnFire(Weapon context) {
        context.PlayerWeapon.GetCamera().Screenshake(1, 1);
    }

    public override void OnHit(Weapon context, RaycastHit hit, int bounceCount)
    {
        Vector3 start       = context.PlayerWeapon.GetCamera().CameraTransform.position;
        Vector3 end         = hit.point;
        Vector3 toPlayer    = -(start - end).normalized;
        Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(toPlayer, hit.normal), hit.normal);

        if (Vector3.Angle(Vector3.up, hit.normal) >= 89.0f) {
            rotation = Quaternion.Euler(-90, rotation.eulerAngles.y, rotation.eulerAngles.z);
        }

        GameObject plane         = Instantiate(hitSpawn, hit.point + (hit.normal / 10.0f), Quaternion.identity);
        plane.transform.rotation = rotation;

        Vector3 reflected = Vector3.Reflect((hit.point - context.PrevHit).normalized, hit.normal);

        context.FireImmediate(hit.point, reflected, bounceCount);
    }
}
