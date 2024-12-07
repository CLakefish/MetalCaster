using System.Collections.Generic;
using UnityEngine;
using HFSMFramework;
using System;
using System.Collections;

// Made by Carson Lakefish
// Concept recommendations by Oliver Beebe

// TODO:
// - Crouching State
// - Slide stick
// - Slide view tilt
// - Wall Run view tilt
// - Wall Run improved gravity
// - GUNS

public class PlayerController : Player.PlayerComponent
{
    [Header("References")]
    [SerializeField] public Rigidbody rb;
    [SerializeField] public CapsuleCollider collider;

    [Header("Collisions")]
    [SerializeField] private LayerMask layers;
    [SerializeField] private float groundCastDist     = 0.8f;
    [SerializeField] private float fallingCastDist    = 0.6f;
    [SerializeField] private int wallCastIncrements = 1;
    [SerializeField] private float wallCastDistance = 1;

    private readonly float groundCastRad              = 0.35f;
    private readonly float raycastMargin              = 0.1f;
    private readonly float floorStickThreshold        = 0.025f;

    private readonly float interpolateNormalCheckDist = 2.5f;
    [SerializeField] private float interpolateNormalSpeed     = 35;

    [Header("Walking Parameters")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float deceleration;
    [SerializeField] private float groundMomentumConserveTime;
    [SerializeField] private float groundStickSpeed;

    [Header("Gravity Parameters")]
    [SerializeField] private float gravity;
    [SerializeField] private float maxFallSpeed;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float coyoteTime;
    [SerializeField] private float jumpBuffer;
    [SerializeField] private float minJumpTime;

    [Header("Sliding Parameters")]
    [SerializeField] private float slideForce;
    [SerializeField] private float slideRotationSpeed;
    [SerializeField] private float slideAcceleration;
    [SerializeField] private float slideJumpForce;
    [SerializeField] private float slideSlopeMomentumGain; 
    private readonly float slideDotMin = -0.25f;

    [Header("Size Change Parameters")]
    [SerializeField] private float crouchSize;
    [SerializeField] private float standardSize;
    [SerializeField] private float crouchTime;

    [Header("Wall Run Parameters")]
    [SerializeField] private float wallRunSpeed;
    [SerializeField] private float wallRunGravityReduction;
    [SerializeField] private float wallRunRotationSpeed;

    [Header("Wall Jump Parameters")]
    [SerializeField] private float wallJumpForce;
    [SerializeField] private float wallJumpHeight;
    [SerializeField] private float wallJumpTime;
    [SerializeField] private float wallJumpMinDot = 0.25f;

    private bool GroundCollision                { get; set; }
    private bool SlopeCollision                 { get; set; }
    private Vector3 GroundNormal                { get; set; }
    private Vector3 NonInterpolatedGroundNormal { get; set; }
    private Vector3 GroundPoint                 { get; set; }

    private bool WallCollision                  { get; set; }
    private Vector3 WallNormal                  { get; set; }
    private Vector3 WallPoint                   { get; set; }

    private float Size {
        get { 
            return collider.height; 
        }
        set {
            float start = collider.height;
            float hD    = value - start;

            collider.height += hD;
            rb.MovePosition(rb.position + Vector3.up * hD / 2.0f);
        }
    }

    private Vector3 Position {
        get {
            return rb.position;
        }
        set {
            Vector3 begin = rb.position;
            Vector3 pD = value - begin;

            rb.MovePosition(rb.position + pD);
        }
    }

    private Vector3 ViewPosition {
        get {
            return playerInput.IsInputting
            ? (playerCamera.CameraForward * playerInput.NormalizedInput.y + playerCamera.CameraRight * playerInput.NormalizedInput.x).normalized
            : Vector3.zero;
        }
    }

    private Vector3 ViewPositionNoY {
        get {
            return playerInput.IsInputting
            ? (playerCamera.CameraForwardNoY * playerInput.NormalizedInput.y + playerCamera.CameraRight * playerInput.NormalizedInput.x).normalized
            : Vector3.zero;
        }
    }

    private bool AllowWallRun {
        get {
            float angle = Vector3.SignedAngle(WallNormal, ViewPositionNoY, Vector3.up);
            return Mathf.Abs(angle) > 75;
        }
    }

    private StateMachine<PlayerController> hfsm;

    public IState CurrentState {
        get {
            return hfsm.CurrentState;
        }
    }

    public IState PreviousState  {
        get {
            return hfsm.PreviousState;
        }
    }

    public bool IsWallRunning { get { return CurrentState == WallRun; } }

    private GroundedState Grounded  { get; set; }
    private FallingState  Falling   { get; set; }
    private JumpingState  Jumping   { get; set; }
    private SlidingState  Sliding   { get; set; }
    private SlideJumping  SlideJump { get; set; }
    private WallRunning   WallRun   { get; set; }
    private WallJumping   WallJump  { get; set; }

    private Vector2 DesiredVelocity { get; set; }
    private Coroutine sizeChange = null;
    private bool slideBoost = true;

    private float jumpBufferTime = 0;


    private void OnEnable()
    {
        ResetGroundCollisions();
        ResetWallCollisions();

        hfsm       = new(this);
        Grounded   = new(this);
        Falling    = new(this);
        Jumping    = new(this);
        Sliding    = new(this);
        WallRun    = new(this);
        WallJump   = new(this);
        SlideJump  = new(this);

#if UNITY_EDITOR
        DebugFly   = new(this);
        DebugPause = new(this);
#endif

        hfsm.AddTransitions(new()
        {
            // Grounded transitions
            new(Grounded, Falling,   () => !GroundCollision),
            new(Grounded, SlideJump, () => Input.GetKeyDown(KeyCode.Space) && PreviousState == Sliding && hfsm.Duration < coyoteTime),
            new(Grounded, Jumping,   () => Input.GetKeyDown(KeyCode.Space) || jumpBufferTime > 0),
            new(Grounded, Sliding,   () => Input.GetKey(KeyCode.LeftControl)),
                                     
            // Jumping transitions   
            new(Jumping, Falling,    () => rb.velocity.y < 0 && hfsm.Duration >= minJumpTime),
            new(Jumping, Grounded,   () => GroundCollision && hfsm.Duration >= minJumpTime),
            new(Jumping, WallRun,    () => WallCollision && AllowWallRun && hfsm.Duration >= minJumpTime),

            // Falling transitions   
            new(Falling, SlideJump,  () => Input.GetKeyDown(KeyCode.Space) && PreviousState == Sliding  && hfsm.Duration < coyoteTime),
            new(Falling, Jumping,    () => Input.GetKeyDown(KeyCode.Space) && PreviousState == Grounded && hfsm.Duration < coyoteTime),
            new(Falling, Sliding,    () => GroundCollision && Input.GetKey(KeyCode.LeftControl)),
            new(Falling, Grounded,   () => GroundCollision && !Input.GetKey(KeyCode.LeftControl)),
            new(Falling, WallRun,    () => WallCollision && !GroundCollision && AllowWallRun),
            new(Falling, WallJump,   () => WallCollision && (Input.GetKeyDown(KeyCode.Space) || jumpBufferTime > 0)),

            // Sliding transitions   
            new(Sliding, SlideJump,  () => Input.GetKeyDown(KeyCode.Space) || jumpBufferTime > 0),
            new(Sliding, Falling,    () => !GroundCollision),
            new(Sliding, Grounded,   () => !Input.GetKey(KeyCode.LeftControl) && GroundCollision),

            // Slide jump transitions
            new(SlideJump, Falling,  () => (rb.velocity.y < 0 && hfsm.Duration > 0) || (!Input.GetKey(KeyCode.LeftControl))),
            new(SlideJump, Grounded, () => GroundCollision && hfsm.Duration >= minJumpTime),

            // Wall run transitions
            new(WallRun, WallJump,   () => Input.GetKeyDown(KeyCode.Space) || jumpBufferTime > 0),
            new(WallRun, Falling,    () => !WallCollision && !GroundCollision),
            new(WallRun, Falling,    () => !AllowWallRun),
            new(WallRun, Grounded,   () => GroundCollision),

            // Wall jump transitions
            new(WallJump, Falling,   () => hfsm.Duration >= wallJumpTime),
            new(WallJump, Grounded,  () => GroundCollision && hfsm.Duration >= wallJumpTime),
            new(WallJump, WallRun,   () => WallCollision && AllowWallRun && hfsm.Duration >= wallJumpTime),

#if UNITY_EDITOR
            new(null,     DebugFly,  () => Input.GetKeyDown(KeyCode.F)),
            new(DebugFly, Falling,   () => Input.GetKeyDown(KeyCode.F)),

            new(null, DebugPause,    () => Input.GetKeyDown(KeyCode.G)),
            new(DebugPause, Falling, () => Input.GetKeyDown(KeyCode.G)),
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
        WallCollisions();

        hfsm.FixedUpdate();

        if (!GroundCollision) rb.velocity -= new Vector3(0, gravity, 0) * Time.fixedDeltaTime;
    }

    private void Move(bool maintainMomentum = true)
    {
        float increasedValue = maintainMomentum
            ? Mathf.Max(moveSpeed, Mathf.Abs(new Vector2(rb.velocity.x, rb.velocity.z).magnitude)) 
            : moveSpeed;

        float speedChange = playerInput.IsInputting
            ? acceleration
            : deceleration;

        DesiredVelocity = Vector2.MoveTowards(DesiredVelocity, new Vector2(ViewPositionNoY.x, ViewPositionNoY.z) * increasedValue, speedChange * Time.fixedDeltaTime);

        Vector3 setVelocity = new (DesiredVelocity.x, rb.velocity.y, DesiredVelocity.y);

        if (hfsm.CurrentState == Grounded && SlopeCollision)
        {
            setVelocity.y = 0;
            setVelocity   = Quaternion.FromToRotation(Vector3.up, GroundNormal) * setVelocity;
        }

        setVelocity.y = Mathf.Clamp(setVelocity.y, maxFallSpeed, Mathf.Infinity);
        rb.velocity   = setVelocity;
    }

    private void GroundCollisions()
    {
        float castDist = (CurrentState == Grounded ? groundCastDist : fallingCastDist) * Size / 2.0f;

        if (Physics.SphereCast(rb.transform.position, groundCastRad, Vector3.down, out RaycastHit interpolated, castDist, layers))
        {
            Vector3 dir = interpolated.point - rb.transform.position;

            // SphereCast interpolates the floor normal, resulting in a jitter when relying soly on it. So, to fix this I use 2 raycasts
            // 1. Is used to derive the nonInterpolated floor normal
            // 2. Is to ensure that the interpolated floor normal is used on sharp ground changes (makes it so much smoother)
            if (Physics.Raycast(rb.transform.position, dir, out RaycastHit nonInterpolated, dir.magnitude + raycastMargin, layers)) {
                // FUCKKKKK!!!!
                float nonInterpolatedAngle = Vector3.Angle(Vector3.up, nonInterpolated.normal);
                if (nonInterpolatedAngle >= 90) return;

                Vector3 normal = nonInterpolated.normal;
                Vector3 vel = new Vector3(rb.velocity.normalized.x, 0.5f, rb.velocity.normalized.z).normalized * (collider.radius / 2.0f);
                Vector3 pos = rb.transform.position + vel - (Vector3.up * (Size / 2.0f));

                // Fix this to do a proper interpolation :)
                bool NormalFix = Physics.Raycast(pos, Vector3.down, out RaycastHit floorCheck, interpolateNormalCheckDist, layers);
                float fixAngle = Vector3.Angle(Vector3.up, floorCheck.normal);

                // This is so gross dude :(
                if (NormalFix && (fixAngle == 0 || nonInterpolatedAngle == 0) && fixAngle != nonInterpolatedAngle && rb.velocity.y <= 0)
                {
                    normal = floorCheck.normal;
                }

                NonInterpolatedGroundNormal = normal;
                GroundNormal = Vector3.Slerp(GroundNormal, normal, Time.fixedDeltaTime * interpolateNormalSpeed);
                GroundPoint  = nonInterpolated.point;
            }
            else
            {
                GroundPoint  = interpolated.point;
                GroundNormal = Vector3.Slerp(GroundNormal, interpolated.normal, Time.fixedDeltaTime * interpolateNormalSpeed);
                NonInterpolatedGroundNormal = interpolated.normal;
            }

            float angle = Vector3.Angle(Vector3.up, interpolated.normal);
            GroundCollision = true;
            SlopeCollision  = angle > 0 && angle < 90;
            return;
        }

        ResetGroundCollisions();
    }

    private void WallCollisions()
    {
        float P2 = Mathf.PI * 2 / wallCastIncrements;

        for (int i = 0; i < wallCastIncrements; ++i)
        {
            Vector3 dir = new Vector3(Mathf.Cos(P2 * i), 0, Mathf.Sin(P2 * i)).normalized;

            if (Physics.Raycast(rb.position, dir, out RaycastHit hit, wallCastDistance, layers))
            {
                WallCollision  = true;
                WallNormal     = hit.normal;
                WallPoint      = hit.point;
                return;
            }
        }

        ResetWallCollisions();
    }

    private void ResetGroundCollisions()
    {
        SlopeCollision = GroundCollision = false;
        GroundNormal = Vector3.up;
    }

    private void ResetWallCollisions()
    {
        WallNormal = Vector3.up;
        WallCollision = false;
    }

    private void ChangeSize(float endSize)
    {
        if (sizeChange != null) StopCoroutine(sizeChange);

        sizeChange = StartCoroutine(ChangeSizeCoroutine(endSize));
    }

    private IEnumerator ChangeSizeCoroutine(float endSize)
    {
        while (Mathf.Abs(Size - endSize) > 0.1f) {
            Size = Mathf.MoveTowards(Size, endSize, Time.fixedDeltaTime * crouchTime);
            yield return null;
        }

        Size = endSize;
    }

    private class GroundedState : State<PlayerController>
    {
        private Vector3 stickVel;
        public GroundedState(PlayerController context) : base(context) { }

        public override void Enter() {
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                context.ChangeSize(context.standardSize);
                if (context.PreviousState == context.Falling) context.playerCamera.JumpBob();
            }

            context.slideBoost = true;
            stickVel = Vector3.zero;
        }

        public override void FixedUpdate() {
            GroundStick();
            context.Move(context.hfsm.Duration < context.groundMomentumConserveTime);
        }

        private void GroundStick() {
            float halfSize = context.Size / 2.0f;
            float yPos     = context.Position.y - halfSize - context.GroundPoint.y;
            float yCheck   = context.floorStickThreshold;

            // Floor sticking/Ground correction
            if (yPos > yCheck)
            {
                context.Position = Vector3.SmoothDamp(context.Position, new Vector3(context.Position.x, context.GroundPoint.y + halfSize, context.Position.z), ref stickVel, context.groundStickSpeed, Mathf.Infinity, Time.fixedDeltaTime);
            }

            context.rb.velocity = new Vector3(context.rb.velocity.x, 0, context.rb.velocity.z);
        }
    }

    private class FallingState : State<PlayerController>
    {
        public FallingState(PlayerController context) : base(context) { }

        public override void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) context.jumpBufferTime = context.jumpBuffer;
        }

        public override void FixedUpdate() => context.Move(true);
    }

