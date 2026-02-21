using UnityEngine;
using System.Collections;

public class BossKnockback : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody rb;
    public Camera cam;
    public FirstPersonController controller;

    [Header("Tuning (Δspeed in m/s)")]
    public float dodgeSpeedChange = 8f;
    [Tooltip("Legacy lock time if you don't want to lock for full burst. Ignored when lockForFullBurst=true.")]
    public float lockDuration = 0.15f;
    public float maxHorizontalSpeed = 14f;

    public float dodgeDuration = 0.14f;

    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Locking Options")]
    [Tooltip("If true, player input is locked for the entire burst duration; otherwise uses lockDuration.")]
    public bool lockForFullBurst = true;
    [Tooltip("If true, clears horizontal velocity before applying the burst so the dodge has consistent speed.")]
    public bool zeroHorizontalBeforeBurst = true;

    public DodgeDash dodgeScript;

    private Coroutine burstRoutine;

    [Range(-60f, 60f)]
    public float upAngleDeg = 12f;

    [Header("Return Yank Settings")]
    public float returnYankMultiplier = 2.5f;
    public float extraLockOnYank = 0.1f;

    // ---------- SAFE LOCKING ----------
    private int _moveLockCount = 0;
    private bool _capturedCanMove;
    private bool _hasCapturedBase = false;
    // ----------------------------------

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

    void OnDisable()
    {
        // Safety: never leave the player permanently locked if this component gets disabled/destroyed.
        ForceUnlockMovement();
    }

    void OnDestroy()
    {
        ForceUnlockMovement();
    }

    // PUBLIC normal knockback
    public void KnockbackPlayer(Vector3 direction)
    {
        DoKnockback(direction, respectDodgeState: true, strengthMult: 1f, lockBonus: 0f);
    }

    // PUBLIC yank knockback
    public void ForceKnockbackPlayer(Vector3 direction)
    {
        DoKnockback(direction, respectDodgeState: false, strengthMult: returnYankMultiplier, lockBonus: extraLockOnYank);
    }

    private void DoKnockback(Vector3 direction, bool respectDodgeState, float strengthMult, float lockBonus)
    {
        Debug.Log("[BossKnockback] Knockback (respectDodge=" + respectDodgeState + ", mult=" + strengthMult + ")");

        if (respectDodgeState && dodgeScript != null && dodgeScript.isDodging)
        {
            Debug.Log("[BossKnockback] Skipped because player is dodging and this is a normal knockback.");
            return;
        }

        Vector3 dir = direction;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f)
        {
            Debug.LogWarning("[BossKnockback] Direction too small, abort.");
            return;
        }
        dir.Normalize();

        float angle = Mathf.Clamp(upAngleDeg, -89f, 89f);
        Vector3 right = Vector3.Cross(Vector3.up, dir);
        if (right.sqrMagnitude < 1e-6f) right = Vector3.right;

        Vector3 tilted = (Quaternion.AngleAxis(angle, right) * dir).normalized;

        float finalBurstSpeedChange = dodgeSpeedChange * Mathf.Max(0.01f, strengthMult);
        float finalDuration = dodgeDuration;
        float finalLockTime = (lockForFullBurst ? dodgeDuration : lockDuration) + lockBonus;

        StartBurst(tilted, finalBurstSpeedChange, finalDuration);

        // SAFE: add a timed lock instead of canceling/overwriting a coroutine that might never restore.
        AddMovementLock(finalLockTime);
    }

    void StartBurst(Vector3 dir, float speedChange, float duration)
    {
        if (burstRoutine != null) StopCoroutine(burstRoutine);

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

        Vector3 v0 = GetVel();
        Vector3 v0H = new Vector3(v0.x, 0f, v0.z);
        float startAlong = Vector3.Dot(v0H, dir);

        float targetAlong = Mathf.Clamp(startAlong + speedChange, -maxHorizontalSpeed, maxHorizontalSpeed);

        float prevK = speedCurve.Evaluate(0f);

        while (t < duration)
        {
            yield return new WaitForFixedUpdate();
            float dt = Time.fixedDeltaTime;
            t = Mathf.Min(t + dt, duration);

            float k = speedCurve.Evaluate(t / duration);
            float desiredDeltaThisStep = (targetAlong - startAlong) * (k - prevK);
            prevK = k;

            Vector3 vCur = GetVel();
            Vector3 vCurH = new Vector3(vCur.x, 0f, vCur.z);
            float curAlong = Vector3.Dot(vCurH, dir);
            float remain = targetAlong - curAlong;

            float stepDeltaV = 0f;
            if (Mathf.Sign(desiredDeltaThisStep) == Mathf.Sign(remain))
                stepDeltaV = Mathf.Clamp(desiredDeltaThisStep, -Mathf.Abs(remain), Mathf.Abs(remain));

            if (dt > 0f && stepDeltaV != 0f)
            {
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

    // ---------- SAFE LOCKING IMPLEMENTATION ----------
    void AddMovementLock(float seconds)
    {
        if (controller == null) return;

        seconds = Mathf.Max(0f, seconds);

        // Capture the "base" state once (so if some other system *permanently* disables movement,
        // we restore back to that, not always to true).
        if (!_hasCapturedBase)
        {
            _capturedCanMove = controller.playerCanMove;
            _hasCapturedBase = true;
        }

        _moveLockCount++;
        controller.playerCanMove = false;

        // Each lock gets its own timer; no cancellation needed.
        StartCoroutine(RemoveMovementLockAfter(seconds));
    }

    IEnumerator RemoveMovementLockAfter(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }

        _moveLockCount = Mathf.Max(0, _moveLockCount - 1);

        if (controller != null && _moveLockCount == 0)
        {
            controller.playerCanMove = _capturedCanMove;
            _hasCapturedBase = false; // ready to capture again next time
        }
    }

    void ForceUnlockMovement()
    {
        if (controller == null) return;

        _moveLockCount = 0;
        controller.playerCanMove = true; // fail-safe
        _hasCapturedBase = false;
    }
    // -----------------------------------------------

    // NOTE: You said you're using rb.linearVelocity.
    Vector3 GetVel() => rb.linearVelocity;
    void SetVel(Vector3 v) => rb.linearVelocity = v;
}