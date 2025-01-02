using HFSMFramework;
using System.Collections;
using UnityEngine;

// Made by Carson Lakefish
// Concept recommendations and help by Oliver Beebe

// TODO:
// - Better modification menu rotation (Do Tomorrow)
// - Save modifications in the slots they were applied in (Do Tomorrow)
// - Pause Menu (Do Tomorrow if Time)
// - Weapon Swap Menu (Do Tuesday)
// - Modifications :)
// - Enemies :)
// - 


public class PlayerController : Player.PlayerComponent
{
    [Header("Collisions")]
    [SerializeField] private float groundCastDist;
    [SerializeField] private float fallingCastDist;
    [SerializeField] private int   wallCastIncrements;
    [SerializeField] private float wallCastDistance;
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
    [SerializeField] private float slideStickSpeed;

    [Header("Slide Jump Parameters")]
    [SerializeField] private float slideJumpForce;

    [Header("Size Change Parameters")]
    [SerializeField] private float crouchSize;
    [SerializeField] private float standardSize;
    [SerializeField] private float crouchTime;

    [Header("Wall Run Parameters")]
    [SerializeField] private float wallRunSpeed;
    [SerializeField] private float wallRunMaxAngle;
    [SerializeField] private float wallRunRotateGraceTime;
    [SerializeField] private float wallRunAngleChangeMax;
    [SerializeField] private float wallRunGravity;

    [Header("Wall Jump Parameters")]
    [SerializeField] private float wallJumpForce;
    [SerializeField] private float wallJumpHeight;
    [SerializeField] private float wallJumpTime;
    [SerializeField] private float wallJumpCoyoteTime;
    [SerializeField] private float wallJumpMinDot;
    [SerializeField] private float wallJumpKickDot;

    private readonly float groundCastRad       = 0.4f;
    private readonly float interpDeviation     = 10;
    private readonly float raycastMargin       = 0.1f;
    private readonly float floorStickThreshold = 0.05f;
    private readonly float slideDotMin         = -0.25f;

    private bool GroundCollision                { get; set; }
    private bool SlopeCollision                 { get; set; }
    private Vector3 GroundNormal                { get; set; }
    private Vector3 GroundPoint                 { get; set; }

    private bool WallCollision                  { get; set; }
    private Vector3 WallNormal                  { get; set; }


    private StateMachine<PlayerController> hfsm;

    private GroundedState Grounded { get; set; }
    private FallingState Falling   { get; set; }
    private JumpingState Jumping   { get; set; }
    private SlidingState Sliding   { get; set; }
    private SlideJumping SlideJump { get; set; }
    private WallRunning WallRun    { get; set; }
    private WallJumping WallJump   { get; set; }

    private float Size {
        get { 
            return CapsuleCollider.height; 
        }
        set {
            float start = CapsuleCollider.height;
            float hD    = value - start;

            CapsuleCollider.height += hD;
            if (GroundCollision) rb.MovePosition(rb.position + Vector3.up * hD / 2.0f);
        }
    }

