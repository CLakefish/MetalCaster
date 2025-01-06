using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Content Manager")]
public class ContentManager : ScriptableObject
{
    public static ContentManager Instance { get; private set; }

    [SerializeField] private List<Modification> modifiers;
    [SerializeField] private List<Weapon>       weapons;

    private Dictionary<string, Weapon>       weaponLookup;
    private Dictionary<string, Modification> modificationLookup;

    public List<Modification> Modifiers => modifiers;
    public List<Weapon>       Weapons   => weapons;

    public void Enable() {
        Instance = this;

        weaponLookup       = new();
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
        return Instantiate(foundWeapon);
    }

    public Modification GetModificationByName(string modName)
    {
        modificationLookup.TryGetValue(modName, out Modification foundMod);
        return foundMod;
    }
}
