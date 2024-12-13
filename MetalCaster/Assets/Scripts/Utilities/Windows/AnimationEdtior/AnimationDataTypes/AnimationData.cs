using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Animation Data/Base")]
public class AnimationData : ScriptableObject
{
    [SerializeField] public float start, end;
    [SerializeField] public string animName;

    [SerializeField] public SceneReference objRef;
    [HideInInspector] public bool isPlaying;

    public void Init(float start, float end, string name) {
        this.start    = start;
        this.end      = end;
        this.animName = name;
    }

    public virtual Color Visual() {
        return Color.green;
    }

    public virtual void HandleAssignment(Object obj) {
        if (obj is not GameObject) return;
        objRef = new SceneReference(obj as GameObject);
    }

    public virtual void Reload() { }

    public virtual void Enter()  { }
    public virtual void Update() { }
    public virtual void Exit()   { }
}