    private bool CanUncrouch {
        get {
            return !Physics.SphereCast(rb.transform.position, CapsuleCollider.radius + 0.05f, Vector3.up, out RaycastHit _, 1.1f, GroundLayer);
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

    private Vector3 HorizontalVelocity {
        get {
            return new Vector2(rb.linearVelocity.x, rb.linearVelocity.z);
        }
    }

    private Vector3 ViewPosition {
        get {
            return PlayerInput.IsInputting
            ? (PlayerCamera.CameraForward * PlayerInput.NormalizedInput.y + PlayerCamera.CameraRight * PlayerInput.NormalizedInput.x).normalized
            : Vector3.zero;
        }
    }

    private Vector3 ViewPositionNoY {
        get {
            return PlayerInput.IsInputting
            ? (PlayerCamera.CameraForwardNoY * PlayerInput.NormalizedInput.y + PlayerCamera.CameraRight * PlayerInput.NormalizedInput.x).normalized
            : Vector3.zero;
        }
    }

    private bool AllowWallRun {
        get {
            float angle = Vector3.SignedAngle(WallNormal, ViewPositionNoY, Vector3.up);
            bool isWall = Vector3.Angle(Vector3.up, WallNormal) >= 89.0f;
            return Mathf.Abs(angle) > wallRunMaxAngle && isWall;
        }
    }

    private IState CurrentState {
        get {
            return hfsm.CurrentState;
        }
    }

    private IState PreviousState  {
        get {
            return hfsm.PreviousState;
        }
    }

    private float PreviousDuration {
        get {
            return previousDuration;
        }
    }

    private Vector2 DesiredHorizontalVelocity { get; set; }
    private Coroutine sizeChange = null;
    private bool slideBoost = true;

    private float previousDuration = 0;
    private float jumpBufferTime   = 0;


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
            new(Grounded, SlideJump, () => jumpBufferTime > 0 && PreviousState == Sliding),
            new(Grounded, Jumping,   () => PlayerInput.Jump || jumpBufferTime > 0),
            new(Grounded, Sliding,   () => PlayerInput.Slide),

            // Jumping transitions   
            new(Jumping, Falling,    () => rb.linearVelocity.y <= 0 && hfsm.Duration >= minJumpTime),
            new(Jumping, Grounded,   () => hfsm.Duration >= minJumpTime && GroundCollision),
            new(Jumping, WallRun,    () => WallCollision && AllowWallRun && hfsm.Duration >= minJumpTime),

            // Falling transitions   
            new(Falling, Jumping,    () => PlayerInput.Jump && PreviousState == Grounded && hfsm.Duration < coyoteTime),
            new(Falling, SlideJump,  () => PlayerInput.Jump && PreviousState == Sliding && hfsm.Duration < coyoteTime),
            new(Falling, WallJump,   () => (PlayerInput.Jump || jumpBufferTime > 0) && WallCollision && !GroundCollision),
            new(Falling, WallJump,   () => (PlayerInput.Jump || jumpBufferTime > 0) && PreviousState == WallRun && hfsm.Duration < wallJumpCoyoteTime),

            new(Falling, Grounded,   () => !PlayerInput.Slide && GroundCollision),
            new(Falling, Sliding,    () => PlayerInput.Slide  && GroundCollision),
            new(Falling, WallRun,    () => WallCollision      && !GroundCollision && AllowWallRun),

            // Sliding transitions   
            new(Sliding, SlideJump,  () => (PlayerInput.Jump || jumpBufferTime > 0) && GroundCollision),
            new(Sliding, Falling,    () => !GroundCollision   && !SlopeCollision),
            new(Sliding, Grounded,   () => !PlayerInput.Slide && GroundCollision),

            // Slide jump transitions
            new(SlideJump, Falling,  () => ((!PlayerInput.Slide) || (rb.linearVelocity.y < 0 && hfsm.Duration > 0)) && hfsm.Duration > minJumpTime),
            new(SlideJump, Grounded, () => !PlayerInput.Slide && GroundCollision && hfsm.Duration >= minJumpTime),
            new(SlideJump, Sliding,  () => PlayerInput.Slide  && GroundCollision && hfsm.Duration >= minJumpTime),

            // Wall run transitions
            new(WallRun, WallJump,   () => PlayerInput.Jump || jumpBufferTime > 0),
            new(WallRun, Falling,    () => !WallCollision   && !GroundCollision),
            new(WallRun, Falling,    () => !AllowWallRun),
            new(WallRun, Grounded,   () => GroundCollision),

            // Wall jump transitions
            new(WallJump, Falling,   () => hfsm.Duration >= wallJumpTime),
            new(WallJump, Grounded,  () => GroundCollision && hfsm.Duration >= wallJumpTime),
            new(WallJump, WallRun,   () => WallCollision   && AllowWallRun && hfsm.Duration >= wallJumpTime),

#if UNITY_EDITOR
            new(null,     DebugFly,  () => Input.GetKeyDown(KeyCode.F)),
            new(DebugFly, Falling,   () => Input.GetKeyDown(KeyCode.F)),

            new(null, DebugPause,    () => Input.GetKeyDown(KeyCode.G)),
            new(DebugPause, Falling, () => Input.GetKeyDown(KeyCode.G)),
#endif
        });

        hfsm.AddOnChange(new()
        {  
            () => previousDuration = hfsm.Duration,
        });

