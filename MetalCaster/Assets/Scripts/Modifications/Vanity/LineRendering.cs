using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Vanity/Line Rendering")]
public class LineRendering : WeaponModification
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float time;

    private void InstantiateLine(Vector3 p0, Vector3 p1)
    {
        LineRenderer line = Instantiate(lineRenderer, Vector3.zero, Quaternion.identity);
        line.SetPosition(0, p0);
        line.SetPosition(1, p1);
        Destroy(line.gameObject, time);
    }

    public override void OnHit(RaycastHit hit, ref Weapon.FireData data)
    {
        if (data.payload.Get<bool>("firstShot")) {
            InstantiateLine(data.context.PlayerWeapon.Viewmodel.transform.position, hit.point);
        }
    }

    public override void OnMiss(ref Weapon.FireData data)
    {
        if (data.payload.Get<bool>("firstShot")) {
            InstantiateLine(data.context.PlayerWeapon.Viewmodel.transform.position, data.direction * 1000);
        }
    }
}
