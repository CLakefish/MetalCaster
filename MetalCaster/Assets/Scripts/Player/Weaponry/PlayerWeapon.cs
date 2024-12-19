using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapon : Player.PlayerComponent
{
    [Header("Viewmodel")]
    [SerializeField] private Transform viewmodelHolder;

    [Header("Weapon references")]
    [SerializeField] private List<Weapon> weapons;
    [SerializeField] private int maxWeapons = 3;

    [Header("Collisions")]
    [SerializeField] private LayerMask collisionLayers;

    private (Weapon Weapon, GameObject Viewmodel) Selected;
    private int selectedIndex = 0;

    public LayerMask CollisionLayers {
        get {
            return collisionLayers;
        }
    }

    public GameObject Viewmodel {
        get {
            return Selected.Viewmodel;
        }
    }

    private void OnEnable() => SelectWeapon();

    private void Update()
    {
        if (PlayerInput.SlotPressed)
        {
            if (PlayerInput.Slot.One.Pressed)   selectedIndex = 0;
            if (PlayerInput.Slot.Two.Pressed)   selectedIndex = 1;
            if (PlayerInput.Slot.Three.Pressed) selectedIndex = 2;

            selectedIndex = Mathf.Clamp(selectedIndex, 0, weapons.Count - 1);

            SelectWeapon();
            return;
        }

        if (PlayerInput.Mouse.Left.Held) {
            Selected.Weapon.Fire(this);
        }

        Selected.Weapon.UpdateWeapon();
    }

    private void SelectWeapon()
    {
        if (Selected.Viewmodel != null) {
            Selected.Weapon.UnEquip();

            Destroy(Selected.Viewmodel);
            Selected.Viewmodel = null;
            Selected.Weapon    = null;
        }

        Selected.Viewmodel = Instantiate(weapons[selectedIndex].gameObject, viewmodelHolder, false);
        Selected.Weapon    = Selected.Viewmodel.GetComponent<Weapon>();

        Selected.Weapon.Equip(this);
    }

    public bool AddWeapon(Weapon weapon, bool equip = false)
    {
        if (weapons.Contains(weapon) || weapons.Count >= maxWeapons - 1) return false;

        weapons.Add(weapon);

        if (equip)
        {
            selectedIndex = weapons.Count - 1;
            SelectWeapon();
        }

        return true;
    }

    public bool RemoveWeapon(Weapon weapon)
    {
        if (weapons.Count <= 1) return false;

        int index = weapons.IndexOf(weapon);
        if (index == -1) return false;

        if (index == selectedIndex)
        {
            selectedIndex++;
            selectedIndex %= weapons.Count;

            SelectWeapon();
        }

        weapons.Remove(weapon);

        return true;
    }
}
