using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;

public class KrakenTentacle : MonoBehaviour
{
    [Header("Rig Setup (Option B: never swap target)")]
    [Tooltip("The ChainIKConstraint on the tentacle rig. Its Target MUST be this transform (the object this script is on).")]
    public ChainIKConstraint chainIK;

    [Tooltip("The animated/idle target Transform (the one you keyframed/animated).")]
    public Transform idleAnimatedTarget;

    [Tooltip("While player is NOT in danger area, this scripted target will follow the idleAnimatedTarget.")]
    public bool followIdleTargetWhenOutOfRange = true;

    [Header("Idle Transition Smoothing")]
    [Tooltip("How long to blend back to the idleAnimatedTarget when the player leaves the danger area.")]
    public float blendToIdleTime = 0.35f;

    [Tooltip("Optional easing for the blend (0->1).")]
    public AnimationCurve blendToIdleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Death")]
    [Tooltip("Move the tentacle to this Transform when Death() is called.")]
    public Transform deathTarget;

    [Tooltip("How long it takes to blend to the death position/rotation.")]
    public float blendToDeathTime = 0.6f;

    [Tooltip("Optional easing for death blend (0->1).")]
    public AnimationCurve blendToDeathCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Trail")]
    [Tooltip("TrailRenderer used only during active attack motion.")]
    public TrailRenderer attackTrail;

    [Tooltip("Optional: clear old trail on new attack start so each strike starts fresh.")]
    public bool clearTrailOnAttackStart = false;

    [Header("Runtime")]
    public bool isDropping = false;

    [Tooltip("Player transform. If left null, the script will try to find an object tagged 'Player'.")]
    public Transform player;

    [Tooltip("Set by KrakenDangerArea.")]
    public bool playerInDangerArea = false;

    [Header("Water Surface (Hover Y Source)")]
    public Transform waterSurface;
    public float waterSurfaceY = 0f;
    public float hoverAboveWater = 6f;

    [Header("Follow Settings")]
    public Vector3 hoverOffset = Vector3.zero;
    public float followSpeed = 12f;
    public float riseSpeed = 12f;
    public bool matchPlayerXZ = true;

    [Header("Attack Settings")]
    public float dropToHeight = 1.6f;
    public float dropSpeed = 25f;
    public Vector2 timeBetweenDropsRange = new Vector2(0.75f, 2.5f);
    public float attackCooldown = 3.0f;
    public float maxDropTime = 2.0f;
    public float requiredAbovePlayerToDrop = 1.0f;

    [Header("Warmup (Wind-up before strike)")]
    [Min(0f)] public float warmupExtraHeight = 2.5f;

    [Tooltip("How long the wind-up takes (seconds).")]
    [Min(0.01f)] public float warmupDuration = 0.45f;

    [Tooltip("Curve used for warmup motion (0->1 time, 0->1 position).")]
    public AnimationCurve warmupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Optional small pause at the top of the warmup before dropping (seconds).")]
    [Min(0f)] public float warmupHoldTime = 0.10f;

    [Header("Triple Strike Attack (every 2 normal attacks)")]
    [Tooltip("After this many NORMAL attacks, do one triple strike.")]
    [Min(1)] public int tripleEveryNormalAttacks = 2;

    [Tooltip("How many slams in the triple strike.")]
    [Min(2)] public int tripleStrikes = 3;

    [Tooltip("Warmup duration for each slam in triple strike.")]
    [Min(0.01f)] public float tripleWarmupDuration = 0.18f;

    [Tooltip("Optional tiny hold at top per slam in triple strike.")]
    [Min(0f)] public float tripleWarmupHold = 0.02f;

    [Tooltip("Drop speed during triple strike.")]
    [Min(0.01f)] public float tripleDropSpeed = 34f;

    [Tooltip("Max time allowed for each slam drop in triple strike (failsafe).")]
    [Min(0.05f)] public float tripleMaxDropTime = 0.55f;

    [Tooltip("How fast it pops back up to apex between slams (seconds).")]
    [Min(0.01f)] public float triplePopUpDuration = 0.14f;

    [Tooltip("Curve for pop-up movement between slams.")]
    public AnimationCurve triplePopUpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Triple Visual Separation")]
    [Tooltip("How much higher than hover Y the triple strike apex goes (adds extra readability).")]
    [Min(0f)] public float tripleExtraApexHeight = 1.5f;

