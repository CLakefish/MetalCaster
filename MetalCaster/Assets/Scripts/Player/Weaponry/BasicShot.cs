using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Basic Shot")]
public class BasicShot : WeaponModification
{
    [SerializeField] private GameObject hitSpawn;

    public override void OnFire(Weapon context) {
        context.context.GetCamera().Screenshake(1, 1);
    }

    public override void OnHit(Weapon context, RaycastHit hit, int bounceCount)
    {
        GameObject plane    = Instantiate(hitSpawn, hit.point + (hit.normal / 10.0f), Quaternion.identity);

        Vector3 start       = context.context.GetCamera().CameraTransform.position;
        Vector3 end         = hit.point;
        Vector3 toPlayer    = -(start - end).normalized;
        Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(toPlayer, hit.normal), hit.normal);

        if (Vector3.Angle(Vector3.up, hit.normal) >= 89.0f) {
            rotation = Quaternion.Euler(-90, rotation.eulerAngles.y, rotation.eulerAngles.z);
        }

        plane.transform.rotation = rotation;

        Vector3 startPos  = context.PrevHit;
        Vector3 reflected = Vector3.Reflect((hit.point - startPos).normalized, hit.normal).normalized;

        context.FireImmediate(hit.point, reflected, bounceCount);
    }
}
