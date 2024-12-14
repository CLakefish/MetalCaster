using System.Collections.Generic;
using UnityEngine;
using HFSMFramework;
using System;
using System.Collections;

// Made by Carson Lakefish
// Concept recommendations and help by Oliver Beebe

// TODO:
// - GUNS

public class PlayerController : Player.PlayerComponent
{
    [Header("References")]
    [SerializeField] public Rigidbody rb;
    [SerializeField] public CapsuleCollider col;

    [Header("Collisions")]
    [SerializeField] private LayerMask layers;
    [SerializeField] private float groundCastDist         = 0.8f;
    [SerializeField] private float fallingCastDist        = 0.6f;
    [SerializeField] private int wallCastIncrements       = 1;
    [SerializeField] private float wallCastDistance       = 1;
    [SerializeField] private float interpolateNormalSpeed;

    [Header("Walking Parameters")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float crouchSpeed;
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
    [SerializeField] private float slideSlopeMomentumGain; 

    [Header("Slide Jump Parameters")]
    [SerializeField] private float slideJumpForce;

    [Header("Size Change Parameters")]
    [SerializeField] private float crouchSize;
    [SerializeField] private float standardSize;
    [SerializeField] private float crouchTime;

    [Header("Wall Run Parameters")]
    [SerializeField] private float wallRunSpeed;
    [SerializeField] private float wallRunMaxAngle = 80f;
    [SerializeField] private float wallRunRotateGraceTime;
    [SerializeField] private float wallRunGravity;

    [Header("Wall Jump Parameters")]
    [SerializeField] private float wallJumpForce;
    [SerializeField] private float wallJumpHeight;
    [SerializeField] private float wallJumpTime;
    [SerializeField] private float wallJumpCoyoteTime;
    [SerializeField] private float wallJumpMinDot  = 0.25f;
    [SerializeField] private float wallJumpKickDot = -0.8f;

    private readonly float groundCastRad       = 0.40f;
    private readonly float raycastMargin       = 0.1f;
    private readonly float floorStickThreshold = 0.05f;
    private readonly float slideDotMin         = -0.25f;

    private bool GroundCollision                { get; set; }
    private bool SlopeCollision                 { get; set; }
    private Vector3 GroundNormal                { get; set; }
    private Vector3 GroundPoint                 { get; set; }

    private bool WallCollision                  { get; set; }
    private Vector3 WallNormal                  { get; set; }

    private float Size {
        get { 
            return col.height; 
        }
        set {
            float start = col.height;
            float hD    = value - start;

            col.height += hD;
            if (GroundCollision) rb.MovePosition(rb.position + Vector3.up * hD / 2.0f);
        }
    }

    private bool CanUncrouch
    {
        get
        {
            return !Physics.SphereCast(rb.transform.position, col.radius + 0.05f, Vector3.up, out RaycastHit _, 1.1f, layers);
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
            return Mathf.Abs(angle) > wallRunMaxAngle;
        }
    }

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

    private StateMachine<PlayerController> hfsm;

    private GroundedState  Grounded  { get; set; }
    private FallingState   Falling   { get; set; }
    private JumpingState   Jumping   { get; set; }
    private SlidingState   Sliding   { get; set; }
    private SlideJumping   SlideJump { get; set; }
    private WallRunning    WallRun   { get; set; }
    private WallJumping    WallJump  { get; set; }

    private Vector2 DesiredVelocity { get; set; }
    private Coroutine sizeChange = null;
    private bool slideBoost = true;

    private float jumpBufferTime = 0;


    private void OnEnable()
    {
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
            new(Grounded, SlideJump, () => Input.GetKeyDown(KeyCode.Space)   && PreviousState == Sliding && hfsm.Duration < coyoteTime),
            new(Grounded, Jumping,   () => Input.GetKeyDown(KeyCode.Space)   || jumpBufferTime > 0),
            new(Grounded, Sliding,   () => Input.GetKey(KeyCode.LeftControl)),

            // Jumping transitions   
            new(Jumping, Falling,    () => rb.linearVelocity.y <= 0 && hfsm.Duration >= minJumpTime),
            new(Jumping, WallRun,    () => WallCollision && AllowWallRun && hfsm.Duration >= minJumpTime),

            // Falling transitions   
            new(Falling, SlideJump,  () => Input.GetKeyDown(KeyCode.Space)    && PreviousState == Sliding  && hfsm.Duration < coyoteTime),
            new(Falling, Jumping,    () => Input.GetKeyDown(KeyCode.Space)    && PreviousState == Grounded && hfsm.Duration < coyoteTime),
            new(Falling, Sliding,    () => Input.GetKey(KeyCode.LeftControl)  && GroundCollision),
            new(Falling, Grounded,   () => GroundCollision),
            new(Falling, WallRun,    () => WallCollision && !GroundCollision  && AllowWallRun),
            new(Falling, WallJump,   () => Input.GetKeyDown(KeyCode.Space) && WallCollision && !GroundCollision),
            new(Falling, WallJump,   () => Input.GetKeyDown(KeyCode.Space) && PreviousState == WallRun  && hfsm.Duration < wallJumpCoyoteTime),

            // Sliding transitions   
            new(Sliding, SlideJump,  () => Input.GetKeyDown(KeyCode.Space) || jumpBufferTime > 0),
            new(Sliding, Falling,    () => !GroundCollision && !SlopeCollision),
            new(Sliding, Grounded,   () => !Input.GetKey(KeyCode.LeftControl) && GroundCollision),

            // Slide jump transitions
            new(SlideJump, Falling,  () => (!Input.GetKey(KeyCode.LeftControl)) || (rb.linearVelocity.y < 0 && hfsm.Duration > 0)),
            new(SlideJump, Grounded, () => !Input.GetKey(KeyCode.LeftControl) && GroundCollision && hfsm.Duration >= minJumpTime),
            new(SlideJump, Sliding,  () => Input.GetKey(KeyCode.LeftControl)  && GroundCollision && hfsm.Duration >= minJumpTime),

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

        hfsm.Start(Falling);
    }

    private void Update()
    {
        jumpBufferTime -= Time.deltaTime;

        hfsm.CheckTransitions();
        hfsm.Update();

        ChangeSize(Input.GetKey(KeyCode.LeftControl) || !CanUncrouch ? crouchSize : standardSize);
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

        if (!GroundCollision && CurrentState != WallRun) rb.linearVelocity -= new Vector3(0, gravity, 0) * Time.fixedDeltaTime;
    }

    private void Move(float moveSpeed, bool maintainMomentum = true)
    {
        float increasedValue = maintainMomentum
            ? Mathf.Max(moveSpeed, Mathf.Abs(new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude)) 
            : moveSpeed;

        float speedChange = playerInput.IsInputting
            ? acceleration
            : deceleration;

        DesiredVelocity = Vector2.MoveTowards(DesiredVelocity, new Vector2(ViewPositionNoY.x, ViewPositionNoY.z) * increasedValue, speedChange * Time.fixedDeltaTime);

        Vector3 setVelocity = new (DesiredVelocity.x, rb.linearVelocity.y, DesiredVelocity.y);

        if (SlopeCollision && CurrentState == Grounded) {
            setVelocity.y = 0;
            setVelocity = Quaternion.FromToRotation(Vector3.up, GroundNormal) * setVelocity;
        }

        setVelocity.y = Mathf.Clamp(setVelocity.y, maxFallSpeed, Mathf.Infinity);
        rb.linearVelocity = setVelocity;
    }

    #region Collision_Detection
    private void GroundCollisions()
    {
        float castDist = (CurrentState == Grounded ? groundCastDist : fallingCastDist) * Size / 2.0f;

        if (Physics.SphereCast(rb.transform.position, groundCastRad, Vector3.down, out RaycastHit interpolated, castDist, layers))
        {
            Vector3 dir = (interpolated.point - rb.transform.position).normalized;
            Vector3 desiredNormal = interpolated.normal;

            // SphereCast interpolates the floor normal, resulting in a jitter when relying soly on it. So, to fix this I use 2 raycasts
            // 1. Is used to derive the nonInterpolated floor normal
            // 2. Is to ensure that the interpolated floor normal is used on sharp ground changes (makes it so much smoother)
            if (Physics.Raycast(rb.transform.position, dir, out RaycastHit nonInterpolated, dir.magnitude + raycastMargin, layers)) {
                // FUCKKKKK!!!!
                if (Vector3.Angle(Vector3.up, nonInterpolated.normal) >= 90)
                {
                    desiredNormal = Vector3.up;
                }
            }

            float angle     = Vector3.Angle(Vector3.up, interpolated.normal);
            GroundNormal    = Vector3.MoveTowards(GroundNormal, desiredNormal, Time.fixedDeltaTime * interpolateNormalSpeed);
            GroundPoint     = interpolated.point;
            GroundCollision = true;
            SlopeCollision  = angle > 0 && angle < 90;
            return;
        }

        ResetGroundCollisions();
    }

    private void WallCollisions()
    {
        float P2 = Mathf.PI * 2 / wallCastIncrements;

        Vector3 combined = Vector3.zero;

        for (int i = 0; i < wallCastIncrements; ++i)
        {
            Vector3 dir = new Vector3(Mathf.Cos(P2 * i), 0, Mathf.Sin(P2 * i)).normalized;

            if (Physics.Raycast(rb.position, dir, out RaycastHit hit, wallCastDistance, layers))
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
        WallNormal    = combined.normalized;
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

    public void Launch()
    {
        ResetGroundCollisions();
        ResetWallCollisions();
        hfsm.ChangeState(Falling);
    }

    #endregion

    private void ChangeSize(float endSize) {
        if (sizeChange != null) StopCoroutine(sizeChange);
        sizeChange = StartCoroutine(ChangeSizeCoroutine(endSize));
    }

    private IEnumerator ChangeSizeCoroutine(float endSize) {
        while (Mathf.Abs(Size - endSize) > 0.01f) {
            Size = Mathf.MoveTowards(Size, endSize, Time.fixedDeltaTime * crouchTime);
            yield return new WaitForFixedUpdate();
        }

        Size = endSize;
    }

    #region Standard_States

    private class GroundedState : State<PlayerController>
    {
        private Vector3 stickVel;

        public GroundedState(PlayerController context) : base(context) { }

        public override void Enter() {
            if (!Input.GetKey(KeyCode.LeftControl) && context.PreviousState == context.Falling) {
                context.playerCamera.JumpBob();
            }

            context.slideBoost = true;
            stickVel = Vector3.zero;
        }

        public override void Update() {
            context.playerCamera.ViewTilt();
        }

        public override void FixedUpdate() {
            float halfSize = context.Size / 2.0f;
            float yPos     = context.Position.y - halfSize - context.GroundPoint.y;
            float yCheck   = context.floorStickThreshold;

            Vector3 targetPos = new(context.Position.x, context.GroundPoint.y + halfSize, context.Position.z);

            if (yPos > yCheck && !context.SlopeCollision)
            {
                Vector3 currentPos = context.rb.position;
                Vector3 smoothPos  = Vector3.SmoothDamp(currentPos, targetPos, ref stickVel, context.groundStickSpeed, Mathf.Infinity, Time.fixedDeltaTime);
                context.rb.MovePosition(smoothPos);
            }

            context.rb.linearVelocity = new Vector3(context.rb.linearVelocity.x, 0, context.rb.linearVelocity.z);

            context.Move(context.moveSpeed, context.hfsm.Duration < context.groundMomentumConserveTime);
        }
    }

    private class FallingState : State<PlayerController>
    {
        public FallingState(PlayerController context) : base(context) { }

        public override void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                context.jumpBufferTime = context.jumpBuffer;
            }
        }

        public override void FixedUpdate() => context.Move(context.moveSpeed, true);
    }

    private class JumpingState : State<PlayerController>
    {
        public JumpingState(PlayerController context) : base(context) { }

        public override void Enter() {
            context.ResetGroundCollisions();
            context.jumpBufferTime = 0;
            context.rb.linearVelocity = new Vector3(context.rb.linearVelocity.x, context.jumpForce, context.rb.linearVelocity.z);
        }

        public override void FixedUpdate() => context.Move(context.moveSpeed, true);
    }

    #endregion

    #region Sliding_States

    private class SlidingState : State<PlayerController>
    {
        private Vector3 momentum;

        public SlidingState(PlayerController context) : base(context) { }

        public override void Enter() {
            momentum = context.rb.linearVelocity;

            if (context.slideBoost) {
                context.playerCamera.FOVPulse();

                if (context.SlopeCollision) {
                    Vector3 dir = (context.ViewPositionNoY * context.slideForce) + (context.gravity * Time.fixedDeltaTime * Vector3.down);
                    momentum    = Quaternion.FromToRotation(Vector3.up, context.GroundNormal) * dir;
                }
                else {
                    momentum = context.ViewPositionNoY * context.slideForce;
                }
            }

            context.DesiredVelocity = new Vector2(momentum.x, momentum.z);
            context.slideBoost      = false;
        }

        public override void Update() => context.playerCamera.SlideRotate(momentum);

        public override void FixedUpdate() {
            Vector3 desiredVelocity;

            // Gaining momentum
            if (context.SlopeCollision) {
                // Get the gravity, angle, and current momentumDirection
                Vector3 slopeGravity  = Vector3.ProjectOnPlane(Vector3.down * context.gravity, context.GroundNormal);
                float normalizedAngle = Vector3.Angle(Vector3.up, context.GroundNormal) / 90.0f;

                // Ez
                Vector3 adjusted = slopeGravity * (1.0f + normalizedAngle);
                desiredVelocity  = momentum + adjusted;
            }
            else {
                desiredVelocity  = new Vector3(context.ViewPositionNoY.x, momentum.y, context.ViewPositionNoY.z);
            }

            // Move towards gathered velocity (y is seperated so I can tinker with it more, it can be simplified ofc)
            Vector3 slideInterpolated = new(
                Mathf.MoveTowards(momentum.x, desiredVelocity.x, Time.fixedDeltaTime * context.slideAcceleration),
                Mathf.MoveTowards(momentum.y, desiredVelocity.y, Time.fixedDeltaTime * context.slideSlopeMomentumGain),
                Mathf.MoveTowards(momentum.z, desiredVelocity.z, Time.fixedDeltaTime * context.slideAcceleration));

            momentum = slideInterpolated;
            
            if (context.ViewPositionNoY != Vector3.zero) {
                Vector3 desiredDirection = Vector3.ProjectOnPlane(context.ViewPositionNoY, context.GroundNormal).normalized;
                Vector3 slopeDirection   = Vector3.ProjectOnPlane(Vector3.down, context.GroundNormal).normalized;

                // Ensure the rotated momentum aligns with the downhill direction
                if (Vector3.Dot(desiredDirection, slopeDirection) >= context.slideDotMin) {
                    Vector3 currentMomentum = context.SlopeCollision ? Vector3.ProjectOnPlane(momentum, context.GroundNormal).normalized : momentum;

                    // Rotate the momentum towards the desired direction
                    Quaternion rotation     = Quaternion.FromToRotation(currentMomentum, desiredDirection);
                    Quaternion interpolated = Quaternion.Slerp(Quaternion.identity, rotation, Time.fixedDeltaTime * context.slideRotationSpeed);

                    momentum = interpolated * momentum;

                    if (Vector3.Dot(momentum, slopeDirection) <= 0 && context.SlopeCollision) {
                        momentum = Vector3.ProjectOnPlane(momentum, context.GroundNormal);
                    }
                }
            }

            context.rb.linearVelocity     = momentum;
            context.DesiredVelocity = new Vector2(momentum.x, momentum.z);
        }
    }

    private class SlideJumping : State<PlayerController>
    {
        public SlideJumping(PlayerController context) : base(context) { }

        public override void Enter() {
            context.ResetGroundCollisions();
            context.jumpBufferTime = 0;
            context.slideBoost     = false;
            context.rb.linearVelocity    = new Vector3(context.rb.linearVelocity.x, context.slideJumpForce, context.rb.linearVelocity.z);
        }

        public override void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                context.jumpBufferTime = context.jumpBuffer;
            }
        }

