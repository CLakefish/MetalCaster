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
            int desiredIndex = 0;

            if (PlayerInput.Slot.One.Pressed)   desiredIndex = 0;
            if (PlayerInput.Slot.Two.Pressed)   desiredIndex = 1;
            if (PlayerInput.Slot.Three.Pressed) desiredIndex = 2;

            desiredIndex = Mathf.Clamp(desiredIndex, 0, weapons.Count - 1);

            if (desiredIndex != selectedIndex)
            {
                selectedIndex = desiredIndex;

                SelectWeapon();
                return;
            }
        }

        if (Selected.Weapon == null) return;

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

        if (weapons.Count == 0) return;

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
        int index = weapons.IndexOf(weapon);
        if (index < 0) return false;

        weapons.Remove(weapon);

        if (index == selectedIndex)
        {
            if (weapons.Count != 0) {
                selectedIndex++;
                selectedIndex %= weapons.Count;
            }
            else selectedIndex = 0;

            SelectWeapon();
        }

        return true;
    }
}
