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

    [Header("Jump Bob")]
    [SerializeField] private AnimCurve jumpBob;

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

    private void Start()
    {
        FOV       = fov;
        MouseLock = true;
    }

    private void Update()
    {
        mouseRotation.x = Mathf.Clamp(mouseRotation.x - playerInput.AlteredMousePosition.y, -89f, 89f);
        mouseRotation.y += playerInput.AlteredMousePosition.x;

        camera.transform.localRotation = Quaternion.Euler(new Vector3(mouseRotation.x, mouseRotation.y, camera.transform.localRotation.z));
    }
}
