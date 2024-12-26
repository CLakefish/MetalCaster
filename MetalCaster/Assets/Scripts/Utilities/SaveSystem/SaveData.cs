using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponSaveData
{
    [SerializeField] public string weaponName;
    [SerializeField] public List<string> modificationNames;
    [SerializeField] public List<string> permanentModificationNames;
}

[CreateAssetMenu(menuName = "Data/Save Data")]
public class SaveData : ScriptableObject
{
    [Header("Data")]
    [SerializeField] private string saveName;
    [SerializeField] private float  saveTime;
    [Header("Unlocks")]
    [SerializeField] private List<WeaponModification> modifications = new();
    [SerializeField] private List<WeaponSaveData>   equippedWeapons = new();
    [SerializeField] private List<WeaponSaveData>   weapons         = new();

    public string SaveName => saveName;
    public float  SaveTime => saveTime;

    public List<WeaponSaveData>     EquippedWeapons => equippedWeapons;
    public List<WeaponModification> Modifications   => modifications;

    public System.Action SaveAltered;

    public void SetSaveName(string name) => saveName = name;
    public void IncrementTime(float time) => saveTime += time;

    public void UnlockModification(WeaponModification modification)
    {
        if (modifications.Contains(modification)) return;
        modifications.Add(modification);
        SaveAltered?.Invoke();
    }

    public void UnlockWeapon(Weapon desiredWeapon) 
    {
        WeaponSaveData data = new()
        {
            weaponName = desiredWeapon.WeaponName,
            modificationNames = desiredWeapon.modifications.ConvertAll(mod => mod.ModificationName),
            permanentModificationNames = desiredWeapon.permanentModifications.ConvertAll(mod => mod.ModificationName),
        };

        foreach (var weapon in weapons)
        {
            if (weapon.weaponName == data.weaponName)
            {
                if (equippedWeapons.Contains(weapon)) return;

                equippedWeapons.Add(weapon);
                SaveAltered?.Invoke();
                return;
            }
        }

        equippedWeapons.Add(data);
        weapons.Add(data);

        SaveAltered?.Invoke();
    }

    public void RemoveWeapon(Weapon desiredWeapon)
    {
        if (equippedWeapons == null) return;

        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            var weapon = equippedWeapons[i];

            if (weapon.weaponName == desiredWeapon.WeaponName)
            {
                weapon.modificationNames.Clear();
                equippedWeapons.Remove(weapon);
                SaveAltered?.Invoke();
                return;
            }
        }
    }

    public void SaveWeapon(Weapon desiredWeapon)
    {
        if (equippedWeapons == null) {
            equippedWeapons = new();
        }

        if (weapons == null) {
            weapons = new();
        }

        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            WeaponSaveData entry = equippedWeapons[i];

            if (entry.weaponName == desiredWeapon.WeaponName)
            {
                entry.modificationNames          = desiredWeapon.modifications.ConvertAll(mod => mod.ModificationName);
                entry.permanentModificationNames = desiredWeapon.permanentModifications.ConvertAll(mod => mod.ModificationName);

                SaveAltered?.Invoke();
                return;
            }
        }

        Debug.LogError("Unable to save weapon " + desiredWeapon.WeaponName + "! It either is not unlocked, or it is null!");
    }


    public List<Weapon> GetWeapons()
    {
        List<Weapon> weapons = new();

        foreach (var entry in EquippedWeapons)
        {
            Weapon data = ContentManager.Instance.GetWeaponDataByName(entry.weaponName);
            weapons.Add(GetWeapon(data));
        }

        return weapons;
    }

    public Weapon GetWeapon(Weapon weapon)
    {
        WeaponSaveData temp = null;

        foreach (var entry in EquippedWeapons)
        {
            if (entry.weaponName == weapon.WeaponName)
            {
                temp = entry;
                break;
            }
        }

        if (temp == null)
        {
            Debug.LogError("Weapon not found in save! File: " + weapon.WeaponName);
            return null;
        }

        Weapon data = ContentManager.Instance.GetWeaponDataByName(temp.weaponName);

        if (data == null)
        {
            Debug.LogError("Weapon with ID: " + weapon.WeaponName + " not found!");
            return null;
        }

        data.modifications.Clear();
        data.permanentModifications.Clear();

        foreach (string mod in temp.modificationNames)
        {
            var modRef = ContentManager.Instance.GetModificationByName(mod);
            if (modRef == null)
            {
                Debug.LogError($"Modification '{mod}' not found! Skipping.");
                continue;
            }
            data.modifications.Add(modRef);
        }

        foreach (var mod in temp.permanentModificationNames)
        {
            var modRef = ContentManager.Instance.GetModificationByName(mod);
            if (modRef == null)
            {
                Debug.LogError($"Modification '{mod}' not found! Skipping.");
                continue;
            }
            data.permanentModifications.Add(modRef);
        }

        return data;
    }

    public void ResetWeapons()
    {
        foreach (var entry in EquippedWeapons)
        {
            var data = ContentManager.Instance.GetWeaponDataByName(entry.weaponName);

            if (data == null)
            {
                Debug.LogError("Weapon with ID: " + entry.weaponName + " not found!");
                continue;
            }

            data.modifications.Clear();
        }
    }
}
