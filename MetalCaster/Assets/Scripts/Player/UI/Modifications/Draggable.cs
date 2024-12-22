using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private Image icon;
    private Transform transformParent;
    private Transform onDragParent;

    public void OnDragParent(Transform parent)    => onDragParent = parent;
    public void SetEndParent(Transform transform) => transformParent = transform;

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        SetEndParent(transform.parent);
        transform.SetParent(onDragParent);
        transform.SetAsLastSibling();
        icon.raycastTarget = false;
    }

    public virtual void OnDrag(PointerEventData eventData) => transform.position = Input.mousePosition;

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(transformParent);
        icon.raycastTarget = true;
    }
}
