using UnityEngine;

[CreateAssetMenu(menuName = "Input/Vector2 Input")]
public class Vector2Input : InputScriptableObject
{
    [Header("Debugging")]
    [SerializeField] private Vector2 value;
    [SerializeField] private bool active;

    public Vector2 Value           { get { return value; } }
    public Vector2 NormalizedValue { get { return value.normalized; } }

    public bool Active { get { return active; } }

    public override void Update()
    {
        value  = action.ReadValue<Vector2>();
        active = value != Vector2.zero;
    }
}
