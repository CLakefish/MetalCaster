using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : Player.PlayerComponent
{
    [Header("Map")]
    [SerializeField] private UnityEngine.InputSystem.PlayerInput map;

    [Header("Movement")]
    [SerializeField] private Vector2Input moveInput;
    [SerializeField] private Vector2Input viewInput;
    [SerializeField] private BoolInput jump;
    [SerializeField] private BoolInput slide;

    [Header("Weaponry")]
    [SerializeField] private BoolInput left;
    [SerializeField] private BoolInput right;
    // Should make this a list :)
    [SerializeField] private BoolInput slot1;
    [SerializeField] private BoolInput slot2;
    [SerializeField] private BoolInput slot3;

    [SerializeField] private BoolInput reload;

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

    public Vector2 MouseDelta { 
        get { 
            return viewInput.Value; 
        } 
    }

    public Vector2 AlteredMouseDelta { 
        get { 
            return new Vector2(MouseDelta.x * (invertX ? -1 : 1) * sensitivity.x, MouseDelta.y * (invertY ? -1 : 1) * sensitivity.y);
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

    public bool Reload {
        get {
            return reload.Pressed;
        }
    }

    public (BoolInput Left, BoolInput Right) Mouse {
        get {
            return (left, right);
        }
    }

    public (BoolInput One, BoolInput Two, BoolInput Three) Slot {
        get {
            return (slot1, slot2, slot3);
        }
    }

    public bool SlotPressed {
        get { 
            return slot1.Pressed || slot2.Pressed || slot3.Pressed;
        }
    }

    private void OnEnable()
    {
        inputSet = new() { jump, moveInput, slide, viewInput, left, right, slot1, slot2, slot3, reload };

        foreach (var action in inputSet) {
            action.action.Enable();
            action.Initialize(map);
        }
    }

    private void Update() {
        foreach (var action in inputSet) action.Update();
    }
}
