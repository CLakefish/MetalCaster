using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Animation Data/Unity Event")]
public class UnityEventAnimationData : AnimationData
{
    [SerializeField] private UnityEngine.Events.UnityEvent enter;
    [SerializeField] private UnityEngine.Events.UnityEvent update;
    [SerializeField] private UnityEngine.Events.UnityEvent exit;

    public override Color Visual()
    {
        return Color.cyan;
    }

    public override void Enter() => enter?.Invoke();
    public override void Update() => update?.Invoke();
    public override void Exit() => exit?.Invoke();
}
