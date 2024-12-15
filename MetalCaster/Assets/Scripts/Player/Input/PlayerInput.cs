using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : Player.PlayerComponent
{
    [Header("Map")]
    [SerializeField] private UnityEngine.InputSystem.PlayerInput map;

    [Header("Inputs")]
    [SerializeField] private Vector2Input moveInput;
    [SerializeField] private Vector2Input viewInput;
    [SerializeField] private BoolInput jump;
    [SerializeField] private BoolInput slide;
    
    [Header("Variables")]
    [SerializeField] private Vector2 sensitivity;
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;

    private HashSet<InputScriptableObject> inputSet;

    public Vector2 Input { 
        get {
            return moveInput.Value;
        } 
    }
    public Vector2 NormalizedInput { 
        get {
            return moveInput.NormalizedValue; 
        } 
    }

    public Vector2 MousePosition { 
        get { 
            return viewInput.Value; 
        } 
    }

    public Vector2 AlteredMousePosition { 
        get { 
            return new Vector2(MousePosition.x * (invertX ? -1 : 1) * sensitivity.x, MousePosition.y * (invertY ? -1 : 1) * sensitivity.y);
        } 
    }

    public bool IsInputting { 
        get { 
            return moveInput.Active; 
        }
    }

    public bool Slide {
        get {
            return slide.Held && !slide.Released;
        }
    }

    public bool Jump {
        get {
            return jump.Pressed;
        }
    }

    private void OnEnable()
    {
        inputSet = new() { jump, moveInput, slide, viewInput };

        foreach (var action in inputSet) action.Initialize(map);
    }

    private void Update()
    {
        foreach (var action in inputSet) action.Update();
    }
}
