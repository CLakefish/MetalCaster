using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

public class Menu : Player.PlayerComponent
{
    [SerializeField] protected bool isActive;

    public bool IsActive => isActive;

    public System.Action OnOpen;
    public System.Action OnClose;
}

public class ModificationMenu : Menu
{
    [Header("Weapon References")]
    [SerializeField] private List<WeaponModification> modifications = new();

    [Header("UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Transform slotHolder, selectedHolder;

    [Header("Prefabs")]
    [SerializeField] private GameObject modificationItem;
    [SerializeField] private GameObject modificationSelectPrefab;
    [SerializeField] private GameObject modificationSlotPrefab;
    [SerializeField] private Popup popup;

    [Header("Interpolation")]
    [SerializeField] private float timeChangeSpeed;

    private readonly List<WeaponModification> equippedMods   = new();
    private readonly List<WeaponModification> unequippedMods = new();

    private readonly List<GameObject> slots       = new();
    private readonly List<GameObject> selectables = new();

    private Coroutine timeSlow;

    private void Open()
    {
        if (timeSlow != null) StopCoroutine(timeSlow);
        timeSlow = StartCoroutine(TimeSlow(0));

        canvas.enabled = true;

        isActive = true;

        PlayerCamera.MouseLock = false;
        PlayerWeapon.MenuOpen  = true;

        PlayerCamera.enabled   = false;
        PlayerMovement.enabled = false;
        PlayerHUD.gameObject.SetActive(false);

        DisplayWeaponSlots();
    }

    private void Close()
    {
        ClearAll();

        TooltipManager.Instance.HidePopup();

        if (timeSlow != null) StopCoroutine(timeSlow);
        timeSlow = StartCoroutine(TimeSlow(1));

        canvas.enabled = false;

        PlayerCamera.enabled   = true;
        PlayerMovement.enabled = true;

        PlayerWeapon.MenuOpen  = false;
        PlayerCamera.MouseLock = true;
        PlayerHUD.gameObject.SetActive(true);

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

    private IEnumerator TimeSlow(float end)
    {
        float velocity = 0;

        while (Mathf.Abs(Time.timeScale - end) > Mathf.Epsilon)
        {
            Time.timeScale = Mathf.SmoothDamp(Time.timeScale, end, ref velocity, timeChangeSpeed, Mathf.Infinity, Time.unscaledDeltaTime);
            yield return null;
        }

        Time.timeScale = end;
    }

    private void Update()
    {
        if (PlayerInput.Reload && PlayerWeapon.Weapon != null)
        {
            if (IsActive) Close();
            else          Open();
        }
    }
}
