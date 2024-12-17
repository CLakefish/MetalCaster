using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Input/Bool Input")]
public class BoolInput : InputScriptableObject
{
    [Header("Debugging")]
    [SerializeField] private bool held     = false;
    [SerializeField] private bool pressed  = false;
    [SerializeField] private bool released = false;

    public bool Pressed  { get { return !Locked && pressed;  }  }
    public bool Held     { get { return !Locked && held; } }
    public bool Released { get { return !Locked && released; } }

    public override void Update()
    {
        pressed  = action.WasPressedThisFrame();
        released = action.WasReleasedThisFrame();
        held     = pressed || (held && !released);
    }
}
