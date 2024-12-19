using UnityEngine;

[System.Serializable]
public class WeaponModification : ScriptableObject
{
    [SerializeField] private bool unlocked;
    public bool Unlocked => unlocked;

    public virtual void Modify(Weapon context)                                 { }
                                                                               
    public virtual void OnFire(Weapon context)                                 { }
    public virtual void OnReload(Weapon context)                               { }
    public virtual void OnUpdate(Weapon context)                               { }

    public virtual void OnHit(Weapon context, RaycastHit hit, int bounceCount) { }
    public virtual void OnMiss(Weapon context)                                 { }
}
