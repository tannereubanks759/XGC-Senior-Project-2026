using UnityEngine;
using UnityEngine.Animations.Rigging;

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

        // If we start out of range, begin in "already idle" mode (no blend needed)
        if (!playerInDangerArea) blendingToIdle = false;
    }

    void Update()
    {
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
                ResetDropCountdown();
            }
            else
            {
                // EXIT: cancel attacks and smoothly blend back to idle target
                CancelAttackState();
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

                    // Note: target is moving (animation), which is fine — we blend toward its current pose.
                    transform.position = Vector3.Lerp(blendStartPos, idleAnimatedTarget.position, eased);
                    transform.rotation = Quaternion.Slerp(blendStartRot, idleAnimatedTarget.rotation, eased);

                    if (t >= 1f)
                    {
                        blendingToIdle = false;
                        // Now that we've arrived, snap-follow each frame (but now it's not harsh).
                        transform.position = idleAnimatedTarget.position;
                        transform.rotation = idleAnimatedTarget.rotation;
                    }
                }
                else
                {
                    // No blend requested, or finished blending
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
                GoUp();
            }
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

        isReturning = true;
        dropElapsed = 0f;

        ResetDropCountdown();
    }

    private void StartDrop()
    {
        isDropping = true;
        dropElapsed = 0f;

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
}
