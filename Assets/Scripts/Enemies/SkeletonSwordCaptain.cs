using RayFire;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class SkeletonSwordCaptain : MonoBehaviour
{
    [Header("Captain Identity")]
    public string captainName = "Skeleton Sword Captain";
    public bool startAggressive = false;

    public enum State
    {
        Idle,
        Patrol,
        Investigate,
        Chase,
        Strafe,
        Defensive,
        Reposition,
        Feint,
        Attack,
        ComboAttack,
        Recover,
        Stunned,
        Dead
    }

    [Header("Curse")]
    public bool isCursed = false;
    public int curseDamageMult = 1;
    public float curseSpeedMult = 1f;
    public bool curseReflectEnabled = false;
    [Range(0f, 1f)] public float curseReflectPercent = 0.25f;

    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform eyePoint;
    public Transform target;
    public SwordHitbox swordHitbox;

    [Header("Optional Manual Hitbox Control")]
    public bool controlHitboxFromScript = false;
    public float hitboxActiveTime = 0.22f;

    [Header("Player Auto-Find")]
    public string playerTag = "Player";
    public float findTargetInterval = 0.5f;
    public bool preferMainCameraForLook = true;

    [Header("Head Look")]
    public LookAtConstraint headLookConstraint;
    public Transform targetHead;
    public float headLookRange = 16f;
    [Range(10f, 180f)] public float headLookMaxAngle = 100f;
    public float headLookBlendSpeed = 10f;
    [Range(0f, 1f)] public float headLookMaxWeight = 1f;

    [Header("Patrol")]
    public bool startPatrolling = true;
    public float patrolRadius = 14f;
    public float patrolArriveTolerance = 1.1f;
    public Vector2 patrolStallTimeRange = new Vector2(0.35f, 1.2f);
    public float patrolRepathCheckInterval = 0.35f;
    public int patrolFindMaxAttempts = 12;
    public float patrolRadiusJitter = 2f;
    public float patrolSpeed = 2.25f;
    public float patrolAcceleration = 10f;
    public float patrolStoppingDistance = 0.4f;

    [Header("Captain Perception")]
    public float visionRange = 26f;
    [Range(10f, 180f)] public float visionFov = 140f;
    public float closeAwarenessRange = 10f;
    public float autoDetectRange = 4.5f;
    public float sightThickness = 0.16f;
    public float fallbackEyeHeight = 1.7f;
    public float targetAimHeight = 1.4f;
    public LayerMask occlusionMask;
    public bool debugVision;
    public float hearingRange = 16f;
    public float aggroMemoryTime = 7f;

    [Header("Captain Combat")]
    public float attackRange = 2.25f;
    public float stopDistance = 1.85f;
    public float turnSpeed = 14f;

    [Header("Smart Combat Decisions")]
    [Range(0f, 1f)] public float strafeChance = 0.75f;
    [Range(0f, 1f)] public float feintChance = 0.25f;
    [Range(0f, 1f)] public float comboChance = 0.55f;
    [Range(0f, 1f)] public float heavyAttackChance = 0.32f;
    [Range(0f, 1f)] public float defensiveModeChance = 0.32f;
    [Range(0f, 1f)] public float repositionChance = 0.25f;

    public Vector2 strafeDurationRange = new Vector2(0.5f, 1.2f);
    public Vector2 attackCooldownRange = new Vector2(0.45f, 1.0f);
    public Vector2 comboCooldownRange = new Vector2(0.75f, 1.35f);

    public float attackWindup = 0.16f;
    public float heavyAttackWindup = 0.28f;
    public float attackRecovery = 0.22f;
    public float comboDelayBetweenHits = 0.18f;
    public int minComboHits = 2;
    public int maxComboHits = 3;

    [Header("Feint")]
    public float feintDuration = 0.35f;
    public float feintForwardStepDistance = 0.75f;
    public string animFeintTrigger = "Feint";

    [Header("Reposition")]
    public float repositionDistance = 3.0f;
    public float repositionDuration = 0.7f;
    public float repositionSpeed = 3.6f;
    public float repositionCooldown = 1.25f;

    [Header("Defense / Blocking")]
    public string animIsBlockingBool = "IsBlocking";
    public bool isBlocking = false;
    [Range(0f, 1f)] public float blockChanceWhileStrafing = 0.55f;
    [Range(0f, 1f)] public float blockChanceAfterHit = 0.45f;
    public float blockAfterHitDuration = 0.45f;

    [Header("Guard Break")]
    public float guardBreakStunTime = 1.0f;
    public float guardBreakNoBlockTime = 1.25f;
    public string animGuardBreakTrigger = "";

    [Header("Defensive Movement")]
    public Vector2 defensiveDurationRange = new Vector2(0.55f, 1.25f);
    public float defensiveBackUpDistance = 2.35f;
    public float defensiveSpeed = 2.75f;
    public float defensiveAcceleration = 12f;
    public float defensiveStoppingDistance = 0.2f;
    public float defensiveCooldown = 1.0f;

    [Header("React To Player Rush")]
    public float rushReactDistance = 6.0f;
    public float rushClosingSpeed = 3.0f;
    [Range(0f, 1f)] public float rushBackUpChance = 0.45f;
    [Range(0f, 1f)] public float rushSideStepChance = 0.55f;

    [Header("Combat Movement")]
    public float chaseSpeedFar = 4.6f;
    public float chaseSpeedNear = 1.85f;
    public float slowDownStartDistance = 7.0f;
    public float slowDownEndDistance = 2.3f;
    public float speedBlend = 9.0f;
    public float accelFar = 20f;
    public float accelNear = 9f;
    [Header("Strafe Facing")]
    [Tooltip("If true, the captain rotates partly toward movement while strafing to reduce sliding.")]
    public bool turnBodyWithStrafe = true;

    [Range(0f, 1f)]
    [Tooltip("0 = fully face target, 1 = fully face movement direction while strafing.")]
    public float strafeMovementFacingBias = 0.7f;

    [Tooltip("Extra turn speed while strafing.")]
    public float strafeTurnSpeedMultiplier = 1.35f;

    [Header("Predictive Chase")]
    public bool usePredictiveChase = true;
    public float predictionTime = 0.35f;
    public float maxPredictionDistance = 2.5f;

    [Header("Enrage")]
    public bool canEnrage = true;
    [Range(0.05f, 0.95f)] public float enrageHealthPercent = 0.35f;
    public float enrageSpeedMultiplier = 1.18f;
    public float enrageAttackCooldownMultiplier = 0.72f;
    public float enrageComboChanceBonus = 0.25f;
    public string animEnrageTrigger = "Enrage";

    [Header("Locomotion Animation")]
    public float animMaxMoveSpeed = 4.6f;

    [Header("Damage / Stun")]
    public float maxHealth = 180f;
    public float stunDuration = 0.45f;
    public bool resistLightStunsWhileEnraged = true;

    [Header("Animation Params")]
    public string animSpeedParam = "Speed";
    public string animAggroBool = "Aggro";
    public string animLightAttackTrigger = "LightAttack";
    public string animHeavyAttackTrigger = "HeavyAttack";
    public string animHitTrigger = "Hit";

    [Header("Tuning")]
    public float senseInterval = 0.10f;
    public float chaseRepathInterval = 0.14f;
    [Range(0f, 1f)] public float staggerStrength = 1f;

    [Header("Sounds")]
    public AudioSource swordAudioSource;
    public AudioClip[] swordBlockClips;
    public AudioSource growlSource;
    public AudioClip growlClip;
    public AudioClip enrageClip;
    public AudioClip feintClip;

    private State _state;
    private float _health;
    private bool _enraged;

    private float _lastSeenTime = -999f;
    private Vector3 _lastKnownPos;

    private float _nextSenseTime;
    private float _nextRepathTime;
    private float _nextFindTargetTime;
    private float _nextAttackAllowedTime;
    private float _nextDefensiveAllowedTime;
    private float _nextRepositionAllowedTime;
    private float _nextForceTargetRefreshTime;

    private Coroutine _stateRoutine;
    private Vector3 _spawnPos;

    private float _headLookW;
    private readonly RaycastHit[] _sightHits = new RaycastHit[24];

    private Vector3 _lastTargetPos;
    private Vector3 _targetVelocity;
    private bool _hasLastTargetPos;

    private bool _blockThisStrafe;
    private float _blockBrokenUntil = -999f;
    private float _stunEndTime = -999f;

    private bool _lookSourceInitialized;
    private bool _registeredAsHostile;
    private bool _hasGrowled;

    private int _targetInstanceId = -1;

    private Collider[] _swordHitColliders;

    private void Awake()
    {
        if (!agent)
            agent = GetComponent<NavMeshAgent>();

        _health = maxHealth;
        _spawnPos = transform.position;
        _nextSenseTime = Time.time + Random.Range(0.05f, 0.25f);

        CacheSwordHitColliders();
        SetSwordHitboxActive(false);
    }
    private IEnumerator Start()
    {
        yield return null;

        if (!agent || !agent.enabled || !agent.isOnNavMesh)
            yield break;

        StartCoroutine(DelayedLookInit());

        if (startAggressive)
        {
            AcquireTargetIfNeeded(true);

            if (target)
                SetState(State.Chase);
            else
                SetState(startPatrolling ? State.Patrol : State.Idle);
        }
        else
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
        }
    }

    private void Update()
    {
        if (_state == State.Dead) return;
        if (!agent || !agent.enabled || !agent.isOnNavMesh) return;

        RefreshTargetReliably();
        UpdateTargetVelocityEstimate();

        if (Time.time >= _nextSenseTime)
        {
            _nextSenseTime = Time.time + senseInterval;
            Sense();
        }

        TryEnrage();
        UpdateCombatMoveTuning();
        UpdateAnimatorLocomotion();
        UpdateHeadLookConstraint();
        UpdateCombatRegistration(IsCombatState(_state));

        if (ShouldFaceTargetOrMove())
            FaceTargetOrMovement();

        if ((_state == State.Chase || _state == State.Recover) && target && Time.time >= _nextAttackAllowedTime)
        {
            float dist = Vector3.Distance(transform.position, target.position);

            if (dist <= attackRange && Time.time >= _nextDefensiveAllowedTime)
                ChooseCaptainCloseRangeAction(dist);
        }

        if (_state == State.Chase && target && Time.time >= _nextDefensiveAllowedTime)
        {
            float dist = Vector3.Distance(transform.position, target.position);

            if (dist <= rushReactDistance && IsTargetRushingMe())
            {
                if (Random.value < rushSideStepChance && Time.time >= _nextRepositionAllowedTime)
                    SetState(State.Reposition);
                else if (Random.value < rushBackUpChance)
                    SetState(State.Defensive);
            }
        }
    }

    private void OnDisable()
    {
        UpdateCombatRegistration(false);
    }

    private void UpdateCombatRegistration(bool shouldBeHostile)
    {
        if (shouldBeHostile && !_registeredAsHostile)
        {
            _registeredAsHostile = true;
            CombatTracker.Instance?.RegisterHostile(this);
        }
        else if (!shouldBeHostile && _registeredAsHostile)
        {
            _registeredAsHostile = false;
            CombatTracker.Instance?.UnregisterHostile(this);
        }
    }

    private bool IsCombatState(State state)
    {
        return state != State.Idle &&
               state != State.Patrol &&
               state != State.Dead;
    }

    private bool ShouldFaceTargetOrMove()
    {
        return _state == State.Patrol ||
               _state == State.Investigate ||
               _state == State.Chase ||
               _state == State.Strafe ||
               _state == State.Defensive ||
               _state == State.Reposition ||
               _state == State.Feint ||
               _state == State.Attack ||
               _state == State.ComboAttack ||
               _state == State.Recover;
    }

    private IEnumerator DelayedLookInit()
    {
        yield return null;
        yield return new WaitForSeconds(0.15f);
        AcquireTargetIfNeeded(true);
    }

    private void RefreshTargetReliably()
    {
        if (target == null)
        {
            targetHead = null;
            _targetInstanceId = -1;
            _hasLastTargetPos = false;
            AcquireTargetIfNeeded(false);
            return;
        }

        if (Time.time < _nextForceTargetRefreshTime)
            return;

        _nextForceTargetRefreshTime = Time.time + 1.0f;

        GameObject currentPlayer = null;

        if (!string.IsNullOrEmpty(playerTag))
            currentPlayer = GameObject.FindGameObjectWithTag(playerTag);

        if (currentPlayer == null)
            return;

        int currentId = currentPlayer.GetInstanceID();

        if (_targetInstanceId == -1)
            _targetInstanceId = currentId;

        if (currentId != _targetInstanceId)
        {
            target = null;
            targetHead = null;
            _lookSourceInitialized = false;
            _targetInstanceId = -1;
            _hasLastTargetPos = false;

            AcquireTargetIfNeeded(true);

            if (_state == State.Chase ||
                _state == State.Strafe ||
                _state == State.Defensive ||
                _state == State.Reposition ||
                _state == State.Feint ||
                _state == State.Attack ||
                _state == State.ComboAttack ||
                _state == State.Recover)
            {
                SetState(startPatrolling ? State.Patrol : State.Idle);
            }
        }
    }

    private void AcquireTargetIfNeeded(bool force)
    {
        if (target != null) return;

        if (!force)
        {
            if (Time.time < _nextFindTargetTime) return;
            _nextFindTargetTime = Time.time + findTargetInterval;
        }

        Transform foundPlayer = null;

        if (!string.IsNullOrEmpty(playerTag))
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null && playerObject.activeInHierarchy)
                foundPlayer = playerObject.transform;
        }

        if (foundPlayer == null) return;

        target = foundPlayer;
        _targetInstanceId = foundPlayer.gameObject.GetInstanceID();

        Transform foundCam = null;

        if (preferMainCameraForLook && Camera.main != null)
            foundCam = Camera.main.transform;

        if (foundCam == null)
        {
            Camera cam = target.GetComponentInChildren<Camera>(true);

            if (cam != null)
                foundCam = cam.transform;
        }

        if (foundCam != null)
        {
            targetHead = foundCam;
            _lookSourceInitialized = false;
            SetLookAtConstraintSource(foundCam);
        }

        _hasLastTargetPos = false;
    }

    private void SetLookAtConstraintSource(Transform lookTarget)
    {
        if (headLookConstraint == null || lookTarget == null) return;
        if (_lookSourceInitialized) return;

        if (headLookConstraint.locked)
            headLookConstraint.locked = false;

        List<ConstraintSource> sources = new List<ConstraintSource>(1)
        {
            new ConstraintSource { sourceTransform = lookTarget, weight = 1f }
        };

        headLookConstraint.SetSources(sources);
        headLookConstraint.constraintActive = true;
        _lookSourceInitialized = true;
    }

    private void UpdateHeadLookConstraint()
    {
        if (headLookConstraint == null) return;

        float desired = 0f;

        bool engaged =
            _state == State.Chase ||
            _state == State.Strafe ||
            _state == State.Defensive ||
            _state == State.Reposition ||
            _state == State.Feint ||
            _state == State.Attack ||
            _state == State.ComboAttack ||
            _state == State.Recover;

        if (engaged && target != null)
        {
            Vector3 lookPos = targetHead != null ? targetHead.position : target.position + Vector3.up * targetAimHeight;
            Vector3 to = lookPos - transform.position;
            float dist = to.magnitude;

            if (dist > 0.001f && dist <= headLookRange)
            {
                Vector3 toFlat = to;
                toFlat.y = 0f;

                Vector3 fwdFlat = transform.forward;
                fwdFlat.y = 0f;

                if (toFlat.sqrMagnitude > 0.0001f && fwdFlat.sqrMagnitude > 0.0001f)
                {
                    float yawAngle = Vector3.Angle(fwdFlat.normalized, toFlat.normalized);

                    if (yawAngle <= headLookMaxAngle)
                        desired = 1f;
                }
            }
        }

        _headLookW = Mathf.MoveTowards(_headLookW, desired, headLookBlendSpeed * Time.deltaTime);
        headLookConstraint.weight = _headLookW * Mathf.Clamp01(headLookMaxWeight);
    }

    private void UpdateTargetVelocityEstimate()
    {
        if (!target)
        {
            _hasLastTargetPos = false;
            _targetVelocity = Vector3.zero;
            return;
        }

        Vector3 pos = target.position;

        if (!_hasLastTargetPos)
        {
            _lastTargetPos = pos;
            _hasLastTargetPos = true;
            _targetVelocity = Vector3.zero;
            return;
        }

        float dt = Mathf.Max(0.0001f, Time.deltaTime);
        Vector3 vel = (pos - _lastTargetPos) / dt;
        vel.y = 0f;

        _targetVelocity = Vector3.Lerp(_targetVelocity, vel, 1f - Mathf.Exp(-10f * Time.deltaTime));
        _lastTargetPos = pos;
    }

    private bool IsTargetRushingMe()
    {
        if (!target) return false;

        Vector3 toEnemy = transform.position - target.position;
        toEnemy.y = 0f;

        if (toEnemy.sqrMagnitude < 0.0001f)
            return false;

        float closing = Vector3.Dot(_targetVelocity, toEnemy.normalized);
        return closing >= rushClosingSpeed;
    }

    private void Sense()
    {
        if (!target)
        {
            AcquireTargetIfNeeded(false);
            return;
        }

        bool detected = CanSeeTarget(out Vector3 seenPos);

        if (detected)
        {
            _lastSeenTime = Time.time;
            _lastKnownPos = seenPos;

            if (_state == State.Idle || _state == State.Patrol || _state == State.Investigate)
                SetState(State.Chase);
        }
        else
        {
            bool hasMemory = Time.time - _lastSeenTime <= aggroMemoryTime;

            if (!hasMemory && (_state == State.Chase || _state == State.Strafe || _state == State.Defensive || _state == State.Reposition))
            {
                if (_lastSeenTime > -998f)
                    SetState(State.Investigate);
                else
                    SetState(startPatrolling ? State.Patrol : State.Idle);
            }
        }
    }

    private bool CanSeeTarget(out Vector3 seenPos)
    {
        seenPos = Vector3.zero;

        if (!target) return false;

        Vector3 origin = eyePoint ? eyePoint.position : transform.position + Vector3.up * fallbackEyeHeight;
        origin += transform.up * 0.05f + transform.forward * 0.12f;

        Vector3 aim = targetHead != null ? targetHead.position : target.position + Vector3.up * targetAimHeight;

        Vector3 to = aim - origin;
        float dist = to.magnitude;

        if (dist <= 0.001f)
            return true;

        if (dist > visionRange)
            return false;

        bool inFov = true;

        if (dist > closeAwarenessRange && dist > autoDetectRange)
        {
            Vector3 toFlat = to;
            toFlat.y = 0f;

            Vector3 fwdFlat = transform.forward;
            fwdFlat.y = 0f;

            if (toFlat.sqrMagnitude > 0.0001f && fwdFlat.sqrMagnitude > 0.0001f)
            {
                float angle = Vector3.Angle(fwdFlat.normalized, toFlat.normalized);
                inFov = angle <= visionFov * 0.5f;
            }
        }

        if (!inFov)
            return false;

        Vector3 dir = to / dist;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            Mathf.Max(0.01f, sightThickness),
            dir,
            _sightHits,
            dist,
            occlusionMask,
            QueryTriggerInteraction.Ignore
        );

        bool blocked = false;
        RaycastHit closestBlock = default;
        float closest = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _sightHits[i];
            Collider hitCollider = hit.collider;

            if (!hitCollider) continue;
            if (hitCollider.transform.IsChildOf(transform)) continue;

            blocked = true;

            if (hit.distance < closest)
            {
                closest = hit.distance;
                closestBlock = hit;
            }
        }

        if (debugVision)
        {
            Debug.DrawLine(origin, aim, blocked ? Color.red : Color.green, senseInterval);

            if (blocked)
                Debug.DrawRay(closestBlock.point, closestBlock.normal * 0.25f, Color.yellow, senseInterval);
        }

        if (blocked)
            return false;

        seenPos = target.position;
        return true;
    }

    public void OnHeardNoise(Vector3 noisePos, float loudness01 = 1f)
    {
        if (_state == State.Dead) return;

        float dist = Vector3.Distance(transform.position, noisePos);
        float effectiveRange = hearingRange * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(loudness01));

        if (dist <= effectiveRange)
        {
            _lastKnownPos = noisePos;

            if (_state == State.Idle || _state == State.Patrol)
                SetState(State.Investigate);
        }
    }

    private void SetState(State newState)
    {
        if (_state == newState) return;

        _state = newState;

        if (agent)
            agent.stoppingDistance = newState == State.Patrol ? patrolStoppingDistance : stopDistance;

        if (_stateRoutine != null)
            StopCoroutine(_stateRoutine);

        _stateRoutine = StartCoroutine(RunState(_state));
    }

    private IEnumerator RunState(State state)
    {
        SetAnimatorAggro(IsCombatState(state));

        switch (state)
        {
            case State.Idle:
                SetBlocking(false);
                agent.isStopped = true;
                yield break;

            case State.Patrol:
                SetBlocking(false);
                yield return PatrolLoop();
                yield break;

            case State.Investigate:
                SetBlocking(false);
                yield return InvestigateLoop();
                yield break;

            case State.Chase:
                PlayGrowlOnce();
                SetBlocking(false);
                yield return ChaseLoop();
                yield break;

            case State.Strafe:
                _blockThisStrafe = Random.value < blockChanceWhileStrafing;
                SetBlocking(_blockThisStrafe);
                yield return StrafeLoop();
                SetBlocking(false);
                yield break;

            case State.Defensive:
                SetBlocking(true);
                yield return DefensiveLoop();
                SetBlocking(false);
                yield break;

            case State.Reposition:
                SetBlocking(Random.value < 0.35f);
                yield return RepositionLoop();
                SetBlocking(false);
                yield break;

            case State.Feint:
                SetBlocking(false);
                yield return FeintLoop();
                yield break;

            case State.Attack:
                SetBlocking(false);
                yield return AttackOnce();
                yield break;

            case State.ComboAttack:
                SetBlocking(false);
                yield return ComboAttack();
                yield break;

            case State.Recover:
                SetBlocking(false);
                yield return RecoverBriefly();
                yield break;

            case State.Stunned:
                SetBlocking(false);
                yield return StunnedBriefly();
                yield break;

            case State.Dead:
                SetBlocking(false);
                SetSwordHitboxActive(false);
                yield break;
        }
    }

    private void ChooseCaptainCloseRangeAction(float dist)
    {
        if (!target) return;

        float comboRoll = comboChance + (_enraged ? enrageComboChanceBonus : 0f);

        if (Time.time >= _nextDefensiveAllowedTime && Random.value < defensiveModeChance)
        {
            SetState(State.Defensive);
            return;
        }

        if (Time.time >= _nextRepositionAllowedTime && Random.value < repositionChance)
        {
            SetState(State.Reposition);
            return;
        }

        if (Random.value < feintChance)
        {
            SetState(State.Feint);
            return;
        }

        if (Random.value < comboRoll)
        {
            SetState(State.ComboAttack);
            return;
        }

        if (Random.value < strafeChance)
        {
            SetState(State.Strafe);
            return;
        }

        SetState(State.Attack);
    }

    private IEnumerator PatrolLoop()
    {
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.acceleration = patrolAcceleration;
        agent.stoppingDistance = patrolStoppingDistance;

        float nextRepathCheck = 0f;

        while (_state == State.Patrol)
        {
            if (!agent.isOnNavMesh)
            {
                yield return null;
                continue;
            }

            if (!TryGetRandomPatrolDestination(out Vector3 dest))
            {
                agent.isStopped = true;
                yield return new WaitForSeconds(Random.Range(patrolStallTimeRange.x, patrolStallTimeRange.y));
                agent.isStopped = false;
                continue;
            }

            agent.isStopped = false;
            agent.SetDestination(dest);

            while (_state == State.Patrol && agent.pathPending)
                yield return null;

            if (_state != State.Patrol)
                yield break;

            if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
                continue;

            while (_state == State.Patrol)
            {
                if (Time.time >= nextRepathCheck)
                {
                    nextRepathCheck = Time.time + patrolRepathCheckInterval;

                    if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
                        break;

                    if (!HasCompletePathTo(agent.destination))
                        break;
                }

                float arriveDist = Mathf.Max(patrolArriveTolerance, agent.stoppingDistance + 0.05f);

                if (!agent.pathPending &&
                    agent.remainingDistance <= arriveDist &&
                    (!agent.hasPath || agent.velocity.sqrMagnitude < 0.02f))
                {
                    break;
                }

                yield return null;
            }

            if (_state != State.Patrol)
                yield break;

            agent.isStopped = true;
            yield return new WaitForSeconds(Random.Range(patrolStallTimeRange.x, patrolStallTimeRange.y));
            agent.isStopped = false;
        }
    }

    private IEnumerator InvestigateLoop()
    {
        if (!agent.isOnNavMesh)
        {
            yield return null;
            yield break;
        }

        agent.isStopped = false;
        agent.SetDestination(_lastKnownPos);

        float investigateTime = _enraged ? 3.0f : 2.0f;
        float timer = 0f;

        while (_state == State.Investigate)
        {
            float arriveDist = Mathf.Max(patrolArriveTolerance, agent.stoppingDistance + 0.05f);

            if (!agent.pathPending && agent.remainingDistance <= arriveDist)
            {
                agent.isStopped = true;
                timer += Time.deltaTime;

                if (timer >= investigateTime)
                {
                    SetState(startPatrolling ? State.Patrol : State.Idle);
                    yield break;
                }
            }
            else
            {
                agent.isStopped = false;
            }

            yield return null;
        }
    }

    private IEnumerator ChaseLoop()
    {
        if (!agent.isOnNavMesh)
        {
            yield return null;
            yield break;
        }

        agent.isStopped = false;

        while (_state == State.Chase)
        {
            if (!target)
            {
                SetState(startPatrolling ? State.Patrol : State.Idle);
                yield break;
            }

            float dist = Vector3.Distance(transform.position, target.position);

            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + chaseRepathInterval;

                Vector3 destination = GetChaseDestination();
                agent.SetDestination(destination);
            }

            if (dist <= attackRange)
            {
                agent.isStopped = true;
                ChooseCaptainCloseRangeAction(dist);
                yield break;
            }

            agent.isStopped = false;
            yield return null;
        }
    }

    private Vector3 GetChaseDestination()
    {
        if (!target) return transform.position;

        if (!usePredictiveChase)
            return target.position;

        Vector3 predictedOffset = _targetVelocity * predictionTime;
        predictedOffset = Vector3.ClampMagnitude(predictedOffset, maxPredictionDistance);

        Vector3 predictedPos = target.position + predictedOffset;

        if (NavMesh.SamplePosition(predictedPos, out NavMeshHit hit, 1.75f, agent.areaMask))
            return hit.position;

        return target.position;
    }

    private IEnumerator StrafeLoop()
    {
        if (!agent.isOnNavMesh)
        {
            yield return null;
            yield break;
        }

        if (!target)
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        float duration = Random.Range(strafeDurationRange.x, strafeDurationRange.y);
        float timer = 0f;
        float side = ChooseSmarterStrafeSide();

        agent.isStopped = false;

        while (_state == State.Strafe && timer < duration)
        {
            if (!target)
            {
                SetState(startPatrolling ? State.Patrol : State.Idle);
                yield break;
            }

            float dist = Vector3.Distance(transform.position, target.position);

            if (dist > attackRange * 1.35f)
            {
                SetState(State.Chase);
                yield break;
            }

            Vector3 toMe = transform.position - target.position;
            toMe.y = 0f;

            if (toMe.sqrMagnitude < 0.0001f)
                toMe = transform.right;

            Vector3 aroundDir = Quaternion.AngleAxis(70f * side, Vector3.up) * toMe.normalized;
            Vector3 desiredPos = target.position + aroundDir * Mathf.Clamp(dist, stopDistance, attackRange + 0.4f);

            if (NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, 1.25f, agent.areaMask))
                agent.SetDestination(hit.position);

            timer += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = true;

        if (!target)
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        if (Time.time >= _nextAttackAllowedTime)
        {
            if (Random.value < comboChance + (_enraged ? enrageComboChanceBonus : 0f))
                SetState(State.ComboAttack);
            else
                SetState(State.Attack);
        }
        else
        {
            SetState(State.Chase);
        }
    }

    private float ChooseSmarterStrafeSide()
    {
        if (!target)
            return Random.value < 0.5f ? -1f : 1f;

        Vector3 targetRight = target.right;
        Vector3 toCaptain = transform.position - target.position;
        toCaptain.y = 0f;

        if (toCaptain.sqrMagnitude < 0.001f)
            return Random.value < 0.5f ? -1f : 1f;

        float sideDot = Vector3.Dot(targetRight, toCaptain.normalized);

        if (Mathf.Abs(sideDot) < 0.2f)
            return Random.value < 0.5f ? -1f : 1f;

        return sideDot > 0f ? 1f : -1f;
    }

    private IEnumerator DefensiveLoop()
    {
        if (!agent.enabled)
            yield break;

        if (!agent.isOnNavMesh)
        {
            yield return null;
            yield break;
        }

        if (!target)
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        _nextDefensiveAllowedTime = Time.time + defensiveCooldown;

        agent.isStopped = false;
        agent.speed = defensiveSpeed;
        agent.acceleration = defensiveAcceleration;
        agent.stoppingDistance = defensiveStoppingDistance;

        float duration = Random.Range(defensiveDurationRange.x, defensiveDurationRange.y);
        float timer = 0f;

        while (_state == State.Defensive && timer < duration)
        {
            if (!target)
            {
                SetState(startPatrolling ? State.Patrol : State.Idle);
                yield break;
            }

            Vector3 away = transform.position - target.position;
            away.y = 0f;

            if (away.sqrMagnitude < 0.0001f)
                away = -transform.forward;

            away.Normalize();

            Vector3 candidate = transform.position + away * defensiveBackUpDistance;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 1.5f, agent.areaMask))
            {
                NavMeshPath path = new NavMeshPath();

                if (agent.CalculatePath(navHit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    agent.SetDestination(navHit.position);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = true;

        if (target && Vector3.Distance(transform.position, target.position) <= attackRange && Time.time >= _nextAttackAllowedTime)
            SetState(State.ComboAttack);
        else
            SetState(State.Chase);
    }

    private IEnumerator RepositionLoop()
    {
        if (!target)
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        _nextRepositionAllowedTime = Time.time + repositionCooldown;

        agent.isStopped = false;
        agent.speed = repositionSpeed;
        agent.acceleration = Mathf.Max(agent.acceleration, 16f);

        float side = Random.value < 0.5f ? -1f : 1f;
        float timer = 0f;

        while (_state == State.Reposition && timer < repositionDuration)
        {
            if (!target)
            {
                SetState(startPatrolling ? State.Patrol : State.Idle);
                yield break;
            }

            Vector3 away = transform.position - target.position;
            away.y = 0f;

            if (away.sqrMagnitude < 0.0001f)
                away = -transform.forward;

            Vector3 sideDir = Quaternion.AngleAxis(90f * side, Vector3.up) * away.normalized;
            Vector3 candidate = transform.position + sideDir * repositionDistance;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, agent.areaMask))
                agent.SetDestination(hit.position);

            timer += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = true;
        SetState(target ? State.Chase : startPatrolling ? State.Patrol : State.Idle);
    }

    private IEnumerator FeintLoop()
    {
        if (!target)
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        agent.isStopped = false;

        if (animator && !string.IsNullOrEmpty(animFeintTrigger))
            animator.SetTrigger(animFeintTrigger);

        if (swordAudioSource && feintClip)
            swordAudioSource.PlayOneShot(feintClip);

        Vector3 forwardStep = transform.position + transform.forward * feintForwardStepDistance;

        if (NavMesh.SamplePosition(forwardStep, out NavMeshHit hit, 1.0f, agent.areaMask))
            agent.SetDestination(hit.position);

        float timer = 0f;

        while (_state == State.Feint && timer < feintDuration)
        {
            FaceTargetOrMovement();
            timer += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = true;

        if (!target)
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        if (Random.value < 0.65f)
            SetState(State.ComboAttack);
        else
            SetState(State.Strafe);
    }

    private IEnumerator AttackOnce()
    {
        if (!target)
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        agent.isStopped = true;

        yield return FaceTargetFor(0.12f);

        bool heavy = Random.value < heavyAttackChance;
        TriggerAttackAnimation(heavy);

        yield return new WaitForSeconds(heavy ? heavyAttackWindup : attackWindup);

        yield return ActivateHitboxWindow();

        yield return new WaitForSeconds(attackRecovery);

        float cooldownMult = _enraged ? enrageAttackCooldownMultiplier : 1f;
        _nextAttackAllowedTime = Time.time + Random.Range(attackCooldownRange.x, attackCooldownRange.y) * cooldownMult;

        SetState(State.Recover);
    }

    private IEnumerator ComboAttack()
    {
        if (!target)
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        agent.isStopped = true;

        int comboHits = Random.Range(minComboHits, maxComboHits + 1);

        if (_enraged)
            comboHits = Mathf.Clamp(comboHits + 1, minComboHits, maxComboHits + 1);

        for (int i = 0; i < comboHits; i++)
        {
            if (!target)
                break;

            float dist = Vector3.Distance(transform.position, target.position);

            if (dist > attackRange * 1.35f)
                break;

            yield return FaceTargetFor(0.08f);

            bool heavy = i == comboHits - 1 && Random.value < heavyAttackChance + 0.2f;
            TriggerAttackAnimation(heavy);

            yield return new WaitForSeconds(heavy ? heavyAttackWindup : attackWindup);

            yield return ActivateHitboxWindow();

            if (i < comboHits - 1)
                yield return new WaitForSeconds(comboDelayBetweenHits);
        }

        yield return new WaitForSeconds(attackRecovery);

        float cooldownMult = _enraged ? enrageAttackCooldownMultiplier : 1f;
        _nextAttackAllowedTime = Time.time + Random.Range(comboCooldownRange.x, comboCooldownRange.y) * cooldownMult;

        SetState(State.Recover);
    }

    private void TriggerAttackAnimation(bool heavy)
    {
        if (!animator) return;

        animator.ResetTrigger(animLightAttackTrigger);
        animator.ResetTrigger(animHeavyAttackTrigger);
        animator.SetTrigger(heavy ? animHeavyAttackTrigger : animLightAttackTrigger);
    }

    private IEnumerator ActivateHitboxWindow()
    {
        SetSwordHitboxActive(true);
        yield return new WaitForSeconds(hitboxActiveTime);
        SetSwordHitboxActive(false);
    }

    private IEnumerator RecoverBriefly()
    {
        float recoverTime = _enraged ? 0.08f : 0.12f;

        while (_state == State.Recover && recoverTime > 0f)
        {
            recoverTime -= Time.deltaTime;
            yield return null;
        }

        SetState(target ? State.Chase : startPatrolling ? State.Patrol : State.Idle);
    }

    private IEnumerator StunnedBriefly()
    {
        agent.isStopped = true;
        SetSwordHitboxActive(false);

        if (_stunEndTime < Time.time)
            _stunEndTime = Time.time + Mathf.Max(0.05f, stunDuration);

        while (_state == State.Stunned && Time.time < _stunEndTime)
            yield return null;

        if (_state == State.Stunned)
            SetState(target ? State.Chase : startPatrolling ? State.Patrol : State.Idle);
    }

    private void FaceTargetOrMovement()
    {
        if (!agent) return;

        Vector3 lookDir = Vector3.zero;
        float currentTurnSpeed = turnSpeed;

        bool combatFacing =
            target &&
            (_state == State.Attack ||
             _state == State.ComboAttack ||
             _state == State.Feint ||
             _state == State.Strafe ||
             _state == State.Defensive ||
             _state == State.Reposition ||
             _state == State.Chase ||
             _state == State.Recover);

        // Special handling for strafe so the captain turns with its movement
        if (_state == State.Strafe && target)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            Vector3 moveDir = agent.desiredVelocity;
            moveDir.y = 0f;

            if (moveDir.sqrMagnitude < 0.01f)
            {
                moveDir = agent.velocity;
                moveDir.y = 0f;
            }

            if (moveDir.sqrMagnitude < 0.01f)
            {
                Vector3 toSteer = agent.steeringTarget - transform.position;
                toSteer.y = 0f;
                moveDir = toSteer;
            }

            if (turnBodyWithStrafe && moveDir.sqrMagnitude > 0.001f && toTarget.sqrMagnitude > 0.001f)
            {
                Vector3 targetDir = toTarget.normalized;
                Vector3 movementDir = moveDir.normalized;

                lookDir = Vector3.Slerp(targetDir, movementDir, strafeMovementFacingBias);
                currentTurnSpeed *= strafeTurnSpeedMultiplier;
            }
            else
            {
                lookDir = toTarget;
            }
        }
        else if (combatFacing)
        {
            lookDir = target.position - transform.position;
        }
        else
        {
            Vector3 toSteer = agent.steeringTarget - transform.position;
            toSteer.y = 0f;

            if (toSteer.sqrMagnitude > 0.0001f)
                lookDir = toSteer;
            else
                lookDir = agent.velocity;
        }

        lookDir.y = 0f;

        if (lookDir.sqrMagnitude < 0.0001f)
            return;

        Quaternion desired = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desired,
            1f - Mathf.Exp(-currentTurnSpeed * Time.deltaTime)
        );
    }

    private IEnumerator FaceTargetFor(float seconds)
    {
        float timer = 0f;

        while (timer < seconds)
        {
            timer += Time.deltaTime;
            FaceTargetOrMovement();
            yield return null;
        }
    }

    private void UpdateAnimatorLocomotion()
    {
        if (!animator || !agent) return;

        float worldSpeed = agent.velocity.magnitude;
        float speed01 = Mathf.Clamp01(worldSpeed / Mathf.Max(0.01f, animMaxMoveSpeed));

        animator.SetFloat(animSpeedParam, speed01, 0.12f, Time.deltaTime);
    }

    private void SetAnimatorAggro(bool aggro)
    {
        if (!animator || string.IsNullOrEmpty(animAggroBool)) return;
        animator.SetBool(animAggroBool, aggro);
    }

    private void SetBlocking(bool blocking)
    {
        if (blocking && Time.time < _blockBrokenUntil)
            blocking = false;

        if (animator && !string.IsNullOrEmpty(animIsBlockingBool))
            animator.SetBool(animIsBlockingBool, blocking);

        isBlocking = blocking;
    }

    private void UpdateCombatMoveTuning()
    {
        if (!agent || !target) return;

        bool engaging =
            _state == State.Chase ||
            _state == State.Strafe ||
            _state == State.Defensive ||
            _state == State.Reposition ||
            _state == State.Investigate;

        if (!engaging) return;

        float dist = Vector3.Distance(transform.position, target.position);

        float nearDist = Mathf.Min(slowDownStartDistance, slowDownEndDistance);
        float farDist = Mathf.Max(slowDownStartDistance, slowDownEndDistance);

        float norm = Mathf.InverseLerp(nearDist, farDist, dist);
        norm = Mathf.Clamp01(norm);
        norm *= norm;

        float desiredSpeed = Mathf.Lerp(chaseSpeedNear, chaseSpeedFar, norm);
        float desiredAccel = Mathf.Lerp(accelNear, accelFar, norm);

        if (_state == State.Defensive)
        {
            desiredSpeed = defensiveSpeed;
            desiredAccel = defensiveAcceleration;
        }

        if (_state == State.Reposition)
        {
            desiredSpeed = repositionSpeed;
            desiredAccel = Mathf.Max(desiredAccel, 16f);
        }

        if (_enraged)
        {
            desiredSpeed *= enrageSpeedMultiplier;
            desiredAccel *= enrageSpeedMultiplier;
        }

        if (isCursed)
        {
            desiredSpeed *= Mathf.Clamp(curseSpeedMult, 0.05f, 1f);
            desiredAccel *= Mathf.Clamp(curseSpeedMult, 0.05f, 1f);
        }

        agent.speed = Mathf.Lerp(agent.speed, desiredSpeed, 1f - Mathf.Exp(-speedBlend * Time.deltaTime));
        agent.acceleration = Mathf.Lerp(agent.acceleration, desiredAccel, 1f - Mathf.Exp(-speedBlend * Time.deltaTime));
    }

    private void TryEnrage()
    {
        if (!canEnrage || _enraged || _state == State.Dead)
            return;

        float healthPercent = _health / Mathf.Max(1f, maxHealth);

        if (healthPercent > enrageHealthPercent)
            return;

        _enraged = true;

        if (animator && !string.IsNullOrEmpty(animEnrageTrigger))
            animator.SetTrigger(animEnrageTrigger);

        if (growlSource && enrageClip)
            growlSource.PlayOneShot(enrageClip);

        _nextAttackAllowedTime = Mathf.Min(_nextAttackAllowedTime, Time.time + 0.25f);
    }

    public void ApplyDamage(float amount, bool canStun = true)
    {
        if (_state == State.Dead) return;

        if (!isBlocking)
        {
            amount *= Mathf.Max(1, curseDamageMult);
            _health -= amount;

            if (_health <= 0f)
            {
                Die();
                return;
            }

            TryEnrage();

            bool shouldIgnoreStun = _enraged && resistLightStunsWhileEnraged && amount < maxHealth * 0.08f;

            if (canStun && !shouldIgnoreStun)
            {
                if (animator && !string.IsNullOrEmpty(animHitTrigger))
                    animator.SetTrigger(animHitTrigger);

                _stunEndTime = Time.time + Mathf.Max(0.05f, stunDuration);
                SetState(State.Stunned);
            }
            else if (target && Random.value < blockChanceAfterHit && Time.time >= _nextDefensiveAllowedTime)
            {
                StartCoroutine(BlockAfterHitBriefly());
            }
        }
        else
        {
            if (swordAudioSource != null && swordBlockClips != null && swordBlockClips.Length > 0)
                swordAudioSource.PlayOneShot(swordBlockClips[Random.Range(0, swordBlockClips.Length)]);

            if (target && Time.time >= _nextAttackAllowedTime && Random.value < 0.35f)
                SetState(State.ComboAttack);
        }
    }

    private IEnumerator BlockAfterHitBriefly()
    {
        SetBlocking(true);

        float timer = 0f;

        while (timer < blockAfterHitDuration && _state != State.Dead && _state != State.Stunned)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        SetBlocking(false);
    }

    public void BreakBlock()
    {
        if (_state == State.Dead) return;

        _blockBrokenUntil = Time.time + guardBreakNoBlockTime;

        SetBlocking(false);
        SetSwordHitboxActive(false);

        if (animator)
        {
            if (!string.IsNullOrEmpty(animGuardBreakTrigger))
                animator.SetTrigger(animGuardBreakTrigger);
            else if (!string.IsNullOrEmpty(animHitTrigger))
                animator.SetTrigger(animHitTrigger);
        }

        _stunEndTime = Time.time + Mathf.Max(0.05f, guardBreakStunTime);
        SetState(State.Stunned);
    }

    private void Die()
    {
        if (_state == State.Dead) return;

        _health = 0f;
        SetState(State.Dead);

        UpdateCombatRegistration(false);

        if (headLookConstraint != null)
            headLookConstraint.weight = 0f;

        SetBlocking(false);
        SetSwordHitboxActive(false);

        if (agent)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        RayfireRigid rayfireRigid = GetComponentInChildren<RayfireRigid>();

        if (rayfireRigid != null)
            rayfireRigid.Demolish();

        Destroy(gameObject);
    }

    private bool TryGetRandomPatrolDestination(out Vector3 destination)
    {
        destination = _spawnPos;

        if (!agent || !agent.enabled || !agent.isOnNavMesh)
            return false;

        float radius = patrolRadius + (patrolRadiusJitter > 0f ? Random.Range(-patrolRadiusJitter, patrolRadiusJitter) : 0f);
        radius = Mathf.Max(0.1f, radius);

        for (int i = 0; i < Mathf.Max(1, patrolFindMaxAttempts); i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 candidate = _spawnPos + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 2.0f, agent.areaMask))
                continue;

            NavMeshPath path = new NavMeshPath();

            if (!agent.CalculatePath(navHit.position, path))
                continue;

            if (path.status != NavMeshPathStatus.PathComplete)
                continue;

            destination = navHit.position;
            return true;
        }

        return false;
    }

    private bool HasCompletePathTo(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();

        if (!agent.CalculatePath(destination, path))
            return false;

        return path.status == NavMeshPathStatus.PathComplete;
    }

    private void PlayGrowlOnce()
    {
        if (growlClip && growlSource && !_hasGrowled)
        {
            growlSource.PlayOneShot(growlClip);
            _hasGrowled = true;
        }
    }

    private void CacheSwordHitColliders()
    {
        if (!swordHitbox)
            return;

        _swordHitColliders = swordHitbox.GetComponentsInChildren<Collider>(true);
    }

    private void SetSwordHitboxActive(bool active)
    {
        if (!controlHitboxFromScript)
            return;

        if (_swordHitColliders == null || _swordHitColliders.Length == 0)
            CacheSwordHitColliders();

        if (_swordHitColliders == null)
            return;

        for (int i = 0; i < _swordHitColliders.Length; i++)
        {
            if (_swordHitColliders[i] != null)
                _swordHitColliders[i].enabled = active;
        }
    }

    public float GetHealth()
    {
        return _health;
    }

    public bool IsEnraged()
    {
        return _enraged;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rushReactDistance);

        if (eyePoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(eyePoint.position, visionRange);
        }
    }
}