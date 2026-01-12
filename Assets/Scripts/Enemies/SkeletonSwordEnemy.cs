using RayFire;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations; // LookAtConstraint + ConstraintSource

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class SkeletonSwordEnemy : MonoBehaviour
{
    public enum State
    {
        Idle,
        Patrol,
        Investigate,
        Chase,
        Strafe,
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

    // -------------------------
    // Player acquisition
    // -------------------------
    [Header("Player Auto-Find")]
    [Tooltip("Tag on the player root GameObject (recommended).")]
    public string playerTag = "Player";

    [Tooltip("How often (seconds) we try to find the player if target is missing.")]
    public float findTargetInterval = 0.5f;

    [Tooltip("If true, prefer using Camera.main as the look target when available.")]
    public bool preferMainCameraForLook = true;

    // -------------------------
    // Head Look (LookAtConstraint)
    // -------------------------
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

    // -------------------------
    // Patrol
    // -------------------------
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

    [Tooltip("How far we stop from patrol destinations (should be <= patrolArriveTolerance usually).")]
    public float patrolStoppingDistance = 0.4f;

    // -------------------------
    // Perception
    // -------------------------
    [Header("Perception (Reworked)")]
    public float visionRange = 20f;

    [Range(10f, 180f)]
    public float visionFov = 120f;

    public float closeAwarenessRange = 8.0f;
    public float autoDetectRange = 3.0f;

    public float sightThickness = 0.12f;
    public float fallbackEyeHeight = 1.6f;
    public float targetAimHeight = 1.4f;

    [Tooltip("Only these layers can block vision (WALLS/LEVEL). EXCLUDE Player/Enemy.")]
    public LayerMask occlusionMask;

    public bool debugVision;

    public float hearingRange = 12f;
    public float aggroMemoryTime = 4.0f;

    // -------------------------
    // Combat
    // -------------------------
    [Header("Combat")]
    public float attackRange = 2.15f;
    public float stopDistance = 1.9f;
    public float turnSpeed = 10f;

    [Header("Locomotion Animation")]
    [Tooltip("World speed (m/s) that corresponds to full run in your blend tree.")]
    public float animMaxMoveSpeed = 4.0f;

    public Vector2 strafeDurationRange = new Vector2(0.4f, 1.0f);
    [Range(0f, 1f)] public float strafeChance = 0.65f;

    public Vector2 attackCooldownRange = new Vector2(0.6f, 1.25f);
    public float attackWindup = 0.18f;
    public float hitboxActiveTime = 0.22f;
    public float attackRecovery = 0.25f;

    // -------------------------
    // Damage
    // -------------------------
    [Header("Damage / Stun")]
    public float maxHealth = 100f;
    public float stunDuration = 0.6f;

    // -------------------------
    // Animation params
    // -------------------------
    [Header("Animation Params")]
    public string animSpeedParam = "Speed";
    public string animAggroBool = "Aggro";
    public string animLightAttackTrigger = "LightAttack";
    public string animHeavyAttackTrigger = "HeavyAttack";
    public string animHitTrigger = "Hit";

    // -------------------------
    // Tuning
    // -------------------------
    [Header("Tuning")]
    public float senseInterval = 0.12f;
    public float chaseRepathInterval = 0.20f;

    [Range(0f, 1f)]
    [Tooltip("0 = no staggering, 1 = full spreading across the whole interval.")]
    public float staggerStrength = 1f;

    // -------------------------
    // Combat movement tuning
    // -------------------------
    [Header("Combat Movement Tuning")]
    public float chaseSpeedFar = 4.0f;
    public float chaseSpeedNear = 1.6f;

    public float slowDownStartDistance = 6.0f;
    public float slowDownEndDistance = 2.2f;

    public float speedBlend = 8.0f;
    public float accelFar = 18f;
    public float accelNear = 8f;

    // --- Internals ---
    private State _state;
    private float _health;

    private float _lastSeenTime = -999f;
    private Vector3 _lastKnownPos;

    private int _patrolIndex = 0;
    private float _nextSenseTime = 0f;
    private float _nextRepathTime = 0f;
    private float _nextFindTargetTime = 0f;

    private float _nextAttackAllowedTime = 0f;
    private Coroutine _stateRoutine;
    private Vector3 _spawnPos;

    // Stagger phases
    private float _sensePhase;   // 0..1
    private float _repathPhase;  // 0..1

    // LookAtConstraint blend
    private float _headLookW = 0f;

    // Vision helpers (ignore self)
    private Collider[] _selfColliders;
    private readonly RaycastHit[] _sightHits = new RaycastHit[24];

    // -------------------------
    // Unity
    // -------------------------
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

        agent.stoppingDistance = stopDistance;
        agent.updateRotation = false;

        _selfColliders = GetComponentsInChildren<Collider>(includeInactive: true);
        _spawnPos = transform.position;

        // Stable phases derived from instance ID (spreads CPU load across frames)
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
    }

    private void Start()
    {
        if (startPatrolling)
            SetState(State.Patrol);
        else
            SetState(State.Idle);
    }

    private void Update()
    {
        if (_state == State.Dead) return;

        // Ensure we can acquire player even if spawned later
        AcquireTargetIfNeeded();

        // Sense at an interval (staggered by initial offset)
        if (Time.time >= _nextSenseTime)
        {
            _nextSenseTime = Time.time + senseInterval;
            Sense();
        }

        UpdateCombatMoveTuning();
        UpdateAnimatorLocomotion();

        if (_state == State.Patrol || _state == State.Chase || _state == State.Strafe || _state == State.Attack || _state == State.Recover || _state == State.Investigate)
            FaceTargetOrMovement();


        // Keep pressure if player stands still in melee range
        if ((_state == State.Chase || _state == State.Recover) && target && Time.time >= _nextAttackAllowedTime)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= attackRange)
                SetState(State.Attack);
        }

        UpdateHeadLookConstraint();
    }

    // -------------------------
    // Player acquisition + constraint wiring
    // -------------------------
    private void AcquireTargetIfNeeded()
    {
        if (target != null && targetHead != null)
            return;

        if (Time.time < _nextFindTargetTime)
            return;

        _nextFindTargetTime = Time.time + findTargetInterval;

        Transform foundPlayer = target;

        // 1) Find by tag (recommended)
        if (foundPlayer == null)
        {
            if (!string.IsNullOrEmpty(playerTag))
            {
                var go = GameObject.FindGameObjectWithTag(playerTag);
                if (go != null) foundPlayer = go.transform;
            }
        }

        // 2) Fallback: use main camera root if tagged player isn't used
        Transform foundCam = null;
        if (preferMainCameraForLook && Camera.main != null)
            foundCam = Camera.main.transform;

        if (foundPlayer == null && foundCam != null)
        {
            // Usually camera is a child of player root
            foundPlayer = foundCam.root;
        }

        if (foundPlayer == null)
            return;

        target = foundPlayer;

        // Choose head look target
        if (foundCam == null && target != null)
        {
            // Try to find a camera in player's children
            var cam = target.GetComponentInChildren<Camera>(includeInactive: true);
            if (cam != null) foundCam = cam.transform;
        }

        // If we found a camera, use it. Otherwise fall back to approximate height.
        if (foundCam != null)
        {
            targetHead = foundCam;
            SetLookAtConstraintSource(foundCam);
        }
        else
        {
            // No camera found: still allow AI to work; head look gating will likely keep weight at 0 unless you set up sources.
            targetHead = null;
        }
    }

    private void SetLookAtConstraintSource(Transform lookTarget)
    {
        if (headLookConstraint == null || lookTarget == null)
            return;

        // Avoid churn if already set
        var sources = new ConstraintSource[headLookConstraint.sourceCount];
        for (int i = 0; i < sources.Length; i++)
            sources[i] = headLookConstraint.GetSource(i);

        if (sources.Length == 1 && sources[0].sourceTransform == lookTarget)
            return;

        headLookConstraint.SetSources(new System.Collections.Generic.List<ConstraintSource>());

        var list = new System.Collections.Generic.List<ConstraintSource>(1)
        {
            new ConstraintSource { sourceTransform = lookTarget, weight = 1f }
        };

        headLookConstraint.SetSources(list);
        headLookConstraint.constraintActive = true;
        headLookConstraint.locked = true;
    }

    // -------------------------
    // LookAtConstraint gating
    // -------------------------
    private void UpdateHeadLookConstraint()
    {
        if (headLookConstraint == null)
            return;

        float desired = 0f;

        bool engaged =
            _state == State.Chase ||
            _state == State.Strafe ||
            _state == State.Attack ||
            _state == State.Recover;

        if (engaged && target != null)
        {
            Vector3 lookPos = (targetHead != null)
                ? targetHead.position
                : (target.position + Vector3.up * targetAimHeight);

            Vector3 to = lookPos - transform.position;
            float dist = to.magnitude;

            if (dist > 0.001f && dist <= headLookRange)
            {
                // Yaw-only angle check
                Vector3 toFlat = to; toFlat.y = 0f;
                Vector3 fwdFlat = transform.forward; fwdFlat.y = 0f;

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

    // -------------------------
    // Sensing
    // -------------------------
    private bool CanSeeTarget(out Vector3 seenPos)
    {
        seenPos = Vector3.zero;
        if (!target) return false;

        Vector3 origin = eyePoint
            ? eyePoint.position
            : (transform.position + Vector3.up * fallbackEyeHeight);

        // Small offset so we don’t start inside our own capsule
        origin += transform.up * 0.05f + transform.forward * 0.12f;

        Vector3 aim = (targetHead != null)
            ? targetHead.position
            : (target.position + Vector3.up * targetAimHeight);

        Vector3 to = aim - origin;
        float dist = to.magnitude;
        if (dist <= 0.001f) return true;

        if (dist > visionRange) return false;

        // FOV check (ignored up close)
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

        // SphereCastNonAlloc + filter out self colliders
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

            if (IsSelfCollider(c)) continue;

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

    private bool IsSelfCollider(Collider c)
    {
        if (!c) return false;
        return c.transform.IsChildOf(transform);
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

            if (!hasMemory && (_state == State.Chase || _state == State.Strafe))
            {
                if (_lastSeenTime > -998f)
                    SetState(State.Investigate);
                else
                    SetState(startPatrolling ? State.Patrol : State.Idle);
            }
        }
    }

    /// <summary>Optional: call this from your player/noise system.</summary>
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

    // -------------------------
    // State Machine
    // -------------------------
    private void SetState(State newState)
    {
        if (_state == newState) return;

        _state = newState;

        if (agent)
        {
            // Default to combat stop distance outside patrol
            if (newState == State.Patrol)
                agent.stoppingDistance = patrolStoppingDistance;
            else
                agent.stoppingDistance = stopDistance;
        }


        if (_stateRoutine != null)
            StopCoroutine(_stateRoutine);

        _stateRoutine = StartCoroutine(RunState(_state));
    }

    private IEnumerator RunState(State s)
    {
        SetAnimatorAggro(s == State.Chase || s == State.Strafe || s == State.Attack || s == State.Recover);

        switch (s)
        {
            case State.Idle:
                agent.isStopped = true;
                yield break;

            case State.Patrol:
                yield return PatrolLoop();
                yield break;

            case State.Investigate:
                yield return InvestigateLoop();
                yield break;

            case State.Chase:
                yield return ChaseLoop();
                yield break;

            case State.Strafe:
                yield return StrafeLoop();
                yield break;

            case State.Attack:
                yield return AttackOnce();
                yield break;

            case State.Recover:
                yield return RecoverBriefly();
                yield break;

            case State.Stunned:
                yield return StunnedBriefly();
                yield break;

            case State.Dead:
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
            // Find a new random destination
            if (!TryGetRandomPatrolDestination(out Vector3 dest))
            {
                // Couldn’t find a point; idle briefly then try again
                agent.isStopped = true;
                yield return new WaitForSeconds(Random.Range(patrolStallTimeRange.x, patrolStallTimeRange.y));
                agent.isStopped = false;
                yield return null;
                continue;
            }

            // Set destination
            agent.isStopped = false;
            agent.SetDestination(dest);

            // Wait for path computation
            while (_state == State.Patrol && agent.pathPending)
                yield return null;

            // If for any reason the path is not valid, immediately pick another point
            if (_state != State.Patrol) yield break;

            if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
                continue;

            // Move until arrived OR path becomes invalid
            while (_state == State.Patrol)
            {
                // Re-check path validity occasionally (helps if NavMesh changes / obstacles)
                if (Time.time >= nextRepathCheck)
                {
                    nextRepathCheck = Time.time + patrolRepathCheckInterval;

                    // If we lost the path or it became partial/invalid, choose a new destination
                    if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
                        break;

                    // Also handle the case where the destination is unreachable now
                    if (!HasCompletePathTo(agent.destination))
                        break;
                }

                float arriveDist = Mathf.Max(patrolArriveTolerance, agent.stoppingDistance + 0.05f);

                // Arrived if within tolerance, OR if agent has basically stopped moving near destination
                if (!agent.pathPending &&
                    agent.remainingDistance <= arriveDist &&
                    (agent.hasPath == false || agent.velocity.sqrMagnitude < 0.02f))
                {
                    break;
                }


                yield return null;
            }

            if (_state != State.Patrol)
                yield break;

            // Stall at destination (or after repath failure)
            agent.isStopped = true;
            float stall = Random.Range(patrolStallTimeRange.x, patrolStallTimeRange.y);
            yield return new WaitForSeconds(stall);
            agent.isStopped = false;

            // Then loop and pick a new random point
            yield return null;
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
            if (!agent.pathPending && agent.remainingDistance <= patrolArriveTolerance)

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

            // Repath occasionally (initially staggered)
            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + chaseRepathInterval;
                agent.SetDestination(target.position);
            }

            // In melee range?
            if (dist <= attackRange)
            {
                agent.isStopped = true;

                if (Time.time < _nextAttackAllowedTime)
                {
                    SetState(State.Strafe);
                    yield break;
                }

                if (Random.value < strafeChance)
                {
                    SetState(State.Strafe);
                    yield break;
                }
                else
                {
                    SetState(State.Attack);
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

            if (dist > attackRange * 1.15f)
            {
                SetState(State.Chase);
                yield break;
            }

            Vector3 toMe = (transform.position - target.position);
            if (toMe.sqrMagnitude < 0.0001f) toMe = transform.right;
            toMe.y = 0f;

            Vector3 aroundDir = Quaternion.AngleAxis(65f * side, Vector3.up) * toMe.normalized;
            Vector3 desiredPos = target.position + aroundDir * Mathf.Clamp(dist, stopDistance, attackRange);

            agent.SetDestination(desiredPos);

            timer += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = true;

        if (Time.time >= _nextAttackAllowedTime)
            SetState(State.Attack);
        else
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

        // Face target before swinging
        yield return FaceTargetFor(0.10f);

        bool heavy = Random.value < 0.25f;

        if (animator)
        {
            animator.ResetTrigger(animLightAttackTrigger);
            animator.ResetTrigger(animHeavyAttackTrigger);
            animator.SetTrigger(heavy ? animHeavyAttackTrigger : animLightAttackTrigger);
        }

        yield return new WaitForSeconds(attackWindup);

        if (swordHitbox)
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

        if (!target)
        {
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        SetState(State.Chase);
    }

    private IEnumerator StunnedBriefly()
    {
        agent.isStopped = true;

        float t = stunDuration;
        while (_state == State.Stunned && t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        if (_state == State.Stunned)
            SetState(target ? State.Chase : (startPatrolling ? State.Patrol : State.Idle));
    }

    // -------------------------
    // Facing / Animation
    // -------------------------
    private void FaceTargetOrMovement()
    {
        if (!agent) return;

        Vector3 lookDir = Vector3.zero;

        bool combatFacing = target && (_state == State.Attack || _state == State.Strafe || _state == State.Chase || _state == State.Recover);

        if (combatFacing)
        {
            lookDir = target.position - transform.position;
        }
        else
        {
            // Prefer steering target (path direction) while navigating/patrolling
            Vector3 toSteer = agent.steeringTarget - transform.position;
            toSteer.y = 0f;

            if (toSteer.sqrMagnitude > 0.0001f)
                lookDir = toSteer;
            else
                lookDir = agent.velocity; // fallback
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

    private void UpdateCombatMoveTuning()
    {
        if (!agent || !target) return;

        bool engaging = _state == State.Chase || _state == State.Strafe || _state == State.Investigate;
        if (!engaging) return;

        float dist = Vector3.Distance(transform.position, target.position);

        float nearDist = Mathf.Min(slowDownStartDistance, slowDownEndDistance);
        float farDist = Mathf.Max(slowDownStartDistance, slowDownEndDistance);

        // 0 close, 1 far
        float norm = Mathf.InverseLerp(nearDist, farDist, dist);
        norm = Mathf.Clamp01(norm);
        norm = norm * norm;

        float desiredSpeed = Mathf.Lerp(chaseSpeedNear, chaseSpeedFar, norm);
        float desiredAccel = Mathf.Lerp(accelNear, accelFar, norm);

        agent.speed = Mathf.Lerp(agent.speed, desiredSpeed, 1f - Mathf.Exp(-speedBlend * Time.deltaTime));
        agent.acceleration = Mathf.Lerp(agent.acceleration, desiredAccel, 1f - Mathf.Exp(-speedBlend * Time.deltaTime));
    }

    // -------------------------
    // Damage API
    // -------------------------
    public void ApplyDamage(float amount, bool canStun = true)
    {
        if (_state == State.Dead) return;

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

            SetState(State.Stunned);
        }
    }

    private void Die()
    {
        if (_state == State.Dead) return;

        _health = 0f;
        SetState(State.Dead);

        if (headLookConstraint != null)
            headLookConstraint.weight = 0f;

        agent.isStopped = true;
        agent.enabled = false;


        var rf = GetComponentInChildren<RayfireRigid>();
        if (rf != null) rf.Demolish();

        Destroy(this.gameObject);
    }

    private int ResolveDamageFrom(Collider other)
    {
        var dealer = other.GetComponentInParent<swordDamageDeterminer>();
        if (dealer != null) return Mathf.Max(1, dealer.damage);
        return 10;
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

            // Snap to NavMesh near the candidate point
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 2.0f, agent.areaMask))
                continue;

            // Must have a complete path
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

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || _state == State.Dead) return;

        if (other.CompareTag("PlayerSword"))
        {
            int dmg = ResolveDamageFrom(other);
            GetComponent<DamageRef>().TakeDamage(dmg);

            var lantern = GameObject.FindAnyObjectByType<chargeOffHandLatern>();
            if (lantern != null && lantern.enabled)
                lantern.hitRegistered();
        }
    }

    // -------------------------
    // Debug Gizmos
    // -------------------------
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
