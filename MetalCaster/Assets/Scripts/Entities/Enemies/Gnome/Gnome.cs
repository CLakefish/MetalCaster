using UnityEngine;
using UnityEngine.AI;
using HFSMFramework;

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class Gnome : MonoBehaviour
{
    [SerializeField] private NavMeshAgent nav;
    [SerializeField] private Health hp;
    [SerializeField] private float stunTime;
    [SerializeField] private float pingDelay = 0.25f;
    [SerializeField] private Vector2 knockbackForce;
    [SerializeField] private float gravity;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask hittableLayer;
    [SerializeField] private ParticleSystem hitParticle;
    [SerializeField] private ParticleSystem deathParticle;

    [Header("Attack")]
    [SerializeField] private float attackTime;
    [SerializeField] private float attackRate;
    [SerializeField] private int attackDamage;
    [SerializeField] private float attackRadius;
    [SerializeField] private float attackDistance;

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
            new(Walking, Attacking, () => Vector3.Distance(transform.position, desiredPos.position) <= attackDistance && hfsm.Duration >= attackRate),
            new(Attacking, Walking, () => hfsm.Duration > attackTime),
            new(Launched, Walking,  () => hfsm.Duration >= stunTime && Physics.Raycast(transform.position, Vector3.down, nav.height / 2.0f + 0.1f, groundLayer)),
        });

        hfsm.Start(Walking);

        nav = GetComponent<NavMeshAgent>();
        rb  = GetComponent<Rigidbody>();
        hp  = GetComponent<Health>();

        nav.updateRotation = false;

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

        Debug.Log(hfsm.CurrentState);
    }

    private void FixedUpdate()
    {
        hfsm.FixedUpdate();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * attackDistance);
        Gizmos.DrawWireSphere(transform.position + (transform.forward * attackDistance), attackRadius);
    }

    private void RotateTowardsPlayer()
    {
        Vector3 dir = (desiredPos.position - transform.position).normalized;
        float deg = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, deg, 0);
    }

    private class GnomeWalk : State<Gnome>
    {
        private float prevStagger;
        public GnomeWalk(Gnome context) : base(context) { }

        public override void FixedUpdate()
        {
            context.RotateTowardsPlayer();

            if (Time.time >= prevStagger + context.pingDelay)
            {
                prevStagger = Time.time;
                context.nav.SetDestination(context.desiredPos.position);
            }
        }
    }

    private class GnomeLaunch : State<Gnome>
    {
        public GnomeLaunch(Gnome context) : base(context) { }

        public override void Enter()
        {
            context.nav.enabled       = false;
            context.rb.isKinematic    = false;
            context.rb.linearVelocity = (context.rb.transform.position - context.desiredPos.position).normalized * context.knockbackForce.x + (Vector3.up * context.knockbackForce.y);
        }

        public override void FixedUpdate()
        {
            context.rb.linearVelocity -= context.gravity * Time.deltaTime * Vector3.up;
        }

        public override void Exit()
        {
            context.rb.linearVelocity = Vector3.zero;
            context.rb.isKinematic    = true;
            context.nav.enabled       = true;
        }
    }

    private class GnomeAttack : State<Gnome>
    {
        private bool hasHit = false;

        public GnomeAttack(Gnome context) : base(context) { }

        public override void Enter()
        {
            hasHit = false;

            context.RotateTowardsPlayer();
        }

        public override void FixedUpdate()
        {
            if (hasHit) return;

            if (!Physics.Raycast(context.transform.position, (context.desiredPos.position - context.rb.transform.position).normalized, out RaycastHit hit, context.attackDistance, context.hittableLayer)) return;

            Debug.Log("hit something");

            if (hit.collider.transform.parent.TryGetComponent(out Health hp))
            {
                Debug.Log("Damaged something");
                hp.Damage(context.attackDamage);
                hasHit = true;
            }
        }
    }
}
