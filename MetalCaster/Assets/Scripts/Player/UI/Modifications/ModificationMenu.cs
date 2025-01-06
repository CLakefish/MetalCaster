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
    private readonly List<GameObject>   slots          = new();
    private readonly List<GameObject>   selectables    = new();

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
        int slotCount = weapon.BaseData.modificationSlots;

        for (int i = 0; i < slotCount; i++)
        {
            var slot = Instantiate(modificationSelectedPrefab, selectedHolder);
            slot.GetComponent<ModificationSlot>().Initialize(this, i);

            if (weapon.modifications[i] != null)
            {
                CreateModificationDraggable(slot.transform, weapon.modifications[i], weapon);
                equippedMods.Add(weapon.modifications[i]);
            }

            slots.Add(slot);
        }

        var allUnlockedMods = GameDataManager.Instance.ActiveSave.Modifications;

        foreach (var mod in allUnlockedMods)
        {
            var slotGO = Instantiate(modificationSlotPrefab, slotHolder);
            if (!equippedMods.Contains(mod)) CreateModificationDraggable(slotGO.transform, mod, weapon);
            selectables.Add(slotGO);
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

        if (slots.Count > 0 || selectables.Count > 0)
        {
            foreach (var slot in slots) Destroy(slot);
            foreach (var select in selectables) Destroy(select);

            slots.Clear();
            selectables.Clear();
        }
    }
}
