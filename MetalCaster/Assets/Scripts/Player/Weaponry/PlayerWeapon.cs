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
    public bool MenuOpen { get; set; }

    public System.Action Fire;
    public System.Action ReloadStart;
    public System.Action ReloadFinished;

    public System.Action<Weapon> Swap;
    public System.Action<Weapon> Added;
    public System.Action<Weapon> Removed;

    private readonly Dictionary<string, int> bulletMap = new();
    private int selectedIndex = 0;

    public LayerMask CollisionLayers {
        get {
            return collisionLayers;
        }
    }

    public GameObject Viewmodel => Selected.Viewmodel;
    public Weapon Weapon        => Selected.Weapon;

    private void OnEnable() => SelectWeapon();

    private void Start()
    {
        weapons = GameDataManager.Instance.ActiveSave.GetWeapons();
        foreach (var weapon in weapons) Added?.Invoke(weapon);
        SelectWeapon();
    }

    private void Update()
    {
        if (MenuOpen) return;

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

        if (PlayerInput.Mouse.Right.Held) {
            Selected.Weapon.AltFire(this);
        }

        Selected.Weapon.UpdateWeapon();
    }

    #region Weapons

    private void SelectWeapon()
    {
        if (Selected.Viewmodel != null) {
            Selected.Weapon.UnEquip();
            Destroy(Selected.Viewmodel);  
        }

        if (weapons.Count == 0) {
            return;
        }

        var weapon = GameDataManager.Instance.ActiveSave.GetWeapon(weapons[selectedIndex]);

        Selected.Viewmodel     = Instantiate(weapon.gameObject, viewmodelHolder, false);
        Selected.Weapon        = Selected.Viewmodel.GetComponent<Weapon>();
        weapons[selectedIndex] = weapon;

        if (!bulletMap.ContainsKey(Selected.Weapon.WeaponName)) {
            bulletMap.Add(Selected.Weapon.WeaponName, Selected.Weapon.WeaponData.magazineSize);
            Selected.Weapon.WeaponData.shotCount = Selected.Weapon.WeaponData.magazineSize;
        }
        else {
            Selected.Weapon.WeaponData.shotCount = bulletMap[Selected.Weapon.WeaponName];
        }

        Fire           += () => { bulletMap[Selected.Weapon.WeaponName] = Selected.Weapon.WeaponData.shotCount;    };
        ReloadFinished += () => { bulletMap[Selected.Weapon.WeaponName] = Selected.Weapon.WeaponData.magazineSize; };

        Selected.Weapon.Equip(this);

        Swap?.Invoke(weapons[selectedIndex]);
    }

    public bool AddWeapon(Weapon weapon, bool equip = false)
    {
        if (weapons.Contains(weapon) || weapons.Count >= maxWeapons - 1) return false;

        weapons.Add(weapon);

        GameDataManager.Instance.ActiveSave.UnlockWeapon(weapon);

        if (equip)
        {
            selectedIndex = weapons.Count - 1;
            SelectWeapon();
        }

        Added?.Invoke(weapon);

        return true;
    }

    public bool RemoveWeapon(Weapon weapon)
    {
        int index = weapons.IndexOf(weapon);
        if (index < 0) return false;

        GameDataManager.Instance.ActiveSave.RemoveWeapon(weapon);
        weapons.Remove(weapon);

        if (index == selectedIndex)
        {
            if (weapons.Count != 0)
            {
                selectedIndex++;
                selectedIndex %= weapons.Count;
            }
            else
            {
                selectedIndex = 0;
            }

            SelectWeapon();
        }

        if (weapons.Count == 0)
        {
            Selected.Weapon.UnEquip();
            Destroy(Selected.Viewmodel);
        }

        if (bulletMap.ContainsKey(weapon.WeaponName)) bulletMap.Remove(weapon.WeaponName);

        weapon.modifications.Clear();

        Removed?.Invoke(weapon);

        return true;
    }

    #endregion
}
