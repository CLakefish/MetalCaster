using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

    private readonly List<WeaponModification> equippedMods   = new();
    private readonly List<WeaponModification> unequippedMods = new();

    private readonly List<GameObject> slots       = new();
    private readonly List<GameObject> selectables = new();

    private void Open()
    {
        canvas.enabled = true;

        isActive = true;

        PlayerCamera.MouseLock = false;
        PlayerWeapon.MenuOpen  = true;

        PlayerCamera.enabled   = false;
        PlayerMovement.enabled = false;
        PlayerHUD.gameObject.SetActive(false);

        ClearAll();

        DisplayWeaponSlots();
    }

    private void Close()
    {
        GameDataManager.Instance.Write();

        ClearAll();

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
        Weapon weapon = PlayerWeapon.Weapon;

        equippedMods.AddRange(weapon.modifications);
        unequippedMods.AddRange(modifications.Except(equippedMods));

        Debug.Log("Equipped: " + equippedMods.Count + "\nBase:" + weapon.modifications.Count);

        for (int i = 0; i < equippedMods.Count; i++)
        {
            GameObject temp = Instantiate(modificationSelectPrefab, selectedHolder);
            temp.GetComponent<ModificationSlot>().Initialize(this);

            GameObject item                 = Instantiate(modificationItem, temp.transform);
            ModificationDraggable draggable = item.GetComponent<ModificationDraggable>();
            draggable.SetModification(equippedMods[i]);

            draggable.onDrop += () => {
                GameDataManager.Instance.ActiveSave.SaveWeapon(weapon);
            };

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

            if (!equippedMods.Contains(mods[i]))  {
                GameObject item                 = Instantiate(modificationItem, temp.transform);
                ModificationDraggable draggable = item.GetComponent<ModificationDraggable>();
                draggable.SetModification(mods[i]);

                draggable.onDrop += () => {
                    GameDataManager.Instance.ActiveSave.SaveWeapon(weapon);
                };
            }

            selectables.Add(temp);
        }
    }

    private void ClearAll()
    {
        equippedMods.Clear();
        unequippedMods.Clear();

        if (slots.Count > 0 || selectables.Count > 0)
        {
            foreach (var slot in slots)         Destroy(slot);
            foreach (var select in selectables) Destroy(select);

            slots.Clear();
            selectables.Clear();
        }
    }

    void Update()
    {
        if (PlayerInput.Reload && PlayerWeapon.Weapon != null)
        {
            if (IsActive) Close();
            else          Open();
        }
    }
}
