using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ModificationDraggable : Draggable
{
    public WeaponModification modification;
    public System.Action onDrop;
    public void SetModification(WeaponModification mod) => modification = mod;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (transform.parent.TryGetComponent(out ModificationSlot slot)) {
            slot.Menu.GetWeapon().Weapon.RemoveModification(modification);
            onDrop?.Invoke();
        }

        base.OnBeginDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);

        if (transform.parent.TryGetComponent(out ModificationSlot slot)) {
            slot.Menu.GetWeapon().Weapon.AddModification(modification);
            onDrop?.Invoke();
        }
    }
}