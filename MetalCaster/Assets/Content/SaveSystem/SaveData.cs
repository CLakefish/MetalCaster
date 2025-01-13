using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

[CustomEditor(typeof(SaveData))]
public class SaveDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SaveData data = (SaveData)target;
        GameDataManager manager = FindFirstObjectByType<GameDataManager>();

        if (GUILayout.Button("Unlock all Modifications"))
        {
            foreach (var mod in manager.ContentManager.Modifiers) data.UnlockModification(mod);
            manager.Write();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
        }

        if (GUILayout.Button("Unlock all Weapons"))
        {
            foreach (var weapon in manager.ContentManager.Weapons) data.UnlockWeapon(weapon);
            manager.Write();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
        }

        if (GUILayout.Button("Lock all Modifications"))
        {
            data.LockAllModifications();
            manager.Write();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
        }

        if (GUILayout.Button("Lock all Weapons"))
        {
            data.LockAllWeapons();
            manager.Write();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
        }

        base.OnInspectorGUI();
    }
}


#endif


[System.Serializable]
public class WeaponSaveData
{
    [System.Serializable]
    public class Slot
    {
        public int slot;
        public string ModificationName;
    }

    [SerializeField] public string weaponName;
    [SerializeField] public List<string> modificationNames;
    [SerializeField] public List<string> permanentModificationNames;
    [SerializeField] public List<Slot> modificationSlots = new();
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
            List<Modification> mods = new();
            foreach (var mod in modifications)
            {
                mods.Add(ContentManager.Instance.GetModificationByName(mod));
            }

            return mods;
        }
    }


    #region Statistic Management
    public string SaveName => saveName;
    public float  SaveTime => saveTime;

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

        // Unlocks the modification and saves it
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

        // If you already have the weapon unlocked, no need to re-add it
        if (unlockedWeapons.Any(w => w.weaponName == desiredWeapon.WeaponName))
        {
            return;
        }

        // Create the data, save it.
        // By default it will NOT be equipped!
        WeaponSaveData data = new()
        {
            weaponName                 = desiredWeapon.WeaponName,
            modificationNames          = desiredWeapon.modifications.ConvertAll(mod => mod.ModificationName),
            permanentModificationNames = desiredWeapon.permanentModifications.ConvertAll(mod => mod.ModificationName),
            modificationSlots          = new(),
        };

        unlockedWeapons.Add(data);
        SaveAltered?.Invoke();
    }

    #endregion

    #region Locking

    public void LockAllWeapons() {
        equippedWeapons.Clear();
        unlockedWeapons.Clear();
    }

    public void LockAllModifications() {
        modifications.Clear();
    }

    #endregion

    /// <summary>
    /// Rechecks save files to alter discrepancies with permanent modifications and other info
    /// </summary>
    public void RecheckSave() {
        // Permanent Modifications
        foreach (var weapon in GameDataManager.Instance.ContentManager.Weapons) {
            var data = equippedWeapons.Find(w => w.weaponName == weapon.WeaponName);
            if (data != null) {
                var mods = weapon.permanentModifications.ConvertAll(mod => mod.ModificationName);

                data.permanentModificationNames = mods;
                unlockedWeapons.Find(w => w.weaponName == weapon.WeaponName).permanentModificationNames = mods;
            }
        }
    }

    #region Weapon Equipping

    /// <summary>
    /// With a given weapon, if the weapon is not unlocked it will not equip. If the weapon is unlocked it will be equipped so long as its not a duplicate
    /// </summary>
    /// <param name="desiredWeapon"></param>
    /// <returns></returns>
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

        if (data.modificationSlots == null) {
            data.modificationSlots = new(desiredWeapon.BaseData.modificationSlots);
        }

        equippedWeapons.Add(data);
        SaveAltered?.Invoke();
        return true;
    }

    /// <summary>
    /// With a given weapon, if the weapon is equipped it will be removed from ONLY the equipped list
    /// </summary>
    /// <param name="desiredWeapon"></param>
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

    /// <summary>
    /// For a given weapon, save the data provided to both unlocked and equipped, saving the slots of the equipped modifications 
    /// </summary>
    /// <param name="desiredWeapon"></param>
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

        // Why clear it? Are you oupid :skull:
        unlockData.modificationSlots.Clear();
        equipData.modificationSlots.Clear();

        // Saving modifications, its a list of a set size initialized w/null by default, so theres no need to push it back.
        for (int i = 0; i < desiredWeapon.modifications.Count; i++)
        {
            var mod = desiredWeapon.modifications[i];
            if (mod == null) continue;

            WeaponSaveData.Slot slot = new()
            {
                slot = i,
                ModificationName = mod.ModificationName,
            };

            unlockData.modificationSlots.Add(slot);
            equipData.modificationSlots.Add(slot);
        }

        // Saving the permanent and customizable modifications via System.Linq (ty system.linq :pray:)

        unlockData.modificationNames = desiredWeapon.modifications
            .Where(m => m != null)
            .Select(m => m.ModificationName)
            .ToList();
        equipData.modificationNames = unlockData.modificationNames;

        var permMods = desiredWeapon.permanentModifications
            .Select(m => m.ModificationName)
            .ToList();
        unlockData.permanentModificationNames = permMods;
        equipData.permanentModificationNames = permMods;

        SaveAltered?.Invoke();
    }
    
    /// <summary>
    /// Returns a list of all weapons currently equipped
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Returns a weapon with its data reloaded (i.e. mods saved)
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Weapon ReloadWeaponData(Weapon data)
    {
        return InstantiateWeapon(equippedWeapons.Find(w => w.weaponName == data.WeaponName));
    }

    /// <summary>
    /// Returns a weapon with a given name
    /// </summary>
    /// <param name="weaponName"></param>
    /// <returns></returns>
    public Weapon GetWeaponByName(string weaponName)
    {
        var data = unlockedWeapons.Find(w => w.weaponName == weaponName);
        return InstantiateWeapon(data);
    }

    /// <summary>
    /// Instantiates a new weapon with a given name and applies all modifications, then returns it
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
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

        weapon.modifications = new();
        weapon.modifications = new(new Modification[weapon.BaseData.modificationSlots]);

        foreach (var entry in data.modificationSlots)
        {
            var mod = ContentManager.Instance.GetModificationByName(entry.ModificationName);

            if (entry.slot >= 0 && entry.slot <= weapon.BaseData.modificationSlots)
            {
                weapon.modifications[entry.slot] = mod;
            }
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
}
