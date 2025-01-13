using System.Collections;
using UnityEngine;

public class PlayerCollisions : Player.PlayerComponent
{
    [Header("Ground")]
    [SerializeField] private float groundCastDist;
    [SerializeField] private float groundCastRadius;
    [SerializeField] private float fallingCastDist;
    [SerializeField] private float fallingCastRadius;
    [SerializeField] private float interpolateNormalSpeed;

    [Header("Wall")]
    [SerializeField] private int wallCastIncrements;
    [SerializeField] private float wallCastDistance;

    public readonly float interpDeviation     = 10;
    public readonly float raycastMargin       = 0.1f;
    public readonly float floorStickThreshold = 0.05f;

    public bool GroundCollision { get; set; }
    public bool SlopeCollision  { get; set; }
    public bool WallCollision   { get; set; }

    public Vector3 GroundNormal { get; set; }
    public Vector3 GroundPoint  { get; set; }
    public Vector3 WallNormal   { get; set; }

    private Coroutine sizeChange = null;

    public float Size {
        get {
            return CapsuleCollider.height;
        }
        set {
            float start = CapsuleCollider.height;
            float hD = value - start;

            CapsuleCollider.height += hD;
            if (GroundCollision) rb.MovePosition(rb.position + Vector3.up * hD / 2.0f);
        }
    }

    public void ChangeSize(float endSize)
    {
        if (sizeChange != null) StopCoroutine(sizeChange);
        sizeChange = StartCoroutine(ChangeSizeCoroutine(endSize));
    }

    private IEnumerator ChangeSizeCoroutine(float endSize)
    {
        while (Mathf.Abs(PlayerCollisions.Size - endSize) > 0.01f)
        {
            PlayerCollisions.Size = Mathf.MoveTowards(PlayerCollisions.Size, endSize, Time.fixedDeltaTime * PlayerMovement.crouchTime);
            yield return new WaitForFixedUpdate();
        }

        PlayerCollisions.Size = endSize;
    }

    public void GroundCollisions()
    {
        bool  Grounded = PlayerMovement.GroundState;
        float castDist = (Grounded ? groundCastDist : fallingCastDist) * (Size / 2.0f);
        float castRad  = Grounded ? groundCastRadius : fallingCastRadius;

        if (Physics.SphereCast(rb.transform.position, castRad, Vector3.down, out RaycastHit interpolated, castDist, GroundLayer))
        {
            Vector3 dir = (interpolated.point - rb.transform.position).normalized;
            Vector3 desiredNormal = interpolated.normal;

            if (Physics.Raycast(rb.transform.position, dir, out RaycastHit nonInterpolated, dir.magnitude + raycastMargin, GroundLayer))
            {
                // FUCKKKKK!!!!
                if (Vector3.Angle(Vector3.up, nonInterpolated.normal) >= 90)
                {
                    desiredNormal = Vector3.up;
                }
                else
                {
                    float interpAngle = Vector3.Angle(Vector3.up, interpolated.normal);
                    float nonInterpAngle = Vector3.Angle(Vector3.up, nonInterpolated.normal);

                    if (interpAngle >= 90 || Mathf.Abs(nonInterpAngle - interpAngle) > interpDeviation)
                    {
                        desiredNormal = nonInterpolated.normal;
                    }
                }
            }

            float angle = Vector3.Angle(Vector3.up, desiredNormal);
            GroundNormal = Vector3.MoveTowards(GroundNormal, desiredNormal, Time.fixedDeltaTime * interpolateNormalSpeed);
            GroundPoint = interpolated.point;
            GroundCollision = true;
            SlopeCollision = angle > 0;
            return;
        }

        ResetGroundCollisions();
    }

    public void WallCollisions()
    {
        float P2 = Mathf.PI * 2 / wallCastIncrements;

        Vector3 combined = Vector3.zero;

        for (int i = 0; i < wallCastIncrements; ++i)
        {
            Vector3 dir = new Vector3(Mathf.Cos(P2 * i), 0, Mathf.Sin(P2 * i)).normalized;

            if (Physics.Raycast(rb.position, dir, out RaycastHit hit, wallCastDistance, GroundLayer))
            {
                combined += hit.normal;
            }
        }

        if (combined == Vector3.zero)
        {
            ResetWallCollisions();
            return;
        }

        WallCollision = true;
        WallNormal = combined.normalized;
    }


    public void ResetGroundCollisions() => SlopeCollision = GroundCollision = false;
    public void ResetWallCollisions()   => WallCollision = false;

    public void ResetAllCollisions()
    {
        ResetWallCollisions();
        ResetGroundCollisions();
    }

}
