using UnityEngine;
using System.Collections;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [SerializeField] private Popup popup;

    [Header("Movement")]
    [SerializeField] private RectTransform startPosition;
    [SerializeField] private RectTransform endPosition;
    [SerializeField] private float moveSpeed;
    private RectTransform desiredPos;
    private Coroutine position;

    private void OnEnable() {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        popup.transform.position = startPosition.position;
        popup.gameObject.SetActive(false);
    }

    public void DisplayPopup(WeaponModification mod)
    {
        if (position != null) {
            StopCoroutine(position);
        }

        popup.gameObject.SetActive(true);
        HandleMovement(endPosition, true);
        popup.Set(mod.ModificationName, mod.Description);
    }

    public void HidePopup()
    {
        HandleMovement(startPosition, false);
    }

    private void HandleMovement(RectTransform pos, bool active)
    {
        if (pos == desiredPos) return;

        desiredPos = pos;

        if (position != null) {
            StopCoroutine(position);
        }

        position = StartCoroutine(MovePosition(pos, active));
    }

    private IEnumerator MovePosition(RectTransform pos, bool active)
    {
        Vector3 velocity = Vector3.zero;

        while (Vector3.Distance(pos.position, popup.transform.position) > Mathf.Epsilon)
        {
            popup.transform.position = Vector3.SmoothDamp(popup.transform.position, pos.position, ref velocity, moveSpeed, Mathf.Infinity, Time.unscaledDeltaTime);
            yield return null;
        }

        popup.transform.position = pos.position;
        popup.gameObject.SetActive(active);
    }
}
