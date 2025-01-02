using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Vanity/Line Rendering")]
public class LineRendering : Modification.AlwaysEmpty
{
    [Header("Prefabs and Timings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float destroyTime;

    private void InstantiateLine(Vector3 p0, Vector3 p1) {
        LineRenderer line = Instantiate(lineRenderer, Vector3.zero, Quaternion.identity);
        line.SetPosition(0, p0);
        line.SetPosition(1, p1);
        Destroy(line.gameObject, destroyTime);
    }

    public override void OnHit(ref RaycastHit hit, ref Bullet bullet) {
        Vector3 muzzlePos = Player.PlayerWeapon.Selected.Weapon.MuzzlePos.transform.position;
        InstantiateLine(muzzlePos, hit.point);
    }

    public override void OnMiss(Vector3 pos, Vector3 dir) {
        Vector3 muzzlePos = Player.PlayerWeapon.Selected.Weapon.transform.position;
        InstantiateLine(muzzlePos, muzzlePos + (Player.PlayerCamera.CameraForward * 1000));
    }
}
