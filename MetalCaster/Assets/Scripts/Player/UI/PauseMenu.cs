using System.Collections;
using UnityEngine;

public interface IMenu
{
    public virtual void OnOpen()        { }
    public virtual void OnClose()       { }
    public virtual void OnUpdate()      { }
    public virtual void OnFixedUpdate() { }
}

public class SubMenu : ScriptableObject, IMenu {
    protected PauseMenu context;

    public event System.Action Opened;
    public event System.Action Closed;

    public void Initialize(PauseMenu context) { 
        this.context = context;
    }

    public virtual void OnOpen() {
        Opened?.Invoke();
    }

    public virtual void OnClose() {
        Closed?.Invoke();
    }
}

public class PauseMenu : Player.PlayerComponent
{
    [SerializeField] protected bool isActive;
    [SerializeField] private Canvas canvas;
    [SerializeField] private SubMenu modificationMenu;

    [Header("Menu Camera")]
    [SerializeField] private Camera menuCamera;
    [SerializeField] private Camera modelCamera;
    [SerializeField] private float menuSmoothTime;
    [SerializeField] private float rotateReturnSpeed;
    [SerializeField] private float timeInterpolateSpeed;

    private const float POSITION_THRESHOLD = 0.001f;

    public Camera MenuCamera  => menuCamera;
    public Camera ModelCamera => modelCamera;

    public bool IsActive => isActive;

    public event System.Action Opened;
    public event System.Action Closed;

    private Coroutine cameraView;

    public void OnOpen() {
        isActive = true;
        PlayerCamera.MouseLock = false;

        Vector3 pos = PlayerCamera.Camera.transform.InverseTransformPoint(PlayerWeapon.Weapon.MenuPos.position);
        MoveMenuCamera(pos, true);

        TimeManager.Instance.SetTime(0, timeInterpolateSpeed);

        modificationMenu.Initialize(this);

        Opened?.Invoke();
    }

    public void OnClose() {
        isActive = false;
        PlayerCamera.MouseLock = true;

        MoveMenuCamera(Vector3.zero, false);

        TimeManager.Instance.SetTime(1, timeInterpolateSpeed);

        Closed?.Invoke();
    }


    private void Update()
    {
        if (PlayerInput.Reload)
        {
            if (!isActive) OnOpen();
            else           OnClose();
        }
    }

    public void MoveMenuCamera(Vector3 pos, bool enabled) 
    {
        if (cameraView != null) StopCoroutine(cameraView);
        cameraView = StartCoroutine(MoveRender(pos, enabled));
    }

    private IEnumerator MoveRender(Vector3 pos, bool enabled)
    {
        if (enabled) {
            menuCamera.fieldOfView  = PlayerCamera.Camera.fieldOfView;
            modelCamera.fieldOfView = PlayerCamera.ViewmodelCamera.fieldOfView;

            menuCamera.enabled   = modelCamera.enabled = enabled;
            PlayerCamera.enabled = false;
        }

        Vector3 vel = Vector3.zero;

        while (Vector3.Distance(pos, menuCamera.transform.localPosition) > POSITION_THRESHOLD) {
            menuCamera.transform.localPosition = Vector3.SmoothDamp(menuCamera.transform.localPosition, pos, ref vel, menuSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            if (enabled) {
                Vector3 dir     = PlayerWeapon.Weapon.MenuPos.right;
                Vector3 up      = PlayerCamera.CameraTransform.up;
                Quaternion look = Quaternion.LookRotation(dir, up);
                menuCamera.transform.rotation = Quaternion.RotateTowards(menuCamera.transform.rotation, look, Time.unscaledDeltaTime * rotateReturnSpeed);
            }
            else {
                menuCamera.transform.localRotation = Quaternion.RotateTowards(menuCamera.transform.localRotation, Quaternion.identity, Time.unscaledDeltaTime * rotateReturnSpeed);
            }

            yield return null;
        }

        menuCamera.transform.localPosition = pos;

        menuCamera.enabled   = modelCamera.enabled = enabled;
        PlayerCamera.enabled = !enabled;
    }
}
