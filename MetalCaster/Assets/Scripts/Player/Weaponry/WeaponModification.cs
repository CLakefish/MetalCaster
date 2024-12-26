using UnityEngine;

public class WeaponModification : ScriptableObject
{
    [SerializeField] private string modificationName;
    [TextArea, SerializeField] private string description;
    [SerializeField] private Sprite modificationSprite;

    public string ModificationName   => modificationName;
    public string Description        => description;
    public Sprite ModificationSprite => modificationSprite;

    public virtual void Modify(Weapon context)             { }
    public virtual void OnReload() { }
    public virtual void OnUpdate() { }

    public virtual void AltFire(Vector3 dir) { }

    // GameObject Specific
    public virtual void OnFire(ref Weapon.FireData data) { }

    // Raycast Specific
    public virtual void OnHit(RaycastHit hit, ref Weapon.FireData data) { }
    public virtual void OnMiss(ref Weapon.FireData payload) { }
}
