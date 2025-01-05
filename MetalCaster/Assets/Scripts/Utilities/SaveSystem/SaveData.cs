using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class WeaponSaveData
{
    [SerializeField] public string weaponName;
    [SerializeField] public List<string> modificationNames;
    [SerializeField] public List<string> permanentModificationNames;
    //public Dictionary<string, int> modificationSlots;
    //public int totalModificationSlots;
}

[CreateAssetMenu(menuName = "Data/Save Data")]
public class SaveData : ScriptableObject
{
    [Header("Data")]
    [SerializeField] private string saveName;
    [SerializeField] private float  saveTime;
    [Header("Unlocks")]
    [SerializeField] private List<string>             modifications   = new();
    [SerializeField] private List<WeaponSaveData>     equippedWeapons = new();
    [SerializeField] private List<WeaponSaveData>     unlockedWeapons = new();

    public event System.Action SaveAltered;

    public List<WeaponSaveData>     EquippedWeapons => equippedWeapons;
    public List<WeaponSaveData>     UnlockedWeapons => unlockedWeapons;
    public List<Modification> Modifications
    {
        get
        {
            List<Modification> mods = new List<Modification>();
            foreach (var mod in modifications)
            {
                mods.Add(ContentManager.Instance.GetModificationByName(mod));
            }

            return mods;
        }
    }


    #region Statistic Management
    public string SaveName => saveName;
    public float SaveTime  => saveTime;

    public void SetSaveName(string name)  => saveName = name;
    public void IncrementTime(float time) => saveTime += time;

    #endregion

    #region Unlocks

    /// <summary>
    /// Adds modification to list of unlocked modification
    /// </summary>
    /// <param name="modification"></param>
    public void UnlockModification(Modification modification)
    {
        if (modification == null) return;
        if (modifications.Contains(modification.ModificationName)) return;

        modifications.Add(modification.ModificationName);
        SaveAltered?.Invoke();
    }

    /// <summary>
    /// Add a weapon to the list of unlocked weapons
    /// By default will not equip it!
    /// </summary>
    /// <param name="desiredWeapon"></param>
    public void UnlockWeapon(Weapon desiredWeapon) 
    {
        if (desiredWeapon == null)
        {
            Debug.LogWarning("Weapon to unlock is null!");
            return;
        }

        if (unlockedWeapons.Any(w => w.weaponName == desiredWeapon.WeaponName)) return;

        WeaponSaveData data = new()
        {
            weaponName                 = desiredWeapon.WeaponName,
            modificationNames          = desiredWeapon.modifications.ConvertAll(mod => mod.ModificationName),
            permanentModificationNames = desiredWeapon.permanentModifications.ConvertAll(mod => mod.ModificationName),
            //modificationSlots          = new(),
            //totalModificationSlots     = desiredWeapon.WeaponData.modificationSlots,
        };

        unlockedWeapons.Add(data);
        SaveAltered?.Invoke();
    }

    #endregion

    #region Weapon Equipping

    public bool EquipWeapon(Weapon desiredWeapon)
    {
        if (desiredWeapon == null)
        {
            Debug.LogWarning("Weapon to equip is null!");
            return false;
        }

        var data = unlockedWeapons.Find(w => w.weaponName == desiredWeapon.WeaponName);
        if (data == null)
        {
            Debug.LogWarning("Weapon is not unlocked lmao.");
            return false;
        }

        if (equippedWeapons.Any(w => w.weaponName == desiredWeapon.WeaponName)) return false;

        /*if (data.modificationSlots == null)
        {
            data.totalModificationSlots = desiredWeapon.WeaponData.modificationSlots;
            data.modificationSlots = new(data.totalModificationSlots);
        }*/

        equippedWeapons.Add(data);
        SaveAltered?.Invoke();
        return true;
    }

