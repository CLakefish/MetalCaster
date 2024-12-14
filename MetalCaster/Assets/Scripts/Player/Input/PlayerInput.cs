using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : Player.PlayerComponent
{
    [Header("Debugging")]
    [SerializeField] private UnityEngine.InputSystem.PlayerInput action;
    [SerializeField] private bool inputting;
    
    [Header("Variables")]
    [SerializeField] private Vector2 sensitivity;
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;

    public Vector2 Input { 
        get {
            return action.actions["Move"].ReadValue<Vector2>();
        } 
    }
    public Vector2 NormalizedInput { 
        get {
            return Input.normalized; 
        } 
    }

    public Vector2 MousePosition { 
        get { 
            return action.actions["View"].ReadValue<Vector2>(); 
        } 
    }

    public Vector2 AlteredMousePosition { 
        get { 
            return new Vector2(MousePosition.x * (invertX ? -1 : 1) * sensitivity.x, MousePosition.y * (invertY ? -1 : 1) * sensitivity.y);
        } 
    }

    public bool IsInputting { 
        get { 
            return inputting; 
        }
    }

    public bool Slide {
        get {
            return action.actions["Slide"].IsPressed();
        }
    }

    public bool Jump {
        get {
            return action.actions["Jump"].IsPressed();
        }
    }

    private void Update()
    {
        inputting = Input != Vector2.zero;
        /*
        mouseVec  = new Vector2(UnityEngine.Input.GetAxisRaw("Mouse X"), UnityEngine.Input.GetAxisRaw("Mouse Y"));
        inputs    = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
        inputting = inputs != Vector2.zero;*/
    }
}
