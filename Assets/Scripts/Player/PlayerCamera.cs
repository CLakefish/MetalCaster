using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : Player.PlayerComponent
{
    [Header("References")]
    [SerializeField] private Camera camera;

    [Header("Camera Control")]
    [SerializeField] private float viewRotationSmoothing = 0.2f;
    [SerializeField] private float FOV = 90;
    private Vector2 mouseRotation;

    public Vector3 CameraForward { get { return camera.transform.forward; } }
    public Vector3 CameraForwardNoY { get { return new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z).normalized; } }
    public Vector3 CameraRight   { get { return camera.transform.right;   } }

    public Transform CameraTransform { get { return camera.transform; } }

    private void Start()
    {
        camera.fieldOfView = FOV;
        LockMouse(true);
    }

    private void Update()
    {
        mouseRotation.x = Mathf.Clamp(mouseRotation.x - playerInput.AlteredMousePosition.y, -89f, 89f);
        mouseRotation.y += playerInput.AlteredMousePosition.x;

        camera.transform.localRotation = Quaternion.Euler(new Vector3(mouseRotation.x, mouseRotation.y, camera.transform.localRotation.z));
    }

    public void LockMouse(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}