    private class JumpingState : State<PlayerController>
    {
        public JumpingState(PlayerController context) : base(context) { }

        public override void Enter() {
            context.ResetGroundCollisions();
            context.jumpBufferTime = 0;
            context.rb.velocity = new Vector3(context.rb.velocity.x, context.jumpForce, context.rb.velocity.z);
        }

        public override void FixedUpdate() => context.Move(true);
    }

    private class SlidingState : State<PlayerController>
    {
        public SlidingState(PlayerController context) : base(context) { }
        private Vector3 momentum;
        private float increasedValue;

        public override void Enter() {
            if (context.PreviousState == context.Falling) context.playerCamera.JumpBob();

            if (context.slideBoost) {
                context.playerCamera.FOVPulse();

                Vector3 dir         = (context.ViewPositionNoY * context.slideForce) + (context.gravity * Time.fixedDeltaTime * Vector3.down);
                momentum            = Vector3.ProjectOnPlane(dir, context.NonInterpolatedGroundNormal);
                context.rb.velocity = momentum;
            }
            else {
                momentum = context.rb.velocity;
                if (!context.SlopeCollision) momentum.y = 0;
            }

            context.DesiredVelocity = new Vector2(momentum.x, momentum.z);
            context.slideBoost      = false;
            increasedValue          = 1;

            context.ChangeSize(context.crouchSize);
        }

