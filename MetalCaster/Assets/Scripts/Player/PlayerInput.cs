using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : Player.PlayerComponent
{
    [Header("Debugging")]
    [SerializeField] private Vector2 inputs;
    [SerializeField] private Vector2 mouseVec;
    [SerializeField] private bool inputting;
    
    [Header("Variables")]
    [SerializeField] private Vector2 sensitivity;
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;

    public Vector2 Input                { get { return inputs; } }
    public Vector2 NormalizedInput      { get { return inputs.normalized; } }

    public Vector2 MousePosition        { get { return mouseVec;  } }
    public Vector2 AlteredMousePosition { get { return new Vector2(mouseVec.x * (invertX ? -1 : 1) * sensitivity.x, mouseVec.y * (invertY ? -1 : 1) * sensitivity.y); } }

    public bool IsInputting             { get { return inputting; } }

    private void Update()
    {
        mouseVec  = new Vector2(UnityEngine.Input.GetAxisRaw("Mouse X"), UnityEngine.Input.GetAxisRaw("Mouse Y"));
        inputs    = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
        inputting = inputs != Vector2.zero;
    }
}
