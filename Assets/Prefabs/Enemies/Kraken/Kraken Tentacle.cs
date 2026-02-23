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

    [Header("Go Up Delay")]
    [Tooltip("When GoUp() is called, wait this long before starting the return/rise movement.")]
    [Min(0f)] public float goUpDelay = 0.35f;
    public Collider tentacleCol;

    private bool isReturning = false;
    private float dropCountdown = 0f;
    private float nextAttackAllowedTime = 0f;
    private float dropElapsed = 0f;

    private Vector2 dropLockedXZ;
    private bool wasInDangerArea = false;

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

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        // Ensure ChainIK is targeting THIS transform
        if (chainIK != null)
        {
            var data = chainIK.data;
            if (data.target != transform)
            {
                data.target = transform;
                chainIK.data = data;
            }
        }

        // Start aligned to idle target so there's no initial snap
        TeleportToIdleTargetIfAvailable();

        ResetDropCountdown();
        wasInDangerArea = playerInDangerArea;

        if (!playerInDangerArea) blendingToIdle = false;
    }

    void Update()
    {
        // If dead, ignore all other behavior and just move to death pose
        if (isDead)
        {
            UpdateDeathBlend();
            return;
        }

        // Detect enter/exit
        if (playerInDangerArea != wasInDangerArea)
        {
            if (playerInDangerArea)
            {
                // ENTER: cancel any blend, teleport to idle target to avoid snap, then run logic
                blendingToIdle = false;
                TeleportToIdleTargetIfAvailable();

                isDropping = false;
                isReturning = false;
                dropElapsed = 0f;

                // cancel any pending GoUp delay
                CancelPendingGoUp();

                ResetDropCountdown();
            }
            else
            {
                // EXIT: cancel attacks and smoothly blend back to idle target
                CancelAttackState();
                CancelPendingGoUp();
                BeginBlendToIdle();
            }

            wasInDangerArea = playerInDangerArea;
        }

        // OUT OF RANGE: smoothly blend to idle, then lock-follow idle every frame
        if (!playerInDangerArea)
        {
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

        // IN RANGE: run attack logic
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (player == null) return;

        if (isDropping)
        {
            dropElapsed += Time.deltaTime;

            float targetY = player.position.y + dropToHeight;
            Vector3 dropPos = new Vector3(dropLockedXZ.x, targetY, dropLockedXZ.y);
            MoveTargetTowards(dropPos, dropSpeed);

            if (dropElapsed >= maxDropTime)
            {
                isDropping = false;
                GoUp(); // now delayed
            }
            return;
        }

        // While waiting for GoUp delay, do nothing movement-wise (prevents weird re-following)
        if (pendingGoUp)
        {
            return;
        }

        if (isReturning)
        {
            Vector3 hoverPos = GetHoverPosition();
            MoveTargetTowards(hoverPos, riseSpeed);

            if ((transform.position - hoverPos).sqrMagnitude <= 0.05f * 0.05f)
            {
                isReturning = false;
                ResetDropCountdown();
            }
            return;
        }

        Vector3 desiredHover = GetHoverPosition();
        MoveTargetTowards(desiredHover, followSpeed);

        bool cooldownReady = Time.time >= nextAttackAllowedTime;
        bool aboveEnough = (transform.position.y - player.position.y) >= requiredAbovePlayerToDrop;

        if (cooldownReady && aboveEnough)
        {
            dropCountdown -= Time.deltaTime;
            if (dropCountdown <= 0f)
                StartDrop();
        }
        else
        {
            ResetDropCountdown();
        }
    }

    public void GoUp()
    {
        nextAttackAllowedTime = Time.time + attackCooldown;

        // Start delayed return
        if (goUpDelay <= 0f)
        {
            
            BeginReturnNow();
            return;
        }

        // restart the delay if called again
        CancelPendingGoUp();
        goUpDelayCo = StartCoroutine(GoUpDelayRoutine(goUpDelay));
    }

    private IEnumerator GoUpDelayRoutine(float delay)
    {
        pendingGoUp = true;

        // lock out drops during delay
        isDropping = false;
        isReturning = false;
        dropElapsed = 0f;
        dropCountdown = Mathf.Infinity;

        float t = 0f;
        while (t < delay)
        {
            // If something invalidates the delay, stop cleanly
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

    private void StartDrop()
    {
        isDropping = true;
        dropElapsed = 0f;
        tentacleCol.enabled = true;
        dropLockedXZ = new Vector2(transform.position.x, transform.position.z);
        dropCountdown = Mathf.Infinity;
    }

    private void CancelAttackState()
    {
        isDropping = false;
        isReturning = false;
        dropElapsed = 0f;
        dropCountdown = 0f;
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

    // =========================
    // Death
    // =========================
    // Call this to kill the tentacle: it stops all logic and moves to the deathTarget pose.
    public void Death()
    {
        if (isDead) return;

        isDead = true;

        // Stop any current behavior
        CancelAttackState();
        CancelPendingGoUp();
        blendingToIdle = false;
        playerInDangerArea = false;

        // If no death target, just freeze where it is
        if (deathTarget == null)
        {
            blendingToDeath = false;
            return;
        }

        // Begin blending to death pose
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
        }
    }
}