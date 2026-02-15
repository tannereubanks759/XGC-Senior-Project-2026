using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("AI/Pirate Boss AI")]
[RequireComponent(typeof(NavMeshAgent))]
public class PirateBossAI : MonoBehaviour
{
    public enum BossState { Idle, Chase, Attack, Block, AnchorThrow, Dead } // <-- added Block
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
    public float throwTurnSpeed = 900f;
    public float attackDuration = 1.2f;
    public float attackCooldown = 0.6f;
    public float stopEpsilon = 0.05f;

    [Header("Blocking")]
    [Range(0f, 1f)] public float blockChance = 0.22f;
    [Tooltip("Boss can attempt block when within this range from player.")]
    public float blockRange = 6f;
    public float blockDurationMin = 0.45f;
    public float blockDurationMax = 1.1f;
    public float blockCooldown = 1.25f;
    [Tooltip("Damage multiplier while blocking. 0 = fully negate, 0.2 = takes 20% damage.")]
    [Range(0f, 1f)] public float blockedDamageMultiplier = 0f;
    [Tooltip("If false, cursed boss won't enter Block state.")]
    public bool canBlockWhileCursed = true;

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
    [SerializeField] string paramIsBlocking = "isBlocking"; // <-- added

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
    [SerializeField] string paramThrowTrigger = "throw";

    // --- Death guards ---
    bool _deathHandled = false;
    int _deathHash;

    float _sqrThrowDist;
    float _sqrBlockDist; // <-- added

    public BossState State { get; private set; } = BossState.Idle;

    // internals
    bool _attackBusy;
    float _sqrAttackEnter;
    float _sqrAttackExit;
    float _attackCooldownUntil;
    int _lastAttack = 0;

