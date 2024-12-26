using UnityEngine;

public class WeaponModification : ScriptableObject
{
    [SerializeField] private string modificationName;
    [TextArea, SerializeField] private string description;
    [SerializeField] private Sprite modificationSprite;

    public string ModificationName   => modificationName;
    public string Description        => description;
    public Sprite ModificationSprite => modificationSprite;

    public virtual void Modify(Weapon context)                                         { }
                                                                                       
    public virtual void OnReload(Weapon context)                                       { }
    public virtual void OnUpdate(Weapon context)                                       { }

    public virtual void AltFire(Weapon context, Vector3 dir)                           { }


    // GameObject Specific
    public virtual void OnFire(Weapon context)                                         { }

    // Raycast Specific
    public virtual void OnHit(Weapon context, RaycastHit hit, ref WeaponModificationData payload) { }
    public virtual void OnMiss(Weapon context, Vector3 dir,   ref WeaponModificationData payload) { }
}
