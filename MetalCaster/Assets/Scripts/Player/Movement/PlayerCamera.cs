using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : Player.PlayerComponent
{
    [Header("References")]
    [SerializeField] private Camera camera;

    [Header("Camera Control")]
    [SerializeField] private float viewRotationSmoothing = 0.2f;
    [SerializeField] private float fov = 90;

    [Header("Viewmodel Recoil")]
    [SerializeField] private Transform viewmodelRecoilHolder;
    [SerializeField] private Vector3 viewmodelRecoil;
    [SerializeField] private float viewmodelKickbackSpeed;
    [SerializeField] private float viewmodelKickbackForce;

    [Header("Camera Recoil")]
    [SerializeField] private float recoilSpeed;

    [Header("Jump Bob")]
    [SerializeField] private AnimCurve jumpBob;
    [SerializeField] private float jumpBobReduction;
    [SerializeField] private float jumpBobMaxIntensity;
    [SerializeField] public float bobDelay;

    [Header("Slide Pulse")]
    [SerializeField] private AnimCurve fovPulse;
    [SerializeField] private float slideAngle;

    [Header("View Tilting")]
    [SerializeField] private float viewTiltAngle;

    [Header("Wall Running")]
    [SerializeField] private float wallRunAngle;
    [SerializeField] public float wallRunRotationSpeed;

    private const float standingHeight = 0.5f;

    private Coroutine screenShakeCoroutine;

    private Vector3 startPos;
    private Vector3 desiredPos;

    private Vector3 recoil;

    private Vector3 recoilVel;
    private Vector3 posVel;

    private Vector2 mouseRotation;
    private Vector2 viewTilt;

    private float baseFOV;
    private float cameraVel;

    public Vector3 CameraForward { 
        get { 
            return camera.transform.forward;
        }
    }

    public Vector3 CameraForwardNoY { 
        get { 
            return new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z).normalized; 
        } 
    }

    public Vector3 CameraRight { 
        get {
            return camera.transform.right;   
        }
    }

    public Transform CameraTransform { 
        get { 
            return camera.transform; 
        } 
    }

    public float FOV {
        get { return fov; }
        set {
            fov = value;
            camera.fieldOfView = fov;
        }
    }

    public bool MouseLock {
        set {
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible   = !value;
        }
    }

    public void JumpBob(float intensity = 1) {
        if (jumpBob.Coroutine != null) StopCoroutine(jumpBob.Coroutine);

        intensity = Mathf.Clamp(intensity / jumpBobReduction, 1, jumpBobMaxIntensity);

        jumpBob.Coroutine = StartCoroutine(jumpBob.Run(() => { 
            camera.transform.localPosition = (intensity * jumpBob.Continue(Time.deltaTime) * Vector3.up) + ((PlayerMovement.col.height / 2.0f) * standingHeight * Vector3.up);
        }));
    }

    public void FOVPulse() {
        if (fovPulse.Coroutine != null) StopCoroutine(fovPulse.Coroutine);

        fovPulse.Coroutine = StartCoroutine(fovPulse.Run(() => {
            FOV = baseFOV + fovPulse.Continue(Time.deltaTime);
        }));
    }

    public void WallRunRotate(Vector3 normal)
    {
        recoil.z = wallRunAngle * -Mathf.Sign(Vector3.SignedAngle(CameraForwardNoY, normal, Vector3.up)) * (1.0f - Mathf.Abs(Vector3.Dot(normal, CameraForward)));
    }

    public void SlideRotate(Vector3 normal)
    {
        recoil.z = slideAngle * -Mathf.Sign(Vector3.SignedAngle(CameraForwardNoY, normal.normalized, Vector3.up)) * (1.0f - Mathf.Abs(Vector3.Dot(normal.normalized, CameraForward)));
    }

    public void ViewTilt()
    {
        viewTilt.x = -PlayerInput.Input.x * viewTiltAngle;
    }

    public void Recoil(Vector3 recoilAmount)
    {
        Vector3 rand = Random.insideUnitSphere;
        Vector3 rec  = new(recoilAmount.x, rand.y * recoilAmount.y, rand.z * recoilAmount.z);
        recoil += rec;
    }

    public void ViewmodelRecoil()
    {
        Vector3 rand = Random.insideUnitSphere;
       // desiredViewmodelRotation *= Quaternion.Euler(-viewmodelRecoil.x, rand.y * viewmodelRecoil.y, rand.z * viewmodelRecoil.z);
        desiredPos -= viewmodelKickbackForce * Vector3.forward;
    }

    public void Screenshake(float duration, float intensity)
    {
        if (screenShakeCoroutine != null) StopCoroutine(screenShakeCoroutine);
        screenShakeCoroutine = StartCoroutine(ScreenShakeCoroutine(duration, intensity));
    }

    private IEnumerator ScreenShakeCoroutine(float duration, float intensity)
    {
        float start = Time.time;

        while (Time.time < start + duration)
        {
            recoil.z = Random.insideUnitCircle.x * intensity;
            yield return null;
        }
    }


    private void Start()
    {
        baseFOV   = fov;
        FOV       = fov;
        MouseLock = true;

        startPos = viewmodelRecoilHolder.localPosition;
    }

    private void Update()
    {
        mouseRotation.x  = Mathf.Clamp(mouseRotation.x - PlayerInput.AlteredMousePosition.y - recoil.x, -89f, 89f);
        mouseRotation.y += PlayerInput.AlteredMousePosition.x + recoil.y;

        float currentZ = Mathf.SmoothDampAngle(camera.transform.localEulerAngles.z, recoil.z + viewTilt.x, ref cameraVel, viewRotationSmoothing);
        viewTilt       = Vector2.zero;

        recoil   = Vector3.SmoothDamp(recoil, Vector3.zero, ref recoilVel, recoilSpeed);
        recoil.z = 0;

        camera.transform.localRotation = Quaternion.Euler(new Vector3(mouseRotation.x, mouseRotation.y, currentZ));

        desiredPos = Vector3.SmoothDamp(desiredPos, startPos, ref posVel, viewmodelKickbackSpeed);

        viewmodelRecoilHolder.transform.localPosition = desiredPos;
    }
}