        hfsm.Start(Falling);
    }

    private void OnDisable()
    {
        ResetGroundCollisions();
        ResetWallCollisions();
        hfsm.Start(Falling);
    }

    private void Update()
    {
        jumpBufferTime -= Time.deltaTime;

        hfsm.CheckTransitions();
        hfsm.Update();

        ChangeSize(PlayerInput.Slide || !CanUncrouch ? crouchSize : standardSize);

        if (PlayerInput.Mouse.Right.Pressed) {
            PlayerHealth.Damage(10);
        }
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
            ? Mathf.Max(moveSpeed, Mathf.Abs(HorizontalVelocity.magnitude)) 
            : moveSpeed;

        float speedChange = PlayerInput.IsInputting
            ? acceleration
            : deceleration;

        DesiredHorizontalVelocity = Vector2.MoveTowards(DesiredHorizontalVelocity, new Vector2(ViewPositionNoY.x, ViewPositionNoY.z) * increasedValue, speedChange * Time.fixedDeltaTime);

        Vector3 setVelocity = new (DesiredHorizontalVelocity.x, rb.linearVelocity.y, DesiredHorizontalVelocity.y);

        if (SlopeCollision && CurrentState == Grounded) {
            setVelocity = Quaternion.FromToRotation(Vector3.up, GroundNormal) * setVelocity;
        }

        setVelocity.y = Mathf.Clamp(setVelocity.y, maxFallSpeed, Mathf.Infinity);
        rb.linearVelocity = setVelocity;
    }

    #region Collision_Detection
    private void GroundCollisions()
    {
        float castDist = (CurrentState == Grounded ? groundCastDist : fallingCastDist) * Size / 2.0f;

        if (Physics.SphereCast(rb.transform.position, groundCastRad, Vector3.down, out RaycastHit interpolated, castDist, GroundLayer))
        {
            Vector3 dir           = (interpolated.point - rb.transform.position).normalized;
            Vector3 desiredNormal = interpolated.normal;

            if (Physics.Raycast(rb.transform.position, dir, out RaycastHit nonInterpolated, dir.magnitude + raycastMargin, GroundLayer)) {
                // FUCKKKKK!!!!
                if (Vector3.Angle(Vector3.up, nonInterpolated.normal) >= 90) {
                    desiredNormal = Vector3.up;
                }
                else {
                    float interpAngle    = Vector3.Angle(Vector3.up, interpolated.normal);
                    float nonInterpAngle = Vector3.Angle(Vector3.up, nonInterpolated.normal);

                    if (interpAngle >= 90 || Mathf.Abs(nonInterpAngle - interpAngle) > interpDeviation) {
                        desiredNormal = nonInterpolated.normal;
                    }
                }
            }

            float angle     = Vector3.Angle(Vector3.up, desiredNormal);
            GroundNormal    = Vector3.MoveTowards(GroundNormal, desiredNormal, Time.fixedDeltaTime * interpolateNormalSpeed);
            GroundPoint     = interpolated.point;
            GroundCollision = true;
            SlopeCollision  = angle > 0;
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

            if (Physics.Raycast(rb.position, dir, out RaycastHit hit, wallCastDistance, GroundLayer)) {
                combined += hit.normal;
            }
        }

        if (combined == Vector3.zero) {
            ResetWallCollisions();
            return;
        }

        WallCollision = true;
        WallNormal    = combined.normalized;
    }

    private void ResetGroundCollisions() => SlopeCollision = GroundCollision = false;
    private void ResetWallCollisions()   => WallCollision  = false;

    public void Launch() {
        hfsm.ChangeState(Falling);
        ResetGroundCollisions();
        ResetWallCollisions();
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
            if (context.PreviousState != context.Sliding && context.PreviousDuration > context.PlayerCamera.bobDelay) {
                context.PlayerCamera.JumpBob();
            }

            stickVel = Vector3.zero;
        }

        public override void Update() {
            if (!context.slideBoost) context.slideBoost = context.hfsm.Duration > context.groundMomentumConserveTime;

            context.PlayerCamera.ViewTilt();
        }

        public override void FixedUpdate() {
            float halfSize    = context.Size / 2.0f;
            float yPos        = context.Position.y - halfSize - context.GroundPoint.y;
            float yCheck      = context.floorStickThreshold;
            Vector3 targetPos = new(context.Position.x, context.GroundPoint.y + halfSize, context.Position.z);

            if (yPos > yCheck)
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

            context.PlayerCamera.ViewTilt();
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

        public override void Update()      => context.PlayerCamera.ViewTilt();

        public override void FixedUpdate() => context.Move(context.moveSpeed, true);
    }

    #endregion

    #region Sliding_States

    private class SlidingState : State<PlayerController>
    {
        private Vector3 momentum;
        private float yVel;
        public SlidingState(PlayerController context) : base(context) { }

        public override void Enter() {
            momentum = context.rb.linearVelocity;

            if (context.slideBoost && context.HorizontalVelocity.magnitude <= context.slideForce) {
                if (context.PlayerInput.IsInputting) context.PlayerCamera.FOVPulse();

                if (context.SlopeCollision) {
                    Vector3 dir = (context.ViewPositionNoY * context.slideForce) + (context.gravity * Time.fixedDeltaTime * Vector3.down);
                    momentum    = Quaternion.FromToRotation(Vector3.up, context.GroundNormal) * dir;
                }
                else {
                    momentum = context.ViewPositionNoY * context.slideForce;
                }
            }

            context.rb.linearVelocity         = momentum;
            context.DesiredHorizontalVelocity = new Vector2(momentum.x, momentum.z);
            context.slideBoost                = false;

            if (context.PreviousState != context.Grounded && context.PreviousDuration > context.PlayerCamera.bobDelay) {
                context.PlayerCamera.JumpBob();
            }
        }

        public override void Update() => context.PlayerCamera.SlideRotate(momentum);

        public override void FixedUpdate() {
            Vector3 desiredVelocity;

            // Gaining momentum
            if (context.SlopeCollision) {
                // Get the gravity, angle, and current momentumDirection
                Vector3 slopeGravity  = Vector3.ProjectOnPlane(Vector3.down * context.gravity, context.GroundNormal);
                float normalizedAngle = Vector3.Angle(Vector3.up, context.GroundNormal) / 90.0f;
                Vector3 adjusted      = slopeGravity * (1.0f + normalizedAngle);
                desiredVelocity       = momentum + adjusted;
            }
            else {
                desiredVelocity = new Vector3(context.ViewPositionNoY.x, momentum.y, context.ViewPositionNoY.z);
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

                if (Vector3.Dot(desiredDirection, slopeDirection) >= context.slideDotMin) {
                    Vector3 currentMomentum = context.SlopeCollision 
                        ? Vector3.ProjectOnPlane(momentum, context.GroundNormal).normalized
                        : momentum;

                    Quaternion rotation     = Quaternion.FromToRotation(currentMomentum, desiredDirection);
                    // Fix this :)
                    Quaternion interpolated = Quaternion.Slerp(Quaternion.identity, rotation, Time.fixedDeltaTime * context.slideRotationSpeed);

                    momentum = interpolated * momentum;

                    if (Vector3.Dot(momentum, slopeDirection) <= 0 && context.SlopeCollision) {
                        momentum = Vector3.ProjectOnPlane(momentum, context.GroundNormal);
                    }
                }
            }
            
            if (new Vector2(context.rb.linearVelocity.x, context.rb.linearVelocity.z).magnitude <= context.slideForce) {
                momentum.y = Mathf.SmoothDamp(momentum.y, Mathf.Min(momentum.y, 0), ref yVel, context.groundStickSpeed);
            }

            context.rb.linearVelocity         = momentum;
            context.DesiredHorizontalVelocity = new Vector2(momentum.x, momentum.z);
        }
    }

    private class SlideJumping : State<PlayerController>
    {
        public SlideJumping(PlayerController context) : base(context) { }

        public override void Enter() {
            context.ResetGroundCollisions();

            context.jumpBufferTime    = 0;
            context.slideBoost        = false;
            context.rb.linearVelocity = new Vector3(context.rb.linearVelocity.x, context.slideJumpForce, context.rb.linearVelocity.z);
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

    // Get angle between current velocity and other normal, if you're currently not on a wall or the angle is too large of a difference ensure its not snapped to the wall if the dot doesnt allow
    // Could probably simplify the logic above, just spitballing is all
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

            context.rb.linearVelocity         = new Vector3(temp.x, tempY, temp.z);
            context.DesiredHorizontalVelocity = context.HorizontalVelocity;

            prevForward = context.WallNormal;
            targetY     = context.rb.transform.localEulerAngles.y;
            rotateVel   = 0;
        }

        public override void Update() {
            if (context.PlayerInput.AlteredMouseDelta == Vector2.zero && context.hfsm.Duration >= context.wallRunRotateGraceTime) {
                float y = Mathf.SmoothDampAngle(context.rb.transform.localEulerAngles.y, targetY, ref rotateVel, context.PlayerCamera.wallRunRotationSpeed);

                context.rb.MoveRotation(Quaternion.Euler(new Vector3(0, y, 0)));

                if (prevForward != context.WallNormal) {
                    float angle = Vector3.SignedAngle(prevForward, context.WallNormal, Vector3.up);
                    targetY    += angle;
                }
            }
            else {
                targetY = context.rb.transform.localEulerAngles.y;
            }

            prevForward = context.WallNormal;

            context.PlayerCamera.WallRunRotate(context.WallNormal);
        }

        public override void FixedUpdate() {
            Vector3 projected = GetProjected();

            // Ensuring if you are not trying to stick to a wall with a large difference in the angle, it wont attempt to! Specifically for corners
            // May cause a few things here and there, will need to stress test to confirm
            float angle = Mathf.Abs(Vector3.SignedAngle(context.WallNormal, context.PlayerCamera.CameraForward, Vector3.up));

            if (angle <= context.wallRunAngleChangeMax) return;

            projected *= Mathf.Max(context.wallRunSpeed, context.rb.linearVelocity.magnitude);

            float downwardForce = context.rb.linearVelocity.y > 0 
                ? context.rb.linearVelocity.y - (context.gravity * Time.fixedDeltaTime)
                : context.rb.linearVelocity.y - (context.wallRunGravity * Time.fixedDeltaTime);

            context.DesiredHorizontalVelocity = new Vector2(projected.x, projected.z) + (new Vector2(context.WallNormal.x, context.WallNormal.z) * Time.fixedDeltaTime);
            context.rb.linearVelocity         = new Vector3(context.DesiredHorizontalVelocity.x, downwardForce, context.DesiredHorizontalVelocity.y);
        }

        Vector3 GetProjected() {
            return Vector3.ProjectOnPlane(context.rb.linearVelocity, context.WallNormal).normalized;
        }
    }

    private class WallJumping : State<PlayerController>
    {
        public WallJumping(PlayerController context) : base(context) { }

        public override void Enter() {
            Vector3 dir = context.PlayerCamera.CameraForwardNoY;
            float dot   = Vector3.Dot(dir, context.WallNormal);

            if (context.WallCollision)
            {
                if (dot <= context.wallJumpMinDot)  dir = (context.PlayerCamera.CameraForwardNoY + context.WallNormal).normalized;
                if (dot <= context.wallJumpKickDot) dir = context.WallNormal;
            }

            Vector3 vel = Quaternion.FromToRotation(context.rb.linearVelocity.normalized, dir) * context.rb.linearVelocity.normalized;

            vel *= Mathf.Abs(context.rb.linearVelocity.magnitude) < context.wallJumpForce 
                ? context.wallJumpForce
                : context.rb.linearVelocity.magnitude;

            vel += Vector3.up * context.wallJumpHeight;

            context.rb.linearVelocity         = vel;
            context.DesiredHorizontalVelocity = new Vector2(vel.x, vel.z);
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

    private class DebugFlyingState : State<PlayerController>
    {
        public DebugFlyingState(PlayerController context) : base(context) { }

        public override void Enter() {
            context.DebugMode       = true;
            context.rb.linearVelocity     = Vector3.zero;
            context.DesiredHorizontalVelocity = Vector2.zero;
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
            context.DesiredHorizontalVelocity = Vector2.zero;

            Time.timeScale = 0;
        }

        public override void Update() {
            context.DebugFlySpeed += Input.GetAxisRaw("Mouse ScrollWheel") * context.scrollWheelSpeed;

            Vector3 movePosition = context.ViewPosition;

            if (Input.GetKey(KeyCode.Space))     movePosition.y += 1;
            if (Input.GetKey(KeyCode.LeftShift)) movePosition.y -= 1;

            Transform cam     = context.PlayerCamera.CameraTransform;
            cam.localPosition = Vector3.MoveTowards(cam.localPosition, cam.localPosition + movePosition, Time.unscaledDeltaTime * context.DebugFlySpeed);
        }

        public override void Exit() {
            Time.timeScale    = 1;
            context.DebugMode = false;
            context.PlayerCamera.CameraTransform.localPosition = Vector3.up * 0.5f;
        }
    }

//#endif

    #endregion
}