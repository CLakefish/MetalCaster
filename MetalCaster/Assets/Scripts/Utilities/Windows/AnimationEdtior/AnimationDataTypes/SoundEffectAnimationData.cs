using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Animation Data/Sound Effect")]
public class SoundEffectAnimationData : AnimationData
{
    [SerializeField] private AudioClip clip;
    [SerializeField] private AudioSource source;

    public override Color Visual()
    {
        return Color.red;
    }

    public override void HandleAssignment(Object obj)
    {
        if (obj is AudioClip clip1)
        {
            clip = clip1;
        }

        if (obj is GameObject gameObj) {
            if (gameObj.TryGetComponent<AudioSource>(out AudioSource src))
            {
                source = src;
                base.HandleAssignment(gameObj);
            }
        }
    }

    public override void Enter()
    {
        if (source == null) return;
        source.PlayOneShot(clip);
    }
}