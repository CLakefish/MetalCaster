using UnityEngine;

public class RotateEffect : MonoBehaviour
{
    [SerializeField] private float rotationSpeed;

    void Update() => transform.eulerAngles += rotationSpeed * Time.deltaTime * Vector3.up;
}
