using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Animation Data/Particle System")]
public class ParticleSystemAnimationData : AnimationData
{
    private ParticleSystem system;

    public override Color Visual() {
        return Color.magenta;
    }

    public override void HandleAssignment(Object obj)
    {
        if (obj is GameObject gameObj)
        {
            if (gameObj.TryGetComponent<ParticleSystem>(out ParticleSystem sys))
            {
                system = sys;
                base.HandleAssignment(obj);
            }
        }
    }

    public override void Enter()
    {
        var obj = objRef.Get();
        if (obj == null) return;
        system = obj.GetComponent<ParticleSystem>();
        system.Play(true);
    }

    public override void Exit()
    {
        if (system == null) return;
        system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
