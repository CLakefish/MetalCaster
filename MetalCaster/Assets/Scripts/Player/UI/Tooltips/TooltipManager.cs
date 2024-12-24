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
    [SerializeField] private float bobSpeed, bobIntensity;
    [SerializeField] private float rotateSpeedZ, rotateSpeedX, rotateIntensityZ, rotateIntensityX;
    private Coroutine position;
    private Vector3 velocity = Vector3.zero;

    private void OnEnable() {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        popup.transform.position = startPosition.position;
        popup.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (popup.gameObject.activeSelf)
        {
            popup.transform.eulerAngles = new Vector3(Mathf.Cos(Time.unscaledTime * rotateSpeedX) * rotateIntensityX, 0, Mathf.Sin(Time.unscaledTime * rotateSpeedZ) * rotateIntensityZ);
        }
    }

    public void DisplayPopup(WeaponModification mod)
    {
        popup.Set(mod.ModificationName, mod.Description);
        popup.gameObject.SetActive(true);
        HandleMovement(endPosition, true);
    }

    public void HidePopup() => HandleMovement(startPosition, false);

    private void HandleMovement(RectTransform pos, bool active)
    {
        if (position != null) {
            StopCoroutine(position);
        }

        position = StartCoroutine(MovePosition(pos, active));
    }

    private IEnumerator MovePosition(RectTransform pos, bool active)
    {
        while (Vector3.Distance(pos.position, popup.transform.position) > Mathf.Epsilon)
        {
            Vector3 desiredPos = pos.position;
            desiredPos        += Vector3.up * (Mathf.Sin(Time.unscaledTime * bobSpeed) * bobIntensity);

            popup.transform.position = Vector3.SmoothDamp(popup.transform.position, desiredPos, ref velocity, moveSpeed, Mathf.Infinity, Time.unscaledDeltaTime);
            yield return null;
        }

        popup.transform.position = pos.position;
        popup.gameObject.SetActive(active);
    }
}
