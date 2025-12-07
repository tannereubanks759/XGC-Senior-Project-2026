using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("AI/Magma Boss AI")]
[RequireComponent(typeof(NavMeshAgent))]
public class MagmaBossAI : MonoBehaviour
{
    public enum BossState { Idle, Chase, Attack, Charge, Spit, Dead }
    public string BossName = "Magma Boss";

    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform player;
    public BossHealthbar healthbar;

    [Header("Tuning")]
    public float attackRange = 3.5f;
    public float turnSpeed = 360f;
    public float attackDuration = 1.2f;
    public float attackCooldown = 0.6f;
    public float stopEpsilon = 0.05f;

    [Header("Locomotion")]
    public float walkEnterSpeed = 0.25f;
    public float walkExitSpeed = 0.10f;
    public float walkToggleDebounce = 0.18f;
    public float speedSmoothing = 10f;
    public float rangeHysteresis = 0.5f;

    [Header("Health")]
    public int maxHealth = 300;
    public int currentHealth = 300;

    [Header("Damage Intake")]
    public string swordTag = "PlayerSword";
    public int defaultSwordDamage = 20;
    public float hitInvulnerability = 0.12f;
    public bool debugDamage = false;

    float _nextDamageAllowedTime = 0f;

    [Header("Animator Parameters")]
    [SerializeField] string paramIsWalking = "isWalking";
    [SerializeField] string paramIsRunning = "isRunning";
    [SerializeField] string paramIsAttackin = "isAttack";
    [SerializeField] string paramAttackIdx = "attack";
    [SerializeField] string paramIsDead = "isDead";

    // --- Charge animator params ---
    [SerializeField] string paramIsCharging = "isCharging";           // bool
    [SerializeField] string paramChargeTelegraph = "ChargeTelegraph"; // trigger
    [SerializeField] string paramChargeSpeed = "chargeSpeed";         // float for anim speed blend

    // --- Spit animator params ---
    [SerializeField] string paramIsSpitting = "isSpitting";           // bool
    [SerializeField] string paramSpitTelegraph = "SpitTelegraph";     // trigger

    [Header("Attack Selection")]
    [Min(1)] public int numAttackAnims = 5;
    public bool avoidImmediateRepeat = true;

    // --- Special Selection Weights ---
    [Header("Special Attack Selection")]
    public float chargeWeight = 1f;
    public float spitWeight = 1f;

    // --- Charge Attack Tuning ---
    [Header("Charge Attack")]
    public float chargeStartDistance = 12f;       // must be at least this far to begin a charge
    public float chargeTriggerDistance = 5f;      // when this close, lock direction and go straight
    public float chargeRunDistance = 8f;          // straight-line distance after locking
    public float chargeSpeedMultiplier = 2.5f;    // movement speed multiplier during charge
    public float chargeCooldown = 6f;             // cooldown after a CHARGE

    public float chargeTelegraphTime = 0.8f;      // telegraph wind-up duration

    [Header("Charge Animation Blend")]
    public float minChargeAnimSpeed = 1.0f;       // low end of charge anim speed
    public float maxChargeAnimSpeed = 1.5f;       // high end of charge anim speed

    // --- Spit Attack Tuning ---
    [Header("Spit Attack")]
    public float spitMinDistance = 6f;            // only spit when player is at least this far
    public float spitMaxDistance = 20f;           // and no farther than this
    public float spitCooldown = 4f;               // cooldown after a SPIT
    public float spitTelegraphTime = 0.7f;        // telegraph before spit
    public float spitDuration = 1.0f;             // how long boss remains in spit state

    // Shared cooldown for all specials (charge + spit)
    float _nextSpecialAllowedTime = 0f;

    bool _deathHandled = false;
    public BossState State { get; private set; } = BossState.Idle;

    // Internals
    bool _attackBusy;
    float _sqrAttackEnter;
    float _sqrAttackExit;
    float _attackCooldownUntil;
    int _lastAttack = 0;

    // locomotion smoothing
    bool _walkState;
    float _nextWalkToggleTime;
    float _speedLPF;

    // Charge internals
    float _baseAgentSpeed;
    float _cursedSpeedMultiplier = 1f;
    bool _chargeBusy;
    bool _chargeCancelRequested;
    Vector3 _chargeDirection;
    Vector3 _chargeStartPos;

    // Spit internals
    bool _spitBusy;
    [Header("Curse")]
    public bool isCursed = false;
    public bool reflectDamageWhenCursed = false;
    public GameObject cursedVfxPrefab;
    public int damageMult = 1;
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

