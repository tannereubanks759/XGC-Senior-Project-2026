using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("AI/Pirate Boss AI")]
[RequireComponent(typeof(NavMeshAgent))]
public class PirateBossAI : MonoBehaviour
{
    public enum BossState { Idle, Chase, Attack, AnchorThrow, Dead }
    public string BossName = "Anchor Boss";

    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform player;
    public BossHealthbar healthbar;
    [Header("Curse")]
    public int damageMult = 1;
    public float cursedSpeedMultiplier = 0.4f;
    public bool reflectDamageWhenCursed = false;
    float _baseAgentSpeed;
    public bool isCursed;
    public GameObject cursedVfxPrefab;
    [Header("Tuning")]
    public float attackRange = 3.5f;
    public float turnSpeed = 360f;
    [Tooltip("How fast the boss can spin specifically during AnchorThrow.")]
    public float throwTurnSpeed = 900f;          // <-- NEW
    public float attackDuration = 1.2f;
    public float attackCooldown = 0.6f;
    public float stopEpsilon = 0.05f;

    [Header("Locomotion")]
    [Tooltip("If speed is above this, we want walk anim ON.")]
    public float walkEnterSpeed = 0.25f;
    [Tooltip("If speed is below this AND we're close enough, we want walk anim OFF.")]
    public float walkExitSpeed = 0.10f;
    [Tooltip("Milliseconds to wait before allowing the next walk/idle flip.")]
    public float walkToggleDebounce = 0.18f;
    [Tooltip("LPF smoothing for agent speed (bigger = snappier).")]
    public float speedSmoothing = 10f;
    [Tooltip("Extra buffer beyond attackRange to leave Attack state (prevents snap-flopping).")]
    public float rangeHysteresis = 0.5f;

    [Header("Health")]
    public int maxHealth = 300;
    public int currentHealth = 300;

    [Header("Damage Intake")]
    [Tooltip("Tag on the player's sword collider(s).")]
    public string swordTag = "PlayerSword";

    [Tooltip("Fallback damage if the sword has no DamageDealer component.")]
    public int defaultSwordDamage = 20;

    [Tooltip("Small invulnerability window after a hit to prevent multi-hit spam from continuous overlap.")]
    public float hitInvulnerability = 0.12f;

    [Tooltip("Log hits to the console for debugging.")]
    public bool debugDamage = false;

    float _nextDamageAllowedTime = 0f;

    [Header("Animator Parameters (match your controller)")]
    [SerializeField] string paramIsWalking = "isWalking";
    [SerializeField] string paramIsRunning = "isRunning";
    [SerializeField] string paramIsAttackin = "isAttackin";
    [SerializeField] string paramAttackIdx = "attack";     // int
    [SerializeField] string paramIsDead = "isDead";

    [Header("Attack Selection")]
    [Min(1)] public int numAttackAnims = 5;
    public bool avoidImmediateRepeat = true;

    [Header("Anchor Throw")]
    [Range(0f, 1f)] public float throwChance = 0.35f;
    [Tooltip("Minimum distance from boss to consider attempting a throw (meters).")]
    public float throwDistance = 8f;
    public float maxThrowDistance = 16.5f;
    [Tooltip("How long the boss stays in the AnchorThrow state (seconds).")]
    public float throwTime = 1.2f;
    public bool canRotate = true;
    [SerializeField] string paramThrowTrigger = "throw"; // Animator trigger name

    // --- Death guards ---
    bool _deathHandled = false;
    int _deathHash;

    float _sqrThrowDist;

    public BossState State { get; private set; } = BossState.Idle;

    // internals
    bool _attackBusy;
    float _sqrAttackEnter;
    float _sqrAttackExit;
    float _attackCooldownUntil;
    int _lastAttack = 0;

