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
    private Vector2 mouseRotation;
    private Vector2 viewTilt;
    private float baseFOV;
    private float desiredZRotation;
    private float cameraVel;

    [Header("Jump Bob")]
    [SerializeField] private AnimCurve jumpBob;

    [Header("Slide Pulse")]
    [SerializeField] private AnimCurve fovPulse;
    [SerializeField] private float slideAngle;

    [Header("View Tilting")]
    [SerializeField] private float viewTiltAngle;

    [Header("Wall Running")]
    [SerializeField] private float wallRunAngle;
    [SerializeField] public float wallRunRotationSpeed;

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

    public void JumpBob() {
        if (jumpBob.Coroutine != null) StopCoroutine(jumpBob.Coroutine);

        jumpBob.Coroutine = StartCoroutine(jumpBob.Run(() => { 
            camera.transform.localPosition = (Vector3.up * jumpBob.Continue(Time.deltaTime)) + (Vector3.up * 0.5f);
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
        viewTilt.x = -playerInput.Input.x * viewTiltAngle;
    }

    public void Rotate(float deg)
    {
        desiredZRotation += deg;
    }

    private void Start()
    {
        baseFOV   = fov;
        FOV       = fov;
        MouseLock = true;
    }

    private void Update()
    {
        mouseRotation.x  = Mathf.Clamp(mouseRotation.x - playerInput.AlteredMousePosition.y, -89f, 89f);
        mouseRotation.y += playerInput.AlteredMousePosition.x;

        float currentZ   = Mathf.SmoothDampAngle(camera.transform.localEulerAngles.z, desiredZRotation + viewTilt.x, ref cameraVel, viewRotationSmoothing);

        desiredZRotation = 0;
        viewTilt         = Vector2.zero;

        camera.transform.localRotation = Quaternion.Euler(new Vector3(mouseRotation.x, mouseRotation.y, currentZ));
    }
}
