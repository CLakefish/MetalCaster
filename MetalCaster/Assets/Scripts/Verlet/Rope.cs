using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Rope))]
class RopeEditor : Editor
{
    private Rope rope;
    private bool runSimulation;

    private void OnSceneGUI()
    {
        rope    = (Rope)target;
        Event e = Event.current;

        if (!Application.isPlaying)
        {
            if (runSimulation) rope.Integrate(Vector3.zero);
            else rope.Initialize();
        }

        HandleEnds();

        var particles = rope.particles;

        if (particles != null && particles.Count > 1)
        {
            for (int i = 1; i < particles.Count - 1; ++i)
            {
                Handles.color = Color.white;
                Handles.FreeMoveHandle(rope.transform.TransformPoint(particles[i].CurrentPos), 0.1f, Vector3.zero, Handles.ConeHandleCap);

                Handles.color = Color.yellow;
                Handles.DrawLine(rope.transform.TransformPoint(particles[i - 1].CurrentPos), rope.transform.TransformPoint(particles[i].CurrentPos));
            }

            Handles.color = Color.yellow;
            Handles.DrawLine(rope.transform.TransformPoint(particles[^2].CurrentPos), rope.transform.TransformPoint(particles[^1].CurrentPos));
        }

        Handles.color = Color.yellow;
        Handles.DrawLine(rope.transform.TransformPoint(rope.Start), Vector3.up + rope.transform.TransformPoint(rope.Start));
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Generate")) rope.Initialize();

        string simulateFlag = runSimulation ? "Stop Simulation" : "Run Simulation";
        if (GUILayout.Button(simulateFlag)) runSimulation = !runSimulation;

        if (!Application.isPlaying && runSimulation) rope.Integrate(Vector3.zero);

        base.OnInspectorGUI();
    }

    private void HandleEnds()
    {
        Handles.color    = rope.Type == RopeContraintType.Ends || rope.Type == RopeContraintType.First ? Color.red : Color.green;
        Vector3 startPos = Handles.FreeMoveHandle(rope.transform.TransformPoint(rope.Start), 0.25f, Vector3.zero, Handles.ConeHandleCap);

        Handles.color  = rope.Type == RopeContraintType.Ends || rope.Type == RopeContraintType.Last    ? Color.red : Color.green;
        Vector3 endPos = Handles.FreeMoveHandle(rope.transform.TransformPoint(rope.End),   0.25f, Vector3.zero, Handles.ConeHandleCap);

        switch (rope.Type)
        {
            case RopeContraintType.First:
                rope.Start = rope.transform.InverseTransformPoint(startPos);

                if (Application.isPlaying)
                {
                    Handles.color = Color.white;
                    Handles.FreeMoveHandle(rope.transform.TransformPoint(rope.particles[^1].CurrentPos), 0.1f, Vector3.zero, Handles.ConeHandleCap);
                    break;
                }

                rope.End = rope.transform.InverseTransformPoint(endPos);

                break;

            case RopeContraintType.Last:

                rope.End = rope.transform.InverseTransformPoint(endPos);

                if (Application.isPlaying)
                {
                    Handles.color = Color.white;
                    Handles.FreeMoveHandle(rope.transform.TransformPoint(rope.particles[0].CurrentPos), 0.1f, Vector3.zero, Handles.ConeHandleCap);
                    break;
                }

                rope.Start = rope.transform.InverseTransformPoint(startPos);

                break;

            case RopeContraintType.Ends:

                rope.Start = rope.transform.InverseTransformPoint(startPos);
                rope.End   = rope.transform.InverseTransformPoint(endPos);

                break;

            case RopeContraintType.None:

                if (Application.isPlaying)
                {
                    Handles.color = Color.white;
                    Handles.FreeMoveHandle(rope.transform.TransformPoint(rope.particles[0].CurrentPos),  0.1f, Vector3.zero, Handles.ConeHandleCap);
                    Handles.FreeMoveHandle(rope.transform.TransformPoint(rope.particles[^1].CurrentPos), 0.1f, Vector3.zero, Handles.ConeHandleCap);
                    break;
                }

                rope.Start = rope.transform.InverseTransformPoint(startPos);
                rope.End   = rope.transform.InverseTransformPoint(endPos);

                break;
        }
    }
}

#endif

public enum RopeContraintType
{
    None,
    First,
    Last,
    Ends,
}

