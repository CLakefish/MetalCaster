using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ModificationDraggable : Draggable, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Interpolation")]
    [SerializeField] private float scaleSize;
    [SerializeField] private float scaleSpeed;

    private WeaponModification modification;
    private Coroutine scale;

    private const float EPSILON = 0.01f;

    public System.Action onDrop;
    public System.Action showPopup;
    public System.Action hidePopup;

    public void SetReferences(WeaponModification modification) => this.modification = modification;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (transform.parent.TryGetComponent(out ModificationSlot slot)) {
            slot.Menu.GetWeapon().Weapon.RemoveModification(modification);
            onDrop?.Invoke();
        }

        hidePopup?.Invoke();

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.Instance.DisplayPopup(modification);

        if (Mathf.Abs(transform.localScale.x - scaleSize) < EPSILON) return;

        if (scale != null) StopCoroutine(scale);
        scale = StartCoroutine(Scale(scaleSize));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Mathf.Abs(transform.localScale.x - 1) < EPSILON) return;

        if (scale != null) StopCoroutine(scale);
        scale = StartCoroutine(Scale(1));
    }

    private IEnumerator Scale(float newScale)
    {
        float scaleVel = 0;
        float start    = transform.localScale.x;

        while (Mathf.Abs(scaleSize - start) > Mathf.Epsilon)
        {
            start                = Mathf.SmoothDamp(start, newScale, ref scaleVel, scaleSpeed, Mathf.Infinity, Time.unscaledDeltaTime);
            transform.localScale = Vector3.one * start;
            yield return null;
        }

        transform.localScale = Vector3.one * newScale;
    }
}