using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("AI/Ghost Boss AI")]
[RequireComponent(typeof(NavMeshAgent))]
public class GhostBossAI : MonoBehaviour
{
    public enum BossState { Idle, Chase, Attack, Dead, PhaseLunge }
    public string BossName = "Ghost Boss";

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

    [Header("Attack Selection")]
    [Min(1)] public int numAttackAnims = 5;
    public bool avoidImmediateRepeat = true;

    [Header("Phase Lunge")]
    public bool enablePhaseLunge = true;
    public float lungeMinDistance = 4f;
    public float lungeMaxDistance = 12f;
    public float lungeWindupTime = 0.5f;
    public float lungeTravelTime = 0.35f;
    public float lungeCooldown = 5f;
    public AnimationCurve lungeEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] string paramIsLunging = "isLunging";
    [SerializeField] string paramLungeTelegraph = "LungeTelegraph";

    bool _lungeBusy;
    float _nextLungeAllowedTime;


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

    float _baseAgentSpeed;
    float _cursedSpeedMultiplier = 1f;

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
            case BossState.PhaseLunge: TickPhaseLunge(); break;
        }
    }
    void TickPhaseLunge()
    {
        FaceTarget();
    }


    public void BeginEncounter(Transform playerTarget)
    {
        if (State == BossState.Dead) return;
        if (playerTarget) player = playerTarget;

        healthbar = playerTarget.GetComponentInChildren<CombatController>().boss_healthbar;
        healthbar.maxHealth = maxHealth;
        healthbar.currentHealth = currentHealth;
        healthbar.text.text = BossName;
        healthbar.gameObject.SetActive(true);
        healthbar.TakeDamage(currentHealth);
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

        // --- PHASE LUNGE TRIGGER ---
        if (enablePhaseLunge &&
            Time.time >= _nextLungeAllowedTime &&
            !_attackBusy &&                      // don’t conflict with normal attack
            dist >= lungeMinDistance &&
            dist <= lungeMaxDistance)
        {
            TransitionTo(BossState.PhaseLunge);
            return;
        }

        // --- existing melee attack check ---
        if (Time.time >= _attackCooldownUntil && sqrDist <= _sqrAttackEnter)
        {
            TransitionTo(BossState.Attack);
            return;
        }

        // Move toward player
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

    void TransitionTo(BossState next)
    {
        if (State == next) return;

        // Exit hooks
        if (State == BossState.Attack)
        {
            animator.SetBool(paramIsAttackin, false);
            _attackBusy = false;
        }

        // You can add a clean-up if you want later:
        // if (State == BossState.PhaseLunge) CleanupPhaseLungeState();

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

            case BossState.PhaseLunge:
                if (!_lungeBusy) StartCoroutine(PhaseLungeRoutine());
                break;

            case BossState.Dead:
                HandleDeath();
                break;
        }
    }

    IEnumerator PhaseLungeRoutine()
    {
        _lungeBusy = true;

        // TELEGRAPH
        agent.isStopped = true;
        agent.speed = _baseAgentSpeed;
        SetWalk(false, true);

        if (!string.IsNullOrEmpty(paramLungeTelegraph))
        {
            animator.ResetTrigger(paramLungeTelegraph);
            animator.SetTrigger(paramLungeTelegraph);
        }

        float telegraphEnd = Time.time + lungeWindupTime;
        while (Time.time < telegraphEnd && State == BossState.PhaseLunge)
        {
            FaceTarget();
            yield return null;
        }

        if (State != BossState.PhaseLunge)
        {
            EndPhaseLunge(false);
            yield break;
        }

        // ==== START LUNGE: go to the player's position at lunge start ====
        Vector3 start = transform.position;

        // Where the player is *right now* when we commit to the lunge
        Vector3 target = start + transform.forward * 2f; // fallback
        if (player)
        {
            target = player.position;
        }

        // keep movement mostly horizontal
        target.y = start.y;

        // Clamp target onto the NavMesh, near the player
        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 1.5f, NavMesh.AllAreas))
        {
            target = hit.position;
        }

        if (!string.IsNullOrEmpty(paramIsLunging))
            animator.SetBool(paramIsLunging, true);

        float elapsed = 0f;
        while (elapsed < lungeTravelTime && State == BossState.PhaseLunge)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lungeTravelTime);
            float eased = (lungeEaseCurve != null) ? lungeEaseCurve.Evaluate(t) : t;

            Vector3 newPos = Vector3.Lerp(start, target, eased);
            agent.Warp(newPos);
            FaceTarget();

            yield return null;
        }

        if (!string.IsNullOrEmpty(paramIsLunging))
            animator.SetBool(paramIsLunging, false);

        EndPhaseLunge(true);
    }

    void EndPhaseLunge(bool applyCooldown)
    {
        _lungeBusy = false;

        if (agent)
        {
            agent.isStopped = false;
            agent.speed = _baseAgentSpeed;
        }

        if (applyCooldown)
        {
            _nextLungeAllowedTime = Time.time + lungeCooldown;
        }

        if (State == BossState.PhaseLunge)
        {
            TransitionTo(BossState.Chase);
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, lungeMinDistance);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, lungeMaxDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
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

        if (other.CompareTag(swordTag))
        {
            if (Time.time < _nextDamageAllowedTime) return;

            int dmg = ResolveDamageFrom(other);
            TakeDamage(dmg);
            var lantern = GameObject.FindAnyObjectByType<chargeOffHandLatern>();
            if (lantern != null && lantern.enabled)
            {
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

    
}