    // block internals
    bool _blockBusy;
    float _blockCooldownUntil;
    Coroutine _blockCo;

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
        animator?.SetBool(paramIsBlocking, false); // <-- added
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
        _sqrBlockDist = Mathf.Max(0f, blockRange) * Mathf.Max(0f, blockRange); // <-- added
    }

    void OnValidate()
    {
        if (agent) agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.1f);
        if (numAttackAnims < 1) numAttackAnims = 1;
        if (walkExitSpeed > walkEnterSpeed) walkExitSpeed = walkEnterSpeed * 0.6f;
        if (blockDurationMax < blockDurationMin) blockDurationMax = blockDurationMin;
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
        if (State == BossState.Dead) return;

        switch (State)
        {
            case BossState.Idle: TickIdle(); break;
            case BossState.Chase: TickChase(); break;
            case BossState.Attack: TickAttack(); break;
            case BossState.Block: TickBlock(); break; // <-- added
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

        int incoming = Mathf.Abs(amount);

        // Block mitigation
        if (State == BossState.Block && animator != null && animator.GetBool(paramIsBlocking))
        {
            incoming = Mathf.RoundToInt(incoming * blockedDamageMultiplier);
            if (debugDamage) Debug.Log($"{name} blocked hit. Reduced damage to {incoming}");
        }

        int finalDamage = incoming * damageMult;

        currentHealth = Mathf.Max(0, currentHealth - finalDamage);

        if (finalDamage > 0)
            animator.SetTrigger("Impacted");

        healthbar?.TakeDamage(currentHealth);

        if (currentHealth <= 0) TransitionTo(BossState.Dead);
    }

    void TickIdle()
    {
        if (!agent.isStopped) agent.isStopped = true;
        SetWalk(false, force: true);
        animator.SetBool(paramIsAttackin, false);
        animator.SetBool(paramIsBlocking, false);
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
        float dist = Mathf.Sqrt(sqrDist);

        // Block check (before attack, so boss sometimes guards instead of instantly swinging)
        bool blockAllowed = (canBlockWhileCursed || !isCursed);
        if (blockAllowed &&
            Time.time >= _blockCooldownUntil &&
            !_blockBusy &&
            sqrDist <= _sqrBlockDist &&
            sqrDist > (_sqrAttackEnter * 0.45f)) // don't block when basically touching
        {
            if (Random.value < blockChance)
            {
                TransitionTo(BossState.Block);
                return;
            }
        }

        // Throw if distance is in range [throwDistance .. maxThrowDistance]
        if (sqrDist >= _sqrThrowDist && dist <= maxThrowDistance)
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
        FaceTarget();

        float rawSpeed = agent.velocity.magnitude;
        float alpha = 1f - Mathf.Exp(-speedSmoothing * Time.deltaTime);
        _speedLPF = Mathf.Lerp(_speedLPF, rawSpeed, alpha);

        bool farFromStop = agent.remainingDistance > agent.stoppingDistance + stopEpsilon;
        bool planning = agent.pathPending;
        bool hasIntent = agent.hasPath || planning || agent.desiredVelocity.sqrMagnitude > 0.01f;

        bool wantWalk = hasIntent && (planning || farFromStop || _speedLPF > walkEnterSpeed);
        bool wantIdle = !planning && !farFromStop && _speedLPF < walkExitSpeed;

        if (Time.time >= _nextWalkToggleTime)
        {
            if (!_walkState && wantWalk) SetWalk(true);
            else if (_walkState && wantIdle) SetWalk(false);
        }

        animator.SetBool(paramIsRunning, false);
        animator.SetBool(paramIsAttackin, false);
        animator.SetBool(paramIsBlocking, false);
    }

    void TickAttack()
    {
        FaceTarget();
        if (player)
        {
            float sqrDist = (player.position - transform.position).sqrMagnitude;
            if (sqrDist > _sqrAttackExit && !_attackBusy)
                TransitionTo(BossState.Chase);
        }
    }

    void TickBlock()
    {
        if (!agent.isStopped) agent.isStopped = true;
        agent.velocity = Vector3.zero;
        SetWalk(false, force: true);
        animator.SetBool(paramIsAttackin, false);
        animator.SetBool(paramIsBlocking, true);

        FaceTarget(turnSpeed * 1.35f);
    }

    void SetWalk(bool value, bool force = false)
    {
        if (!force && _walkState == value) return;
        _walkState = value;
        animator.SetBool(paramIsWalking, value);
        _nextWalkToggleTime = Time.time + walkToggleDebounce;
    }

    void FaceTarget() => FaceTarget(turnSpeed);

    void FaceTarget(float customTurnSpeed)
    {
        if (!player) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, customTurnSpeed * Time.deltaTime);
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

        if (State == BossState.Block)
        {
            animator.SetBool(paramIsBlocking, false);
            _blockBusy = false;
            if (_blockCo != null) { StopCoroutine(_blockCo); _blockCo = null; }
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
                animator.SetBool(paramIsBlocking, false);
                break;

            case BossState.Chase:
                agent.isStopped = false;
                animator.SetBool(paramIsBlocking, false);
                break;

            case BossState.Attack:
                animator.SetBool(paramIsBlocking, false);
                if (!_attackBusy) StartCoroutine(AttackRoutine());
                break;

            case BossState.Block:
                if (!_blockBusy) _blockCo = StartCoroutine(BlockRoutine());
                break;

            case BossState.AnchorThrow:
                animator.SetBool(paramIsBlocking, false);
                agent.isStopped = true;
                SetWalk(false, force: true);
                agent.velocity = Vector3.zero;
                AnchorThrowSet();
                break;

            case BossState.Dead:
                isCursed = false;
                reflectDamageWhenCursed = false;
                damageMult = 1;
                animator.SetBool(paramIsBlocking, false);

                if (agent != null) agent.speed = _baseAgentSpeed;
                if (_deathHandled) return;

                IslandTeleporter tel = GameObject.FindAnyObjectByType<IslandTeleporter>()?.GetComponent<IslandTeleporter>();
                if (tel != null) tel.OpenDoor();

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

    IEnumerator BlockRoutine()
    {
        _blockBusy = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        SetWalk(false, force: true);

        animator.SetBool(paramIsAttackin, false);
        animator.SetBool(paramIsBlocking, true);

        float dur = Random.Range(blockDurationMin, blockDurationMax);
        float end = Time.time + dur;

        while (Time.time < end && State == BossState.Block)
        {
            FaceTarget(turnSpeed * 1.35f);
            yield return null;
        }

        animator.SetBool(paramIsBlocking, false);
        _blockCooldownUntil = Time.time + blockCooldown;
        _blockBusy = false;
        _blockCo = null;

        if (State != BossState.Dead)
            TransitionTo(BossState.Chase);
    }

    public void AnchorThrowSet()
    {
        animator.ResetTrigger(paramThrowTrigger);
        animator.SetTrigger(paramThrowTrigger);
    }

    void TickAnchorThrow()
    {
        if (!agent.isStopped) agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (canRotate)
            FaceTarget(throwTurnSpeed);
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
        foreach (var col in cols) col.enabled = false;

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
        if (!other.CompareTag(swordTag)) return;
        if (Time.time < _nextDamageAllowedTime) return;

        int dmg = ResolveDamageFrom(other);
        GetComponent<DamageRef>().TakeDamage(dmg);

        var lantern = GameObject.FindAnyObjectByType<chargeOffHandLatern>();
        if (lantern != null && lantern.enabled)
            lantern.hitRegistered();

        _nextDamageAllowedTime = Time.time + hitInvulnerability;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!enabled || State == BossState.Dead) return;
        if (!collision.collider.CompareTag(swordTag)) return;
        if (Time.time < _nextDamageAllowedTime) return;

        int dmg = ResolveDamageFrom(collision.collider);
        TakeDamage(dmg);
        _nextDamageAllowedTime = Time.time + hitInvulnerability;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, blockRange);
    }

    public void RemoveCurse()
    {
        isCursed = false;
        reflectDamageWhenCursed = false;
        damageMult = 1;
        if (agent != null) agent.speed = _baseAgentSpeed;
    }
}
