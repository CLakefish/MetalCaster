using System.Collections.Generic;
using UnityEngine;

public class ModificationManager : PlayerWeapon.PlayerWeaponSystem
{
    private readonly Dictionary<string, Modification> modificationLookup = new();
    private readonly List<Modification> modifications = new();

    public List<Modification> ActiveModifications => modifications;

    public void AddModification(string modName) {
        if (modificationLookup.ContainsKey(modName)) {
            modificationLookup[modName].MarkedToRemove = false;
            return;
        }

        var mod = ContentManager.Instance.GetModificationByName(modName);
        mod.SetPlayer(Player);
        modificationLookup.Add(modName, mod);
        modifications.Add(mod);
    }

    public void AddModifications(List<Modification> mods) {
        foreach (var mod in mods) {
            AddModification(mod.ModificationName);
        }
    }

    private void RemoveModification(string modName) {
        modifications.Remove(modificationLookup[modName]);
        modificationLookup.Remove(modName);
    }

    public void ApplyAllModifications(ref Bullet bullet) {
        foreach (var mod in bullet.modifications) {
            if (!modificationLookup.ContainsKey(mod))
            {
                Debug.Log("Modification of type " + mod + " was not found!");
                continue;
            }

            modificationLookup[mod].ProvideBullet(ref bullet);
        }
    }

    public void MarkModifications(List<Modification> mods) {
        foreach (var mod in mods) {
            mod.MarkedToRemove = true;
        }
    }

    public void UpdateModifications() {
        for (int i = 0; i < modifications.Count; ++i) {
            var mod = modifications[i];
            mod.OnUpdate();
        }
    }

    public void CheckModificationRemoval()
    {
        for (int i = 0; i < modifications.Count; ++i) {
            var mod = modifications[i];
            if (mod.IsEmpty() && mod.MarkedToRemove) {
                RemoveModification(mod.ModificationName);
            }
        }
    }
}