    public void UnEquipWeapon(Weapon desiredWeapon)
    {
        if (desiredWeapon == null)
        {
            Debug.LogWarning("Weapon to unequip is null!");
            return;
        }

        var data = equippedWeapons.Find(w => w.weaponName == desiredWeapon.WeaponName);
        if (data == null)
        {
            Debug.LogWarning("Weapon is not equipped somehow lmao.");
            return;
        }

        equippedWeapons.Remove(data);
        SaveAltered?.Invoke();
    }

    #endregion


    public void SaveWeapon(Weapon desiredWeapon)
    {
        if (desiredWeapon == null) return;

        var unlockData = unlockedWeapons.Find(w => w.weaponName == desiredWeapon.WeaponName);
        var equipData  = equippedWeapons.Find(w => w.weaponName == desiredWeapon.WeaponName);

        if (unlockData == null || equipData == null)
        {
            Debug.LogWarning("Weapon is not unlocked lmao.");
            return;
        }

        /*
        Dictionary<string, int> modSlots = new();
        List<string> mods = new();

        for (int i = 0; i < desiredWeapon.WeaponData.modificationSlots; ++i)
        {
            var mod = desiredWeapon.modifications[i];

            if (mod == null) continue;
            
            mods.Add(mod.ModificationName);
            modSlots.Add(mod.ModificationName, i);
        }

        List<string> permMods = desiredWeapon.permanentModifications.ConvertAll(w => w.ModificationName);*/
        var mods = desiredWeapon.modifications.ConvertAll(w => w.ModificationName);
        var permMods = desiredWeapon.permanentModifications.ConvertAll(w => w.ModificationName);

        unlockData.modificationNames = mods;
        equipData.modificationNames  = mods;
        unlockData.permanentModificationNames = permMods;
        equipData.permanentModificationNames  = permMods;

        //equipData.modificationSlots  = modSlots;
        //unlockData.modificationSlots = modSlots;

        SaveAltered?.Invoke();
    }

    public List<Weapon> GetEquippedWeapons()
    {
        List<Weapon> weapons = new();

        foreach (var weapon in equippedWeapons)
        {
            Weapon instantiated = InstantiateWeapon(weapon);
            if (instantiated != null) weapons.Add(instantiated);
        }

        return weapons;
    }

    public Weapon ReloadWeaponData(Weapon data)
    {
        return InstantiateWeapon(equippedWeapons.Find(w => w.weaponName == data.WeaponName));
    }

    private Weapon InstantiateWeapon(WeaponSaveData data)
    {
        if (data == null) return null;

        Weapon weapon = ContentManager.Instance.GetWeaponDataByName(data.weaponName);
        if (weapon == null)
        {
            Debug.LogWarning("Weapon not found in ContentManager! Weapon type: " + weapon.WeaponName);
            return null;
        }

        weapon.modifications.Clear();
        weapon.permanentModifications.Clear();

        //weapon.modifications = new(data.totalModificationSlots);

        foreach (var name in data.modificationNames)
        {
            var mod = ContentManager.Instance.GetModificationByName(name);
            if (mod == null) {
                Debug.LogError("Unable to load modification! Modification ID: " + mod.ModificationName);
                continue;
            }

            /*if (data.modificationSlots == null)
            {
                weapon.modifications.Add(mod);
                continue;
            }

            weapon.modifications[data.modificationSlots[mod.ModificationName]] = mod;*/
            weapon.modifications.Add(mod);
        }

        foreach (var name in data.permanentModificationNames)
        {
            var mod = ContentManager.Instance.GetModificationByName(name);
            if (mod == null)
            {
                Debug.LogError("Unable to load modification! Modification ID: " + mod.ModificationName);
                continue;
            }

            weapon.permanentModifications.Add(mod);
        }

        return weapon;
    }

    public Weapon GetWeaponByName(string weaponName)
    {
        var data = unlockedWeapons.Find(w => w.weaponName == weaponName);
        return InstantiateWeapon(data);
    }
}
