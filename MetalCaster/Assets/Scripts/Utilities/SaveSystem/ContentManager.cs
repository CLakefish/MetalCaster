using System.Collections.Generic;
using UnityEngine;

public class ContentManager : MonoBehaviour
{
    [SerializeField] private List<WeaponModification> modifiers;
    [SerializeField] private List<Weapon> weapons;

    private Dictionary<string, Weapon> weaponLookup;
    private Dictionary<string, WeaponModification> modificationLookup;

    public List<WeaponModification> Modifiers => modifiers;

    public static ContentManager Instance;

    public void Awake()
    {
        if (Instance == null) Instance = this;

        weaponLookup = new();
        modificationLookup = new();

        foreach (var mod in modifiers) {
            modificationLookup[mod.ModificationName] = mod;
        }

        foreach (var weapon in weapons) {
            weaponLookup[weapon.WeaponName] = weapon;
        }
    }

    public Weapon GetWeaponDataFromWeapon(Weapon weapon)
    {
        foreach (var data in weapons) {
            if (data.WeaponName == weapon.WeaponName) return weaponLookup[data.WeaponName];
        }

        return null;
    }

    public Weapon GetWeaponDataByName(string weaponName)
    {
        weaponLookup.TryGetValue(weaponName, out Weapon foundWeapon);
        return foundWeapon;
    }

    public WeaponModification GetModificationByName(string modName)
    {
        modificationLookup.TryGetValue(modName, out WeaponModification foundMod);
        return foundMod;
    }
}
