using RayFire;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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

    [Tooltip("Set at runtime or in inspector (player root).")]
    public Transform target;

    [Tooltip("Sword hitbox component (child object) that has a trigger collider.")]
    public SwordHitbox swordHitbox;

    [Header("Patrol")]
    public bool startPatrolling = true;
    public Transform[] patrolPoints;
    public float patrolPointTolerance = 1.1f;
    public Vector2 patrolWaitRange = new Vector2(0.5f, 2.0f);
    [Header("Perception (Reworked)")]
    [Tooltip("How far the skeleton can see the player.")]
    public float visionRange = 20f;

    [Range(10f, 180f)]
    [Tooltip("Field of view angle (degrees).")]
    public float visionFov = 120f;

    [Tooltip("Within this range we ignore FOV (still requires line of sight).")]
    public float closeAwarenessRange = 8.0f;

    [Tooltip("If within this range, the skeleton 'auto-detects' even if not facing (still LOS).")]
    public float autoDetectRange = 3.0f;

    [Tooltip("How thick the vision ray is. Helps prevent tiny gaps/edges breaking vision.")]
    public float sightThickness = 0.12f;

    [Tooltip("Eye height if eyePoint is null.")]
    public float fallbackEyeHeight = 1.6f;

    [Tooltip("Where on the target we aim the sight check (chest/head).")]
    public float targetAimHeight = 1.4f;

    [Tooltip("Only these layers can block vision (WALLS/LEVEL). EXCLUDE Player.")]
    public LayerMask occlusionMask;

    [Tooltip("Optional: draw vision debug rays.")]
    public bool debugVision;



    [Tooltip("How far the skeleton can 'hear' (if you want to call OnHeardNoise).")]
    public float hearingRange = 12f;

    [Tooltip("Seconds the skeleton keeps chasing after losing line of sight.")]
    public float aggroMemoryTime = 4.0f;

    [Tooltip("Layers considered obstacles for vision line-of-sight.")]
    public LayerMask obstacleMask = ~0;

    [Header("Combat")]
    [Tooltip("Preferred melee distance to start attacks.")]
    public float attackRange = 2.15f;

    [Tooltip("Extra distance to stop the agent when engaging.")]
    public float stopDistance = 1.9f;

    [Tooltip("Face target speed in melee.")]
    public float turnSpeed = 10f;

    [Header("Locomotion Animation")]
    [Tooltip("World speed (m/s) that corresponds to full run in your blend tree.")]
    public float animMaxMoveSpeed = 4.0f; // set to chaseSpeedFar (or your clip's intended run speed)



    [Tooltip("How long we spend circling/strafing before attempting an attack.")]
    public Vector2 strafeDurationRange = new Vector2(0.4f, 1.0f);

    [Tooltip("Chance to strafe instead of immediate attack when in range.")]
    [Range(0f, 1f)]
    public float strafeChance = 0.65f;

    [Tooltip("Cooldown between attacks.")]
    public Vector2 attackCooldownRange = new Vector2(0.6f, 1.25f);

    [Tooltip("Attack windup time before the hitbox turns on (telegraph).")]
    public float attackWindup = 0.18f;

    [Tooltip("How long hitbox stays active during a swing.")]
    public float hitboxActiveTime = 0.22f;

    [Tooltip("How long we wait after a swing (recovery).")]
    public float attackRecovery = 0.25f;

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

    [Tooltip("Animator trigger for death.")]
    public string animDieTrigger = "Die";

    [Header("Tuning")]
    [Tooltip("How often (seconds) we refresh detection checks to save CPU.")]
    public float senseInterval = 0.12f;

    [Tooltip("How often we update destination while chasing.")]
    public float chaseRepathInterval = 0.20f;


    [Range(0f, 1f)]
    [Tooltip("0 = no staggering, 1 = full spreading across the whole interval.")]
    public float staggerStrength = 1f;

    private float _sensePhase;   // 0..1
    private float _repathPhase;  // 0..1


    [Header("Look IK")]
    public bool useLookIK = true;

    [Tooltip("If null, we'll approximate target head position using targetAimHeight.")]
    public Transform targetHead;

    [Range(0f, 1f)] public float lookWeight = 0.85f;
    [Range(0f, 1f)] public float bodyWeight = 0.25f;
    [Range(0f, 1f)] public float headWeight = 0.85f;
    [Range(0f, 1f)] public float eyesWeight = 0.0f;

    [Tooltip("How fast the look blends in/out.")]
    public float lookBlendSpeed = 8f;

    [Tooltip("How far away we stop looking (prevents weird long-distance twist).")]
    public float lookMaxDistance = 18f;


    [Tooltip("Max distance to start looking.")]
    public float lookRange = 12f;

    [Tooltip("Stop looking if player is more than this many degrees off forward (yaw).")]
    [Range(10f, 180f)]
    public float maxLookAngle = 85f;



    [Tooltip("Clamp to reduce extreme twisting (0..1). Lower = less twist.")]
    [Range(0f, 1f)] public float lookClamp = 0.55f;

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
    public float accelNear = 8f;


    private float _lookW; // smoothed 0..1

    // --- Vision helpers ---
    private Collider[] _selfColliders;
    private readonly RaycastHit[] _sightHits = new RaycastHit[24];



    private State _state;
    private float _health;

    private float _lastSeenTime = -999f;
    private Vector3 _lastKnownPos;

    private int _patrolIndex = 0;
    private float _nextSenseTime = 0f;
    private float _nextRepathTime = 0f;

    private float _nextAttackAllowedTime = 0f;

    private Coroutine _stateRoutine;

    // --- Unity ---
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

        // Stable phases derived from instance ID (so enemies spread out consistently)
        int id = Mathf.Abs(GetInstanceID());
        _sensePhase = ((id * 0.6180339f) % 1f);       // pseudo-random 0..1
        _repathPhase = ((id * 0.3819660f) % 1f);

        float senseOffset = senseInterval * Mathf.Lerp(0f, _sensePhase, staggerStrength);
        float repathOffset = chaseRepathInterval * Mathf.Lerp(0f, _repathPhase, staggerStrength);

        _nextSenseTime = Time.time + senseOffset;
        _nextRepathTime = Time.time + repathOffset;
    }


    private void Start()
    {
        if (startPatrolling && patrolPoints != null && patrolPoints.Length > 0)
            SetState(State.Patrol);
        else
            SetState(State.Idle);
    }

    private void Update()
    {
        if (_state == State.Dead) return;

        // Sense at an interval
        if (Time.time >= _nextSenseTime)
        {
            _nextSenseTime = Time.time + senseInterval * (1f + 0f); // keep base interval
                                                                    // and add phase offset by shifting the "start" only once (we already did in Awake)
                                                                    // So this line is fine as-is; the initial offset prevents sync.

            Sense();
        }
        UpdateCombatMoveTuning();

        // Animation locomotion blend
        UpdateAnimatorLocomotion();

        // Manual facing (only in relevant states)
        if (_state == State.Chase || _state == State.Strafe || _state == State.Attack || _state == State.Recover || _state == State.Investigate)
            FaceTargetOrMovement();

        // Keeps attacking if you're standing still in range (SoT pressure)
        if (_state == State.Chase || _state == State.Recover)
        {
            if (target && Time.time >= _nextAttackAllowedTime)
            {
                float dist = Vector3.Distance(transform.position, target.position);
                if (dist <= attackRange)
                {
                    SetState(State.Attack);
                }
            }
        }

    }
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

            // Ignore our own colliders
            if (IsSelfCollider(c)) continue;

            // If it's on occlusionMask and not self, it blocks
            if (h.distance < closest)
            {
                closest = h.distance;
                closestBlock = h;
            }
            blocked = true;
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
        // Fast path: most enemies are one hierarchy
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


    

    /// <summary>
    /// Optional: call this from your player/noise system.
    /// </summary>
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

    // --- State Machine ---
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
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            SetState(State.Idle);
            yield break;
        }

        agent.isStopped = false;

        while (_state == State.Patrol)
        {
            Transform p = patrolPoints[_patrolIndex % patrolPoints.Length];
            if (p)
            {
                agent.SetDestination(p.position);

                // Wait until reached
                while (_state == State.Patrol && agent.pathPending)
                    yield return null;

                while (_state == State.Patrol && agent.remainingDistance > patrolPointTolerance)
                    yield return null;

                // small idle pause
                float wait = Random.Range(patrolWaitRange.x, patrolWaitRange.y);
                float t = 0f;
                while (_state == State.Patrol && t < wait)
                {
                    agent.isStopped = true;
                    t += Time.deltaTime;
                    yield return null;
                }

                agent.isStopped = false;
                _patrolIndex++;
            }
            else
            {
                _patrolIndex++;
                yield return null;
            }
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
            if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
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

            // Repath occasionally
            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + chaseRepathInterval;

                agent.SetDestination(target.position);
            }

            // In melee range?
            if (dist <= attackRange)
            {
                agent.isStopped = true;

                // if attack is on cooldown, strafe a bit (SoT vibe)
                if (Time.time < _nextAttackAllowedTime)
                {
                    SetState(State.Strafe);
                    yield break;
                }

                // choose strafe or attack
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

        // Choose left or right circle
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

            // If we drift out, go back to chase
            if (dist > attackRange * 1.15f)
            {
                SetState(State.Chase);
                yield break;
            }

            // Create a strafing destination around the target
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

        // After strafe, try attack if allowed; else chase
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

        // Decide attack type (light/heavy)
        bool heavy = Random.value < 0.25f;

        if (animator)
        {
            animator.ResetTrigger(animLightAttackTrigger);
            animator.ResetTrigger(animHeavyAttackTrigger);
            animator.SetTrigger(heavy ? animHeavyAttackTrigger : animLightAttackTrigger);
        }

        // Windup telegraph (no hitbox yet)
        yield return new WaitForSeconds(attackWindup);

        // Turn on hitbox
        if (swordHitbox)
        {
            swordHitbox.BeginHitWindow(this);
            yield return new WaitForSeconds(hitboxActiveTime);
            swordHitbox.EndHitWindow();
        }
        else
        {
            yield return new WaitForSeconds(hitboxActiveTime);
        }

        // Recovery
        yield return new WaitForSeconds(attackRecovery);

        // Cooldown
        _nextAttackAllowedTime = Time.time + Random.Range(attackCooldownRange.x, attackCooldownRange.y);

        SetState(State.Recover);
    }

    private IEnumerator RecoverBriefly()
    {
        // Tiny “reset / decision” window like SoT
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

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= attackRange * 1.1f)
            SetState(State.Chase); // Chase decides strafe/attack again
        else
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

    // --- Facing / Animation ---
    private void FaceTargetOrMovement()
    {
        Vector3 lookDir;

        if (target && (_state == State.Attack || _state == State.Strafe || _state == State.Chase || _state == State.Recover))
        {
            lookDir = target.position - transform.position;
        }
        else
        {
            // face movement direction if no target focus
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

        // Use *actual movement* speed in world space.
        float worldSpeed = agent.velocity.magnitude;

        // Normalize against a fixed max so slowing down actually lowers the param.
        float speed01 = worldSpeed / Mathf.Max(0.01f, animMaxMoveSpeed);
        speed01 = Mathf.Clamp01(speed01);

        animator.SetFloat(animSpeedParam, speed01, 0.12f, Time.deltaTime);
    }


    private void SetAnimatorAggro(bool aggro)
    {
        if (!animator || string.IsNullOrEmpty(animAggroBool)) return;
        animator.SetBool(animAggroBool, aggro);
    }

    // --- Damage API ---
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

        agent.isStopped = true;
        agent.enabled = false;

        if (swordHitbox) swordHitbox.EndHitWindow();

        GetComponentInChildren<RayfireRigid>().Demolish();
        Destroy(this.gameObject);

        // Optional: disable colliders / drop loot / ragdoll, etc.
        // Destroy(gameObject, 8f);
    }
    int ResolveDamageFrom(Collider other)
    {
        var dealer = other.GetComponentInParent<swordDamageDeterminer>();
        if (dealer != null) return Mathf.Max(1, dealer.damage);
        return Mathf.Max(1, 10);
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
            {
                // Debug.Log("Boss hit by sword, notifying lantern");
                lantern.hitRegistered();
            }
        }
    }
    private void OnAnimatorIK(int layerIndex)
    {
        if (!useLookIK || !animator || _state == State.Dead)
            return;

        // Only do head tracking while engaged (tweak if you want it during patrol too)
        bool engaged =
            _state == State.Chase ||
            _state == State.Strafe ||
            _state == State.Attack ||
            _state == State.Recover;

        float desired = 0f;

        if (engaged && target)
        {
            Vector3 origin = eyePoint ? eyePoint.position : (transform.position + Vector3.up * 1.6f);

            Vector3 lookPos = (targetHead != null)
                ? targetHead.position
                : (target.position + Vector3.up * targetAimHeight);

            Vector3 to = lookPos - origin;
            float dist = to.magnitude;

            if (dist <= lookRange && dist > 0.001f)
            {
                // Yaw angle only (ignore vertical so hills don't break it)
                Vector3 toFlat = to; toFlat.y = 0f;
                Vector3 fwdFlat = transform.forward; fwdFlat.y = 0f;

                if (toFlat.sqrMagnitude > 0.0001f && fwdFlat.sqrMagnitude > 0.0001f)
                {
                    float yawAngle = Vector3.Angle(fwdFlat.normalized, toFlat.normalized);

                    // Look only if player is within allowed angle
                    if (yawAngle <= maxLookAngle)
                        desired = 1f;
                }

                // Apply look target even while blending so it doesn't pop
                animator.SetLookAtPosition(lookPos);
            }
        }

        // Smooth blend in/out
        _lookW = Mathf.MoveTowards(_lookW, desired, lookBlendSpeed * Time.deltaTime);

        if (_lookW <= 0.001f)
        {
            animator.SetLookAtWeight(0f);
            return;
        }

        animator.SetLookAtWeight(
            _lookW * lookWeight,
            bodyWeight,
            headWeight,
            eyesWeight,
            lookClamp
        );
    }
    private void UpdateCombatMoveTuning()
    {
        if (!agent || !target) return;

        // Only scale speed while actually moving/engaging
        bool engaging = _state == State.Chase || _state == State.Strafe || _state == State.Investigate;
        if (!engaging) return;

        float dist = Vector3.Distance(transform.position, target.position);

        // Ensure correct ordering even if inspector values are reversed
        float nearDist = Mathf.Min(slowDownStartDistance, slowDownEndDistance); // usually ~attackRange-ish
        float farDist = Mathf.Max(slowDownStartDistance, slowDownEndDistance); // farther away

        // norm: 0 when close (nearDist), 1 when far (farDist and beyond)
        float norm = Mathf.InverseLerp(nearDist, farDist, dist);
        norm = Mathf.Clamp01(norm);

        // Optional easing (keeps it smoother)
        norm = norm * norm; // ease-in

        // When far -> chaseSpeedFar, when close -> chaseSpeedNear
        float desiredSpeed = Mathf.Lerp(chaseSpeedNear, chaseSpeedFar, norm);
        float desiredAccel = Mathf.Lerp(accelNear, accelFar, norm);

        agent.speed = Mathf.Lerp(agent.speed, desiredSpeed, 1f - Mathf.Exp(-speedBlend * Time.deltaTime));
        agent.acceleration = Mathf.Lerp(agent.acceleration, desiredAccel, 1f - Mathf.Exp(-speedBlend * Time.deltaTime));
    }


    // --- Debug Gizmos ---
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
