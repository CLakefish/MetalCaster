using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif


[System.Serializable]
class Particle {
    [SerializeField] public Vector3 currentPos;
    [SerializeField] public Vector3 prevPos;
    [SerializeField] private bool isFixed;

    public bool IsFixed       { get { return isFixed; } }

    public Particle(Vector3 current, Vector3 previous, bool isfixed) {
        currentPos = current;
        prevPos = previous;
        isFixed = isfixed;
    }

    public bool SetFixed(bool value) => isFixed = value;

    public void Draw()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(currentPos, 0.1f);
    }
}

public enum RopeContraintType
{
    None,
    First,
    Last,
    Ends,
}

[System.Serializable]
class Rope
{
    [SerializeField] public List<Particle> particles;
    [SerializeField] private Vector3 start, end;
    [SerializeField] private RopeContraintType constraintType;
    [SerializeField] private int totalPoints;
    private Transform parent;
    private float targetDistance;

    public void Initialize(Transform transform)
    {
        parent = transform;

        particles = new();

        int segments = totalPoints - 1;

        for (int i = 0; i < totalPoints; ++i)
        {
            float w = (float)i / segments;

            Vector3 position = new(
                w * end.x + (1 - w) * start.x, 
                w * end.y + (1 - w) * start.y,
                w * end.z + (1 - w) * start.z);

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

        targetDistance = Vector3.Distance(start, end) / segments;
    }

    public void Integrate(Vector3 externalForce, Vector3 gravity, float drag)
    {
        switch (constraintType)
        {
            case RopeContraintType.First:
                particles[0].currentPos = start + parent.transform.position;
                break;

            case RopeContraintType.Last:
                particles[^1].currentPos = end + parent.transform.position;
                break;

            case RopeContraintType.Ends:
                particles[0].currentPos = start + parent.transform.position;
                particles[^1].currentPos = end + parent.transform.position;
                break;
        }

        foreach (var p in particles)
        {
            if (p.IsFixed) continue;

            if (p.prevPos == Vector3.zero) {
                p.prevPos = p.currentPos;
            }

            Vector3 force = externalForce + gravity;
            Vector3 acceleration = force;

            Vector3 velocity = p.currentPos - p.prevPos;
            velocity *= 1 - drag;

            Vector3 prevPos = p.currentPos;

            p.currentPos = p.currentPos + velocity + acceleration * Time.deltaTime * Time.deltaTime;
            p.prevPos = prevPos;
        }

        for (int iteration = 0; iteration < 10; ++iteration)
        {
            for (int i = 1; i < particles.Count; ++i)
            {
                Particle p1 = particles[i - 1];
                Particle p2 = particles[i];

                if (p1.IsFixed && p2.IsFixed) continue;

                float dist = Vector3.Distance(p1.currentPos, p2.currentPos);
                float error = dist - targetDistance;

                Vector3 diff = p2.currentPos - p1.currentPos;
                Vector3 dir = diff.normalized * error;

                if (p1.IsFixed)
                {
                    p2.currentPos -= dir;
                }
                else if (p2.IsFixed)
                {
                    p1.currentPos += dir;
                }
                else
                {
                    dir *= 0.5f;
                    p1.currentPos += dir;
                    p2.currentPos -= dir;
                }
            }
        }
    }

    public void Draw(Vector3 offset)
    {
        if (particles == null) return;

        if (Application.isPlaying)
        {
            particles[0].Draw();

            for (int i = 1; i < particles.Count; ++i)
            {
                particles[i].Draw();

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(particles[i].currentPos, particles[i - 1].currentPos);
            }
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(start + offset, end + offset);

            for (int i = 0; i < totalPoints; ++i)
            {
                float w = (float)i / (totalPoints - 1);

                Vector3 midPos = new(
                    w * end.x + (1 - w) * start.x,
                    w * end.y + (1 - w) * start.y,
                    w * end.z + (1 - w) * start.z);

                Gizmos.color = Color.white;
                Gizmos.DrawSphere(midPos + offset, 0.1f);
            }
        }
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(Verlet))]
class VerletEditor : Editor
{

    private void OnSceneGUI()
    {
        Verlet v = (Verlet)target;

        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {

        }
    }
}

#endif

public class Verlet : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float gravity;
    [SerializeField] private float drag;

    [Header("Ropes")]
    [SerializeField] private List<Rope> ropes;
    private List<Particle> particles = new();

    private void OnEnable()
    {
        foreach (var rope in ropes)
        {
            particles.AddRange(rope.particles);
            rope.Initialize(rb.transform);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (var p in particles)
            {
                p.currentPos = p.prevPos = Vector3.zero;
            }
        }

        foreach (var rope in ropes)
        {
            rope.Integrate(rb.velocity, Vector3.up * gravity, drag);
        }
    }

    private void OnDrawGizmos()
    {
        if (ropes == null) return;

        foreach (var rope in ropes)
        {
            rope.Draw(rb.position);
        }
    }
}
