using UnityEngine;
using UnityEngine.InputSystem;

public class InputScriptableObject : ScriptableObject
{
    [HideInInspector] public InputAction action;
    [HideInInspector] public UnityEngine.InputSystem.PlayerInput map;
    [SerializeField] private string bindName;
    [SerializeField] private bool locked;

    public string BindName { get { return bindName; } }
    public bool Locked     { get { return locked; } }

    public virtual void Initialize(UnityEngine.InputSystem.PlayerInput map)
    {
        this.map = map;
        action   = map.actions[bindName];
    }

    public virtual void Update() { 
        
    }
}
