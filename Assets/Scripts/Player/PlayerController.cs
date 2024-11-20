using System.Collections.Generic;
using UnityEngine;
using HFSMFramework;
using System;

// Made by Carson Lakefish
// Concept recommendations by Oliver Beebe

// TODO:
// Add in HFSM substatemachine support and implement!!!
// - Sliding
// - Fix inconsistent slope sliding boost bug 
// - Smooth crouching 
// - Movement when sliding (slow turning)?
// - Wall Jump
// - GUNS

public class PlayerController : Player.PlayerComponent
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Collisions")]
    [SerializeField] private LayerMask layers;
    [SerializeField] private float groundCastDist     = 0.8f;
    [SerializeField] private float fallingCastDist    = 0.6f;

    private readonly float groundCastRad              = 0.25f;
    private readonly float raycastMargin              = 0.1f;
    private readonly float floorStickThreshold        = 0.1f;
    private readonly float floorStickCheckOffset      = 0.05f;
    private readonly float interpolateNormalCheckDist = 2.5f;

    [Header("Walking Parameters")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float deceleration;
    [SerializeField] private float groundMomentumConserveTime;

    [Header("Gravity Parameters")]
    [SerializeField] private float gravity;
    [SerializeField] private float maxFallSpeed;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float coyoteTime;
    [SerializeField] private float jumpBuffer;
    [SerializeField] private float minJumpTime;
    private float jumpBufferTime;

    [Header("Sliding Parameters")]
    [SerializeField] private float slideForce;
    [SerializeField] private float slideRotationSpeed;
    [SerializeField] private float slideAcceleration;
    [SerializeField] private float slideJumpForce;
    [SerializeField] private float slideMomentumIncrease;
    [SerializeField] private float slideFloorStickSpeed;
    [SerializeField] private float minSpeedIncreaseThreshold;

    private bool GroundCollision { get; set; }
    private bool SlopeCollision  { get; set; }
    private Vector3 GroundNormal { get; set; }
    private Vector3 GroundPoint  { get; set; }
    private float HalfSize {
        get { return rb.transform.localScale.y; }
    }

    private Vector3 ViewPosition
    {
        get {
            return playerInput.IsInputting
            ? (playerCamera.CameraForward * playerInput.NormalizedInput.y + playerCamera.CameraRight * playerInput.NormalizedInput.x).normalized
            : Vector3.zero;
        }
    }

    private Vector3 ViewPositionNoY
    {
        get
        {
            return playerInput.IsInputting
            ? (playerCamera.CameraForwardNoY * playerInput.NormalizedInput.y + playerCamera.CameraRight * playerInput.NormalizedInput.x).normalized
            : Vector3.zero;
        }
    }

    private bool WallCollision   { get; set; }

    private bool slideBoost = true;
    private bool crouching  = false;

    private Vector2 DesiredVelocity { get; set; }

    private StateMachine<PlayerController> hfsm;
    private GroundedState Grounded  { get; set; }
    private FallingState  Falling   { get; set; }
    private JumpingState  Jumping   { get; set; }
    private SlidingState  Sliding   { get; set; }
    private SlideJumping  SlideJump { get; set; }

#if UNITY_EDITOR
    [Header("Debugging")]
    [SerializeField] private float DebugFlySpeed = 10;
    [SerializeField] private float scrollWheelSpeed;
    private bool DebugMode = false;
    private DebugFlyingState   DebugFly   { get; set; }
    private DebugPauseState    DebugPause { get; set; }
#endif

    private void OnEnable()
    {
        hfsm       = new(this);
        Grounded   = new(this);
        Falling    = new(this);
        Jumping    = new(this);
        Sliding    = new(this);
        SlideJump  = new(this);
        DebugFly   = new(this);
        DebugPause = new(this);

        hfsm.AddTransitions(new()
        {
            // Grounded transitions
            new(Grounded, Falling,   () => !GroundCollision),
            new(Grounded, Jumping,   () => Input.GetKeyDown(KeyCode.Space) || jumpBufferTime > 0),
            new(Grounded, Sliding,   () => Input.GetKey(KeyCode.LeftControl)),
                                     
            // Jumping transitions   
            new(Jumping, Falling,    () => rb.velocity.y < 0),
            new(Jumping, Grounded,   () => GroundCollision && hfsm.Duration >= minJumpTime),
                                     
            // Falling transitions   
            new(Falling, SlideJump,  () => Input.GetKeyDown(KeyCode.Space) && hfsm.PreviousState == Sliding  && hfsm.Duration < coyoteTime),
            new(Falling, Jumping,    () => Input.GetKeyDown(KeyCode.Space) && hfsm.PreviousState == Grounded && hfsm.Duration < coyoteTime),
            new(Falling, Sliding,    () => GroundCollision && Input.GetKey(KeyCode.LeftControl)),
            new(Falling, Grounded,   () => GroundCollision),
                                     
            // Sliding transitions   
            new(Sliding, Falling,    () => !GroundCollision),
            new(Sliding, SlideJump,  () => Input.GetKeyDown(KeyCode.Space)),
            new(Sliding, Grounded,   () => !Input.GetKey(KeyCode.LeftControl)),

            // Slide jump transitions
            new(SlideJump, Falling,  () => rb.velocity.y < 0 && hfsm.Duration > 0),
            new(SlideJump, Grounded, () => GroundCollision && hfsm.Duration >= minJumpTime),

#if UNITY_EDITOR
            new(null,     DebugFly,  () => Input.GetKeyDown(KeyCode.V)),
            new(DebugFly, Falling,   () => Input.GetKeyDown(KeyCode.V)),

            new(null, DebugPause,    () => Input.GetKeyDown(KeyCode.B)),
            new(DebugPause, Falling, () => Input.GetKeyDown(KeyCode.B)),
#endif
        });

        hfsm.Start(Grounded);
    }

    private void Update()
    {
        jumpBufferTime -= Time.deltaTime;

        hfsm.CheckTransitions();
        hfsm.Update();
    }

    private void FixedUpdate()
    {
#if UNITY_EDITOR
        if (DebugMode)
        {
            hfsm.FixedUpdate();
            return;
        }
#endif

        GroundCollisions();

        hfsm.FixedUpdate();

        if (!GroundCollision && !SlopeCollision) rb.velocity -= new Vector3(0, gravity, 0) * Time.fixedDeltaTime;
    }

    private void Move(bool maintainMomentum = true)
    {
        float increasedValue = maintainMomentum
            ? Mathf.Max(moveSpeed, Mathf.Abs(new Vector2(rb.velocity.x, rb.velocity.z).magnitude)) 
            : moveSpeed;

        float speedChange = playerInput.IsInputting
            ? acceleration
            : deceleration;

        DesiredVelocity     = Vector2.MoveTowards(DesiredVelocity, new Vector2(ViewPositionNoY.x, ViewPositionNoY.z) * increasedValue, speedChange * Time.fixedDeltaTime);
        Vector3 setVelocity = new (DesiredVelocity.x, rb.velocity.y, DesiredVelocity.y);

        if (hfsm.CurrentState == Grounded && SlopeCollision) setVelocity = Quaternion.FromToRotation(Vector3.up, GroundNormal) * setVelocity;

        setVelocity.y = Mathf.Clamp(setVelocity.y, maxFallSpeed, Mathf.Infinity);
        rb.velocity   = setVelocity;
    }

    private void GroundCollisions()
    {
        float castDist = hfsm.CurrentState == Grounded || hfsm.CurrentState == Sliding ? groundCastDist : fallingCastDist;

        if (Physics.SphereCast(rb.transform.position, groundCastRad * HalfSize, Vector3.down, out RaycastHit interpolated, castDist, layers))
        {
            Vector3 dir  = interpolated.point - rb.transform.position;

            // SphereCast interpolates the floor normal, resulting in a jitter when relying soly on it. So, to fix this I use 2 raycasts
            // 1. Is used to derive the nonInterpolated floor normal
            // 2. Is to ensure that the interpolated floor normal is used on sharp ground changes (makes it so much smoother)
            if (Physics.Raycast(rb.transform.position, dir, out RaycastHit nonInterpolated, dir.magnitude + raycastMargin, layers)) {
                // FUCKKKKK!!!!
                if (Vector3.Angle(Vector3.up, nonInterpolated.normal) >= 90) return;

                Vector3 vel            = new Vector3(rb.velocity.x, -interpolateNormalCheckDist, rb.velocity.z).normalized * interpolateNormalCheckDist;
                Vector3 pos            = rb.transform.position + vel - (Vector3.up * (HalfSize - floorStickCheckOffset));
                bool interpolateNormal = Physics.Raycast(pos, -playerCamera.CameraForwardNoY, out RaycastHit interpolateCheck, interpolateNormalCheckDist, layers);

                GroundPoint = nonInterpolated.point;
                /*
                if (interpolateNormal)
                {
                    float interpolateCheckAngle = Vector3.Angle(Vector3.up, interpolateCheck.normal);
                    if (interpolateCheckAngle == 0) rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                }*/

                //Debug.DrawRay(pos, -PlayerCamera.CameraForward * interpolateNormalCheckDist, Color.green, 1);

                GroundNormal = interpolateNormal ? interpolated.normal : nonInterpolated.normal;
            }

            float angle     = Vector3.Angle(Vector3.up, GroundNormal);
            GroundCollision = true;
            SlopeCollision  = angle > 0 && angle < 90;

            //Debug.DrawRay(interpolated.point, GroundNormal * 10, Color.yellow, 10);

            return;
        }

        ResetGroundCollisions();
    }

    private void ResetGroundCollisions()
    {
        SlopeCollision  = false;
        GroundCollision = false;
    }

    private class GroundedState : State<PlayerController>
    {
        public GroundedState(PlayerController context) : base(context) { }

        public override void Enter()
        {
            GroundStick();
            context.slideBoost = true;
        }

        public override void FixedUpdate()
        {
            GroundStick();
            context.Move(context.hfsm.Duration < context.groundMomentumConserveTime);
        }

        private Vector3 stickVelocity;

        private void GroundStick()
        {
            float yPos   = context.rb.position.y - context.HalfSize - context.GroundPoint.y;
            float yCheck = context.floorStickThreshold;

            // Floor sticking/Ground correction
            if (yPos > yCheck)
            {
                context.rb.position = Vector3.SmoothDamp(
                    context.rb.position,
                    new Vector3(context.rb.position.x, context.GroundPoint.y + context.HalfSize, context.rb.position.z),
                    ref stickVelocity,
                    context.slideFloorStickSpeed);
            }

            context.rb.velocity = new Vector3(context.rb.velocity.x, 0, context.rb.velocity.z);
        }
    }

    private class FallingState : State<PlayerController>
    {
        public FallingState(PlayerController context) : base(context) { }

        public override void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                context.jumpBufferTime = context.jumpBuffer;
            }

            if (context.crouching && !Input.GetKey(KeyCode.LeftControl))
            {
                context.slideBoost = true;
                context.crouching = false;
                context.rb.transform.localScale = Vector3.one;
            }
        }

        public override void FixedUpdate() => context.Move(true);
    }

    private class JumpingState : State<PlayerController>
    {
        public JumpingState(PlayerController context) : base(context) { }

        public override void Enter()
        {
            context.ResetGroundCollisions();
            context.jumpBufferTime = 0;
            context.rb.velocity = new Vector3(context.rb.velocity.x, context.jumpForce, context.rb.velocity.z);
        }

        public override void FixedUpdate()
        {
            context.Move(true);
        }
    }

    private class SlidingState : State<PlayerController>
    {
        public SlidingState(PlayerController context) : base(context) { }
        private Vector3 momentumVelocity;
        private Vector3 stickVelocity;
        private float stickVel;
        private float increasedValue;

        public override void Enter()
        {
            stickVelocity  = Vector3.zero;
            increasedValue = 1;
            stickVel = 0;

            if (context.slideBoost)
            {
                /*
                Vector3 momentumDirection = context.ViewPositionNoY;
                Vector3 slopeGravity       = Vector3.ProjectOnPlane(Vector3.down * (context.gravity + 1), context.GroundNormal) * Time.fixedDeltaTime;
                float newMomentumMagnitude = context.slideForce;

                momentumVelocity    = momentumDirection * newMomentumMagnitude + slopeGravity;
                context.rb.velocity = momentumVelocity;*/
                Vector3 dir = (context.ViewPositionNoY * context.slideForce) + (Vector3.ProjectOnPlane(Vector3.down * context.gravity, context.GroundNormal) * Time.fixedDeltaTime);

                momentumVelocity = Quaternion.FromToRotation(dir, context.GroundNormal) * dir;
                context.rb.velocity = momentumVelocity;
            }
            else
            {
                momentumVelocity = new Vector3(context.rb.velocity.x, context.rb.velocity.y, context.rb.velocity.z);
            }

            context.DesiredVelocity = new Vector2(momentumVelocity.x, momentumVelocity.z);
            context.slideBoost      = false;
        }

        public override void FixedUpdate()
        {
            Vector3 desiredVelocity;

            // Gaining momentum
            if (context.SlopeCollision)
            {
                // Get the gravity, angle, and current momentumDirection
                Vector3 slopeGravity       = Vector3.ProjectOnPlane(Vector3.down * (context.gravity + increasedValue), context.GroundNormal);
                Vector3 momentumDirection  = momentumVelocity.normalized;
                float angle                = Vector3.Angle(Vector3.up, context.GroundNormal);
                float newMomentumMagnitude = Mathf.Max(1, momentumVelocity.magnitude - angle);
                
                // Ez
                desiredVelocity = momentumDirection * newMomentumMagnitude + slopeGravity;
                increasedValue  = Mathf.Abs(context.rb.velocity.magnitude) <= context.minSpeedIncreaseThreshold ? 0 : increasedValue + (context.slideMomentumIncrease * (angle / 90) * Time.fixedDeltaTime);
            }
            else
            {
                desiredVelocity  = Vector3.zero;
                increasedValue   = 1;
            }

            // Move towards gathered velocity
            momentumVelocity = Vector3.MoveTowards(momentumVelocity, desiredVelocity, Time.fixedDeltaTime * context.slideAcceleration);

            // Rotate the momentum based off of a speed value, preserve the gravity on the Y axis
            if (context.ViewPosition != Vector3.zero)
            {
                Quaternion rotation = Quaternion.FromToRotation(momentumVelocity, context.ViewPosition);
                Quaternion interpolated = Quaternion.Slerp(Quaternion.identity, rotation, Time.fixedDeltaTime * context.slideRotationSpeed);
                float tempY = momentumVelocity.y;
                momentumVelocity = interpolated * momentumVelocity;
                momentumVelocity.y = tempY;
            }

            // Floor stick check
            float yPos   = context.rb.position.y - context.HalfSize - context.GroundPoint.y;
            float yCheck = context.floorStickThreshold;
            if (yPos > yCheck)
            {
                // Smooth damp position
                context.rb.position = Vector3.SmoothDamp(
                    context.rb.position,
                    new Vector3(context.rb.position.x, context.GroundPoint.y + context.HalfSize, context.rb.position.z),
                    ref stickVelocity,
                    context.slideFloorStickSpeed);

                // Smooth damp velocity
                momentumVelocity.y = Mathf.SmoothDamp(
                    momentumVelocity.y,
                    Mathf.Min(momentumVelocity.y, -yPos / Time.fixedDeltaTime), 
                    ref stickVel,
                    context.slideFloorStickSpeed);
            }

            // Setting values
            context.rb.velocity     = momentumVelocity;
            context.DesiredVelocity = new Vector2(context.rb.velocity.x, context.rb.velocity.z);

            Debug.DrawRay(context.rb.position, context.rb.velocity.normalized * 10, Color.red, 2);
        }

    }

    private class SlideJumping : State<PlayerController>
    {
        public SlideJumping(PlayerController context) : base(context) { }

        public override void Enter()
        {
            context.ResetGroundCollisions();
            context.jumpBufferTime = 0;
            context.slideBoost     = false;
            context.rb.velocity    = new Vector3(context.rb.velocity.x, context.slideJumpForce, context.rb.velocity.z);
        }

        public override void FixedUpdate()
        {
            context.Move(true);
        }
    }

    #region Debugging

