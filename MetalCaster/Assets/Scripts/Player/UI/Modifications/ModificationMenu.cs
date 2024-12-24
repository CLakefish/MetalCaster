using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Menus/Sub Menus/Modification Menu")]
public class ModificationMenu : SubMenu
{
    [Header("UI References")]
    [SerializeField] private Canvas modificationCanvas;
    [SerializeField] private Transform slotHolder, selectedHolder;

    [Header("Prefabs")]
    [SerializeField] private GameObject modificationItem;
    [SerializeField] private GameObject modificationSelectPrefab;
    [SerializeField] private GameObject modificationSlotPrefab;
    [SerializeField] private Popup popup;

    private readonly List<WeaponModification> equippedMods   = new();
    private readonly List<WeaponModification> unequippedMods = new();
    private readonly List<GameObject>         slots          = new();
    private readonly List<GameObject>         selectables    = new();

    public override void OnOpen()
    {
        context.MoveMenuCamera(context.GetCamera().Camera.transform.InverseTransformPoint(context.GetWeapon().Weapon.ModificationPos.position), true);

        base.OnOpen();
    }

    public override void OnClose()
    {
        context.MoveMenuCamera(context.GetCamera().Camera.transform.InverseTransformPoint(context.GetWeapon().Weapon.MenuPos.position), true);

        base.OnClose();
    }

            /*
    private void Open()
    {

        canvas.enabled = true;

        isActive = true;

        PlayerCamera.MouseLock = false;
        PlayerWeapon.MenuOpen  = true;

        PlayerCamera.enabled   = false;
        PlayerMovement.enabled = false;
        PlayerHUD.gameObject.SetActive(false);

        menuCamera.enabled = modelCamera.enabled = true;
        Vector3 pos = menuCamera.transform.parent.InverseTransformPoint(PlayerWeapon.Weapon.CameraPos.position);
        MoveMenuCamera(pos, true);

        DisplayWeaponSlots();
    }

    private void Close()
    {
        ClearAll();

        TooltipManager.Instance.HidePopup();

        canvas.enabled = false;

        PlayerCamera.enabled   = true;
        PlayerMovement.enabled = true;

        PlayerWeapon.MenuOpen  = false;
        PlayerCamera.MouseLock = true;
        PlayerHUD.gameObject.SetActive(true);

        MoveMenuCamera(Vector3.zero, false);

        isActive = false;
    }

    private void DisplayWeaponSlots()
    {
        ClearAll();

        Weapon weapon = PlayerWeapon.Weapon;

        equippedMods.AddRange(weapon.modifications);
        unequippedMods.AddRange(modifications.Except(equippedMods));

        for (int i = 0; i < equippedMods.Count; i++)
        {
            GameObject temp = Instantiate(modificationSelectPrefab, selectedHolder);
            temp.GetComponent<ModificationSlot>().Initialize(this);

            CreateModificationDraggable(temp.transform, equippedMods[i], weapon);

            selectables.Add(temp);
        }

        for (int i = 0; i < weapon.WeaponData.modificationSlots - equippedMods.Count; ++i)
        {
            GameObject temp = Instantiate(modificationSelectPrefab, selectedHolder);
            temp.GetComponent<ModificationSlot>().Initialize(this);
            selectables.Add(temp);
        }

        List<WeaponModification> mods = GameDataManager.Instance.ActiveSave.Modifications;

        for (int i = 0; i < mods.Count; ++i)
        {
            GameObject temp = Instantiate(modificationSlotPrefab, slotHolder);

            if (!equippedMods.Contains(mods[i])) {
                CreateModificationDraggable(temp.transform, mods[i], weapon);
            }

            selectables.Add(temp);
        }
    }

    private GameObject CreateModificationDraggable(Transform parent, WeaponModification mod, Weapon weapon)
    {
        GameObject item = Instantiate(modificationItem, parent);
        var draggable   = item.GetComponent<ModificationDraggable>();

        draggable.SetReferences(mod);
        draggable.OnDragParent(canvas.transform);

        draggable.GetComponent<Image>().sprite = mod.ModificationSprite;

        draggable.onDrop += () => {
            GameDataManager.Instance.ActiveSave.SaveWeapon(weapon);
        };

        return item;
    }

    private void ClearAll()
    {
        equippedMods.Clear();
        unequippedMods.Clear();

        if (slots.Count > 0 || selectables.Count > 0)
        {
            foreach (var slot in slots) Destroy(slot);
            foreach (var select in selectables) Destroy(select);

            slots.Clear();
            selectables.Clear();
        }
    }

    private void MoveMenuCamera(Vector3 pos, bool enabled)
    {
        if (modificationView != null) StopCoroutine(modificationView);
        modificationView = StartCoroutine(MoveRender(pos, enabled));
    }

    private IEnumerator MoveRender(Vector3 pos, bool enabled)
    {
        Vector3 vel = Vector3.zero;

        while (Vector3.Distance(pos, menuCamera.transform.localPosition) > 0.001f)
        {
            menuCamera.transform.localPosition = Vector3.SmoothDamp(menuCamera.transform.localPosition, pos, ref vel, menuSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            if (enabled) {
                // https://matteolopiccolo.medium.com/math-in-unity-lookat-5a9eb2b36fc6
                //menuCamera.transform.LookAt(PlayerWeapon.Weapon.CameraPos.right, PlayerCamera.CameraTransform.up);
                Vector3 dir     = PlayerWeapon.Weapon.CameraPos.right;
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
        menuCamera.enabled = modelCamera.enabled = enabled;
    }

    private void Update()
    {
        if (PlayerInput.Reload && PlayerWeapon.Weapon != null)
        {
            if (IsActive) Close();
            else          Open();
        }

        if (isActive) {
            menuCamera.fieldOfView  = PlayerCamera.Camera.fieldOfView;
            modelCamera.fieldOfView = PlayerCamera.ViewmodelCamera.fieldOfView;
        }
    }
            */
}
