using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerWeapon : Player.PlayerComponent
{
    public class PlayerWeaponSystem : MonoBehaviour {
        protected Player       Player;
        protected PlayerWeapon PlayerWeapon;

        public void Initialize(PlayerWeapon playerWeapon, Player player) {
            PlayerWeapon = playerWeapon;
            Player       = player;
        }
    }

    [Header("References")]
    [SerializeField] private BulletManager       bulletManager;
    [SerializeField] private ModificationManager modificationManager;
    [SerializeField] private Transform           viewmodelHolder;

    [Header("Weapon Management")]
    [SerializeField] private int maxWeapons = 3;

    public BulletManager       BulletManager       => bulletManager;
    public ModificationManager ModificationManager => modificationManager;

    public (Weapon Weapon, GameObject Viewmodel) Selected { get; set; }

    private readonly Dictionary<string, int> bulletMap       = new();
    private readonly List<Weapon>            equippedWeapons = new();

    private int selectedIndex = 0;

    private void Start()
    {
        bulletManager.Initialize(this, Player);
        modificationManager.Initialize(this, Player);

        foreach (var weapon in GameDataManager.Instance.ActiveSave.GetEquippedWeapons()) {
            equippedWeapons.Add(weapon);
            PlayerHUD.AddWeapon(weapon);
        }

        if (equippedWeapons.Count > 0) {
            PlayerHUD.SelectWeapon(equippedWeapons[selectedIndex]);
        }

        SelectWeapon();
    }

    private void Update()
    {
        modificationManager.UpdateModifications();
        bulletManager.OnUpdate();
        modificationManager.CheckModificationRemoval();

        if (PlayerInput.SlotPressed) {
            int desiredIndex = 0;

            if (PlayerInput.Slot.One.Pressed)   desiredIndex = 0;
            if (PlayerInput.Slot.Two.Pressed)   desiredIndex = 1;
            if (PlayerInput.Slot.Three.Pressed) desiredIndex = 2;

            desiredIndex  = Mathf.Clamp(desiredIndex, 0, equippedWeapons.Count - 1);
            if (selectedIndex != desiredIndex)
            {
                selectedIndex = desiredIndex;
                SelectWeapon();
            }
        }

        if (Selected.Weapon == null) return;

        if (PlayerInput.Mouse.Left.Held) {
            Selected.Weapon.Fire();
        }

        if (PlayerInput.Mouse.Right.Held) {
            Selected.Weapon.AltFire();
        }

        Selected.Weapon.CheckReload();
    }

    #region Weapons

    private void SelectWeapon()
    {
        if (Selected.Viewmodel != null)
        {
            ModificationManager.MarkModifications(GetModifications(Selected.Weapon));
            Selected.Weapon.UnEquip();
            Destroy(Selected.Viewmodel);
        }

        if (equippedWeapons.Count == 0) return;

        equippedWeapons[selectedIndex] = GameDataManager.Instance.ActiveSave.ReloadWeaponData(equippedWeapons[selectedIndex]);
        GameObject model = Instantiate(equippedWeapons[selectedIndex].gameObject, viewmodelHolder, false);
        Weapon newWeapon = model.GetComponent<Weapon>();
        Selected         = (newWeapon, model);

        newWeapon.Equip(Player);

        newWeapon.OnFire      += () => { bulletMap[newWeapon.WeaponName] = newWeapon.WeaponData.shotCount; };
        newWeapon.OnReloadEnd += () => { bulletMap[newWeapon.WeaponName] = newWeapon.WeaponData.magazineSize; };

        if (!bulletMap.ContainsKey(newWeapon.WeaponName)) {
            bulletMap.Add(newWeapon.WeaponName, newWeapon.WeaponData.magazineSize);
        }

        newWeapon.WeaponData.shotCount = bulletMap[Selected.Weapon.WeaponName];

        foreach (var mod in Selected.Weapon.modifications) {
            modificationManager.AddModification(mod.ModificationName);
        }

        foreach (var mod in Selected.Weapon.permanentModifications) {
            modificationManager.AddModification(mod.ModificationName);
        }

        PlayerHUD.SelectWeapon(newWeapon);
    }

    public bool AddWeapon(Weapon weapon, bool equip = false)
    {
        GameDataManager.Instance.ActiveSave.UnlockWeapon(weapon);

        if (equippedWeapons.Count >= maxWeapons)
        {
            Debug.LogWarning("Inventory full! Cannot hold weapons");
            return false;
        }

        if (GameDataManager.Instance.ActiveSave.EquipWeapon(weapon)) {
            Weapon w = GameDataManager.Instance.ActiveSave.GetWeaponByName(weapon.WeaponName);

            if (w == null) return false;

            equippedWeapons.Add(w);

            PlayerHUD.AddWeapon(weapon);

            if (equip) {
                selectedIndex = equippedWeapons.Count - 1;
                PlayerHUD.SelectWeapon(equippedWeapons[selectedIndex]);
                SelectWeapon();
            }

            return true;
        }

        return false;
    }

    public bool RemoveWeapon(Weapon weaponToRemove)
    {
        GameDataManager.Instance.ActiveSave.UnEquipWeapon(weaponToRemove);

        int index = equippedWeapons.FindIndex(w => w.WeaponName == weaponToRemove.WeaponName);
        if (index < 0) return false;

        equippedWeapons.RemoveAt(index);
        bulletMap.Remove(weaponToRemove.WeaponName);

        if (index == selectedIndex) {
            if (equippedWeapons.Count != 0) {
                selectedIndex++;
                selectedIndex %= equippedWeapons.Count;
            }
            else {
                selectedIndex = 0;
            }

            if (equippedWeapons.Count > 0)
            {
                PlayerHUD.SelectWeapon(equippedWeapons[selectedIndex]);
                SelectWeapon();
            }
            else
            {
                Selected.Weapon.UnEquip();
                Destroy(Selected.Viewmodel);
            }
        }

        var mods = GetModifications(weaponToRemove);
        PlayerHUD.RemoveWeapon(weaponToRemove);

        foreach (var weapon in equippedWeapons) {
            if (weapon.WeaponName == weaponToRemove.WeaponName) continue;

            foreach (var mod in weapon.modifications) {
                if (mods.Contains(mod)) mods.Remove(mod);
            }

            foreach (var mod in weapon.permanentModifications) {
                if (mods.Contains(mod)) mods.Remove(mod);
            }
        }

        ModificationManager.MarkModifications(mods);

        return true;
    }

    private List<Modification> GetModifications(Weapon weapon)
    {
        List<Modification> mods = new();

        mods.AddRange(weapon.modifications);
        mods.AddRange(weapon.permanentModifications);
        return mods;
    }

    #endregion
}