#if UNITY_EDITOR

    private void OnGUI()
    {
        hfsm.OnGUI();

        GUILayout.BeginArea(new Rect(10, 100, 800, 200));

        string current = $"Current Velocity: { rb.velocity }\nCurrent Magnitude: { rb.velocity.magnitude }";
        GUILayout.Label($"<size=15>{current}</size>");
        GUILayout.EndArea();
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rb.transform.position + new Vector3(0, -groundCastDist, 0), groundCastRad);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(rb.transform.position + new Vector3(0, -fallingCastDist, 0), groundCastRad);
    }

    private class DebugFlyingState : State<PlayerController>
    {
        public DebugFlyingState(PlayerController context) : base(context) { }

        public override void Enter()
        {
            context.DebugMode       = true;
            context.rb.velocity     = Vector3.zero;
            context.DesiredVelocity = Vector2.zero;
        }

        public override void Update()
        {
            context.DebugFlySpeed += Input.GetAxisRaw("Mouse ScrollWheel") * context.scrollWheelSpeed;

            Vector3 movePosition = context.ViewPosition;

            if (Input.GetKey(KeyCode.Space))     movePosition.y += 1;
            if (Input.GetKey(KeyCode.LeftShift)) movePosition.y -= 1;

            context.rb.transform.localPosition = Vector3.MoveTowards(context.rb.transform.localPosition, context.rb.transform.localPosition + movePosition, Time.unscaledDeltaTime * context.DebugFlySpeed);
        }

        public override void Exit()
        {
            context.DebugMode = false;
        }
    }

    private class DebugPauseState : State<PlayerController>
    {
        public DebugPauseState(PlayerController context) : base(context) { }

        public override void Enter()
        {
            context.DebugMode = true;
            context.rb.velocity     = Vector3.zero;
            context.DesiredVelocity = Vector2.zero;

            Time.timeScale = 0;
        }

        public override void Update()
        {
            context.DebugFlySpeed += Input.GetAxisRaw("Mouse ScrollWheel") * context.scrollWheelSpeed;

            Vector3 movePosition = context.ViewPosition;

            if (Input.GetKey(KeyCode.Space))     movePosition.y += 1;
            if (Input.GetKey(KeyCode.LeftShift)) movePosition.y -= 1;

            Transform cam     = context.playerCamera.CameraTransform;
            cam.localPosition = Vector3.MoveTowards(cam.localPosition, cam.localPosition + movePosition, Time.unscaledDeltaTime * context.DebugFlySpeed);
        }

        public override void Exit()
        {
            Time.timeScale    = 1;
            context.DebugMode = false;
            context.playerCamera.CameraTransform.localPosition = Vector3.up * 0.5f;
        }
    }

#endif

    #endregion
}