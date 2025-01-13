using HFSMFramework;
using System.Collections;
using UnityEngine;

// Made by Carson Lakefish
// Concept recommendations and help by Oliver Beebe

// TODO:
// - Fix wall run angle determinant (probably w/the dot)
// - Weapon Swap Menu (Do Tuesday)
// - Modifications :)
// - Enemies :)


public class PlayerController : Player.PlayerComponent
{
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
    [SerializeField] public float crouchTime;

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

    private readonly float slideDotMin = -0.25f;

    private StateMachine<PlayerController> hfsm;

    private GroundedState Grounded { get; set; }
    private FallingState Falling   { get; set; }
    private JumpingState Jumping   { get; set; }
    private SlidingState Sliding   { get; set; }
    private SlideJumping SlideJump { get; set; }
    private WallRunning WallRun    { get; set; }
    private WallJumping WallJump   { get; set; }

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
            return new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
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
            float angle = Vector3.SignedAngle(PlayerCollisions.WallNormal, ViewPositionNoY, Vector3.up);
            bool isWall = Vector3.Angle(Vector3.up, PlayerCollisions.WallNormal) >= 89.0f;
            return Mathf.Abs(angle) > wallRunMaxAngle && isWall;
        }
    }

    public bool GroundState => hfsm.CurrentState == Grounded;

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
            new(Grounded, Falling,   () => !PlayerCollisions.GroundCollision),
            new(Grounded, SlideJump, () => jumpBufferTime > 0 && PreviousState == Sliding),
            new(Grounded, Jumping,   () => PlayerInput.Jump   || jumpBufferTime > 0),
            new(Grounded, Sliding,   () => PlayerInput.Slide),

            // Jumping transitions   
            new(Jumping, Falling,    () => rb.linearVelocity.y <= 0 && hfsm.Duration >= minJumpTime),
            new(Jumping, Grounded,   () => hfsm.Duration >= minJumpTime && PlayerCollisions.GroundCollision),
            new(Jumping, WallRun,    () => PlayerCollisions.WallCollision && AllowWallRun && hfsm.Duration >= minJumpTime),

            // Falling transitions   
            new(Falling, Jumping,    () => PlayerInput.Jump  && PreviousState == Grounded && hfsm.Duration < coyoteTime),
            new(Falling, SlideJump,  () => PlayerInput.Jump  && PreviousState == Sliding  && hfsm.Duration < coyoteTime),
            new(Falling, WallJump,   () => (PlayerInput.Jump || jumpBufferTime > 0)       && PlayerCollisions.WallCollision && !PlayerCollisions.GroundCollision),
            new(Falling, WallJump,   () => (PlayerInput.Jump || jumpBufferTime > 0)       && PreviousState == WallRun       && hfsm.Duration < wallJumpCoyoteTime),

            new(Falling, Grounded,   () => !PlayerInput.Slide && PlayerCollisions.GroundCollision && hfsm.Duration > 0.1f),
            new(Falling, Sliding,    () => PlayerInput.Slide  && PlayerCollisions.GroundCollision),
            new(Falling, WallRun,    () => PlayerCollisions.WallCollision && !PlayerCollisions.GroundCollision && AllowWallRun),

            // Sliding transitions   
            new(Sliding, SlideJump,  () => (PlayerInput.Jump || jumpBufferTime > 0) && PlayerCollisions.GroundCollision),
            new(Sliding, Falling,    () => !PlayerCollisions.GroundCollision        && !PlayerCollisions.SlopeCollision),
            new(Sliding, Grounded,   () => !PlayerInput.Slide                       && PlayerCollisions.GroundCollision),

            // Slide jump transitions
            new(SlideJump, Falling,  () => ((!PlayerInput.Slide) || (rb.linearVelocity.y < 0 && hfsm.Duration > 0)) && hfsm.Duration > minJumpTime),
            new(SlideJump, Grounded, () => !PlayerInput.Slide && PlayerCollisions.GroundCollision                   && hfsm.Duration >= minJumpTime),
            new(SlideJump, Sliding,  () => PlayerInput.Slide  && PlayerCollisions.GroundCollision                   && hfsm.Duration >= minJumpTime),

            // Wall run transitions
            new(WallRun, WallJump,   () => PlayerInput.Jump || jumpBufferTime > 0),
            new(WallRun, Falling,    () => !PlayerCollisions.WallCollision   && !PlayerCollisions.GroundCollision),
            new(WallRun, Falling,    () => !AllowWallRun),
            new(WallRun, Grounded,   () => PlayerCollisions.GroundCollision),

            // Wall jump transitions
            new(WallJump, Falling,   () => hfsm.Duration >= wallJumpTime),
            new(WallJump, Grounded,  () => PlayerCollisions.GroundCollision && hfsm.Duration >= wallJumpTime),
            new(WallJump, WallRun,   () => PlayerCollisions.WallCollision   && AllowWallRun && hfsm.Duration >= wallJumpTime),

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
        PlayerCollisions.ResetAllCollisions();
        hfsm.Start(Falling);
    }

    private void Update()
    {
        jumpBufferTime -= Time.deltaTime;

        hfsm.CheckTransitions();
        hfsm.Update();

        PlayerCollisions.ChangeSize(PlayerInput.Slide || !CanUncrouch ? crouchSize : standardSize);

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

        PlayerCollisions.GroundCollisions();
        PlayerCollisions.WallCollisions();

        Debug.Log(PlayerCollisions.GroundCollision);

        hfsm.FixedUpdate();

        if (!PlayerCollisions.GroundCollision && CurrentState != WallRun) rb.linearVelocity -= new Vector3(0, gravity, 0) * Time.fixedDeltaTime;
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

        if (PlayerCollisions.SlopeCollision && CurrentState == Grounded) {
            setVelocity = Quaternion.FromToRotation(Vector3.up, PlayerCollisions.GroundNormal) * setVelocity;
        }

        setVelocity.y = Mathf.Clamp(setVelocity.y, maxFallSpeed, Mathf.Infinity);
        rb.linearVelocity = setVelocity;
    }

    public void Launch() {
        hfsm.ChangeState(Falling);
        PlayerCollisions.ResetAllCollisions();
        DesiredHorizontalVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z);
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
            float halfSize    = context.PlayerCollisions.Size / 2.0f;
            float yPos        = context.Position.y - halfSize - context.PlayerCollisions.GroundPoint.y;
            float yCheck      = context.PlayerCollisions.floorStickThreshold;
            Vector3 targetPos = new(context.Position.x, context.PlayerCollisions.GroundPoint.y + halfSize, context.Position.z);

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
            context.PlayerCollisions.ResetGroundCollisions();
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

                if (context.PlayerCollisions.SlopeCollision) {
                    Vector3 dir = (context.ViewPositionNoY * context.slideForce) + (context.gravity * Time.fixedDeltaTime * Vector3.down);
                    momentum    = Quaternion.FromToRotation(Vector3.up, context.PlayerCollisions.GroundNormal) * dir;
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
            if (context.PlayerCollisions.SlopeCollision) {
                // Get the gravity, angle, and current momentumDirection
                Vector3 slopeGravity  = Vector3.ProjectOnPlane(Vector3.down * context.gravity, context.PlayerCollisions.GroundNormal);
                float normalizedAngle = Vector3.Angle(Vector3.up, context.PlayerCollisions.GroundNormal) / 90.0f;
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
                Vector3 desiredDirection = Vector3.ProjectOnPlane(context.ViewPositionNoY, context.PlayerCollisions.GroundNormal).normalized;
                Vector3 slopeDirection   = Vector3.ProjectOnPlane(Vector3.down, context.PlayerCollisions.GroundNormal).normalized;

                if (Vector3.Dot(desiredDirection, slopeDirection) >= context.slideDotMin) {
                    Vector3 currentMomentum = context.PlayerCollisions.SlopeCollision 
                        ? Vector3.ProjectOnPlane(momentum, context.PlayerCollisions.GroundNormal).normalized
                        : momentum;

                    Quaternion rotation     = Quaternion.FromToRotation(currentMomentum, desiredDirection);
                    // Fix this :)
                    Quaternion interpolated = Quaternion.Slerp(Quaternion.identity, rotation, Time.fixedDeltaTime * context.slideRotationSpeed);

                    momentum = interpolated * momentum;

                    if (Vector3.Dot(momentum, slopeDirection) <= 0 && context.PlayerCollisions.SlopeCollision) {
                        momentum = Vector3.ProjectOnPlane(momentum, context.PlayerCollisions.GroundNormal);
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
            context.PlayerCollisions.ResetGroundCollisions();

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
                temp = context.PlayerCollisions.WallNormal;
            }
            else {
                temp = Quaternion.FromToRotation(Vector3.up, context.PlayerCollisions.WallNormal) * temp;
            }

            context.rb.linearVelocity         = new Vector3(temp.x, tempY, temp.z);
            context.DesiredHorizontalVelocity = new Vector2(context.HorizontalVelocity.x, context.HorizontalVelocity.z);

            prevForward = context.PlayerCollisions.WallNormal;
            targetY     = context.rb.transform.localEulerAngles.y;
            rotateVel   = 0;
        }

        public override void Update() {
            if (context.PlayerInput.AlteredMouseDelta == Vector2.zero && context.hfsm.Duration >= context.wallRunRotateGraceTime) {
                float y = Mathf.SmoothDampAngle(context.rb.transform.localEulerAngles.y, targetY, ref rotateVel, context.PlayerCamera.wallRunRotationSpeed);

                context.rb.MoveRotation(Quaternion.Euler(new Vector3(0, y, 0)));

                if (prevForward != context.PlayerCollisions.WallNormal) {
                    float angle = Vector3.SignedAngle(prevForward, context.PlayerCollisions.WallNormal, Vector3.up);
                    targetY    += angle;
                }
            }
            else {
                targetY = context.rb.transform.localEulerAngles.y;
            }

            prevForward = context.PlayerCollisions.WallNormal;

            context.PlayerCamera.WallRunRotate(context.PlayerCollisions.WallNormal);
        }

        public override void FixedUpdate() {
            Vector3 projected = GetProjected();

            // Ensuring if you are not trying to stick to a wall with a large difference in the angle, it wont attempt to! Specifically for corners
            // May cause a few things here and there, will need to stress test to confirm
            float angle = Mathf.Abs(Vector3.SignedAngle(context.PlayerCollisions.WallNormal, context.PlayerCamera.CameraForward, Vector3.up));

            if (angle <= context.wallRunAngleChangeMax) return;

            projected *= Mathf.Max(context.wallRunSpeed, context.DesiredHorizontalVelocity.magnitude);

            float downwardForce = context.rb.linearVelocity.y > 0 
                ? context.rb.linearVelocity.y - (context.gravity * Time.fixedDeltaTime)
                : context.rb.linearVelocity.y - (context.wallRunGravity * Time.fixedDeltaTime);

            context.DesiredHorizontalVelocity = new Vector2(projected.x, projected.z);
            context.rb.linearVelocity         = new Vector3(context.DesiredHorizontalVelocity.x, downwardForce, context.DesiredHorizontalVelocity.y);
        }

        Vector3 GetProjected() {
            Vector3 dir;

            if (context.HorizontalVelocity.magnitude <= Mathf.Epsilon) dir = context.PlayerCamera.CameraForwardNoY;
            else                                                       dir = context.HorizontalVelocity;

            Vector3 projected = Vector3.ProjectOnPlane(dir, context.PlayerCollisions.WallNormal).normalized;
            return projected;
        }
    }

    private class WallJumping : State<PlayerController>
    {
        public WallJumping(PlayerController context) : base(context) { }

        public override void Enter() {
            Vector3 dir = context.PlayerCamera.CameraForwardNoY;
            float dot   = Vector3.Dot(dir, context.PlayerCollisions.WallNormal);

            if (context.PlayerCollisions.WallCollision)
            {
                if (dot <= context.wallJumpMinDot)  dir = (context.PlayerCamera.CameraForwardNoY + context.PlayerCollisions.WallNormal).normalized;
                if (dot <= context.wallJumpKickDot) dir = context.PlayerCollisions.WallNormal;
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