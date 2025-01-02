using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SubMenu : MonoBehaviour {
    [SerializeField] private string menuName;

    protected PauseMenu context;
    protected Player Player;

    public string MenuName => menuName;

    public event System.Action Opened;
    public event System.Action Closed;

    public void Initialize(PauseMenu context, Player player) { 
        this.context = context;
        this.Player = player;
    }

    public virtual void OnOpen() {
        Opened?.Invoke();
    }

    public virtual void OnClose() {
        Closed?.Invoke();
    }

    public virtual void OnUpdate() { }
}

public class PauseMenu : Player.PlayerComponent
{
    [SerializeField] protected bool isActive;
    [SerializeField] private List<SubMenu> subMenus = new();

    [Header("Opened Menu Prefabs")]
    [SerializeField] private Button menuButton;
    [SerializeField] private Canvas worldspaceCanvas;
    [SerializeField] private Transform buttonHolder;
    [SerializeField] private float worldspaceCanvasScale;
    private readonly List<Button> transitionButtons = new();
    private Canvas worldCanvas;

    [Header("Menu Camera")]
    [SerializeField] private Camera menuCamera;
    [SerializeField] private Camera modelCamera;
    [SerializeField] private float menuSmoothTime;
    [SerializeField] private float rotateReturnSpeed;
    [SerializeField] private float timeInterpolateSpeed;

    private const float POSITION_THRESHOLD = 0.001f;

    public Camera MenuCamera  => menuCamera;
    public Camera ModelCamera => modelCamera;

    public PlayerWeapon WeaponRef => PlayerWeapon;

    public bool IsActive => isActive;

    public event System.Action Opened;
    public event System.Action Closed;

    private SubMenu   currentMenu = null;
    private Coroutine cameraView;

    private void Update()
    {
        if (currentMenu != null) {
            currentMenu.OnUpdate();

            if (PlayerInput.Reload) {
                currentMenu.OnClose();
                currentMenu = null;
            }
        }
        else
        {
            if (PlayerInput.Reload) {
                if (IsActive) OnClose();
                else          OnOpen();
            }
        }
    }

    private void OnOpen() {
        isActive = true;

        PlayerCamera.MouseLock = false;
        PlayerWeapon.enabled   = false;
        PlayerHUD.enabled      = false;

        Vector3 pos = PlayerCamera.Camera.transform.InverseTransformPoint(PlayerWeapon.Selected.Weapon.MenuPos.position);
        MoveMenuCamera(pos, true, 
            () => {
                menuCamera.fieldOfView = PlayerCamera.Camera.fieldOfView;
                modelCamera.fieldOfView = PlayerCamera.ViewmodelCamera.fieldOfView;

                menuCamera.enabled = modelCamera.enabled = enabled;
                PlayerCamera.enabled = false;
            },
            () => {
                menuCamera.enabled   = modelCamera.enabled = true;
                PlayerCamera.enabled = false;
            }
        );

        TimeManager.Instance.SetTime(0, timeInterpolateSpeed);

        DeleteButtons();
        CreateButtons();

        Opened?.Invoke();
    }

    private void OnClose() {
        isActive = false;

        PlayerCamera.MouseLock = true;
        PlayerWeapon.enabled   = true;
        PlayerHUD.enabled      = true;

        MoveMenuCamera(Vector3.zero, false, null, () => {
            menuCamera.enabled   = modelCamera.enabled = false;
            PlayerCamera.enabled = true;
        });

        TimeManager.Instance.SetTime(1, timeInterpolateSpeed);

        DeleteButtons();
        currentMenu = null;

        Closed?.Invoke();
    }

    private void ChangeSubMenu(SubMenu newMenu)
    {
        if (currentMenu != null) currentMenu.OnClose();
        currentMenu = newMenu;
        if (currentMenu != null) currentMenu.OnOpen();
    }

    private void CreateButtons()
    {
        void CreateButton(string text, UnityAction OnClick)
        {
            Button button = Instantiate(menuButton, worldCanvas.transform.GetChild(0));
            transitionButtons.Add(button);
            button.onClick.AddListener(OnClick);

            button.GetComponentInChildren<TMPro.TMP_Text>().text = text;
        }

        worldCanvas     = Instantiate(worldspaceCanvas, PlayerWeapon.Selected.Weapon.MenuHolder);
        Vector3 dir     = PlayerWeapon.Selected.Weapon.MenuPos.right;
        Vector3 up      = PlayerCamera.CameraTransform.up;
        Quaternion look = Quaternion.LookRotation(dir, up);

        worldCanvas.transform.rotation         = look;
        worldCanvas.transform.localEulerAngles = new Vector3(worldCanvas.transform.localEulerAngles.x, worldCanvas.transform.localEulerAngles.y, 0);
        worldCanvas.worldCamera = modelCamera;

        CreateButton("Resume", () => { OnClose(); });

        foreach (var subMenu in subMenus)
        {
            subMenu.Initialize(this, Player);
            CreateButton(subMenu.MenuName, () => { ChangeSubMenu(subMenu); });
        }

        CreateButton("Quit", () => { Application.Quit(); });

        worldCanvas.transform.localScale = Vector3.one * worldspaceCanvasScale;
    }

    private void DeleteButtons()
    {
        if (worldCanvas != null) Destroy(worldCanvas.gameObject);

        if (transitionButtons.Count > 0)
        {
            foreach (var button in transitionButtons) Destroy(button.gameObject);
            transitionButtons.Clear();
        }
    }

    public void MoveMenuCamera(Vector3 pos, bool enabled, System.Action OnStart = null, System.Action OnEnd = null) 
    {
        if (cameraView != null) StopCoroutine(cameraView);
        cameraView = StartCoroutine(MoveRender(pos, enabled, OnStart, OnEnd));
    }

    private IEnumerator MoveRender(Vector3 pos, bool enabled, System.Action OnStart, System.Action OnEnd)
    {
        OnStart?.Invoke();

        Vector3 vel = Vector3.zero;

        while (Vector3.Distance(pos, menuCamera.transform.localPosition) > POSITION_THRESHOLD) {
            menuCamera.transform.localPosition = Vector3.SmoothDamp(menuCamera.transform.localPosition, pos, ref vel, menuSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            if (enabled) {
                Vector3 dir     = PlayerWeapon.Selected.Weapon.MenuPos.right;
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

        OnEnd?.Invoke();
    }
}
