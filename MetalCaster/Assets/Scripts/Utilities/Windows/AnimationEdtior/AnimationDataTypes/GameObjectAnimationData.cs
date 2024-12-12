using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Animation Data/Game Object")]
public class GameObjectAnimationData : AnimationData
{
    public override Color Visual()
    {
        return Color.yellow;
    }

    public override void Reload() => SetActive(true);

    public override void Enter()  => SetActive(true);
    public override void Exit()   => SetActive(false);

    private void SetActive(bool on)
    {
        GameObject obj = objRef.Get();
        if (obj == null) return;
        obj.SetActive(on);
    }
}