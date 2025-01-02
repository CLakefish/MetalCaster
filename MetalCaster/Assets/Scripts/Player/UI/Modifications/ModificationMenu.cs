using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

public class ModificationMenu : SubMenu
{
    [Header("UI References")]
    [SerializeField] private Canvas modificationCanvas;
    [SerializeField] private Transform slotHolder, selectedHolder;

    [Header("Prefabs")]
    [SerializeField] private GameObject modificationItem;
    [SerializeField] private GameObject modificationSelectedPrefab;
    [SerializeField] private GameObject modificationSlotPrefab;

    public PlayerWeapon PlayerWeapon {
        get {
            return Player.PlayerWeapon;
        }   
    }

    private readonly List<Modification> equippedMods   = new();
    private readonly List<Modification> unequippedMods = new();
    private readonly List<GameObject>         slots          = new();
    private readonly List<GameObject>         selectables    = new();

    public override void OnOpen()
    {
        modificationCanvas.enabled = true;
        context.MoveMenuCamera(Player.PlayerCamera.Camera.transform.InverseTransformPoint(PlayerWeapon.Selected.Weapon.ModificationPos.position), true);
        DisplayWeaponSlots();
    }

    public override void OnClose()
    {
        modificationCanvas.enabled = false;
        context.MoveMenuCamera(Player.PlayerCamera.Camera.transform.InverseTransformPoint(PlayerWeapon.Selected.Weapon.MenuPos.position), true);
        TooltipManager.Instance.HidePopup();
        ClearAll();
    }

    private void DisplayWeaponSlots()
    {
        ClearAll();

        Weapon weapon = PlayerWeapon.Selected.Weapon;

        equippedMods.AddRange(weapon.modifications);
        unequippedMods.AddRange(GameDataManager.Instance.ActiveSave.Modifications.Except(equippedMods));

        for (int i = 0; i < equippedMods.Count; i++)
        {
            GameObject temp = Instantiate(modificationSelectedPrefab, selectedHolder);
            temp.GetComponent<ModificationSlot>().Initialize(this);

            CreateModificationDraggable(temp.transform, equippedMods[i], weapon);

            selectables.Add(temp);
        }

        for (int i = 0; i < weapon.WeaponData.modificationSlots - equippedMods.Count; ++i)
        {
            GameObject temp = Instantiate(modificationSelectedPrefab, selectedHolder);
            temp.GetComponent<ModificationSlot>().Initialize(this);
            selectables.Add(temp);
        }

        List<Modification> mods = GameDataManager.Instance.ActiveSave.Modifications;

        for (int i = 0; i < mods.Count; ++i)
        {
            GameObject temp = Instantiate(modificationSlotPrefab, slotHolder);

            if (!equippedMods.Contains(mods[i]))
            {
                CreateModificationDraggable(temp.transform, mods[i], weapon);
            }

            selectables.Add(temp);
        }
    }

    private GameObject CreateModificationDraggable(Transform parent, Modification mod, Weapon weapon)
    {
        GameObject item = Instantiate(modificationItem, parent);
        var draggable = item.GetComponent<ModificationDraggable>();

        draggable.SetReferences(mod);
        draggable.OnDragParent(modificationCanvas.transform);

        draggable.GetComponent<Image>().sprite = mod.ModificationSprite;

        draggable.onDrop += () =>
        {
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
}