    [Tooltip("How close (meters) counts as 'reached' during slam/pop.")]
    [Min(0.001f)] public float reachEpsilon = 0.08f;

    [Header("Targeting")]
    [Tooltip("Lock slam XZ to the player's position at the moment the slam starts (normal + each triple slam).")]
    public bool lockToPlayerAtSlamStart = true;

    [Tooltip("Optional XZ offset from player when slamming (0,0 = dead center).")]
    public Vector2 slamXZOffset = Vector2.zero;

    [Header("Debug")]
    public bool debugLogs = true;

    [Tooltip("Shows current state in Inspector for quick sanity checks.")]
    public string debugState = "Idle";

    [Header("Go Up Delay")]
    [Tooltip("When GoUp() is called, wait this long before starting the return/rise movement.")]
    [Min(0f)] public float goUpDelay = 0.35f;

    public Collider tentacleCol;

    private bool isReturning = false;
    private float dropCountdown = 0f;
    private float nextAttackAllowedTime = 0f;
    private float dropElapsed = 0f;

    // Warmup state (normal attack)
    private bool isWarmingUp = false;
    private float warmupT = 0f;
    private float warmupHoldElapsed = 0f;

    private Vector2 dropLockedXZ;
    private bool wasInDangerArea = false;

    // Warmup lerp endpoints (normal attack)
    private Vector3 warmupStartPos;
    private Vector3 warmupApexPos;

    // Triple strike scheduling: counts normals since last triple (based on ATTACK START)
    private int normalsSinceTriple = 0;

    // Triple sequence state
    private bool sequenceActive = false;
    private Coroutine sequenceCo;

    // Blend-to-idle state
    private bool blendingToIdle = false;
    private float blendTimer = 0f;
    private Vector3 blendStartPos;
    private Quaternion blendStartRot;

    // Death state
    private bool isDead = false;
    private bool blendingToDeath = false;
    private float deathBlendTimer = 0f;
    private Vector3 deathBlendStartPos;
    private Quaternion deathBlendStartRot;

    // GoUp delay state
    private bool pendingGoUp = false;
    private Coroutine goUpDelayCo;

    // Trail/death carry state
    private bool carryTrailThroughDeath = false;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (chainIK != null)
        {
            var data = chainIK.data;
            if (data.target != transform)
            {
                data.target = transform;
                chainIK.data = data;
            }
        }

        TeleportToIdleTargetIfAvailable();

        ResetDropCountdown();
        wasInDangerArea = playerInDangerArea;

        if (!playerInDangerArea) blendingToIdle = false;

        if (tentacleCol) tentacleCol.enabled = false;