    // locomotion smoothing
    bool _walkState;
    float _nextWalkToggleTime;
    float _speedLPF;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.1f);
        _baseAgentSpeed = agent.speed;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        animator?.SetBool(paramIsWalking, false);
        animator?.SetBool(paramIsRunning, false);
        animator?.SetBool(paramIsAttackin, false);
        animator?.ResetTrigger(paramIsDead);

        RecalcRanges();
        _deathHash = Animator.StringToHash("Base Layer.Death");
    }
    public void curseBoss(bool slow, bool reflection)
    {
        if (isCursed) return;
        isCursed = true;
        damageMult = 2;
        if (slow)
        {
            agent.speed = _baseAgentSpeed * cursedSpeedMultiplier;
        }
        reflectDamageWhenCursed = reflection;
    }
    void RecalcRanges()
    {
        _sqrAttackEnter = attackRange * attackRange;
        float exitR = attackRange + Mathf.Max(0.05f, rangeHysteresis);
        _sqrAttackExit = exitR * exitR;

        _sqrThrowDist = Mathf.Max(0f, throwDistance) * Mathf.Max(0f, throwDistance);
    }

    void OnValidate()
    {
        if (agent) agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.1f);
        if (numAttackAnims < 1) numAttackAnims = 1;
        if (walkExitSpeed > walkEnterSpeed) walkExitSpeed = walkEnterSpeed * 0.6f;
        RecalcRanges();
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
        //Debug.Log(State);
        if (State == BossState.Dead) return;

        switch (State)
        {
            case BossState.Idle: TickIdle(); break;
            case BossState.Chase: TickChase(); break;
            case BossState.Attack: TickAttack(); break;
            case BossState.AnchorThrow: TickAnchorThrow(); break;
        }
    }

    public void BeginEncounter(Transform playerTarget)
    {
        if (State == BossState.Dead) return;
        if (playerTarget) player = playerTarget;
        healthbar = playerTarget.GetComponentInChildren<CombatController>().boss_healthbar;
        healthbar.maxHealth = maxHealth;
        healthbar.currentHealth = currentHealth;
        healthbar.TakeDamage(currentHealth);
        healthbar.text.text = BossName;
        healthbar.gameObject.SetActive(true);
        healthbar.ShowHealthbarOnBossTriggered();
        
        TransitionTo(BossState.Chase);
    }

    public void TakeDamage(int amount)
    {
        
        if (State == BossState.Dead) return;
        int finalDamage = amount * damageMult;
        
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(finalDamage));
        animator.SetTrigger("Impacted");

        healthbar.TakeDamage(currentHealth);

        if (currentHealth <= 0) TransitionTo(BossState.Dead);
    }

    void TickIdle()
    {
        if (!agent.isStopped) agent.isStopped = true;
        SetWalk(false, force: true);
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

        // Throw if distance is in range [throwDistance .. maxThrowDistance]
        if (sqrDist >= _sqrThrowDist && toPlayer.magnitude <= maxThrowDistance)
        {
            if (Random.value < throwChance)
            {
                TransitionTo(BossState.AnchorThrow);
                return;
            }
        }

        // Attack if close enough & off cooldown
        if (Time.time >= _attackCooldownUntil && sqrDist <= _sqrAttackEnter)
        {
            TransitionTo(BossState.Attack);
            return;
        }

        // Move toward player
        agent.isStopped = false;
        agent.SetDestination(player.position);
        FaceTarget(); // normal turn speed

        // speed smoothing -> walk anim blend logic
        float rawSpeed = agent.velocity.magnitude;
        float alpha = 1f - Mathf.Exp(-speedSmoothing * Time.deltaTime);
        _speedLPF = Mathf.Lerp(_speedLPF, rawSpeed, alpha);

        bool farFromStop = agent.remainingDistance > agent.stoppingDistance + stopEpsilon;
        bool planning = agent.pathPending;
        bool hasIntent = agent.hasPath || planning || agent.desiredVelocity.sqrMagnitude > 0.01f;

        bool wantWalk =
            hasIntent && (
                planning ||
                farFromStop ||
                _speedLPF > walkEnterSpeed
            );

        bool wantIdle =
            !planning && !farFromStop && _speedLPF < walkExitSpeed;

        if (Time.time >= _nextWalkToggleTime)
        {
            if (!_walkState && wantWalk)
            {
                SetWalk(true);
            }
            else if (_walkState && wantIdle)
            {
                SetWalk(false);
            }
        }

        animator.SetBool(paramIsRunning, false);
        animator.SetBool(paramIsAttackin, false);
    }

    void TickAttack()
    {
        FaceTarget(); // normal turnSpeed
        if (player)
        {
            float sqrDist = (player.position - transform.position).sqrMagnitude;
            if (sqrDist > _sqrAttackExit && !_attackBusy)
                TransitionTo(BossState.Chase);
        }
    }

    void SetWalk(bool value, bool force = false)
    {
        if (!force && _walkState == value) return;
        _walkState = value;
        animator.SetBool(paramIsWalking, value);
        _nextWalkToggleTime = Time.time + walkToggleDebounce;
    }

    // Overload 1: default turn speed
    void FaceTarget()
    {
        FaceTarget(turnSpeed);
    }

    // Overload 2: custom turn speed (used for throw spin)
    void FaceTarget(float customTurnSpeed)
    {
        if (!player) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            customTurnSpeed * Time.deltaTime
        );
    }

    Coroutine _throwCo;

    void TransitionTo(BossState next)
    {
        if (State == next)
        {
            if (next == BossState.Dead) return;
            return;
        }

        // Exit hooks
        if (State == BossState.Attack)
        {
            animator.SetBool(paramIsAttackin, false);
            _attackBusy = false;
        }
        if (State == BossState.AnchorThrow && _throwCo != null)
        {
            StopCoroutine(_throwCo);
            _throwCo = null;
        }

        State = next;

        switch (next)
        {
            case BossState.Idle:
                agent.isStopped = true;
                SetWalk(false, force: true);
                break;

            case BossState.Chase:
                agent.isStopped = false;
                break;

            case BossState.Attack:
                if (!_attackBusy) StartCoroutine(AttackRoutine());
                break;

            case BossState.AnchorThrow:
                agent.isStopped = true;
                SetWalk(false, force: true);
                agent.velocity = Vector3.zero;
                AnchorThrowSet();
                break;

            case BossState.Dead:
                isCursed = false;
                reflectDamageWhenCursed = false;
                damageMult = 1;
                if (agent != null) agent.speed = _baseAgentSpeed;
                if (_deathHandled) return;
                IslandTeleporter tel = GameObject.FindAnyObjectByType<IslandTeleporter>().GetComponent<IslandTeleporter>();
                if (tel != null)
                {
                    tel.OpenDoor();
                }
                _deathHandled = true;

                agent.isStopped = true;
                agent.updatePosition = false;
                agent.updateRotation = false;

                SetWalk(false, force: true);
                animator.SetBool(paramIsAttackin, false);

                foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

                StartCoroutine(DieRoutine());
                break;
        }
    }

    public void AnchorThrowSet()
    {
        animator.ResetTrigger(paramThrowTrigger);
        animator.SetTrigger(paramThrowTrigger);
    }

    void TickAnchorThrow()
    {
        // keep agent frozen in place
        if (!agent.isStopped) agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // spin FAST to face player while throwing
        if (canRotate)
        {
            FaceTarget(throwTurnSpeed); // <-- fast turn during throw
        }
    }

    public void AnchorThrowLeave()
    {
        TransitionTo(BossState.Chase);
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
        SetWalk(false, force: true);

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
        animator.SetTrigger(paramIsDead);

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var col in cols)
        {
            col.enabled = false;
        }

        yield return new WaitForSeconds(.01f);

        enabled = false;
    }

    int ResolveDamageFrom(Collider other)
    {
        var dealer = other.GetComponentInParent<swordDamageDeterminer>();
        if (dealer != null) return Mathf.Max(1, dealer.damage);
        return Mathf.Max(1, defaultSwordDamage);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!enabled || State == BossState.Dead) return;
        if (other.CompareTag(swordTag))
        {
            if (Time.time < _nextDamageAllowedTime) return;
            int dmg = ResolveDamageFrom(other);
            GetComponent<DamageRef>().TakeDamage(dmg);
            var lantern = GameObject.FindAnyObjectByType<chargeOffHandLatern>();
            if (lantern != null && lantern.enabled)
            {
               // Debug.Log("Boss hit by sword, notifying lantern");
                lantern.hitRegistered();
            }

            _nextDamageAllowedTime = Time.time + hitInvulnerability;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!enabled || State == BossState.Dead) return;
        if (collision.collider.CompareTag(swordTag))
        {
            if (Time.time < _nextDamageAllowedTime) return;
            int dmg = ResolveDamageFrom(collision.collider);
            TakeDamage(dmg);
            _nextDamageAllowedTime = Time.time + hitInvulnerability;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
