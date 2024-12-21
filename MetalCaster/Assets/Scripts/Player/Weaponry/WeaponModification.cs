using UnityEngine;

[System.Serializable]
public class WeaponModification : ScriptableObject
{
    [SerializeField] private bool unlocked;
    public bool Unlocked => unlocked;

    public virtual void Modify(Weapon context)                                         { }
                                                                                       
    public virtual void OnReload(Weapon context)                                       { }
    public virtual void OnUpdate(Weapon context)                                       { }

    public virtual void AltFire(Weapon context, Vector3 dir) { }


    // GameObject Specific
    public virtual void OnFire(Weapon context)                                         { }

    // Raycast Specific
    public virtual void OnHit(Weapon context, RaycastHit hit, ref ShotPayload payload) { }
    public virtual void OnMiss(Weapon context, Vector3 dir, ref ShotPayload payload)   { }
}