        public override void FixedUpdate() {
            Vector3 desiredVelocity;

            // Gaining momentum
            if (context.SlopeCollision) {
                // Get the gravity, angle, and current momentumDirection
                Vector3 slopeGravity       = Vector3.ProjectOnPlane(Vector3.down * (context.gravity + increasedValue), context.NonInterpolatedGroundNormal);
                Vector3 momentumDirection  = momentum.normalized;
                float angle                = Vector3.Angle(Vector3.up, context.GroundNormal);
                float newMomentumMagnitude = Mathf.Max(1, momentum.magnitude - angle);
                
                // Ez
                desiredVelocity = momentumDirection * newMomentumMagnitude + slopeGravity;
                increasedValue  = Mathf.Abs(new Vector2(context.rb.velocity.x, context.rb.velocity.z).magnitude) < context.moveSpeed 
                    ? 1
                    : increasedValue + (context.slideSlopeMomentumGain * (angle / 90) * Time.fixedDeltaTime);
            }
            else {
                desiredVelocity  = context.ViewPositionNoY;
                increasedValue   = 1;
            }

            // Move towards gathered velocity
            momentum = Vector3.MoveTowards(momentum, desiredVelocity, Time.fixedDeltaTime * context.slideAcceleration);

            if (context.ViewPositionNoY != Vector3.zero) {
                Vector3 desiredDirection = Vector3.ProjectOnPlane(context.ViewPositionNoY, context.NonInterpolatedGroundNormal).normalized;
                Vector3 slopeDirection   = Vector3.ProjectOnPlane(Vector3.down, context.NonInterpolatedGroundNormal).normalized;

                // Ensure the rotated momentum aligns with the downhill direction
                if (Vector3.Dot(desiredDirection, slopeDirection) >= context.slideDotMin) {
                    Vector3 currentMomentum = Vector3.ProjectOnPlane(momentum, context.NonInterpolatedGroundNormal).normalized;

                    // Rotate the momentum towards the desired direction
                    Quaternion rotation     = Quaternion.FromToRotation(currentMomentum, desiredDirection);
                    Quaternion interpolated = Quaternion.Slerp(Quaternion.identity, rotation, Time.fixedDeltaTime * context.slideRotationSpeed);
                    momentum = interpolated * momentum;

                    if (Vector3.Dot(momentum, slopeDirection) <= 0) momentum = Vector3.ProjectOnPlane(momentum, context.NonInterpolatedGroundNormal);
                }
            }

            // Setting values
            context.rb.velocity     = momentum;
            context.DesiredVelocity = new Vector2(context.rb.velocity.x, context.rb.velocity.z);
        }
    }

