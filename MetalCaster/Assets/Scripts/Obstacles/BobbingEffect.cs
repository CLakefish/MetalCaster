using UnityEngine;

public class BobbingEffect : MonoBehaviour
{
    [SerializeField] private float intensity;
    [SerializeField] private float speed;
    private Vector3 startPos;

    private void Start()  => startPos = transform.localPosition;
    private void Update() => transform.localPosition = Vector3.up * ((Mathf.Sin(Time.time * speed) - 0.5f) * intensity) + startPos;
}
