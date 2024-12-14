using System.Collections;
using UnityEngine;

public class PlayerCollider : Player.PlayerComponent
{
    [SerializeField] public CapsuleCollider col;
    private Coroutine sizeChange = null;

    public float Size {
        get {
            return col.height;
        }
        set
        {
            float start = col.height;
            float hD = value - start;

            col.height += hD;
        }
    }

    public void ChangeSize(float endSize, float time)
    {
        if (sizeChange != null) StopCoroutine(sizeChange);
        sizeChange = StartCoroutine(ChangeSizeCoroutine(endSize, time));
    }

    private IEnumerator ChangeSizeCoroutine(float endSize, float time)
    {
        while (Mathf.Abs(Size - endSize) > 0.01f)
        {
            Size = Mathf.MoveTowards(Size, endSize, Time.fixedDeltaTime * time);
            yield return new WaitForFixedUpdate();
        }

        Size = endSize;
    }
}