    private class SlideJumping : State<PlayerController>
    {
        public SlideJumping(PlayerController context) : base(context) { }

        public override void Enter() {
            context.ResetGroundCollisions();
            context.jumpBufferTime = 0;
            context.slideBoost     = false;
            context.rb.velocity    = new Vector3(context.rb.velocity.x, context.slideJumpForce, context.rb.velocity.z);
        }

        public override void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) context.jumpBufferTime = context.jumpBuffer;
        }

        public override void FixedUpdate() => context.Move(true);
    }

    private class WallRunning : State<PlayerController>
    {
        private Vector3 prevForward;
        private Quaternion targetRotation;
        public WallRunning(PlayerController context) : base(context) { }

        public override void Enter() {
            float tempY             = Mathf.Max(context.rb.velocity.y, 0);
            context.rb.velocity     = GetProjected();
            context.rb.velocity     = new Vector3(context.rb.velocity.x, tempY, context.rb.velocity.z);
            context.DesiredVelocity = new Vector2(context.rb.velocity.x, context.rb.velocity.z);

            prevForward    = context.WallNormal;
            targetRotation = context.rb.transform.localRotation;
        }

        public override void Update() {

            if (context.playerInput.MousePosition == Vector2.zero)
            {
                context.rb.MoveRotation(Quaternion.Slerp(
                    context.rb.transform.localRotation,
                    targetRotation,
                    Time.deltaTime * context.wallRunRotationSpeed
                ));

                if (prevForward != context.WallNormal)
                {
                    float angle              = Vector3.SignedAngle(prevForward, context.WallNormal, Vector3.up);
                    Quaternion angleRotation = Quaternion.Euler(0, angle, 0);

                    targetRotation = context.rb.transform.localRotation * angleRotation;
                    prevForward    = context.WallNormal;
                }
            }
            else
            {
                prevForward    = context.WallNormal;
                targetRotation = context.rb.transform.localRotation;
            }
        }

        public override void FixedUpdate() {
            Vector3 projected   = GetProjected();

            float downwardForce = context.rb.velocity.y < 0 
                ? context.gravity * Time.fixedDeltaTime * context.wallRunGravityReduction
                : 0;

            context.DesiredVelocity = new Vector2(projected.x, projected.z);
            context.rb.velocity     = new Vector3(context.DesiredVelocity.x, context.rb.velocity.y + downwardForce, context.DesiredVelocity.y) - (context.WallNormal * Time.fixedDeltaTime);
        }

        Vector3 GetProjected() {
            return Vector3.ProjectOnPlane(context.rb.velocity, context.WallNormal).normalized * Mathf.Max(context.wallRunSpeed, Mathf.Abs(context.rb.velocity.magnitude));
        }
    }

    private class WallJumping : State<PlayerController>
    {
        public WallJumping(PlayerController context) : base(context) { }

        public override void Enter() {
            Vector3 dir    = context.playerCamera.CameraForwardNoY;

            if (Vector3.Dot(dir, context.WallNormal) <= context.wallJumpMinDot) dir = (context.playerCamera.CameraForwardNoY + context.WallNormal).normalized;

            Vector3 vel    = Quaternion.FromToRotation(context.rb.velocity.normalized, dir) * context.rb.velocity.normalized;
            Vector2 noYVel = new(context.rb.velocity.x, context.rb.velocity.z);

            vel *= Mathf.Abs(noYVel.magnitude) < context.wallJumpForce 
                ? context.wallJumpForce
                : noYVel.magnitude;

            vel += Vector3.up * context.wallJumpHeight;

            context.rb.velocity     = vel;
            context.DesiredVelocity = new Vector2(vel.x, vel.z);
        }
    }

    #region Debugging

