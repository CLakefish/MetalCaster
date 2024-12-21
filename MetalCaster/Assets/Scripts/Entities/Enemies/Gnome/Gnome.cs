using UnityEngine;
using UnityEngine.AI;
using HFSMFramework;

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class Gnome : MonoBehaviour
{
    [SerializeField] private NavMeshAgent nav;
    [SerializeField] private Health hp;
    [SerializeField] private float stunTime;
    [SerializeField] private Vector2 knockbackForce;
    [SerializeField] private float gravity;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float attackTime;
    [SerializeField] private ParticleSystem hitParticle;
    [SerializeField] private ParticleSystem deathParticle;
    private Transform desiredPos;
    private Rigidbody rb;

    private StateMachine<Gnome> hfsm;

    private GnomeWalk   Walking   { get; set; }
    private GnomeAttack Attacking { get; set; }
    private GnomeLaunch Launched  { get; set; }

    private void OnEnable()
    {
        hfsm = new(this);
        Walking = new(this);
        Attacking = new(this);
        Launched = new(this);

        hfsm.AddTransitions(new()
        {
            new(Walking, Attacking, () => Vector3.Distance(transform.position, desiredPos.position) <= nav.stoppingDistance),
            new(Attacking, Walking, () => hfsm.Duration > attackTime),
            new(Launched, Walking,  () => hfsm.Duration >= stunTime && Physics.Raycast(transform.position, Vector3.down, nav.height / 2.0f + 0.1f, groundLayer)),
        });

        hfsm.Start(Walking);

        nav = GetComponent<NavMeshAgent>();
        rb  = GetComponent<Rigidbody>();
        hp  = GetComponent<Health>();

        hp.damaged += () =>
        {
            Instantiate(hitParticle, transform.position, Quaternion.identity);
            hfsm.ChangeState(Launched);
        };

        hp.killed  += () =>
        {
            Instantiate(deathParticle, transform.position, Quaternion.identity);
            Destroy(gameObject);
        };
    }

    private void Start()
    {
        desiredPos = Player.Instance.rb.transform;
    }

    private void Update()
    {
        hfsm.CheckTransitions();
        hfsm.Update();
    }

    private void FixedUpdate()
    {
        hfsm.FixedUpdate();
    }

    private class GnomeWalk : State<Gnome>
    {
        public GnomeWalk(Gnome context) : base(context) { }

        public override void Update()
        {
            context.nav.SetDestination(context.desiredPos.position);
        }
    }

    private class GnomeLaunch : State<Gnome>
    {
        public GnomeLaunch(Gnome context) : base(context) { }

        public override void Enter()
        {
            context.nav.enabled = false;
            context.rb.linearVelocity = (context.rb.transform.position - context.desiredPos.position).normalized * context.knockbackForce.x + (Vector3.up * context.knockbackForce.y);
        }

        public override void FixedUpdate()
        {
            context.rb.linearVelocity -= context.gravity * Time.deltaTime * Vector3.up;
        }

        public override void Exit()
        {
            context.rb.linearVelocity = Vector3.zero;
            context.nav.enabled = true;
        }
    }

    private class GnomeAttack : State<Gnome>
    {
        public GnomeAttack(Gnome context) : base(context) { }
    }
}
