using UnityEngine;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour, IDropHandler
{
    public virtual void OnDrop(PointerEventData data)
    {
        if (transform.childCount > 0) return;

        GameObject dropped = data.pointerDrag;

        if (dropped == null) return;
        if (!dropped.TryGetComponent<Draggable>(out Draggable item)) return;

        item.SetEndParent(transform);
    }
}