#if UNITY_EDITOR

    [Header("Debugging")]
    [SerializeField] private float DebugFlySpeed = 10;
    [SerializeField] private float scrollWheelSpeed;
    private bool DebugMode = false;
    private DebugFlyingState DebugFly { get; set; }
    private DebugPauseState DebugPause { get; set; }

    private void OnGUI() {
        hfsm.OnGUI();

        GUILayout.BeginArea(new Rect(10, 150, 800, 200));

        string current = $"Current Velocity: { rb.velocity }\nCurrent Magnitude: { rb.velocity.magnitude }";
        GUILayout.Label($"<size=15>{current}</size>");
        GUILayout.EndArea();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rb.transform.position + new Vector3(0, -groundCastDist * Size / 2, 0), groundCastRad);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(rb.transform.position + new Vector3(0, -fallingCastDist * Size / 2, 0), groundCastRad);

        if (wallCastIncrements <= 0) return;

        Gizmos.color = Color.green;
        float P2 = Mathf.PI * 2 / wallCastIncrements;
        for (float i = 0; i < wallCastIncrements; i++) Gizmos.DrawRay(rb.position, new Vector3(Mathf.Cos(P2 * i), 0, Mathf.Sin(P2 * i)).normalized * wallCastDistance);
    }

    private class DebugFlyingState : State<PlayerController>
    {
        public DebugFlyingState(PlayerController context) : base(context) { }

        public override void Enter() {
            context.DebugMode       = true;
            context.rb.velocity     = Vector3.zero;
            context.DesiredVelocity = Vector2.zero;
        }

        public override void Update() {
            context.DebugFlySpeed += Input.GetAxisRaw("Mouse ScrollWheel") * context.scrollWheelSpeed;

            Vector3 movePosition = context.ViewPosition;

            if (Input.GetKey(KeyCode.Space))     movePosition.y += 1;
            if (Input.GetKey(KeyCode.LeftShift)) movePosition.y -= 1;

            context.rb.transform.localPosition = Vector3.MoveTowards(context.rb.transform.localPosition, context.rb.transform.localPosition + movePosition, Time.unscaledDeltaTime * context.DebugFlySpeed);
        }

        public override void Exit() {
            context.DebugMode = false;
        }
    }

    private class DebugPauseState : State<PlayerController>
    {
        public DebugPauseState(PlayerController context) : base(context) { }

        public override void Enter() {
            context.DebugMode = true;
            context.rb.velocity     = Vector3.zero;
            context.DesiredVelocity = Vector2.zero;

            Time.timeScale = 0;
        }

        public override void Update() {
            context.DebugFlySpeed += Input.GetAxisRaw("Mouse ScrollWheel") * context.scrollWheelSpeed;

            Vector3 movePosition = context.ViewPosition;

            if (Input.GetKey(KeyCode.Space))     movePosition.y += 1;
            if (Input.GetKey(KeyCode.LeftShift)) movePosition.y -= 1;

            Transform cam     = context.playerCamera.CameraTransform;
            cam.localPosition = Vector3.MoveTowards(cam.localPosition, cam.localPosition + movePosition, Time.unscaledDeltaTime * context.DebugFlySpeed);
        }

        public override void Exit() {
            Time.timeScale    = 1;
            context.DebugMode = false;
            context.playerCamera.CameraTransform.localPosition = Vector3.up * 0.5f;
        }
    }

#endif

    #endregion
}