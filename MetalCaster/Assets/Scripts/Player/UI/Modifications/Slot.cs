using UnityEngine;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour, IDropHandler
{
    public virtual void OnDrop(PointerEventData data)
    {
        if (transform.childCount > 0) return;

        GameObject dropped = data.pointerDrag;
        Draggable item     = dropped.GetComponent<Draggable>();
        item.SetEndParent(transform);
    }
}
