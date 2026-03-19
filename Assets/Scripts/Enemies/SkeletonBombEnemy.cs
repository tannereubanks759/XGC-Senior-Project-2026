using RayFire;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class SkeletonBombEnemy : MonoBehaviour
{
    [Header("Curse")]
    public bool isCursed = false;
    public int curseDamageMult = 1;
    public float curseSpeedMult = 1f;

    public enum State
    {
        Idle,
        Patrol,
        Chase,
        Fusing,
        Stunned,
        Dead
    }

    [Header("References")]
    [Tooltip("Optional animator for movement / aggro / hit.")]
    public Animator animator;

    [Tooltip("Optional. If null, this will use GetComponent<NavMeshAgent>().")]
    public NavMeshAgent agent;

    [Tooltip("Where the enemy considers 'eyes' to be for vision checks.")]
    public Transform eyePoint;

    [Tooltip("Optional. If not set, the enemy will auto-find the player.")]
    public Transform target;

    [Tooltip("Assign the bomb object/script here.")]
    public ExplosiveBarrel bombExplosive;

    [Header("Player Auto-Find")]
    [Tooltip("Tag on the player root GameObject.")]
    public string playerTag = "Player";

    [Tooltip("How often we try to find the player if target is missing.")]
    public float findTargetInterval = 0.5f;

    [Header("Patrol")]
    public bool startPatrolling = true;

    [Tooltip("How far from spawn point the skeleton is allowed to patrol.")]
    public float patrolRadius = 12f;

    [Tooltip("How close we need to be to consider we reached the patrol destination.")]
    public float patrolArriveTolerance = 1.1f;

    [Tooltip("How long to wait at a patrol point.")]
    public Vector2 patrolStallTimeRange = new Vector2(0.5f, 2.0f);

    [Tooltip("How many attempts to find a valid patrol point.")]
    public int patrolFindMaxAttempts = 12;

    [Tooltip("NavMeshAgent speed while patrolling.")]
    public float patrolSpeed = 2.0f;

    [Tooltip("NavMeshAgent acceleration while patrolling.")]
    public float patrolAcceleration = 8.0f;

    [Tooltip("Stopping distance while patrolling.")]
    public float patrolStoppingDistance = 0.4f;

    [Header("Perception")]
    [Tooltip("How far the skeleton can see the player.")]
    public float visionRange = 20f;

    [Range(10f, 180f)]
    [Tooltip("Field of view angle in degrees.")]
    public float visionFov = 120f;

    [Tooltip("Within this range we ignore FOV, but still require line of sight.")]
    public float closeAwarenessRange = 8.0f;

    [Tooltip("If very close, skeleton can detect even if not facing, still requires LOS.")]
    public float autoDetectRange = 3.0f;

    [Tooltip("Thickness of the vision cast.")]
    public float sightThickness = 0.12f;

    [Tooltip("Eye height if eyePoint is null.")]
    public float fallbackEyeHeight = 1.6f;

    [Tooltip("Where on the target to aim the vision check.")]
    public float targetAimHeight = 1.4f;

    [Tooltip("Only world geometry should block vision.")]
    public LayerMask occlusionMask;

    [Tooltip("Optional debug rays for vision.")]
    public bool debugVision = false;

    [Header("Chase / Explosion")]
    [Tooltip("Speed while chasing.")]
    public float chaseSpeed = 4.0f;

    [Tooltip("Acceleration while chasing.")]
    public float chaseAcceleration = 18f;

    [Tooltip("Distance to stop from the player while holding the bomb.")]
    public float chaseStoppingDistance = 1.5f;

    [Tooltip("Time after spotting the player before auto-exploding.")]
    public float explodeDelay = 3.0f;

    [Tooltip("Optional: explode immediately once within this distance. Set 0 to disable.")]
    public float instantExplodeDistance = 1.25f;

    [Header("Movement")]
    [Tooltip("How quickly the body rotates toward movement / target direction.")]
    public float turnSpeed = 10f;

    [Header("Damage / Stun")]
    public float maxHealth = 100f;
    public float stunDuration = 0.6f;

    [Header("Animation Params")]
    [Tooltip("Animator float param for movement speed.")]
    public string animSpeedParam = "Speed";

    [Tooltip("Animator bool param for aggro.")]
    public string animAggroBool = "Aggro";

    [Tooltip("Animator trigger for getting hit/stunned.")]
    public string animHitTrigger = "Hit";

    [Header("Tuning")]
    [Tooltip("How often detection checks happen.")]
    public float senseInterval = 0.12f;

    [Tooltip("How often the chase destination updates.")]
    public float chaseRepathInterval = 0.20f;

    public AudioSource fuse;

    private State state;
    private float health;

    private float nextSenseTime;
    private float nextRepathTime;
    private float nextFindTargetTime;
    private float stunEndTime = -999f;

    private Coroutine stateRoutine;
    private Vector3 spawnPos;
    private readonly RaycastHit[] sightHits = new RaycastHit[24];

    private bool fuseStarted = false;
    private float explodeAtTime = -1f;
    private bool hasExploded = false;
    private bool registeredAsHostile = false;
    private void OnDisable()
    {
        if (registeredAsHostile)
        {
            CombatTracker.Instance?.UnregisterHostile(this);
            registeredAsHostile = false;
        }
    }
    private void UpdateCombatRegistration(bool shouldBeHostile)
    {
        if (shouldBeHostile && !registeredAsHostile)
        {
            registeredAsHostile = true;
            CombatTracker.Instance?.RegisterHostile(this);
        }
        else if (!shouldBeHostile && registeredAsHostile)
        {
            registeredAsHostile = false;
            CombatTracker.Instance?.UnregisterHostile(this);
        }
    }
    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        eyePoint = transform;
    }

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        health = maxHealth;
        spawnPos = transform.position;
        nextSenseTime = Time.time + 2f;
    }

    private IEnumerator Start()
    {
        yield return null;

        //TrySnapToNavMesh();

        if (!agent || !agent.enabled || !agent.isOnNavMesh)
            yield break;

        SetState(startPatrolling ? State.Patrol : State.Idle);
    }

    private void Update()
    {
        if (state == State.Dead) return;

        AcquireTargetIfNeeded(false);

        if (!agent || !agent.enabled || !agent.isOnNavMesh) return;

        if (Time.time >= nextSenseTime)
        {
            nextSenseTime = Time.time + senseInterval;
            Sense();
        }

        UpdateAnimatorLocomotion();
        FaceTargetOrMovement();
        bool hostile =
        state != State.Idle &&
        state != State.Patrol &&
        state != State.Dead;

        UpdateCombatRegistration(hostile);
        if (fuseStarted && !hasExploded && Time.time >= explodeAtTime)
        {
            ExplodeNow();
        }

        if (state == State.Chase && target != null && instantExplodeDistance > 0f && !hasExploded)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= instantExplodeDistance)
            {
                ExplodeNow();
            }
        }
    }

    private void TrySnapToNavMesh()
    {
        if (!agent || !agent.enabled) return;
        if (agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, agent.areaMask))
        {
            agent.Warp(hit.position);
        }
    }

    private void AcquireTargetIfNeeded(bool force)
    {
        if (target != null) return;

        if (!force)
        {
            if (Time.time < nextFindTargetTime) return;
            nextFindTargetTime = Time.time + findTargetInterval;
        }

        if (string.IsNullOrEmpty(playerTag)) return;

        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
        {
            target = go.transform;
        }
    }

    private void Sense()
    {
        if (!target) return;

        bool detected = CanSeeTarget(out _);

        if (detected)
        {
            if (!fuseStarted)
            {
                fuseStarted = true;
                explodeAtTime = Time.time + explodeDelay;

                if (fuse != null && !fuse.isPlaying)
                {
                    fuse.Play();
                }
            }

            if (state == State.Idle || state == State.Patrol)
            {
                SetState(State.Chase);
            }
        }
        else
        {
            if (state == State.Chase && !fuseStarted)
            {
                SetState(startPatrolling ? State.Patrol : State.Idle);
            }
        }
    }

    private bool CanSeeTarget(out Vector3 seenPos)
    {
        seenPos = Vector3.zero;
        if (!target) return false;

        Vector3 origin = eyePoint ? eyePoint.position : (transform.position + Vector3.up * fallbackEyeHeight);
        origin += transform.up * 0.05f + transform.forward * 0.12f;

        Vector3 aim = target.position + Vector3.up * targetAimHeight;
        Vector3 to = aim - origin;

        float dist = to.magnitude;
        if (dist <= 0.001f) return true;
        if (dist > visionRange) return false;

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
                inFov = angle <= (visionFov * 0.5f);
            }
        }

        if (!inFov) return false;

        Vector3 dir = to / dist;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            Mathf.Max(0.01f, sightThickness),
            dir,
            sightHits,
            dist,
            occlusionMask,
            QueryTriggerInteraction.Ignore
        );

        bool blocked = false;
        RaycastHit closestBlock = default;
        float closest = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = sightHits[i];
            Collider col = hit.collider;
            if (!col) continue;

            if (col.transform.IsChildOf(transform)) continue;

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

        if (blocked) return false;

        seenPos = target.position;
        return true;
    }

    private void SetState(State newState)
    {
        if (state == newState) return;

        state = newState;

        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        stateRoutine = StartCoroutine(RunState(state));
    }

    private IEnumerator RunState(State currentState)
    {
        SetAnimatorAggro(currentState == State.Chase || currentState == State.Fusing);

        switch (currentState)
        {
            case State.Idle:
                agent.isStopped = true;
                yield break;

            case State.Patrol:
                yield return PatrolLoop();
                yield break;

            case State.Chase:
                yield return ChaseLoop();
                yield break;

            case State.Fusing:
                yield return FuseLoop();
                yield break;

            case State.Stunned:
                yield return StunnedLoop();
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

        while (state == State.Patrol)
        {
            if (!agent.isOnNavMesh)
            {
                yield return null;
                continue;
            }

            if (!TryGetRandomPatrolDestination(out Vector3 destination))
            {
                agent.isStopped = true;
                yield return new WaitForSeconds(Random.Range(patrolStallTimeRange.x, patrolStallTimeRange.y));
                agent.isStopped = false;
                continue;
            }

            agent.SetDestination(destination);

            while (state == State.Patrol && agent.pathPending)
                yield return null;

            while (state == State.Patrol)
            {
                float arriveDist = Mathf.Max(patrolArriveTolerance, agent.stoppingDistance + 0.05f);

                if (!agent.pathPending &&
                    agent.remainingDistance <= arriveDist &&
                    (!agent.hasPath || agent.velocity.sqrMagnitude < 0.02f))
                {
                    break;
                }

                yield return null;
            }

            if (state != State.Patrol) yield break;

            agent.isStopped = true;
            yield return new WaitForSeconds(Random.Range(patrolStallTimeRange.x, patrolStallTimeRange.y));
            agent.isStopped = false;
        }
    }

    private IEnumerator ChaseLoop()
    {
        agent.isStopped = false;
        agent.speed = isCursed ? chaseSpeed * Mathf.Clamp(curseSpeedMult, 0.05f, 1f) : chaseSpeed;
        agent.acceleration = isCursed ? chaseAcceleration * Mathf.Clamp(curseSpeedMult, 0.05f, 1f) : chaseAcceleration;
        agent.stoppingDistance = chaseStoppingDistance;

        while (state == State.Chase)
        {
            if (!target)
            {
                SetState(startPatrolling ? State.Patrol : State.Idle);
                yield break;
            }

            if (Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + chaseRepathInterval;
                if (agent.isOnNavMesh)
                    agent.SetDestination(target.position);
            }

            yield return null;
        }
    }

    private IEnumerator FuseLoop()
    {
        agent.isStopped = true;

        
        while (state == State.Fusing && !hasExploded)
        {
            
            if (Time.time >= explodeAtTime)
            {
                ExplodeNow();
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator StunnedLoop()
    {
        agent.isStopped = true;

        if (stunEndTime < Time.time)
            stunEndTime = Time.time + Mathf.Max(0.05f, stunDuration);

        while (state == State.Stunned && Time.time < stunEndTime)
            yield return null;

        if (state == State.Stunned)
        {
            if (target != null)
                SetState(State.Chase);
            else
                SetState(startPatrolling ? State.Patrol : State.Idle);
        }
    }

    private void ExplodeNow()
    {

        if (hasExploded || state == State.Dead) return;

        hasExploded = true;

        this.GetComponent<DamageRef>().TakeDamage(100);

        if (bombExplosive != null)
        {
            // If your ExplosiveBarrel method is lowercase, change this to bombExplosive.explode();
            bombExplosive.gameObject.transform.parent = null;
            bombExplosive.Explode();
        }
        else
        {
            Debug.LogWarning($"{name}: SkeletonBombEnemy has no ExplosiveBarrel assigned.");
        }
        
    }

    private void FaceTargetOrMovement()
    {
        if (!agent) return;

        Vector3 lookDir = Vector3.zero;

        if (target != null && (state == State.Chase || fuseStarted))
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

    private void UpdateAnimatorLocomotion()
    {
        if (!animator || !agent) return;

        float speed01 = Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(0.01f, chaseSpeed));
        animator.SetFloat(animSpeedParam, speed01, 0.12f, Time.deltaTime);
    }

    private void SetAnimatorAggro(bool aggro)
    {
        if (!animator || string.IsNullOrEmpty(animAggroBool)) return;
        animator.SetBool(animAggroBool, aggro);
    }

    public void ApplyDamage(float amount, bool canStun = true)
    {
        if (state == State.Dead) return;

        amount *= Mathf.Max(1, curseDamageMult);
        health -= amount;

        if (health <= 0f)
        {
            Die();
            return;
        }

        if (canStun)
        {
            if (animator && !string.IsNullOrEmpty(animHitTrigger))
                animator.SetTrigger(animHitTrigger);

            stunEndTime = Time.time + Mathf.Max(0.05f, stunDuration);
            SetState(State.Stunned);
        }
    }

    private void Die()
    {
        if (state == State.Dead) return;

        health = 0f;
        state = State.Dead;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        RayfireRigid rf = GetComponentInChildren<RayfireRigid>();
        if (rf != null) rf.Demolish();

        Destroy(gameObject);
    }

    private bool TryGetRandomPatrolDestination(out Vector3 destination)
    {
        destination = spawnPos;

        if (!agent || !agent.enabled || !agent.isOnNavMesh)
            return false;

        for (int i = 0; i < Mathf.Max(1, patrolFindMaxAttempts); i++)
        {
            Vector2 random = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = spawnPos + new Vector3(random.x, 0f, random.y);

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

    public float GetHealth()
    {
        return health;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (eyePoint)
            Gizmos.DrawWireSphere(eyePoint.position, visionRange);
        else
            Gizmos.DrawWireSphere(transform.position + Vector3.up * fallbackEyeHeight, visionRange);

        Gizmos.color = Color.red;
        if (instantExplodeDistance > 0f)
            Gizmos.DrawWireSphere(transform.position, instantExplodeDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}