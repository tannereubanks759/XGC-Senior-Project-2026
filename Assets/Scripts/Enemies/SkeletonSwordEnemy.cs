using RayFire;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class SkeletonSwordEnemy : MonoBehaviour
{
    [Header("Curse")]
    public bool isCursed = false;
    public int curseDamageMult = 1;
    public float curseSpeedMult = 1f;
    public bool curseReflectEnabled = false;
    [Range(0f, 1f)] public float curseReflectPercent = 0.25f;
    public enum State
    {
        Idle,
        Patrol,
        Investigate,
        Chase,
        Strafe,
        Defensive,
        Attack,
        Recover,
        Stunned,
        Dead
    }

    [Header("References")]
    [Tooltip("Animator with locomotion + attack animations.")]
    public Animator animator;

    [Tooltip("Optional. If null, will use GetComponent<NavMeshAgent>().")]
    public NavMeshAgent agent;

    [Tooltip("Where the enemy considers 'eyes' to be for vision checks.")]
    public Transform eyePoint;

    [Tooltip("Optional. If not set, the enemy will find the player after they spawn.")]
    public Transform target;

    [Tooltip("Sword hitbox component (child object) that has a trigger collider.")]
    public SwordHitbox swordHitbox;

    [Header("Player Auto-Find")]
    [Tooltip("Tag on the player root GameObject (recommended).")]
    public string playerTag = "Player";

    [Tooltip("How often (seconds) we try to find the player if target is missing.")]
    public float findTargetInterval = 0.5f;

    [Tooltip("If true, prefer using Camera.main as the look target when available.")]
    public bool preferMainCameraForLook = true;

    [Header("Head Look (LookAtConstraint)")]
    [Tooltip("LookAtConstraint on the head (or head rig).")]
    public LookAtConstraint headLookConstraint;

    [Tooltip("Optional: player's head/camera. Auto-assigned when player is found.")]
    public Transform targetHead;

    [Tooltip("Max distance for head look.")]
    public float headLookRange = 12f;

    [Range(10f, 180f)]
    [Tooltip("Head look only if enemy forward is within this yaw angle to the target.")]
    public float headLookMaxAngle = 85f;

    [Tooltip("How fast the constraint weight blends in/out.")]
    public float headLookBlendSpeed = 8f;

    [Range(0f, 1f)]
    [Tooltip("Maximum weight the LookAtConstraint is allowed to reach (acts like strength).")]
    public float headLookMaxWeight = 1f;

    [Header("Patrol (Random Wander)")]
    public bool startPatrolling = true;

    [Tooltip("How far from spawn point the skeleton is allowed to patrol.")]
    public float patrolRadius = 12f;

    [Tooltip("How close we need to be to consider we've reached the patrol destination.")]
    public float patrolArriveTolerance = 1.1f;

    [Tooltip("How long to stall at a patrol point (seconds).")]
    public Vector2 patrolStallTimeRange = new Vector2(0.5f, 2.0f);

    [Tooltip("How often we check whether the current patrol path is still valid.")]
    public float patrolRepathCheckInterval = 0.35f;

    [Tooltip("How many attempts to find a valid random patrol destination before giving up briefly.")]
    public int patrolFindMaxAttempts = 12;

    [Tooltip("Extra randomness on top of patrolRadius (0 = none).")]
    public float patrolRadiusJitter = 0.0f;

    [Tooltip("NavMeshAgent speed while patrolling.")]
    public float patrolSpeed = 2.0f;

    [Tooltip("NavMeshAgent acceleration while patrolling.")]
    public float patrolAcceleration = 8.0f;

    [Tooltip("How far we stop from patrol destinations.")]
    public float patrolStoppingDistance = 0.4f;

    [Header("Perception")]
    [Tooltip("How far the skeleton can see the player.")]
    public float visionRange = 20f;

    [Range(10f, 180f)]
    [Tooltip("Field of view angle (degrees).")]
    public float visionFov = 120f;

    [Tooltip("Within this range we ignore FOV (still requires line of sight).")]
    public float closeAwarenessRange = 8.0f;

    [Tooltip("If within this range, the skeleton can detect even if not facing (still LOS).")]
    public float autoDetectRange = 3.0f;

    [Tooltip("How thick the vision cast is. Helps prevent tiny gaps/edges breaking vision.")]
    public float sightThickness = 0.12f;

    [Tooltip("Eye height if eyePoint is null.")]
    public float fallbackEyeHeight = 1.6f;

    [Tooltip("Where on the target we aim the sight check (chest/head).")]
    public float targetAimHeight = 1.4f;

    [Tooltip("Only these layers can block vision (WALLS/LEVEL). EXCLUDE Player/Enemy.")]
    public LayerMask occlusionMask;

    [Tooltip("Optional: draw vision debug rays.")]
    public bool debugVision;

    [Tooltip("How far the skeleton can 'hear' (if you want to call OnHeardNoise).")]
    public float hearingRange = 12f;

    [Tooltip("Seconds the skeleton keeps chasing after losing line of sight.")]
    public float aggroMemoryTime = 4.0f;

    [Header("Combat")]
    [Tooltip("Preferred melee distance to start attacks.")]
    public float attackRange = 2.15f;

    [Tooltip("Extra distance to stop the agent when engaging.")]
    public float stopDistance = 1.9f;

    [Tooltip("How quickly the body rotates toward facing direction.")]
    public float turnSpeed = 10f;

    [Tooltip("How long we spend circling/strafing before attempting an attack.")]
    public Vector2 strafeDurationRange = new Vector2(0.4f, 1.0f);

    [Range(0f, 1f)]
    [Tooltip("Chance to strafe instead of immediate attack when in range.")]
    public float strafeChance = 0.65f;

    [Range(0f, 1f)]
    [Tooltip("Chance to raise block during a strafe.")]
    public float blockChanceWhileStrafing = 0.35f;

    [Tooltip("Cooldown between attacks.")]
    public Vector2 attackCooldownRange = new Vector2(0.6f, 1.25f);

    [Range(0f, 1f)]
    [Tooltip("Chance a swing is heavy instead of light.")]
    public float heavyAttackChance = 0.25f;

    [Tooltip("Attack windup time before the hitbox turns on (telegraph).")]
    public float attackWindup = 0.18f;

    [Tooltip("How long hitbox stays active during a swing.")]
    public float hitboxActiveTime = 0.22f;

    [Tooltip("How long we wait after a swing (recovery).")]
    public float attackRecovery = 0.25f;

    [Header("Defense / Blocking")]
    [Tooltip("Animator Boolean for your blocking layer.")]
    public string animIsBlockingBool = "IsBlocking";
    public bool isBlocking = false;

    [Header("Guard Break")]
    [Tooltip("How long the skeleton is stunned when its block is broken.")]
    public float guardBreakStunTime = 1.0f;

    [Tooltip("How long after a guard break the skeleton is not allowed to block.")]
    public float guardBreakNoBlockTime = 1.25f;

    [Tooltip("Optional: trigger name to play a guard break reaction. If empty, uses animHitTrigger.")]
    public string animGuardBreakTrigger = "";


    [Range(0f, 1f)]
    [Tooltip("Chance to enter defensive mode instead of strafing/attacking when close.")]
    public float defensiveModeChance = 0.25f;

    [Tooltip("How long defensive mode lasts (seconds).")]
    public Vector2 defensiveDurationRange = new Vector2(0.6f, 1.4f);

    [Tooltip("How far to back up from current position during defensive mode.")]
    public float defensiveBackUpDistance = 2.0f;

    [Tooltip("NavMeshAgent speed while backing up defensively.")]
    public float defensiveSpeed = 2.2f;

    [Tooltip("NavMeshAgent acceleration while backing up defensively.")]
    public float defensiveAcceleration = 10f;

    [Tooltip("How close we stop from the defensive destination.")]
    public float defensiveStoppingDistance = 0.2f;

    [Tooltip("Minimum time between defensive activations.")]
    public float defensiveCooldown = 1.2f;

    [Header("React To Player Rush")]
    [Tooltip("Distance within which we consider reacting to a player rushing at us.")]
    public float rushReactDistance = 5.0f;

    [Tooltip("If player closing speed toward enemy exceeds this, they are 'rushing'.")]
    public float rushClosingSpeed = 3.0f;

    [Range(0f, 1f)]
    [Tooltip("Chance to back up defensively when player rushes.")]
    public float rushBackUpChance = 0.65f;

    [Header("Combat Movement Tuning")]
    [Tooltip("Agent speed when far away (chasing).")]
    public float chaseSpeedFar = 4.0f;

    [Tooltip("Agent speed when close to the player (near melee).")]
    public float chaseSpeedNear = 1.6f;

    [Tooltip("Distance where we start slowing down.")]
    public float slowDownStartDistance = 6.0f;

    [Tooltip("Distance where we are at 'near' speed (usually around attack range).")]
    public float slowDownEndDistance = 2.2f;

    [Tooltip("How quickly speed blends (higher = snappier).")]
    public float speedBlend = 8.0f;

    [Tooltip("Optional: also scale acceleration to reduce twitchiness near player.")]
    public float accelFar = 18f;

    [Tooltip("Optional: also scale acceleration to reduce twitchiness near player.")]
    public float accelNear = 8f;

    [Header("Locomotion Animation")]
    [Tooltip("World speed (m/s) that corresponds to full run in your blend tree.")]
    public float animMaxMoveSpeed = 4.0f;

    [Header("Damage / Stun")]
    public float maxHealth = 100f;
    public float stunDuration = 0.6f;

    [Header("Animation Params")]
    [Tooltip("Animator float param for movement speed (0..1).")]
    public string animSpeedParam = "Speed";

    [Tooltip("Animator bool param for having target / aggro.")]
    public string animAggroBool = "Aggro";

    [Tooltip("Animator trigger for light attack.")]
    public string animLightAttackTrigger = "LightAttack";

    [Tooltip("Animator trigger for heavy attack.")]
    public string animHeavyAttackTrigger = "HeavyAttack";

    [Tooltip("Animator trigger for getting hit/stunned.")]
    public string animHitTrigger = "Hit";

    [Header("Tuning")]
    [Tooltip("How often (seconds) we refresh detection checks to save CPU.")]
    public float senseInterval = 0.12f;

    [Tooltip("How often we update destination while chasing.")]
    public float chaseRepathInterval = 0.20f;

    [Range(0f, 1f)]
    [Tooltip("0 = no staggering, 1 = full spreading across the whole interval.")]
    public float staggerStrength = 1f;

    [Header("Sounds")]
    public AudioSource SwordAudioSource;
    public AudioClip[] swordBlockClips;


    private State _state;
    private float _health;

    private float _lastSeenTime = -999f;
    private Vector3 _lastKnownPos;

    private float _nextSenseTime;
    private float _nextRepathTime;
    private float _nextFindTargetTime;

    private float _nextAttackAllowedTime;
    private float _nextDefensiveAllowedTime;

    private Coroutine _stateRoutine;
    private Vector3 _spawnPos;

    private float _sensePhase;
    private float _repathPhase;

    private float _headLookW;
    private readonly RaycastHit[] _sightHits = new RaycastHit[24];

    private Vector3 _lastTargetPos;
    private Vector3 _targetVelocity;
    private bool _hasLastTargetPos;

    private bool _blockThisStrafe;
    private float _blockBrokenUntil = -999f;
    private float _stunEndTime = -999f;



    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        eyePoint = transform;
    }

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        _health = maxHealth;

        agent.updateRotation = false;
        agent.stoppingDistance = stopDistance;

        _spawnPos = transform.position;

        int id = Mathf.Abs(GetInstanceID());
        _sensePhase = ((id * 0.6180339f) % 1f);
        _repathPhase = ((id * 0.3819660f) % 1f);

        float senseOffset = senseInterval * Mathf.Lerp(0f, _sensePhase, staggerStrength);
        float repathOffset = chaseRepathInterval * Mathf.Lerp(0f, _repathPhase, staggerStrength);

        _nextSenseTime = Time.time + senseOffset;
        _nextRepathTime = Time.time + repathOffset;
        _nextFindTargetTime = Time.time + (findTargetInterval * Mathf.Lerp(0f, _sensePhase, staggerStrength));

        if (headLookConstraint != null)
            headLookConstraint.weight = 0f;

        SetBlocking(false);
    }

    private void Start()
    {
        SetState(startPatrolling ? State.Patrol : State.Idle);
    }

    private void Update()
    {
        if (!agent.enabled && !agent.isOnNavMesh) return;
        if (_state == State.Dead) return;

        AcquireTargetIfNeeded();
        UpdateTargetVelocityEstimate();

        if (Time.time >= _nextSenseTime)
        {
            _nextSenseTime = Time.time + senseInterval;
            Sense();
        }

        UpdateCombatMoveTuning();
        UpdateAnimatorLocomotion();
        UpdateHeadLookConstraint();

        if (_state == State.Patrol || _state == State.Investigate || _state == State.Chase || _state == State.Strafe || _state == State.Defensive || _state == State.Attack || _state == State.Recover)
            FaceTargetOrMovement();

        if ((_state == State.Chase || _state == State.Recover) && target && Time.time >= _nextAttackAllowedTime)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= attackRange && Time.time >= _nextDefensiveAllowedTime)
            {
                if (ShouldEnterDefensive(dist))
                    SetState(State.Defensive);
                else
                    SetState(State.Attack);
            }
        }

        if (_state == State.Chase && target && Time.time >= _nextDefensiveAllowedTime)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= rushReactDistance && IsTargetRushingMe() && Random.value < rushBackUpChance)
                SetState(State.Defensive);
        }
    }

    private void AcquireTargetIfNeeded()
    {
        if (target != null && targetHead != null) return;
        if (Time.time < _nextFindTargetTime) return;

        _nextFindTargetTime = Time.time + findTargetInterval;

        Transform foundPlayer = target;

        if (foundPlayer == null && !string.IsNullOrEmpty(playerTag))
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) foundPlayer = go.transform;
        }

        Transform foundCam = null;
        if (preferMainCameraForLook && Camera.main != null)
            foundCam = Camera.main.transform;

        if (foundPlayer == null && foundCam != null)
            foundPlayer = foundCam.root;

        if (foundPlayer == null) return;

        target = foundPlayer;

        if (foundCam == null)
        {
            var cam = target.GetComponentInChildren<Camera>(includeInactive: true);
            if (cam != null) foundCam = cam.transform;
        }

        if (foundCam != null)
        {
            targetHead = foundCam;
            SetLookAtConstraintSource(foundCam);
        }
    }

    private void SetLookAtConstraintSource(Transform lookTarget)
    {
        if (headLookConstraint == null || lookTarget == null) return;

        if (headLookConstraint.sourceCount == 1)
        {
            var s = headLookConstraint.GetSource(0);
            if (s.sourceTransform == lookTarget) return;
        }

        var list = new System.Collections.Generic.List<ConstraintSource>(1)
        {
            new ConstraintSource { sourceTransform = lookTarget, weight = 1f }
        };

        headLookConstraint.SetSources(list);
        headLookConstraint.constraintActive = true;
        headLookConstraint.locked = true;
    }

    private void UpdateHeadLookConstraint()
    {
        if (headLookConstraint == null) return;

        float desired = 0f;

        bool engaged =
            _state == State.Chase ||
            _state == State.Strafe ||
            _state == State.Defensive ||
            _state == State.Attack ||
            _state == State.Recover;

        if (engaged && target != null)
        {
            Vector3 lookPos = (targetHead != null) ? targetHead.position : (target.position + Vector3.up * targetAimHeight);
            Vector3 to = lookPos - transform.position;
            float dist = to.magnitude;

            if (dist > 0.001f && dist <= headLookRange)
            {
                Vector3 toFlat = to; toFlat.y = 0f;
                Vector3 fwdFlat = transform.forward; fwdFlat.y = 0f;

                if (toFlat.sqrMagnitude > 0.0001f && fwdFlat.sqrMagnitude > 0.0001f)
                {
                    float yawAngle = Vector3.Angle(fwdFlat.normalized, toFlat.normalized);
                    if (yawAngle <= headLookMaxAngle) desired = 1f;
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

        Vector3 toEnemy = (transform.position - target.position);
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude < 0.0001f) return false;

        Vector3 dir = toEnemy.normalized;
        float closing = Vector3.Dot(_targetVelocity, dir);
        return closing >= rushClosingSpeed;
    }

    private bool ShouldEnterDefensive(float dist)
    {
        if (dist > attackRange) return false;
        return Random.value < defensiveModeChance;
    }

    private void Sense()
    {
        if (!target) return;

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
            bool hasMemory = (Time.time - _lastSeenTime) <= aggroMemoryTime;

            if (!hasMemory && (_state == State.Chase || _state == State.Strafe || _state == State.Defensive))
            {
                if (_lastSeenTime > -998f) SetState(State.Investigate);
                else SetState(startPatrolling ? State.Patrol : State.Idle);
            }
        }
    }

    private bool CanSeeTarget(out Vector3 seenPos)
    {
        seenPos = Vector3.zero;
        if (!target) return false;

        Vector3 origin = eyePoint ? eyePoint.position : (transform.position + Vector3.up * fallbackEyeHeight);
        origin += transform.up * 0.05f + transform.forward * 0.12f;

        Vector3 aim = (targetHead != null) ? targetHead.position : (target.position + Vector3.up * targetAimHeight);

        Vector3 to = aim - origin;
        float dist = to.magnitude;
        if (dist <= 0.001f) return true;
        if (dist > visionRange) return false;

        bool inFov = true;
        if (dist > closeAwarenessRange && dist > autoDetectRange)
        {
            Vector3 toFlat = to; toFlat.y = 0f;
            Vector3 fwdFlat = transform.forward; fwdFlat.y = 0f;

            if (toFlat.sqrMagnitude > 0.0001f && fwdFlat.sqrMagnitude > 0.0001f)
            {
                float ang = Vector3.Angle(fwdFlat.normalized, toFlat.normalized);
                inFov = ang <= (visionFov * 0.5f);
            }
        }

        if (!inFov) return false;

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
            var h = _sightHits[i];
            var c = h.collider;
            if (!c) continue;

            if (c.transform.IsChildOf(transform)) continue;

            blocked = true;
            if (h.distance < closest)
            {
                closest = h.distance;
                closestBlock = h;
            }
        }

        if (debugVision)
        {
            Debug.DrawLine(origin, aim, blocked ? Color.red : Color.green, senseInterval);
            if (blocked) Debug.DrawRay(closestBlock.point, closestBlock.normal * 0.25f, Color.yellow, senseInterval);
        }

        if (blocked) return false;

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
        {
            agent.stoppingDistance = (newState == State.Patrol) ? patrolStoppingDistance : stopDistance;
        }

        if (_stateRoutine != null)
            StopCoroutine(_stateRoutine);

        _stateRoutine = StartCoroutine(RunState(_state));
    }

    private IEnumerator RunState(State s)
    {
        SetAnimatorAggro(s == State.Chase || s == State.Strafe || s == State.Defensive || s == State.Attack || s == State.Recover);

        switch (s)
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

            case State.Attack:
                SetBlocking(false);
                yield return AttackOnce();
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
                yield break;
        }
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

            if (_state != State.Patrol) yield break;
            if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete) continue;

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

            if (_state != State.Patrol) yield break;

            agent.isStopped = true;
            yield return new WaitForSeconds(Random.Range(patrolStallTimeRange.x, patrolStallTimeRange.y));
            agent.isStopped = false;
        }
    }

    private IEnumerator InvestigateLoop()
    {
        agent.isStopped = false;
        agent.SetDestination(_lastKnownPos);

        float investigateTime = 2.0f;
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
                agent.SetDestination(target.position);
            }

            if (dist <= attackRange)
            {
                agent.isStopped = true;

                if (Time.time < _nextAttackAllowedTime)
                {
                    if (Time.time >= _nextDefensiveAllowedTime && ShouldEnterDefensive(dist))
                    {
                        SetState(State.Defensive);
                        yield break;
                    }

                    SetState(State.Strafe);
                    yield break;
                }

                if (Time.time >= _nextDefensiveAllowedTime && Random.value < defensiveModeChance)
                {
                    SetState(State.Defensive);
                    yield break;
                }

                if (Random.value < strafeChance)
                {
                    SetState(State.Strafe);
                    yield break;
                }

                SetState(State.Attack);
                yield break;
            }

            agent.isStopped = false;
            yield return null;
        }
    }

    private IEnumerator StrafeLoop()
    {
        if (!target)
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        float duration = Random.Range(strafeDurationRange.x, strafeDurationRange.y);
        float timer = 0f;

        float side = Random.value < 0.5f ? -1f : 1f;

        agent.isStopped = false;

        while (_state == State.Strafe && timer < duration)
        {
            if (!target)
            {
                SetState(startPatrolling ? State.Patrol : State.Idle);
                yield break;
            }

            float dist = Vector3.Distance(transform.position, target.position);

            if (dist > attackRange * 1.25f)
            {
                SetState(State.Chase);
                yield break;
            }

            Vector3 toMe = (transform.position - target.position);
            toMe.y = 0f;
            if (toMe.sqrMagnitude < 0.0001f) toMe = transform.right;

            Vector3 aroundDir = Quaternion.AngleAxis(65f * side, Vector3.up) * toMe.normalized;
            Vector3 desiredPos = target.position + aroundDir * Mathf.Clamp(dist, stopDistance, attackRange);

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(desiredPos);
            }
            

            timer += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = true;

        if (target && Time.time >= _nextAttackAllowedTime)
            SetState(State.Attack);
        else
            SetState(State.Chase);
    }

    private IEnumerator DefensiveLoop()
    {
        if (!agent.enabled) yield break;
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

            Vector3 away = (transform.position - target.position);
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
            away.Normalize();

            Vector3 candidate = transform.position + away * defensiveBackUpDistance;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 1.5f, agent.areaMask))
            {
                var path = new NavMeshPath();
                if (agent.isOnNavMesh && agent.CalculatePath(navHit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    agent.SetDestination(navHit.position);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = true;
        SetState(State.Chase);
    }

    private IEnumerator AttackOnce()
    {
        if (!target)
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        agent.isStopped = true;

        yield return FaceTargetFor(0.10f);

        bool heavy = Random.value < heavyAttackChance;

        if (animator)
        {
            animator.ResetTrigger(animLightAttackTrigger);
            animator.ResetTrigger(animHeavyAttackTrigger);
            animator.SetTrigger(heavy ? animHeavyAttackTrigger : animLightAttackTrigger);
        }

        yield return new WaitForSeconds(attackWindup);

        if (swordHitbox != null)
        {
            yield return new WaitForSeconds(hitboxActiveTime);
        }
        else
        {
            yield return new WaitForSeconds(hitboxActiveTime);
        }

        yield return new WaitForSeconds(attackRecovery);

        _nextAttackAllowedTime = Time.time + Random.Range(attackCooldownRange.x, attackCooldownRange.y);

        SetState(State.Recover);
    }

    private IEnumerator RecoverBriefly()
    {
        float t = 0.12f;
        while (_state == State.Recover && t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        SetState(target ? State.Chase : (startPatrolling ? State.Patrol : State.Idle));
    }

    private IEnumerator StunnedBriefly()
    {
        agent.isStopped = true;

        // Fallback if something didn't set it
        if (_stunEndTime < Time.time)
            _stunEndTime = Time.time + Mathf.Max(0.05f, stunDuration);

        while (_state == State.Stunned && Time.time < _stunEndTime)
            yield return null;

        if (_state == State.Stunned)
            SetState(target ? State.Chase : (startPatrolling ? State.Patrol : State.Idle));
    }


    private void FaceTargetOrMovement()
    {
        if (!agent) return;

        Vector3 lookDir = Vector3.zero;

        bool combatFacing = target && (_state == State.Attack || _state == State.Strafe || _state == State.Defensive || _state == State.Chase || _state == State.Recover);

        if (combatFacing)
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
        if (lookDir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    private IEnumerator FaceTargetFor(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            FaceTargetOrMovement();
            yield return null;
        }
    }

    private void UpdateAnimatorLocomotion()
    {
        if (!animator || !agent) return;

        float worldSpeed = agent.velocity.magnitude;
        float speed01 = worldSpeed / Mathf.Max(0.01f, animMaxMoveSpeed);
        speed01 = Mathf.Clamp01(speed01);

        animator.SetFloat(animSpeedParam, speed01, 0.12f, Time.deltaTime);
    }

    private void SetAnimatorAggro(bool aggro)
    {
        if (!animator || string.IsNullOrEmpty(animAggroBool)) return;
        animator.SetBool(animAggroBool, aggro);
    }

    private void SetBlocking(bool blocking)
    {
        if (!animator || string.IsNullOrEmpty(animIsBlockingBool)) return;

        // If block is broken, ignore attempts to enable blocking until lockout expires
        if (blocking && Time.time < _blockBrokenUntil)
            blocking = false;

        animator.SetBool(animIsBlockingBool, blocking);
        isBlocking = blocking;
    }


    private void UpdateCombatMoveTuning()
    {
        if (!agent || !target) return;

        bool engaging = _state == State.Chase || _state == State.Strafe || _state == State.Defensive || _state == State.Investigate;
        if (!engaging) return;

        float dist = Vector3.Distance(transform.position, target.position);

        float nearDist = Mathf.Min(slowDownStartDistance, slowDownEndDistance);
        float farDist = Mathf.Max(slowDownStartDistance, slowDownEndDistance);

        float norm = Mathf.InverseLerp(nearDist, farDist, dist);
        norm = Mathf.Clamp01(norm);
        norm = norm * norm;

        float desiredSpeed = Mathf.Lerp(chaseSpeedNear, chaseSpeedFar, norm);
        float desiredAccel = Mathf.Lerp(accelNear, accelFar, norm);

        if (_state == State.Defensive)
        {
            desiredSpeed = defensiveSpeed;
            desiredAccel = defensiveAcceleration;
        }
        if (isCursed)
        {
            desiredSpeed *= Mathf.Clamp(curseSpeedMult, 0.05f, 1f);
            desiredAccel *= Mathf.Clamp(curseSpeedMult, 0.05f, 1f);
        }
        agent.speed = Mathf.Lerp(agent.speed, desiredSpeed, 1f - Mathf.Exp(-speedBlend * Time.deltaTime));
        agent.acceleration = Mathf.Lerp(agent.acceleration, desiredAccel, 1f - Mathf.Exp(-speedBlend * Time.deltaTime));
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

            if (canStun)
            {
                if (animator && !string.IsNullOrEmpty(animHitTrigger))
                    animator.SetTrigger(animHitTrigger);

                _stunEndTime = Time.time + Mathf.Max(0.05f, stunDuration);
                SetState(State.Stunned);
            }

        }
        else
        {
            SwordAudioSource.PlayOneShot(swordBlockClips[Random.Range(0, swordBlockClips.Length)]);
        }
       
    }
    private void Die()
    {
        if (_state == State.Dead) return;

        _health = 0f;
        SetState(State.Dead);

        if (headLookConstraint != null)
            headLookConstraint.weight = 0f;

        SetBlocking(false);

        agent.isStopped = true;
        agent.enabled = false;

        var rf = GetComponentInChildren<RayfireRigid>();
        if (rf != null) rf.Demolish();

        Destroy(gameObject);
    }

    private bool TryGetRandomPatrolDestination(out Vector3 destination)
    {
        destination = _spawnPos;

        float radius = patrolRadius + (patrolRadiusJitter > 0f ? Random.Range(-patrolRadiusJitter, patrolRadiusJitter) : 0f);
        radius = Mathf.Max(0.1f, radius);

        for (int i = 0; i < Mathf.Max(1, patrolFindMaxAttempts); i++)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            Vector3 candidate = _spawnPos + new Vector3(r.x, 0f, r.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 2.0f, agent.areaMask))
                continue;

            var path = new NavMeshPath();
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
        var path = new NavMeshPath();
        if (!agent.CalculatePath(destination, path)) return false;
        return path.status == NavMeshPathStatus.PathComplete;
    }

    public float GetHealth() => _health;


    public void BreakBlock()
    {
        if (_state == State.Dead) return;

        // Even if not blocking, you can still use this to force a stun/lockout if you want
        _blockBrokenUntil = Time.time + guardBreakNoBlockTime;

        // Drop block immediately
        SetBlocking(false);

        // Optional guard-break animation
        if (animator)
        {
            if (!string.IsNullOrEmpty(animGuardBreakTrigger))
                animator.SetTrigger(animGuardBreakTrigger);
            else if (!string.IsNullOrEmpty(animHitTrigger))
                animator.SetTrigger(animHitTrigger);
        }

        // Force a longer stun than normal
        _stunEndTime = Time.time + Mathf.Max(0.05f, guardBreakStunTime);
        SetState(State.Stunned);
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (eyePoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(eyePoint.position, visionRange);
        }
    }
}
