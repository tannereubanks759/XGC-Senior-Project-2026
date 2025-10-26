using UnityEngine;
using System.Collections;

public class BossKnockback : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody rb;
    public Camera cam;
    public FirstPersonController controller;

    [Header("Tuning (Δspeed in m/s)")]
    public float dodgeSpeedChange = 8f;     // base burst strength
    [Tooltip("Legacy lock time if you don't want to lock for full burst. Ignored when lockForFullBurst=true.")]
    public float lockDuration = 0.15f;      // fallback lock duration
    public float maxHorizontalSpeed = 14f;  // hard cap on horizontal speed

    public float dodgeDuration = 0.14f;     // seconds to deliver dodge Δv

    // Maps 0→1 time to 0→1 of the total Δv delivered.
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Locking Options")]
    [Tooltip("If true, player input is locked for the entire burst duration; otherwise uses lockDuration.")]
    public bool lockForFullBurst = true;
    [Tooltip("If true, clears horizontal velocity before applying the burst so the dodge has consistent speed.")]
    public bool zeroHorizontalBeforeBurst = true;

    public DodgeDash dodgeScript;

    private Coroutine lockRoutine;
    private Coroutine burstRoutine;

    [Range(-60f, 60f)]
    public float upAngleDeg = 12f;

    // NEW >>> Yank multiplier for when the anchor drags the player back to the boss
    [Header("Return Yank Settings")]
    [Tooltip("How much stronger the pull is on the anchor RETURN vs the initial hit.\n1 = same strength, 2 = twice as hard, etc.")]
    public float returnYankMultiplier = 2.5f;

    [Tooltip("Extra time to keep movement locked on the yank (seconds added).")]
    public float extraLockOnYank = 0.1f; // optional stickiness

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
        controller = GetComponent<FirstPersonController>();
    }

    void Start()
    {
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        dodgeScript = GetComponentInChildren<DodgeDash>();
    }

    // PUBLIC normal knockback (respects isDodging, this is used on initial anchor hit push-away)
    public void KnockbackPlayer(Vector3 direction)
    {
        DoKnockback(
            direction,
            respectDodgeState: true,
            strengthMult: 1f,
            lockBonus: 0f
        );
    }

    // PUBLIC yank knockback (ignores isDodging, used when anchor reels back in)
    public void ForceKnockbackPlayer(Vector3 direction)
    {
        DoKnockback(
            direction,
            respectDodgeState: false,
            strengthMult: returnYankMultiplier,   // NEW >>> stronger burst
            lockBonus: extraLockOnYank            // NEW >>> slightly longer lock
        );
    }

    // Shared core logic
    private void DoKnockback(Vector3 direction, bool respectDodgeState, float strengthMult, float lockBonus)
    {
        Debug.Log("[BossKnockback] Knockback (respectDodge=" + respectDodgeState + ", mult=" + strengthMult + ")");

        // Optionally skip if we're mid-dodge/burst already (first hit only)
        if (respectDodgeState && dodgeScript != null && dodgeScript.isDodging)
        {
            Debug.Log("[BossKnockback] Skipped because player is dodging and this is a normal knockback.");
            return;
        }

        // Horizontal-only base dir
        Vector3 dir = direction;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f)
        {
            Debug.LogWarning("[BossKnockback] Direction too small, abort.");
            return;
        }
        dir.Normalize();

        // Tilt slightly upward so it pops you instead of just sliding
        float angle = Mathf.Clamp(upAngleDeg, -89f, 89f);
        Vector3 right = Vector3.Cross(Vector3.up, dir);
        if (right.sqrMagnitude < 1e-6f) right = Vector3.right;

        Vector3 tilted = (Quaternion.AngleAxis(angle, right) * dir).normalized;

        // Scale burst and lock duration for yank
        float finalBurstSpeedChange = dodgeSpeedChange * Mathf.Max(0.01f, strengthMult);
        float finalDuration = dodgeDuration; // keep duration the same so the feel is still snappy
        float finalLockTime = (lockForFullBurst ? dodgeDuration : lockDuration) + lockBonus;

        StartBurst(tilted, finalBurstSpeedChange, finalDuration);
        LockController(finalLockTime);
    }

    void StartBurst(Vector3 dir, float speedChange, float duration)
    {
        if (burstRoutine != null) StopCoroutine(burstRoutine);

        // Optional horizontal reset first
        if (zeroHorizontalBeforeBurst)
        {
            Vector3 v = GetVel();
            SetVel(new Vector3(0f, v.y, 0f));
        }

        burstRoutine = StartCoroutine(AccelerateBurst(dir, speedChange, duration));
    }

    IEnumerator AccelerateBurst(Vector3 dir, float speedChange, float duration)
    {
        duration = Mathf.Max(0.0001f, duration);
        float t = 0f;

        // figure out where we are now, how fast along that direction, and where we want to end up
        Vector3 v0 = GetVel();
        Vector3 v0H = new Vector3(v0.x, 0f, v0.z);
        float startAlong = Vector3.Dot(v0H, dir);

        float targetAlong = Mathf.Clamp(
            startAlong + speedChange,
            -maxHorizontalSpeed,
            maxHorizontalSpeed
        );

        float prevK = speedCurve.Evaluate(0f);

        while (t < duration)
        {
            yield return new WaitForFixedUpdate();
            float dt = Time.fixedDeltaTime;
            t = Mathf.Min(t + dt, duration);

            float k = speedCurve.Evaluate(t / duration); // 0→1 curve progress
            float desiredDeltaThisStep = (targetAlong - startAlong) * (k - prevK);
            prevK = k;

            // recompute to prevent overshoot
            Vector3 vCur = GetVel();
            Vector3 vCurH = new Vector3(vCur.x, 0f, vCur.z);
            float curAlong = Vector3.Dot(vCurH, dir);
            float remain = targetAlong - curAlong;

            float stepDeltaV = 0f;
            if (Mathf.Sign(desiredDeltaThisStep) == Mathf.Sign(remain))
                stepDeltaV = Mathf.Clamp(desiredDeltaThisStep, -Mathf.Abs(remain), Mathf.Abs(remain));

            if (dt > 0f && stepDeltaV != 0f)
            {
                // a = Δv / Δt
                Vector3 a = (dir * stepDeltaV) / dt;
                a.y = 0f;
                rb.AddForce(a, ForceMode.Acceleration);
            }

            LimitHorizontalSpeed();
        }

        burstRoutine = null;
    }

    void LimitHorizontalSpeed()
    {
        Vector3 v = GetVel();
        Vector3 vH = new Vector3(v.x, 0f, v.z);
        float h = vH.magnitude;
        if (h > maxHorizontalSpeed)
        {
            Vector3 vHClamped = vH.normalized * maxHorizontalSpeed;
            SetVel(new Vector3(vHClamped.x, v.y, vHClamped.z));
        }
    }

    void LockController(float seconds)
    {
        if (controller == null) return;
        if (lockRoutine != null) StopCoroutine(lockRoutine);
        lockRoutine = StartCoroutine(LockRoutine(seconds));
    }

    IEnumerator LockRoutine(float seconds)
    {
        bool prev = controller.playerCanMove;
        controller.playerCanMove = false;

        float t = 0f;
        while (t < seconds)
        {
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }

        controller.playerCanMove = prev;
        lockRoutine = null;
    }

    // NOTE: You said you're using rb.linearVelocity.
    // If that's an extension property in your project that wraps rb.velocity, cool.
    // If not, swap to rb.velocity / rb.velocity = ...
    Vector3 GetVel() => rb.linearVelocity;
    void SetVel(Vector3 v) => rb.linearVelocity = v;
}
