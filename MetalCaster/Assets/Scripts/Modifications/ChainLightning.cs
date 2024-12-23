using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/Chain Lightning")]
public class ChainLightning : WeaponModification
{
    [SerializeField] private float radius = 10;
    [SerializeField] private LayerMask hittable;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int baseBounce;
    private (float distance, GameObject obj) Closest;

    public override void Modify(Weapon context)
    {
        context.WeaponData.ricochetCount += baseBounce;
    }

    public override void OnFire(Weapon context)
    {
        Closest = (Mathf.Infinity, null);
    }

    public override void OnHit(Weapon context, RaycastHit hit, ref ShotPayload payload)
    {
        if (payload.ricochetTotal <= 0 && context.WeaponData.ricochetCount != 0) return;
        if (hit.collider.GetComponent<Health>() == null) return;

        payload.ricochetTotal--;

        payload.hashSet.Add(hit.collider.gameObject);

        Closest = (Mathf.Infinity, null);

        Collider[] colliders = Physics.OverlapSphere(hit.point, radius, hittable, QueryTriggerInteraction.UseGlobal);

        foreach (var collider in colliders)
        {
            if (!collider.TryGetComponent(out Health _)) continue;
            if (payload.hashSet.Contains(collider.gameObject)) continue;

            float dist = Vector3.Distance(hit.collider.transform.position, collider.transform.position);

            if (dist < Closest.distance)
            {
                Closest = (dist, collider.gameObject);
            }
        }

        // Avoid weird VFX
        if (Closest.obj == null || payload.ricochetTotal <= 0) return;

        Vector3 dir = (Closest.obj.transform.position - hit.collider.transform.position).normalized;

        InstantiateLine(hit.collider.transform.position, Closest.obj.transform.position);
        context.FireImmediate(hit.collider.transform.position, dir, ref payload);
    }

    private void InstantiateLine(Vector3 p0, Vector3 p1)
    {
        LineRenderer line = Instantiate(lineRenderer, Vector3.zero, Quaternion.identity);
        line.SetPosition(0, p0);
        line.SetPosition(1, p1);
        Destroy(line.gameObject, 0.5f);
    }
}
