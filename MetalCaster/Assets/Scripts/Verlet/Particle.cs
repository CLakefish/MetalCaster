using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Particle
{
    [SerializeField] public Vector3 CurrentPos;
    [SerializeField] public Vector3 PrevPos;
    [SerializeField] private bool isFixed;

    public bool IsFixed       { get { return isFixed; } }

    public Particle(Vector3 current, Vector3 previous, bool isfixed)
    {
        CurrentPos = current;
        PrevPos = previous;
        isFixed = isfixed;
    }

    public bool SetFixed(bool value) => isFixed = value;
}