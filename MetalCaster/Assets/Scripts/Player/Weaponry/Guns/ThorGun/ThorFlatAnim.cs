using UnityEngine;

public class ThorFlatAnim : MonoBehaviour
{
    [Header("Sizing")]
    [SerializeField] private float endSize;
    [SerializeField] private float scaleTime;
    [SerializeField] private float rotationSpeed;
    private Vector3 scaleVel;

    private void Start() => transform.localScale = Vector3.zero;

    void Update()
    {
        transform.Rotate(Vector3.up, Time.deltaTime * rotationSpeed, Space.Self);
        transform.localScale = Vector3.SmoothDamp(transform.localScale, Vector3.one * endSize, ref scaleVel, scaleTime);
    }
}
