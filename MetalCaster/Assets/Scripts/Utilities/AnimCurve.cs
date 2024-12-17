using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AnimCurve
{
    [SerializeField] private AnimationCurve curve;
    [SerializeField] private float speed = 1;
    private float totalTime = 0;

    public Coroutine Coroutine { get; set; }

    public bool IsComplete {
        get {
            return totalTime >= curve.length;
        }
    }

    public void Reset() => totalTime = 0;

    public float Continue(float dT) {
        totalTime += dT * speed;
        return curve.Evaluate(totalTime);
    }

    public float GetAtTime(float dT) {
        return curve.Evaluate(dT);
    }

    public IEnumerator Run(System.Action onTick, System.Action onComplete = null) {
        Reset();

        while (!IsComplete) {
            onTick();
            yield return null;
        }

        onComplete?.Invoke();
    }
}