        public override void FixedUpdate() => context.Move(context.moveSpeed, true);
    }

    #endregion

    #region Wall_States

    private class WallRunning : State<PlayerController>
    {
        private Vector3 prevForward;
        private float targetY;
        private float rotateVel;
        public WallRunning(PlayerController context) : base(context) { }

        public override void Enter() {
            Vector3 temp = new(context.rb.linearVelocity.x, 0, context.rb.linearVelocity.z);
            float tempY  = Mathf.Max(context.rb.linearVelocity.y, 0);

            if (temp.magnitude == 0) {
                temp = context.WallNormal;
            }
            else {
                temp = Quaternion.FromToRotation(Vector3.up, context.WallNormal) * temp;
            }

            context.rb.linearVelocity     = new Vector3(temp.x, tempY, temp.z);
            context.DesiredVelocity = new Vector2(context.rb.linearVelocity.x, context.rb.linearVelocity.z);

            prevForward = context.WallNormal;
            targetY     = context.rb.transform.localEulerAngles.y;
            rotateVel   = 0;
        }

        public override void Update() {

            if (context.playerInput.MousePosition == Vector2.zero && context.hfsm.Duration > context.wallRunRotateGraceTime) {
                float y = Mathf.SmoothDampAngle(context.rb.transform.localEulerAngles.y, targetY, ref rotateVel, context.playerCamera.wallRunRotationSpeed);

                context.rb.MoveRotation(Quaternion.Euler(new Vector3(0, y, 0)));

                if (prevForward != context.WallNormal) {
                    float angle = Vector3.SignedAngle(prevForward, context.WallNormal, Vector3.up);
                    targetY += angle;
                }
            }
            else {
                targetY = context.rb.transform.localEulerAngles.y;
            }

            prevForward = context.WallNormal;

            context.playerCamera.WallRunRotate(context.WallNormal);
        }

        public override void FixedUpdate() {
            Vector3 projected = GetProjected();

            if (new Vector3(projected.x, 0, projected.z).magnitude == 0) return;

            float downwardForce = context.rb.linearVelocity.y > 0 
                ? context.rb.linearVelocity.y - (context.gravity * Time.fixedDeltaTime)
                : context.rb.linearVelocity.y - (context.wallRunGravity * Time.fixedDeltaTime);

            context.DesiredVelocity = new Vector2(projected.x, projected.z);
            context.rb.linearVelocity     = new Vector3(context.DesiredVelocity.x, downwardForce, context.DesiredVelocity.y);
        }

        Vector3 GetProjected() {
            return Vector3.ProjectOnPlane(context.rb.linearVelocity, context.WallNormal).normalized * Mathf.Max(context.wallRunSpeed, context.rb.linearVelocity.magnitude);
        }
    }

    private class WallJumping : State<PlayerController>
    {
        public WallJumping(PlayerController context) : base(context) { }

        public override void Enter() {
            Vector3 dir = context.playerCamera.CameraForwardNoY;
            float dot   = Vector3.Dot(dir, context.WallNormal);

            if (dot <= context.wallJumpMinDot)  dir = (context.playerCamera.CameraForwardNoY + context.WallNormal).normalized;
            if (dot <= context.wallJumpKickDot) dir = context.WallNormal;

            Vector3 vel    = Quaternion.FromToRotation(context.rb.linearVelocity.normalized, dir) * context.rb.linearVelocity.normalized;
            Vector2 noYVel = new(context.rb.linearVelocity.x, context.rb.linearVelocity.z);

            vel *= Mathf.Abs(noYVel.magnitude) < context.wallJumpForce 
                ? context.wallJumpForce
                : noYVel.magnitude;

            vel += Vector3.up * context.wallJumpHeight;

            context.rb.linearVelocity     = vel;
            context.DesiredVelocity = new Vector2(vel.x, vel.z);
        }

        public override void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                context.jumpBufferTime = context.jumpBuffer;
            }
        }
    }

    #endregion

    #region Debugging_States

