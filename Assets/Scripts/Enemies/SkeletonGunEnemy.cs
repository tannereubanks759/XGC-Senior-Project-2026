using RayFire;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class SkeletonGunEnemy : MonoBehaviour
{
    public enum State
    {
        Idle,
        Patrol,
        Investigate,
        Chase,
        Aim,
        Fire,
        Recover,
        Stunned,
        Dead
    }

    [Header("Curse")]
    public bool isCursed = false;
    public int curseDamageMult = 1;
    public float curseSpeedMult = 1f;

    [Header("References")]
    [Tooltip("Animator with locomotion + gun animations.")]
    public Animator animator;

    [Tooltip("Optional. If null, will use GetComponent<NavMeshAgent>().")]
    public NavMeshAgent agent;

    [Tooltip("Where the enemy considers 'eyes' to be for vision checks.")]
    public Transform eyePoint;

    [Tooltip("Optional. If not set, the enemy will find the player after they spawn.")]
    public Transform target;

    [Header("Player Auto-Find")]
    public string playerTag = "Player";
    public float findTargetInterval = 0.5f;
    public bool preferMainCameraForLook = true;

    [Header("Head Look (LookAtConstraint)")]
    public LookAtConstraint headLookConstraint;
    public Transform targetHead;
    public float headLookRange = 14f;

    [Range(10f, 180f)]
    public float headLookMaxAngle = 95f;

    public float headLookBlendSpeed = 8f;

    [Range(0f, 1f)]
    public float headLookMaxWeight = 1f;

    [Header("Patrol (Random Wander)")]
    public bool startPatrolling = true;
    public float patrolRadius = 12f;
    public float patrolArriveTolerance = 1.1f;
    public Vector2 patrolStallTimeRange = new Vector2(0.6f, 2.0f);
    public float patrolRepathCheckInterval = 0.35f;
    public int patrolFindMaxAttempts = 12;
    public float patrolRadiusJitter = 0.0f;
    public float patrolSpeed = 2.0f;
    public float patrolAcceleration = 8.0f;
    public float patrolStoppingDistance = 0.4f;

    [Header("Perception")]
    public float visionRange = 25f;

    [Range(10f, 180f)]
    public float visionFov = 120f;

    public float closeAwarenessRange = 8.0f;
    public float autoDetectRange = 3.0f;
    public float sightThickness = 0.12f;
    public float fallbackEyeHeight = 1.6f;
    public float targetAimHeight = 1.4f;
    public LayerMask occlusionMask;
    public bool debugVision;
    public float aggroMemoryTime = 4.0f;

    [Header("Gun Combat")]
    public float preferredShootDistance = 9f;
    public float tooCloseDistance = 6f;
    public float maxEngageDistance = 18f;
    public float aimDuration = 3f;
    public float postFireRecoverTime = 0.6f;
    public Vector2 shotCooldownRange = new Vector2(2.0f, 4.0f);

    [Range(0f, 1f)]
    public float repositionAfterShotChance = 0.55f;

    public float repositionRadius = 4.5f;
    public float repositionMaxTime = 1.25f;

    [Header("Movement / Rotation")]
    public float turnSpeed = 10f;

    [Header("Combat Movement Tuning")]
    public float chaseSpeedFar = 4.0f;
    public float chaseSpeedNear = 2.2f;
    public float slowDownStartDistance = 12.0f;
    public float slowDownEndDistance = 8.5f;
    public float speedBlend = 8.0f;
    public float accelFar = 18f;
    public float accelNear = 10f;

    [Header("Locomotion Animation")]
    public float animMaxMoveSpeed = 4.0f;

    [Header("Animator Params")]
    public string animSpeedParam = "Speed";
    public string animAggroBool = "Aggro";
    public string animHitTrigger = "Hit";
    public string animDieTrigger = "Die";
    public string animIsAimingBool = "isAiming";
    public string animShootTrigger = "shoot";

    [Header("Tuning")]
    public float senseInterval = 0.12f;
    public float chaseRepathInterval = 0.20f;

    [Range(0f, 1f)]
    public float staggerStrength = 1f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float stunDuration = 0.6f;

    private State _state;
    private float _health;

    private float _lastSeenTime = -999f;
    private Vector3 _lastKnownPos;

    private float _nextSenseTime;
    private float _nextRepathTime;
    private float _nextFindTargetTime;

    private float _nextShotAllowedTime;
    private float _stunEndTime = -999f;

    private Coroutine _stateRoutine;
    private Vector3 _spawnPos;

    private float _sensePhase;
    private float _repathPhase;

    private float _headLookW;
    private readonly RaycastHit[] _sightHits = new RaycastHit[24];

    // ---------------------------
    // Reposition fix: persistent reposition mode + small stoppingDistance
    // ---------------------------
    private bool _isRepositioning = false;
    private Vector3 _repositionDest;
    private float _repositionEndTime = -999f;
    private float _nextRepositionAttemptTime = -999f;
    private bool _wantsRepositionAfterShot = false;

    [Header("Reposition (Advanced)")]
    [Tooltip("Stopping distance used ONLY while repositioning (should be small).")]
    public float repositionStoppingDistance = 0.15f;

    [Tooltip("If reposition fails, wait this long before trying again (prevents spam).")]
    public float repositionRetryCooldown = 0.25f;

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
        _spawnPos = transform.position;

        int id = Mathf.Abs(GetInstanceID());
        _sensePhase = ((id * 0.6180339f) % 1f);
        _repathPhase = ((id * 0.3819660f) % 1f);

        float senseOffset = senseInterval * Mathf.Lerp(0f, _sensePhase, staggerStrength);
        float repathOffset = chaseRepathInterval * Mathf.Lerp(0f, _repathPhase, staggerStrength);

        _nextSenseTime = Time.time + senseOffset;
        _nextRepathTime = Time.time + repathOffset;
        _nextFindTargetTime = Time.time + (findTargetInterval * Mathf.Lerp(0f, _sensePhase, staggerStrength));

        // Aim immediately on first engage; cooldown is applied after first shot.
        _nextShotAllowedTime = Time.time;


        if (headLookConstraint != null)
            headLookConstraint.weight = 0f;

        SetAiming(false);
    }

    private void Start()
    {
        SetState(startPatrolling ? State.Patrol : State.Idle);
    }

    private void Update()
    {
        if (_state == State.Dead) return;

        AcquireTargetIfNeeded();

        if (Time.time >= _nextSenseTime)
        {
            _nextSenseTime = Time.time + senseInterval;
            Sense();
        }

        UpdateCombatMoveTuning();
        UpdateAnimatorLocomotion();
        UpdateHeadLookConstraint();

        if (_state == State.Patrol || _state == State.Investigate || _state == State.Chase)
            FaceTargetOrMovement();
        else if (_state == State.Aim || _state == State.Fire || _state == State.Recover)
            FaceTargetOnly();
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
        headLookConstraint.locked = false;
    }

    private void UpdateHeadLookConstraint()
    {
        if (headLookConstraint == null) return;

        float desired = 0f;

        bool engaged =
            _state == State.Chase ||
            _state == State.Aim ||
            _state == State.Fire ||
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

            if (!hasMemory && (_state == State.Chase || _state == State.Aim || _state == State.Fire || _state == State.Recover))
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

        bool blocked = hitCount > 0;

        if (debugVision)
            Debug.DrawLine(origin, aim, blocked ? Color.red : Color.green, senseInterval);

        if (blocked) return false;

        seenPos = target.position;
        return true;
    }

    private void SetState(State newState)
    {
        if (_state == newState) return;
        _state = newState;

        if (_stateRoutine != null)
            StopCoroutine(_stateRoutine);

        _stateRoutine = StartCoroutine(RunState(_state));
    }

    private IEnumerator RunState(State s)
    {
        SetAnimatorAggro(s == State.Chase || s == State.Aim || s == State.Fire || s == State.Recover);

        switch (s)
        {
            case State.Idle:
                CancelReposition();
                agent.isStopped = true;
                SetAiming(false);
                yield break;

            case State.Patrol:
                CancelReposition();
                agent.isStopped = false;
                SetAiming(false);
                yield return PatrolLoop();
                yield break;

            case State.Investigate:
                CancelReposition();
                agent.isStopped = false;
                SetAiming(false);
                yield return InvestigateLoop();
                yield break;

            case State.Chase:
                agent.isStopped = false;
                SetAiming(false);
                yield return ChaseLoop();
                yield break;

            case State.Aim:
                CancelReposition();
                yield return AimLoop();
                yield break;

            case State.Fire:
                CancelReposition();
                yield return FireOnce();
                yield break;

            case State.Recover:
                CancelReposition();
                yield return RecoverBriefly();
                yield break;

            case State.Stunned:
                CancelReposition();
                yield return StunnedBriefly();
                yield break;

            case State.Dead:
                yield break;
        }
    }

    private IEnumerator PatrolLoop()
    {
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
        agent.stoppingDistance = patrolStoppingDistance;
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
        // restore default chase stop distance
        agent.stoppingDistance = preferredShootDistance;

        while (_state == State.Chase)
        {
            if (!target)
            {
                CancelReposition();
                SetState(startPatrolling ? State.Patrol : State.Idle);
                yield break;
            }

            float dist = Vector3.Distance(transform.position, target.position);

            // If we are currently repositioning, keep doing it until done/timeout.
            if (_isRepositioning)
            {
                if (Time.time >= _repositionEndTime)
                {
                    EndReposition();
                }
                else if (!agent.pathPending && agent.hasPath)
                {
                    float arriveDist = Mathf.Max(0.15f, agent.stoppingDistance + 0.05f);
                    if (agent.remainingDistance <= arriveDist && agent.velocity.sqrMagnitude < 0.03f)
                        EndReposition();
                }

                // While repositioning, do NOT override destination every frame.
                yield return null;
                continue;
            }

            // After-shot requested reposition
            if (_wantsRepositionAfterShot)
            {
                _wantsRepositionAfterShot = false;
                BeginRepositionAwayFromTarget();
                yield return null;
                continue;
            }

            // Too far: move closer
            if (dist > preferredShootDistance)
            {
                agent.isStopped = false;
                agent.stoppingDistance = preferredShootDistance;

                if (Time.time >= _nextRepathTime)
                {
                    _nextRepathTime = Time.time + chaseRepathInterval;
                    agent.SetDestination(target.position);
                }
            }
            // Too close: reposition away (fixed)
            else if (dist < tooCloseDistance)
            {
                BeginRepositionAwayFromTarget();

                // If reposition couldn't start, fallback to simple chase update (so it never freezes)
                if (!_isRepositioning)
                {
                    agent.isStopped = false;
                    agent.stoppingDistance = preferredShootDistance;

                    if (Time.time >= _nextRepathTime)
                    {
                        _nextRepathTime = Time.time + chaseRepathInterval;
                        agent.SetDestination(target.position);
                    }
                }
            }
            // In the “gun range window”
            else
            {
                agent.isStopped = true;
                agent.ResetPath();

                if (dist <= maxEngageDistance)
                {
                    SetState(State.Aim);
                    yield break;
                }

            }

            yield return null;
        }
    }

    private IEnumerator AimLoop()
    {
        if (!target)
        {
            SetAiming(false);
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        agent.isStopped = true;
        agent.ResetPath();
        SetAiming(true);

        float aimT = 0f;

        while (_state == State.Aim)
        {
            if (!target)
            {
                SetAiming(false);
                SetState(startPatrolling ? State.Patrol : State.Idle);
                yield break;
            }

            float dist = Vector3.Distance(transform.position, target.position);

            // If player runs out of engagement distance, cancel aim and chase
            if (dist > maxEngageDistance || dist > preferredShootDistance + 6f)
            {
                SetAiming(false);
                SetState(State.Chase);
                yield break;
            }

            // If player pushes too close, cancel aim and reposition/chase
            if (dist < tooCloseDistance)
            {
                SetAiming(false);
                SetState(State.Chase);
                yield break;
            }

            // Spend time aiming
            if (aimT < aimDuration)
            {
                aimT += Time.deltaTime;
            }
            else
            {
                // Aim finished: wait until cooldown allows firing
                if (Time.time >= _nextShotAllowedTime)
                {
                    SetState(State.Fire);
                    yield break;
                }
                // else: keep holding aim pose until allowed
            }

            yield return null;
        }
    }


    private IEnumerator FireOnce()
    {
        if (!target)
        {
            SetAiming(false);
            SetState(startPatrolling ? State.Patrol : State.Idle);
            yield break;
        }

        agent.isStopped = true;
        agent.ResetPath();
        SetAiming(true);

        if (animator && !string.IsNullOrEmpty(animShootTrigger))
            animator.SetTrigger(animShootTrigger);

        yield return new WaitForSeconds(0.15f);

        SetAiming(false);

        _nextShotAllowedTime = Time.time + Random.Range(shotCooldownRange.x, shotCooldownRange.y);

        // Defer reposition until we get back to Chase (so Recover doesn't instantly cancel it)
        _wantsRepositionAfterShot = (Random.value < repositionAfterShotChance);

        SetState(State.Recover);
    }

    private IEnumerator RecoverBriefly()
    {
        agent.isStopped = true;
        agent.ResetPath();

        float t = 0f;
        while (_state == State.Recover && t < postFireRecoverTime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        SetState(State.Chase);
    }

    private IEnumerator StunnedBriefly()
    {
        agent.isStopped = true;

        if (_stunEndTime < Time.time)
            _stunEndTime = Time.time + Mathf.Max(0.05f, stunDuration);

        while (_state == State.Stunned && Time.time < _stunEndTime)
            yield return null;

        SetState(target ? State.Chase : (startPatrolling ? State.Patrol : State.Idle));
    }

    // ---------------------------
    // Reposition (FIXED)
    // ---------------------------
    private void BeginRepositionAwayFromTarget()
    {
        if (Time.time < _nextRepositionAttemptTime) return;
        _nextRepositionAttemptTime = Time.time + Mathf.Max(0.01f, repositionRetryCooldown);

        if (!target || !agent || !agent.enabled || !agent.isOnNavMesh) return;

        if (TryPickRepositionDestination(out Vector3 dest))
        {
            _isRepositioning = true;
            _repositionDest = dest;
            _repositionEndTime = Time.time + Mathf.Max(0.1f, repositionMaxTime);

            agent.isStopped = false;
            agent.ResetPath();

            // CRITICAL FIX: small stopping distance during reposition (NOT preferredShootDistance)
            agent.stoppingDistance = Mathf.Max(0.01f, repositionStoppingDistance);

            agent.SetDestination(_repositionDest);
        }
    }

    private bool TryPickRepositionDestination(out Vector3 destination)
    {
        destination = transform.position;

        Vector3 away = (transform.position - target.position);
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
        away.Normalize();

        Vector3 side = Vector3.Cross(Vector3.up, away).normalized;

        Vector3[] dirs =
        {
            away,
            (away + side * 0.6f).normalized,
            (away - side * 0.6f).normalized,
            side,
            -side
        };

        for (int i = 0; i < dirs.Length; i++)
        {
            Vector3 candidate = transform.position + dirs[i] * repositionRadius;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 2.0f, agent.areaMask))
                continue;

            var path = new NavMeshPath();
            if (!agent.CalculatePath(navHit.position, path)) continue;
            if (path.status != NavMeshPathStatus.PathComplete) continue;

            destination = navHit.position;
            return true;
        }

        return false;
    }

    private void EndReposition()
    {
        _isRepositioning = false;

        if (agent && agent.enabled)
        {
            // Restore normal combat stopping distance
            agent.stoppingDistance = preferredShootDistance;
        }
    }

    private void CancelReposition()
    {
        _isRepositioning = false;
        _wantsRepositionAfterShot = false;
    }

    private void FaceTargetOnly()
    {
        if (!target) return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    private void FaceTargetOrMovement()
    {
        if (!agent) return;

        Vector3 lookDir;

        if (target && (_state == State.Chase))
        {
            lookDir = target.position - transform.position;
        }
        else
        {
            Vector3 toSteer = agent.steeringTarget - transform.position;
            toSteer.y = 0f;
            lookDir = (toSteer.sqrMagnitude > 0.0001f) ? toSteer : agent.velocity;
        }

        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
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

    private void SetAiming(bool aiming)
    {
        if (!animator || string.IsNullOrEmpty(animIsAimingBool)) return;
        animator.SetBool(animIsAimingBool, aiming);
    }

    private void UpdateCombatMoveTuning()
    {
        if (!agent || !target) return;

        bool engaging = _state == State.Chase || _state == State.Investigate;
        if (!engaging) return;

        // While repositioning, don't fight the movement tuning (but it's safe either way)
        float dist = Vector3.Distance(transform.position, target.position);

        float nearDist = Mathf.Min(slowDownStartDistance, slowDownEndDistance);
        float farDist = Mathf.Max(slowDownStartDistance, slowDownEndDistance);

        float norm = Mathf.InverseLerp(nearDist, farDist, dist);
        norm = Mathf.Clamp01(norm);
        norm = norm * norm;

        float desiredSpeed = Mathf.Lerp(chaseSpeedNear, chaseSpeedFar, norm);
        float desiredAccel = Mathf.Lerp(accelNear, accelFar, norm);

        if (isCursed)
        {
            float m = Mathf.Clamp(curseSpeedMult, 0.05f, 1f);
            desiredSpeed *= m;
            desiredAccel *= m;
        }

        agent.speed = Mathf.Lerp(agent.speed, desiredSpeed, 1f - Mathf.Exp(-speedBlend * Time.deltaTime));
        agent.acceleration = Mathf.Lerp(agent.acceleration, desiredAccel, 1f - Mathf.Exp(-speedBlend * Time.deltaTime));
    }

    public void ApplyDamage(float amount, bool canStun = true)
    {
        if (_state == State.Dead) return;

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

    private void Die()
    {
        if (_state == State.Dead) return;

        _health = 0f;
        SetState(State.Dead);

        if (headLookConstraint != null)
            headLookConstraint.weight = 0f;

        SetAiming(false);

        if (agent)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (animator && !string.IsNullOrEmpty(animDieTrigger))
            animator.SetTrigger(animDieTrigger);

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

    public float GetHealth() => _health;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, preferredShootDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxEngageDistance);

        if (eyePoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(eyePoint.position, visionRange);
        }

        if (_isRepositioning)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_repositionDest, 0.25f);
            Gizmos.DrawLine(transform.position, _repositionDest);
        }
    }
}
