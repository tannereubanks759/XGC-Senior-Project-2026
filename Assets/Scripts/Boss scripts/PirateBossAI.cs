using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("AI/Pirate Boss AI")]
[RequireComponent(typeof(NavMeshAgent))]
public class PirateBossAI : MonoBehaviour
{
    public enum BossState { Idle, Chase, Attack, Dead }

    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform player;

    [Header("Tuning")]
    public float attackRange = 3.5f;
    public float turnSpeed = 360f;
    public float attackDuration = 1.2f;
    public float attackCooldown = 0.6f;

    [Tooltip("How close the agent must be to consider itself 'arrived' for idle anims.")]
    public float stopEpsilon = 0.05f;

    // NEW: locomotion thresholds so walk anim stops when agent slows to a creep
    [Header("Locomotion")]
    [Tooltip("Minimum agent speed (m/s) to consider walking for the animation.")]
    public float walkAnimMinSpeed = 0.15f;
    [Tooltip("Extra buffer beyond attackRange to leave Attack state (prevents snap-flopping).")]
    public float rangeHysteresis = 0.5f;

    [Header("Health")]
    public int maxHealth = 300;
    public int currentHealth = 300;

    [Header("Animator Parameters (match your controller)")]
    [SerializeField] string paramIsWalking = "isWalking";
    [SerializeField] string paramIsRunning = "isRunning";
    [SerializeField] string paramIsAttackin = "isAttackin"; // your spelling
    [SerializeField] string paramAttackIdx = "attack";     // int
    [SerializeField] string paramIsDead = "isDead";
    [SerializeField] string paramIsStunned = "isStunned";

    // Attack selection
    [Header("Attack Selection")]
    [Min(1)] public int numAttackAnims = 5;
    public bool avoidImmediateRepeat = true;

    public BossState State { get; private set; } = BossState.Idle;

    // internals
    bool _attackBusy;
    float _sqrAttackEnter;
    float _sqrAttackExit;
    float _attackCooldownUntil;
    int _lastAttack = 0;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();

        // Agent settings: let us control facing; stop just short of the player
        agent.updateRotation = false;
        agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.1f);

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        animator?.SetBool(paramIsWalking, false);
        animator?.SetBool(paramIsRunning, false);
        animator?.SetBool(paramIsAttackin, false);
        animator?.SetBool(paramIsDead, false);

        RecalcRanges();
    }

    void OnValidate()
    {
        if (agent) agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.1f);
        if (numAttackAnims < 1) numAttackAnims = 1;
        RecalcRanges();
    }

    void RecalcRanges()
    {
        _sqrAttackEnter = attackRange * attackRange;
        _sqrAttackExit = (attackRange + Mathf.Max(0.05f, rangeHysteresis)) * (attackRange + Mathf.Max(0.05f, rangeHysteresis));
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        TransitionTo(BossState.Idle);
    }

    void Update()
    {
        if (State == BossState.Dead) return;

        switch (State)
        {
            case BossState.Idle: TickIdle(); break;
            case BossState.Chase: TickChase(); break;
            case BossState.Attack: TickAttack(); break;
        }
    }

    public void BeginEncounter(Transform playerTarget)
    {
        if (State == BossState.Dead) return;
        if (playerTarget) player = playerTarget;
        TransitionTo(BossState.Chase);
    }

    public void TakeDamage(int amount)
    {
        if (State == BossState.Dead) return;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(amount));
        if (currentHealth <= 0) TransitionTo(BossState.Dead);
    }

    void TickIdle()
    {
        if (!agent.isStopped) agent.isStopped = true;
        SetWalk(false);
        animator.SetBool(paramIsAttackin, false);
    }

    void TickChase()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
            if (!player) { TransitionTo(BossState.Idle); return; }
        }

        Vector3 toPlayer = player.position - transform.position;
        float sqrDist = toPlayer.sqrMagnitude;

        // Ready to attack?
        if (Time.time >= _attackCooldownUntil && sqrDist <= _sqrAttackEnter)
        {
            TransitionTo(BossState.Attack);
            return;
        }

        // Move toward player
        agent.isStopped = false;
        agent.SetDestination(player.position);
        FaceTarget();

        // --- KEY FIX: drive walk anim from actual motion ---
        bool shouldWalk =
            agent.hasPath &&
            !agent.pathPending &&
            (agent.remainingDistance > agent.stoppingDistance + stopEpsilon ||
             agent.velocity.sqrMagnitude > walkAnimMinSpeed * walkAnimMinSpeed);

        SetWalk(shouldWalk);

        // No running yet
        animator.SetBool(paramIsRunning, false);
        animator.SetBool(paramIsAttackin, false);
    }

    void TickAttack()
    {
        // Face player while attacking
        FaceTarget();

        // If player moved far away (beyond hysteresis), go back to chase
        if (player)
        {
            float sqrDist = (player.position - transform.position).sqrMagnitude;
            if (sqrDist > _sqrAttackExit && !_attackBusy)
            {
                TransitionTo(BossState.Chase);
            }
        }
    }

    void SetWalk(bool value) => animator.SetBool(paramIsWalking, value);

    void FaceTarget()
    {
        if (!player) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        var targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    void TransitionTo(BossState next)
    {
        if (State == next) return;

        // exit hooks
        if (State == BossState.Attack)
        {
            animator.SetBool(paramIsAttackin, false);
            _attackBusy = false;
        }

        State = next;

        switch (next)
        {
            case BossState.Idle:
                agent.isStopped = true;
                SetWalk(false);
                break;

            case BossState.Chase:
                agent.isStopped = false;
                break;

            case BossState.Attack:
                if (!_attackBusy) StartCoroutine(AttackRoutine());
                break;

            case BossState.Dead:
                StartCoroutine(DieRoutine());
                break;
        }
    }

    int PickAttackIndex()
    {
        if (numAttackAnims <= 1 || !avoidImmediateRepeat)
            return Mathf.Clamp(Random.Range(1, numAttackAnims + 1), 1, numAttackAnims);

        if (numAttackAnims == 2) return (_lastAttack == 1) ? 2 : 1;

        int pick;
        do { pick = Random.Range(1, numAttackAnims + 1); } while (pick == _lastAttack);
        return pick;
    }

    IEnumerator AttackRoutine()
    {
        _attackBusy = true;
        agent.isStopped = true;
        SetWalk(false);

        int idx = PickAttackIndex();
        _lastAttack = idx;

        animator.SetInteger(paramAttackIdx, idx);
        animator.SetBool(paramIsAttackin, true);

        float endTime = Time.time + attackDuration;
        while (Time.time < endTime)
        {
            FaceTarget();
            yield return null;
        }

        animator.SetBool(paramIsAttackin, false);
        _attackCooldownUntil = Time.time + attackCooldown;

        _attackBusy = false;
        TransitionTo(BossState.Chase);
    }

    IEnumerator DieRoutine()
    {
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        SetWalk(false);
        animator.SetBool(paramIsAttackin, false);
        animator.SetBool(paramIsDead, true);

        yield return new WaitForSeconds(3f);
        // Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
