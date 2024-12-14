using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : Player.PlayerComponent
{
    [Header("Debugging")]
    [SerializeField] private UnityEngine.InputSystem.PlayerInput map;
    [SerializeField] private BoolInput jump;
    [SerializeField] private Vector2Input input;
    
    [Header("Variables")]
    [SerializeField] private Vector2 sensitivity;
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;

    private HashSet<InputScriptableObject> inputSet;

    public Vector2 Input { 
        get {
            return input.Value;
        } 
    }
    public Vector2 NormalizedInput { 
        get {
            return input.NormalizedValue; 
        } 
    }

    public Vector2 MousePosition { 
        get { 
            return map.actions["View"].ReadValue<Vector2>(); 
        } 
    }

    public Vector2 AlteredMousePosition { 
        get { 
            return new Vector2(MousePosition.x * (invertX ? -1 : 1) * sensitivity.x, MousePosition.y * (invertY ? -1 : 1) * sensitivity.y);
        } 
    }

    public bool IsInputting { 
        get { 
            return input.Active; 
        }
    }

    public bool Slide {
        get {
            return map.actions["Slide"].IsPressed();
        }
    }

    public bool Jump {
        get {
            return jump.Pressed;
        }
    }

    private void OnEnable()
    {
        inputSet = new() { jump, input };

        foreach (var action in inputSet) action.Initialize(map);
    }

    private void Update()
    {
        foreach (var action in inputSet) action.Update();
    }
}