        RecalcRanges();
    }

    void RecalcRanges()
    {
        _sqrAttackEnter = attackRange * attackRange;
        float exitR = attackRange + Mathf.Max(0.05f, rangeHysteresis);
        _sqrAttackExit = exitR * exitR;
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
            case BossState.Charge: TickCharge(); break;
            case BossState.Spit: TickSpit(); break;
        }
        
    }

    public void BeginEncounter(Transform playerTarget)
    {
        if (State == BossState.Dead) return;
        if (playerTarget) player = playerTarget;

        healthbar = playerTarget.GetComponentInChildren<CombatController>().boss_healthbar;
        healthbar.maxHealth = maxHealth;
        healthbar.currentHealth = currentHealth;
        healthbar.text.text = BossName;
        healthbar.TakeDamage(currentHealth);
        healthbar.gameObject.SetActive(true);
        healthbar.ShowHealthbarOnBossTriggered();
        
        TransitionTo(BossState.Chase);
    }

    public void TakeDamage(int amount)
    {
        if (State == BossState.Dead) return;
        

        int finalDamage = Mathf.Max(1, amount * damageMult);
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(finalDamage));
        animator.SetTrigger("Impacted");

        healthbar.TakeDamage(currentHealth);

        if (currentHealth <= 0)
            TransitionTo(BossState.Dead);
    }
    public void CurseBoss(bool slow, bool reflection)
    {
        if (isCursed) return;

        isCursed = true;
        reflectDamageWhenCursed = reflection;
        damageMult = 2;

        if (slow)
        {
            _cursedSpeedMultiplier = 0.75f;
            _baseAgentSpeed *= _cursedSpeedMultiplier;
            agent.speed = _baseAgentSpeed;
        }
    }
    public void OnDealtDamageToPlayer(int dealtDamage)
    {
        if (isCursed && reflectDamageWhenCursed)
        {
            int reflected = Mathf.RoundToInt(dealtDamage * 0.5f);
            TakeDamage(reflected);
            
        }
    }
    void TickIdle()
    {
        agent.isStopped = true;
        agent.speed = _baseAgentSpeed;
        SetWalk(false, true);
        animator.SetBool(paramIsAttackin, false);
        animator.SetBool(paramIsCharging, false);
        animator.SetBool(paramIsSpitting, false);
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

        // --- SPECIAL (Charge/Spit) trigger: shared cooldown ---
        if (Time.time >= _nextSpecialAllowedTime)
        {
            bool canCharge = dist >= chargeStartDistance;
            bool canSpit = dist >= spitMinDistance && dist <= spitMaxDistance;

            if (canCharge || canSpit)
            {
                BossState choice = ChooseSpecial(canCharge, canSpit);
                if (choice == BossState.Charge)
                {
                    TransitionTo(BossState.Charge);
                    return;
                }
                else if (choice == BossState.Spit)
                {
                    TransitionTo(BossState.Spit);
                    return;
                }
            }
        }

        // Attack if close enough & off cooldown (original logic)
        if (Time.time >= _attackCooldownUntil && sqrDist <= _sqrAttackEnter)
        {
            TransitionTo(BossState.Attack);
            return;
        }

        // Move toward player (original chase logic)
        agent.isStopped = false;
        agent.speed = _baseAgentSpeed;
        agent.SetDestination(player.position);
        FaceTarget();

        // speed smoothing
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
            if (!_walkState && wantWalk) SetWalk(true);
            else if (_walkState && wantIdle) SetWalk(false);
        }

        animator.SetBool(paramIsRunning, false);
        animator.SetBool(paramIsAttackin, false);
        animator.SetBool(paramIsCharging, false);
        animator.SetBool(paramIsSpitting, false);
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

    void TickCharge()
    {
        // Charge steering handled mostly by coroutine; keep facing direction
        if (_chargeDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = _chargeDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    turnSpeed * Time.deltaTime
                );
            }
        }
    }

    void TickSpit()
    {
        // Just face the player, do not move
        FaceTarget();
        agent.isStopped = true;
        agent.speed = 0f;
    }

    void SetWalk(bool value, bool force = false)
    {
        if (!force && _walkState == value) return;
        _walkState = value;
        animator.SetBool(paramIsWalking, value);
        _nextWalkToggleTime = Time.time + walkToggleDebounce;
    }

    void FaceTarget()
    {
        if (!player) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            turnSpeed * Time.deltaTime
        );
    }

    BossState ChooseSpecial(bool canCharge, bool canSpit)
    {
        float cw = canCharge ? Mathf.Max(0f, chargeWeight) : 0f;
        float sw = canSpit ? Mathf.Max(0f, spitWeight) : 0f;

        if (cw <= 0f && sw <= 0f)
        {
            // fallback – prefer charge if legal
            if (canCharge) return BossState.Charge;
            if (canSpit) return BossState.Spit;
            return BossState.Chase;
        }

        float total = cw + sw;
        float r = Random.value * total;

        if (r < cw && canCharge) return BossState.Charge;
        if (canSpit) return BossState.Spit;
        return canCharge ? BossState.Charge : BossState.Chase;
    }

    void TransitionTo(BossState next)
    {
        if (State == next) return;

        // Exit hooks
        if (State == BossState.Attack)
        {
            animator.SetBool(paramIsAttackin, false);
            _attackBusy = false;
        }

        if (State == BossState.Charge)
        {
            CleanupChargeState();
        }

        if (State == BossState.Spit)
        {
            CleanupSpitState();
        }

        State = next;

        switch (next)
        {
            case BossState.Idle:
                agent.isStopped = true;
                agent.speed = _baseAgentSpeed;
                SetWalk(false, true);
                break;

            case BossState.Chase:
                agent.isStopped = false;
                agent.speed = _baseAgentSpeed;
                break;

            case BossState.Attack:
                if (!_attackBusy) StartCoroutine(AttackRoutine());
                break;

            case BossState.Charge:
                if (!_chargeBusy) StartCoroutine(ChargeRoutine());
                break;

            case BossState.Spit:
                if (!_spitBusy) StartCoroutine(SpitRoutine());
                break;

            case BossState.Dead:
                HandleDeath();
                break;
        }
    }

    void HandleDeath()
    {
        if (_deathHandled) return;

        _deathHandled = true;

        IslandTeleporter tel = GameObject.FindAnyObjectByType<IslandTeleporter>()?
            .GetComponent<IslandTeleporter>();
        if (tel != null) tel.OpenDoor();

        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        SetWalk(false, true);
        animator.SetBool(paramIsAttackin, false);
        animator.SetBool(paramIsCharging, false);
        animator.SetBool(paramIsSpitting, false);

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        StartCoroutine(DieRoutine());
    }

    IEnumerator AttackRoutine()
    {
        _attackBusy = true;
        agent.isStopped = true;
        agent.speed = _baseAgentSpeed;
        SetWalk(false, true);

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

    // --- CHARGE ---

    IEnumerator ChargeRoutine()
    {
        _chargeBusy = true;
        _chargeCancelRequested = false;
        _chargeDirection = Vector3.zero;
        _chargeStartPos = transform.position;

        // TELEGRAPH PHASE
        agent.isStopped = true;
        agent.speed = _baseAgentSpeed;
        SetWalk(false, true);
        animator.SetBool(paramIsCharging, false);

        if (!string.IsNullOrEmpty(paramChargeTelegraph))
        {
            animator.ResetTrigger(paramChargeTelegraph);
            animator.SetTrigger(paramChargeTelegraph);
        }

        float telegraphEnd = Time.time + chargeTelegraphTime;
        while (Time.time < telegraphEnd && State == BossState.Charge && !_chargeCancelRequested)
        {
            FaceTarget();
            yield return null;
        }

        if (State != BossState.Charge || _chargeCancelRequested)
        {
            EndCharge(false);
            yield break;
        }

        // HOMING PHASE - move toward player until within chargeTriggerDistance
        agent.isStopped = false;
        agent.speed = _baseAgentSpeed * chargeSpeedMultiplier;
        animator.SetBool(paramIsCharging, true);

        while (State == BossState.Charge && !_chargeCancelRequested)
        {
            if (!player)
            {
                EndCharge(false);
                yield break;
            }

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            if (dist <= chargeTriggerDistance)
            {
                // lock direction
                if (toPlayer.sqrMagnitude > 0.0001f)
                    _chargeDirection = toPlayer.normalized;
                else
                    _chargeDirection = transform.forward;

                _chargeStartPos = transform.position;
                break;
            }

            // continue homing
            agent.SetDestination(player.position);
            FaceTarget();

            // Blend anim speed based on velocity
            UpdateChargeAnimSpeed();

            yield return null;
        }

        if (State != BossState.Charge || _chargeCancelRequested)
        {
            EndCharge(false);
            yield break;
        }

        // FORWARD BURST PHASE - run straight until distance or navmesh fails
        float traveled = 0f;
        Vector3 lastPos = transform.position;

        while (State == BossState.Charge && !_chargeCancelRequested)
        {
            float stepDistance = agent.speed * Time.deltaTime;
            Vector3 nextPos = transform.position + _chargeDirection * stepDistance;

            // Check navmesh
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(nextPos, out hit, 0.3f, NavMesh.AllAreas))
            {
                // no navmesh to run on
                break;
            }

            agent.Warp(hit.position);

            traveled = Vector3.Distance(_chargeStartPos, transform.position);
            if (traveled >= chargeRunDistance)
                break;

            // Blend anim speed based on actual movement
            float actualSpeed = (transform.position - lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPos = transform.position;
            UpdateChargeAnimSpeed(actualSpeed);

            yield return null;
        }

        EndCharge(true);
    }

    void UpdateChargeAnimSpeed(float actualSpeed = -1f)
    {
        if (string.IsNullOrEmpty(paramChargeSpeed) || animator == null) return;

        float refSpeed = _baseAgentSpeed * chargeSpeedMultiplier;
        float speedNorm;

        if (actualSpeed < 0f)
        {
            // fallback to agent velocity if not provided
            speedNorm = refSpeed > 0.01f ? Mathf.Clamp01(agent.velocity.magnitude / refSpeed) : 0f;
        }
        else
        {
            speedNorm = refSpeed > 0.01f ? Mathf.Clamp01(actualSpeed / refSpeed) : 0f;
        }

        float animSpeed = Mathf.Lerp(minChargeAnimSpeed, maxChargeAnimSpeed, speedNorm);
        animator.SetFloat(paramChargeSpeed, animSpeed);
    }

    void CleanupChargeState()
    {
        _chargeBusy = false;
        _chargeCancelRequested = false;
        _chargeDirection = Vector3.zero;

        if (animator)
        {
            animator.SetBool(paramIsCharging, false);
            if (!string.IsNullOrEmpty(paramChargeSpeed))
                animator.SetFloat(paramChargeSpeed, 1f);
        }

        if (agent)
        {
            agent.speed = _baseAgentSpeed;
            agent.isStopped = false;
        }
    }

    void EndCharge(bool applyCooldown)
    {
        if (State == BossState.Charge)
        {
            CleanupChargeState();
            TransitionTo(BossState.Chase);
        }
        else
        {
            CleanupChargeState();
        }

        if (applyCooldown)
        {
            _nextSpecialAllowedTime = Time.time + chargeCooldown;
        }
    }

    // --- SPIT ---

    IEnumerator SpitRoutine()
    {
        _spitBusy = true;

        // TELEGRAPH PHASE
        agent.isStopped = true;
        agent.speed = 0f;
        SetWalk(false, true);
        animator.SetBool(paramIsSpitting, false);

        if (!string.IsNullOrEmpty(paramSpitTelegraph))
        {
            animator.ResetTrigger(paramSpitTelegraph);
            animator.SetTrigger(paramSpitTelegraph);
        }

        float telegraphEnd = Time.time + spitTelegraphTime;
        while (Time.time < telegraphEnd && State == BossState.Spit)
        {
            FaceTarget();
            yield return null;
        }

        if (State != BossState.Spit)
        {
            EndSpit(false);
            yield break;
        }

        // SPITTING PHASE – stay in place, let animation handle VFX
        animator.SetBool(paramIsSpitting, true);
        agent.isStopped = true;
        agent.speed = 0f;

        float spitEnd = Time.time + spitDuration;
        while (Time.time < spitEnd && State == BossState.Spit)
        {
            FaceTarget();
            yield return null;
        }

        EndSpit(true);
    }

    void CleanupSpitState()
    {
        _spitBusy = false;

        if (animator)
        {
            animator.SetBool(paramIsSpitting, false);
        }

        if (agent)
        {
            agent.speed = _baseAgentSpeed;
            agent.isStopped = false;
        }
    }

    void EndSpit(bool applyCooldown)
    {
        if (State == BossState.Spit)
        {
            CleanupSpitState();
            TransitionTo(BossState.Chase);
        }
        else
        {
            CleanupSpitState();
        }

        if (applyCooldown)
        {
            _nextSpecialAllowedTime = Time.time + spitCooldown;
        }
    }

    IEnumerator DieRoutine()
    {
        animator.SetTrigger(paramIsDead);

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        yield return new WaitForSeconds(.01f);
        enabled = false;
    }

    int PickAttackIndex()
    {
        if (numAttackAnims <= 1 || !avoidImmediateRepeat)
            return Mathf.Clamp(Random.Range(1, numAttackAnims + 1), 1, numAttackAnims);

        if (numAttackAnims == 2)
            return (_lastAttack == 1) ? 2 : 1;

        int pick;
        do { pick = Random.Range(1, numAttackAnims + 1); }
        while (pick == _lastAttack);

        return pick;
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

        // Stop charge if we hit the player OR an arena wall trigger
        if (State == BossState.Charge &&
            (other.CompareTag("Player") || other.CompareTag("ArenaWallTrigger")))
        {
            _chargeCancelRequested = true;
        }

        if (other.CompareTag(swordTag))
        {
            if (Time.time < _nextDamageAllowedTime) return;

            int dmg = ResolveDamageFrom(other);
            TakeDamage(dmg);
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

        // Stop charge if we hit anything solid (wall, environment, player collider)
        if (State == BossState.Charge && !collision.collider.isTrigger)
        {
            _chargeCancelRequested = true;
        }

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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chargeStartDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, chargeTriggerDistance);

        // Spit distances (min & max)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spitMinDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, spitMaxDistance);
    }
}