//#if UNITY_EDITOR

    [Header("Debugging")]
    [SerializeField] private float DebugFlySpeed = 10;
    [SerializeField] private float scrollWheelSpeed;
    private bool DebugMode = false;
    private DebugFlyingState DebugFly { get; set; }
    private DebugPauseState DebugPause { get; set; }

    private void OnGUI() {
        hfsm.OnGUI();

        GUILayout.BeginArea(new Rect(10, 150, 800, 200));

        string current = $"Current Velocity: { rb.linearVelocity }\nCurrent Magnitude: { rb.linearVelocity.magnitude }";
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
            context.rb.linearVelocity     = Vector3.zero;
            context.DesiredVelocity = Vector2.zero;
        }

        public override void Update() {
            context.DebugFlySpeed += Input.GetAxisRaw("Mouse ScrollWheel") * context.scrollWheelSpeed;

            Vector3 movePosition = context.ViewPosition;

            if (Input.GetKey(KeyCode.Space))     movePosition.y += 1;
            if (Input.GetKey(KeyCode.LeftShift)) movePosition.y -= 1;

            context.rb.MovePosition(context.rb.position + (context.DebugFlySpeed * Time.unscaledDeltaTime * movePosition));
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
            context.rb.linearVelocity     = Vector3.zero;
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

//#endif

    #endregion
}