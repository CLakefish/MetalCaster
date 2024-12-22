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
    [SerializeField] private string saveName;
    [SerializeField] private float  saveTime;
    [SerializeField] private List<WeaponModification> modifications = new();
    [SerializeField] private List<WeaponSaveData> weapons = new();

    public string SaveName => saveName;
    public float  SaveTime => saveTime;

    public List<WeaponSaveData>     Weapons       => weapons;
    public List<WeaponModification> Modifications => modifications;

    public System.Action SaveAltered;

    public void SetSaveName(string name) => saveName = name;
    public void IncrementTime(float time) => saveTime += time;

    public void UnlockModification(WeaponModification modification)
    {
        if (modifications.Contains(modification)) return;
        modifications.Add(modification);
        SaveAltered?.Invoke();
    }

    public void RemoveWeapon(Weapon desiredWeapon)
    {
        if (weapons == null) return;

        for (int i = 0; i < weapons.Count; i++)
        {
            var weapon = weapons[i];

            if (weapon.weaponName == desiredWeapon.WeaponName)
            {
                weapon.modificationNames.Clear();
                weapons.Remove(weapon);
                SaveAltered?.Invoke();
                return;
            }
        }
    }

    public void SaveWeapon(Weapon desiredWeapon)
    {
        if (weapons == null) {
            weapons = new();
        }

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponSaveData entry = weapons[i];

            if (entry.weaponName == desiredWeapon.WeaponName)
            {
                entry.modificationNames          = desiredWeapon.modifications.ConvertAll(mod => mod.ModificationName);

                SaveAltered?.Invoke();
                return;
            }
        }

        WeaponSaveData data = new()
        {
            weaponName                 = desiredWeapon.WeaponName,
            modificationNames          = desiredWeapon.modifications.ConvertAll(mod => mod.ModificationName),
            permanentModificationNames = desiredWeapon.permanentModifications.ConvertAll(mod => mod.ModificationName),
        };

        weapons.Add(data);
        SaveAltered?.Invoke();
    }


    public List<Weapon> GetWeapons()
    {
        List<Weapon> weapons = new();

        foreach (var entry in Weapons)
        {
            var temp = ContentManager.Instance.GetWeaponDataByName(entry.weaponName);

            if (temp == null)
            {
                Debug.LogError("Weapon with ID: " + entry.weaponName + " not found!");
                continue;
            }

            Weapon data = Instantiate(temp);
            data.modifications.Clear();
            data.permanentModifications.Clear();

            foreach (string mod in entry.modificationNames) {
                var modRef = ContentManager.Instance.GetModificationByName(mod);
                data.modifications.Add(modRef);
            }

            foreach (var mod in entry.permanentModificationNames) {
                var modRef = ContentManager.Instance.GetModificationByName(mod);
                data.permanentModifications.Add(modRef);
            }

            weapons.Add(data);
        }

        return weapons;
    }
}
