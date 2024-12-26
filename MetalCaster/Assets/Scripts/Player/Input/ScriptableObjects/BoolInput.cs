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
        bool wasHeld   = held;
        bool isPressed = action.IsPressed();

        pressed  = !wasHeld && isPressed;
        released = wasHeld && !isPressed;
        held     = isPressed;
    }
}
