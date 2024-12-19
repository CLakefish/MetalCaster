using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Advertisement")]
public class Advertisement : WeaponModification
{
    [SerializeField] private Material advertisementMaterial;
    [SerializeField] private LineRenderer lineRenderer;

    public override void OnMiss(Weapon context)
    {
        LineRenderer renderer = Instantiate(lineRenderer, Vector3.zero, Quaternion.identity);
        renderer.SetPosition(0, context.playerWeapon.GetCamera().CameraForward * 4000);
        renderer.SetPosition(1, context.playerWeapon.Viewmodel.transform.position);
        Destroy(renderer, 0.25f);
    }

    public override void OnHit(Weapon context, RaycastHit hit, int bounceCount)
    {
        LineRenderer renderer = Instantiate(lineRenderer, Vector3.zero, Quaternion.identity);
        renderer.SetPosition(0, hit.point);
        renderer.SetPosition(1, context.playerWeapon.Viewmodel.transform.position);
        Destroy(renderer, 0.25f);

        if (hit.collider.TryGetComponent<MeshRenderer>(out MeshRenderer mesh)) mesh.material = advertisementMaterial;
    }
}