        SetTrailEmitting(false, false);
    }

    void Update()
    {
        if (isDead)
        {
            UpdateDeathBlend();
            return;
        }

        if (playerInDangerArea != wasInDangerArea)
        {
            if (playerInDangerArea)
            {
                blendingToIdle = false;
                TeleportToIdleTargetIfAvailable();

                CancelAttackState();
                isReturning = false;

                CancelPendingGoUp();
                ResetDropCountdown();
            }
            else
            {
                CancelAttackState();
                CancelPendingGoUp();
                BeginBlendToIdle();
            }

            wasInDangerArea = playerInDangerArea;
        }

        if (!playerInDangerArea)
        {
            debugState = "OutOfRange";

            if (followIdleTargetWhenOutOfRange && idleAnimatedTarget != null)
            {
                if (blendingToIdle && blendToIdleTime > 0f)
                {
                    blendTimer += Time.deltaTime;
                    float t = Mathf.Clamp01(blendTimer / blendToIdleTime);
                    float eased = (blendToIdleCurve != null) ? blendToIdleCurve.Evaluate(t) : t;

                    transform.position = Vector3.Lerp(blendStartPos, idleAnimatedTarget.position, eased);
                    transform.rotation = Quaternion.Slerp(blendStartRot, idleAnimatedTarget.rotation, eased);

                    if (t >= 1f)
                    {
                        blendingToIdle = false;
                        transform.position = idleAnimatedTarget.position;
                        transform.rotation = idleAnimatedTarget.rotation;
                    }
                }
                else
                {
                    transform.position = idleAnimatedTarget.position;
                    transform.rotation = idleAnimatedTarget.rotation;
                    blendingToIdle = false;
                }
            }
            return;
        }

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (player == null) return;

        if (sequenceActive)
        {
            debugState = "TripleSequence";
            return;
        }

        if (pendingGoUp)
        {
            debugState = "GoUpDelay";
            return;
        }

        if (isWarmingUp)
        {
            debugState = "Warmup";
            warmupT += Time.deltaTime;

            float dur = Mathf.Max(0.01f, warmupDuration);
            float t01 = Mathf.Clamp01(warmupT / dur);

            float eased = (warmupCurve != null) ? warmupCurve.Evaluate(t01) : t01;
            transform.position = Vector3.LerpUnclamped(warmupStartPos, warmupApexPos, eased);

            if (t01 >= 1f)
            {
                if (warmupHoldTime > 0f)
                {
                    warmupHoldElapsed += Time.deltaTime;
                    if (warmupHoldElapsed < warmupHoldTime)
                        return;
                }

                BeginStrikeDropNow();
            }
            return;
        }

        if (isDropping)
        {
            debugState = "NormalDrop";
            dropElapsed += Time.deltaTime;

            float targetY = player.position.y + dropToHeight;
            Vector3 dropPos = new Vector3(dropLockedXZ.x, targetY, dropLockedXZ.y);
            MoveTargetTowards(dropPos, dropSpeed);

            if (Mathf.Abs(transform.position.y - targetY) <= reachEpsilon || dropElapsed >= maxDropTime)
            {
                isDropping = false;
                if (tentacleCol) tentacleCol.enabled = false;

                OnAttackImpact();

                GoUp();
            }
            return;
        }

        if (isReturning)
        {
            debugState = "Returning";
            Vector3 hoverPos = GetHoverPosition();
            MoveTargetTowards(hoverPos, riseSpeed);

            if ((transform.position - hoverPos).sqrMagnitude <= 0.05f * 0.05f)
            {
                isReturning = false;
                ResetDropCountdown();
            }
            return;
        }

        debugState = "Following";
        Vector3 desiredHover = GetHoverPosition();
        MoveTargetTowards(desiredHover, followSpeed);

        bool cooldownReady = Time.time >= nextAttackAllowedTime;
        bool aboveEnough = (transform.position.y - player.position.y) >= requiredAbovePlayerToDrop;

        if (cooldownReady && aboveEnough)
        {
            dropCountdown -= Time.deltaTime;
            if (dropCountdown <= 0f)
            {
                if (normalsSinceTriple >= tripleEveryNormalAttacks)
                {
                    normalsSinceTriple = 0;
                    if (debugLogs) Debug.Log($"[{name}] TRIPLE STRIKE TRIGGERED (scheduled).");
                    StartTripleStrike();
                }
                else
                {
                    normalsSinceTriple++;
                    if (debugLogs) Debug.Log($"[{name}] Normal attack triggered. normalsSinceTriple={normalsSinceTriple}/{tripleEveryNormalAttacks}");
                    StartWarmup();
                }
            }
        }
        else
        {
            ResetDropCountdown();
        }
    }

    private void SetTrailEmitting(bool emit, bool clearTrail)
    {
        if (attackTrail == null) return;

        if (clearTrail)
            attackTrail.Clear();

        attackTrail.emitting = emit;
    }

    private void BeginAttackTrail()
    {
        SetTrailEmitting(true, clearTrailOnAttackStart);
    }

    private void EndAttackTrail()
    {
        SetTrailEmitting(false, false);
    }

    private void OnAttackImpact()
    {
        EndAttackTrail();
    }

    private Vector2 GetPlayerXZLocked()
    {
        if (player == null) return new Vector2(transform.position.x, transform.position.z);
        return new Vector2(player.position.x + slamXZOffset.x, player.position.z + slamXZOffset.y);
    }

    private float GetHoverY()
    {
        float waterY = waterSurface ? waterSurface.position.y : waterSurfaceY;
        return waterY + hoverAboveWater;
    }

    private Vector3 GetHoverPosition()
    {
        Vector3 p = player.position + hoverOffset;

        if (!matchPlayerXZ)
        {
            p.x = transform.position.x;
            p.z = transform.position.z;
        }

        p.y = GetHoverY();
        return p;
    }

    private void MoveTargetTowards(Vector3 targetPos, float speed)
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
    }

    private void ResetDropCountdown()
    {
        float min = Mathf.Max(0f, timeBetweenDropsRange.x);
        float max = Mathf.Max(min, timeBetweenDropsRange.y);
        dropCountdown = Random.Range(min, max);
    }

    public void GoUp()
    {
        nextAttackAllowedTime = Time.time + attackCooldown;

        if (goUpDelay <= 0f)
        {
            BeginReturnNow();
            return;
        }

        CancelPendingGoUp();
        goUpDelayCo = StartCoroutine(GoUpDelayRoutine(goUpDelay));
    }

    private IEnumerator GoUpDelayRoutine(float delay)
    {
        pendingGoUp = true;

        CancelAttackState(false);
        isReturning = false;
        dropCountdown = Mathf.Infinity;

        float t = 0f;
        while (t < delay)
        {
            if (isDead || !playerInDangerArea)
            {
                pendingGoUp = false;
                goUpDelayCo = null;
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        pendingGoUp = false;
        goUpDelayCo = null;

        BeginReturnNow();
    }

    private void BeginReturnNow()
    {
        isReturning = true;
        dropElapsed = 0f;
        ResetDropCountdown();
    }

    private void CancelPendingGoUp()
    {
        pendingGoUp = false;
        if (goUpDelayCo != null)
        {
            StopCoroutine(goUpDelayCo);
            goUpDelayCo = null;
        }
    }

    private void StartWarmup()
    {
        BeginAttackTrail();

        isWarmingUp = true;
        warmupT = 0f;
        warmupHoldElapsed = 0f;

        isDropping = false;
        isReturning = false;
        dropElapsed = 0f;

        if (tentacleCol) tentacleCol.enabled = false;

        dropLockedXZ = new Vector2(transform.position.x, transform.position.z);

        warmupStartPos = transform.position;

        float apexY = GetHoverY() + warmupExtraHeight;
        warmupApexPos = new Vector3(dropLockedXZ.x, apexY, dropLockedXZ.y);

        dropCountdown = Mathf.Infinity;
    }

    private void BeginStrikeDropNow()
    {
        isWarmingUp = false;

        if (lockToPlayerAtSlamStart)
            dropLockedXZ = GetPlayerXZLocked();

        isDropping = true;
        dropElapsed = 0f;

        if (tentacleCol) tentacleCol.enabled = true;

        dropCountdown = Mathf.Infinity;
    }

    private void StartTripleStrike()
    {
        CancelAttackState(false);
        CancelPendingGoUp();

        if (sequenceCo != null)
            StopCoroutine(sequenceCo);

        sequenceCo = StartCoroutine(TripleStrikeRoutine());
    }

    private IEnumerator TripleStrikeRoutine()
    {
        sequenceActive = true;
        debugState = "TripleSequence";

        if (tentacleCol) tentacleCol.enabled = false;
        dropCountdown = Mathf.Infinity;

        for (int i = 0; i < tripleStrikes; i++)
        {
            if (isDead || !playerInDangerArea || player == null)
                break;

            BeginAttackTrail();

            if (lockToPlayerAtSlamStart)
                dropLockedXZ = GetPlayerXZLocked();
            else
                dropLockedXZ = new Vector2(transform.position.x, transform.position.z);

            if (debugLogs) Debug.Log($"[{name}] Triple slam {i + 1}/{tripleStrikes}");

            float apexY = GetHoverY() + warmupExtraHeight + tripleExtraApexHeight;
            Vector3 apex = new Vector3(dropLockedXZ.x, apexY, dropLockedXZ.y);

            Vector3 start = transform.position;
            float windDur = Mathf.Max(0.01f, tripleWarmupDuration);
            float t = 0f;
            while (t < windDur)
            {
                if (isDead || !playerInDangerArea)
                {
                    CleanupTriple();
                    EndAttackTrail();
                    yield break;
                }

                float t01 = Mathf.Clamp01(t / windDur);
                float eased = (warmupCurve != null) ? warmupCurve.Evaluate(t01) : t01;
                transform.position = Vector3.LerpUnclamped(start, apex, eased);

                t += Time.deltaTime;
                yield return null;
            }
            transform.position = apex;

            if (tripleWarmupHold > 0f)
            {
                float hold = 0f;
                while (hold < tripleWarmupHold)
                {
                    if (isDead || !playerInDangerArea)
                    {
                        CleanupTriple();
                        EndAttackTrail();
                        yield break;
                    }

                    hold += Time.deltaTime;
                    yield return null;
                }
            }

            if (tentacleCol) tentacleCol.enabled = true;

            float slamTimer = 0f;
            while (slamTimer < tripleMaxDropTime)
            {
                if (isDead || !playerInDangerArea)
                {
                    if (tentacleCol) tentacleCol.enabled = false;
                    CleanupTriple();
                    EndAttackTrail();
                    yield break;
                }

                float slamY = player.position.y + dropToHeight;
                Vector3 slamPos = new Vector3(dropLockedXZ.x, slamY, dropLockedXZ.y);

                transform.position = Vector3.MoveTowards(transform.position, slamPos, tripleDropSpeed * Time.deltaTime);

                if (Mathf.Abs(transform.position.y - slamY) <= reachEpsilon)
                    break;

                slamTimer += Time.deltaTime;
                yield return null;
            }

            if (tentacleCol) tentacleCol.enabled = false;

            OnAttackImpact();

            yield return null;

            Vector3 popStart = transform.position;
            float popDur = Mathf.Max(0.01f, triplePopUpDuration);
            float popT = 0f;
            while (popT < popDur)
            {
                if (isDead || !playerInDangerArea)
                {
                    CleanupTriple();
                    EndAttackTrail();
                    yield break;
                }

                float t01 = Mathf.Clamp01(popT / popDur);
                float eased = (triplePopUpCurve != null) ? triplePopUpCurve.Evaluate(t01) : t01;
                transform.position = Vector3.LerpUnclamped(popStart, apex, eased);

                popT += Time.deltaTime;
                yield return null;
            }
            transform.position = apex;
        }

        nextAttackAllowedTime = Time.time + attackCooldown;

        pendingGoUp = false;
        if (goUpDelayCo != null)
        {
            StopCoroutine(goUpDelayCo);
            goUpDelayCo = null;
        }

        isReturning = true;

        CleanupTriple();
        ResetDropCountdown();
    }

    private void CleanupTriple()
    {
        sequenceActive = false;
        sequenceCo = null;
        debugState = "PostTriple";
    }

    private void CancelAttackState(bool stopTrail = true)
    {
        sequenceActive = false;
        if (sequenceCo != null)
        {
            StopCoroutine(sequenceCo);
            sequenceCo = null;
        }

        isWarmingUp = false;
        warmupT = 0f;
        warmupHoldElapsed = 0f;

        isDropping = false;
        dropElapsed = 0f;

        if (tentacleCol) tentacleCol.enabled = false;

        dropCountdown = 0f;

        if (stopTrail)
            EndAttackTrail();
    }

    private void BeginBlendToIdle()
    {
        if (idleAnimatedTarget == null || blendToIdleTime <= 0f)
        {
            blendingToIdle = false;
            return;
        }

        blendingToIdle = true;
        blendTimer = 0f;
        blendStartPos = transform.position;
        blendStartRot = transform.rotation;
    }

    private void TeleportToIdleTargetIfAvailable()
    {
        if (idleAnimatedTarget == null) return;
        transform.position = idleAnimatedTarget.position;
        transform.rotation = idleAnimatedTarget.rotation;
    }

    public void Death()
    {
        if (isDead) return;

        carryTrailThroughDeath =
            attackTrail != null &&
            (attackTrail.emitting || isWarmingUp || isDropping || sequenceActive);

        isDead = true;

        CancelAttackState(false);
        CancelPendingGoUp();
        blendingToIdle = false;
        playerInDangerArea = false;

        if (carryTrailThroughDeath && attackTrail != null)
            attackTrail.emitting = true;

        if (deathTarget == null)
        {
            blendingToDeath = false;
            EndAttackTrail();
            return;
        }

        blendingToDeath = true;
        deathBlendTimer = 0f;
        deathBlendStartPos = transform.position;
        deathBlendStartRot = transform.rotation;
    }

    private void UpdateDeathBlend()
    {
        if (!blendingToDeath || deathTarget == null)
            return;

        if (blendToDeathTime <= 0f)
        {
            transform.position = deathTarget.position;
            transform.rotation = deathTarget.rotation;
            blendingToDeath = false;
            EndAttackTrail();
            return;
        }

        deathBlendTimer += Time.deltaTime;
        float t = Mathf.Clamp01(deathBlendTimer / blendToDeathTime);
        float eased = (blendToDeathCurve != null) ? blendToDeathCurve.Evaluate(t) : t;

        transform.position = Vector3.Lerp(deathBlendStartPos, deathTarget.position, eased);
        transform.rotation = Quaternion.Slerp(deathBlendStartRot, deathTarget.rotation, eased);

        if (t >= 1f)
        {
            blendingToDeath = false;
            transform.position = deathTarget.position;
            transform.rotation = deathTarget.rotation;
            EndAttackTrail();
        }
    }
}