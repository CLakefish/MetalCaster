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

    /// <summary>
    /// Returns the weapon from a given name. It IS instantiated
    /// </summary>
    /// <param name="weaponName"></param>
    /// <returns></returns>
    public Weapon GetWeaponDataByName(string weaponName)
    {
        weaponLookup.TryGetValue(weaponName, out Weapon foundWeapon);
        return Instantiate(foundWeapon);
    }

    /// <summary>
    /// Returns the modification scriptable object. It is NOT instantiated. 
    /// </summary>
    /// <param name="modName"></param>
    /// <returns></returns>
    public Modification GetModificationByName(string modName)
    {
        modificationLookup.TryGetValue(modName, out Modification foundMod);
        return foundMod;
    }
}