public class Rope : MonoBehaviour
{
    [Header("Rope Variables")]
    [SerializeField] private RopeContraintType constraintType = RopeContraintType.First;
    [SerializeField] private int totalPoints;
    [SerializeField] private bool applyIK;
    [HideInInspector] public Vector3 Start;
    [HideInInspector] public Vector3 End;

    [Header("Physics")]
    [SerializeField] private Vector3 gravityDir;
    [SerializeField] private float gravityForce;
    [SerializeField] private float drag;

    internal List<Particle> particles = new();
    private float targetDistance;

    public float TargetDist         { get { return targetDistance; } }
    public int   TotalPoints        { get { return totalPoints;    } }

    public RopeContraintType Type   { get { return constraintType; } }

    private const int ITERATIONS = 5;

    public void Initialize() 
    {
        particles = new();

        int segments = totalPoints - 1;

        for (int i = 0; i < totalPoints; ++i)
        {
            float w = (float)i / segments;

            Vector3 position = Vector3.Lerp(Start, End, w);
            Particle p = new(position, position, false);

            particles.Add(p);
        }

        switch (constraintType)
        {
            case RopeContraintType.First:
                particles[0].SetFixed(true);
                break;

            case RopeContraintType.Last:
                particles[^1].SetFixed(true);
                break;

            case RopeContraintType.Ends:
                particles[0].SetFixed(true);
                particles[^1].SetFixed(true);
                break;
        }

        targetDistance = Vector3.Distance(Start, End) / segments;
    }

    public void Integrate(Vector3 externalForce) 
    {
        switch (constraintType)
        {
            case RopeContraintType.First:
                particles[0].CurrentPos = Start;
                break;

            case RopeContraintType.Last:
                particles[^1].CurrentPos = End;
                break;

            case RopeContraintType.Ends:
                particles[0].CurrentPos  = Start;
                particles[^1].CurrentPos = End;
                break;
        }

        foreach (var p in particles)
        {
            if (p.IsFixed) continue;

            if (p.PrevPos == Vector3.zero)
            {
                p.PrevPos = p.CurrentPos;
            }

            Vector3 force = externalForce + (gravityDir.normalized * gravityForce);
            Vector3 acceleration = force;

            Vector3 velocity = p.CurrentPos - p.PrevPos;
            velocity *= 1 - drag;

            Vector3 prevPos = p.CurrentPos;

            p.CurrentPos = p.CurrentPos + velocity + acceleration * Time.deltaTime * Time.deltaTime;
            p.PrevPos = prevPos;
        }

        for (int iteration = 0; iteration < ITERATIONS; ++iteration)
        {
            for (int i = 1; i < particles.Count; ++i)
            {
                Particle p1 = particles[i - 1];
                Particle p2 = particles[i];

                if (p1.IsFixed && p2.IsFixed) continue;

                float dist = Vector3.Distance(p1.CurrentPos, p2.CurrentPos);
                float error = dist - targetDistance;

                Vector3 diff = p2.CurrentPos - p1.CurrentPos;
                Vector3 dir = diff.normalized * error;

                if (p1.IsFixed)
                {
                    p2.CurrentPos -= dir;
                }
                else if (p2.IsFixed)
                {
                    p1.CurrentPos += dir;
                }
                else
                {
                    dir *= 0.5f;
                    p1.CurrentPos += dir;
                    p2.CurrentPos -= dir;
                }
            }
        }

        if (applyIK) ApplyFABRIK();
    }

    public void ApplyFABRIK()
    {
        if (Type != RopeContraintType.Ends) return;

        particles[^1].CurrentPos = End;
        for (int i = particles.Count - 2; i >= 0; i--)
        {
            Vector3 dir = (particles[i].CurrentPos - particles[i + 1].CurrentPos).normalized;
            particles[i].CurrentPos = particles[i + 1].CurrentPos + dir * targetDistance;
        }

        particles[0].CurrentPos = Start;
        for (int i = 1; i < particles.Count; i++)
        {
            Vector3 dir = (particles[i].CurrentPos - particles[i - 1].CurrentPos).normalized;
            particles[i].CurrentPos = particles[i - 1].CurrentPos + dir * targetDistance;
        }
    }

    public void OnEnable() => Initialize();
    public void Update()   => Integrate(Vector3.zero);
}
