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
    [SerializeField] private float viewmodelRecoilSpeed;
    [SerializeField] private float viewmodelReturnSpeed;

    [Header("Viewmodel Kickback")]
    [SerializeField] private float viewmodelKickbackSpeed;
    [SerializeField] private float viewmodelKickbackForce;

    [Header("Camera Recoil")]
    [SerializeField] private float returnSpeed;
    [SerializeField] private float recoilSpeed;

    [Header("Jump Bob")]
    [SerializeField] private AnimCurve jumpBob;
    [SerializeField] private float jumpBobReduction;
    [SerializeField] private float jumpBobMaxIntensity;

    [Header("Slide Pulse")]
    [SerializeField] private AnimCurve fovPulse;
    [SerializeField] private float slideAngle;

    [Header("View Tilting")]
    [SerializeField] private float viewTiltAngle;

    [Header("Wall Running")]
    [SerializeField] private float wallRunAngle;
    [SerializeField] public float wallRunRotationSpeed;

    private const float standingHeight = 0.5f;

    private Quaternion desiredRotation          = Quaternion.identity;
    private Quaternion currentRotation          = Quaternion.identity;

    private Quaternion desiredViewmodelRotation = Quaternion.identity;
    private Quaternion currentViewmodelRotation = Quaternion.identity;

    private Coroutine screenShakeCoroutine;

    private Vector3 startPos;
    private Vector3 desiredPos;
    private Vector3 posVel;

    private Vector2 mouseRotation;
    private Vector2 viewTilt;

    private float baseFOV;
    private float desiredZRotation;
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
        desiredZRotation = wallRunAngle * -Mathf.Sign(Vector3.SignedAngle(CameraForwardNoY, normal, Vector3.up)) * (1.0f - Mathf.Abs(Vector3.Dot(normal, CameraForward)));
    }

    public void SlideRotate(Vector3 normal)
    {
        desiredZRotation = slideAngle * -Mathf.Sign(Vector3.SignedAngle(CameraForwardNoY, normal.normalized, Vector3.up)) * (1.0f - Mathf.Abs(Vector3.Dot(normal.normalized, CameraForward)));
    }

    public void ViewTilt()
    {
        viewTilt.x = -PlayerInput.Input.x * viewTiltAngle;
    }

    public void Recoil(Vector3 recoil)
    {
        Vector3 rand     = Random.insideUnitSphere;
        desiredRotation *= Quaternion.Euler(-recoil.x, rand.y * recoil.y, rand.z * recoil.z);
    }

    public void ViewmodelRecoil()
    {
        Vector3 rand = Random.insideUnitSphere;
        desiredViewmodelRotation *= Quaternion.Euler(-viewmodelRecoil.x, rand.y * viewmodelRecoil.y, rand.z * viewmodelRecoil.z);
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
            desiredZRotation = Random.insideUnitCircle.x * intensity;
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
        mouseRotation.x  = Mathf.Clamp(mouseRotation.x - PlayerInput.AlteredMousePosition.y, -89f, 89f);
        mouseRotation.y += PlayerInput.AlteredMousePosition.x;

        float currentZ   = Mathf.SmoothDampAngle(camera.transform.localEulerAngles.z, desiredZRotation + viewTilt.x, ref cameraVel, viewRotationSmoothing);
        desiredZRotation = 0;
        viewTilt         = Vector2.zero;

        desiredRotation          = Quaternion.RotateTowards(desiredRotation, Quaternion.Euler(Vector3.zero), Time.deltaTime * returnSpeed);
        currentRotation          = Quaternion.RotateTowards(currentRotation, desiredRotation, Time.deltaTime * recoilSpeed);

        desiredViewmodelRotation = Quaternion.RotateTowards(desiredViewmodelRotation, Quaternion.Euler(Vector3.zero), Time.deltaTime * viewmodelReturnSpeed);
        currentViewmodelRotation = Quaternion.RotateTowards(currentViewmodelRotation, desiredViewmodelRotation,       Time.deltaTime * viewmodelRecoilSpeed);
        desiredPos               = Vector3.SmoothDamp(desiredPos, startPos, ref posVel, viewmodelKickbackSpeed);

        camera.transform.localRotation     = Quaternion.Euler(new Vector3(mouseRotation.x, mouseRotation.y, currentZ));
        camera.transform.localEulerAngles += currentRotation.eulerAngles;

        viewmodelRecoilHolder.transform.localRotation = desiredViewmodelRotation;
        viewmodelRecoilHolder.transform.localPosition = desiredPos;
    }
}
