using JetBrains.Annotations;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class AnchorWeapon : MonoBehaviour
{
    public int AnchorDamage = 20;
    public float throwStrength = 10f;
    public float rotationDegrees = 1f;

    [Header("Return Settings")]
    public float returnPosSmoothTime = 0.12f;    // lower = snappier
    public float returnRotDegPerSec = 540f;      // max deg/sec toward boss hand
    public float finishPosEpsilon = 0.01f;       // meters
    public float finishRotEpsilonDeg = 0.5f;     // degrees

    [Header("Constraint")]
    [SerializeField] private int sourceIndex = 0; // ParentConstraint source index (usually 0)

    private Collider col;
    private Rigidbody rb;
    private ParentConstraint constraint;
    private PirateBossAI boss;

    private GameObject player;
    private bool isInAir = false;

    // Pose of anchor in boss hand
    private Vector3 _cachedTransOffset;
    private Vector3 _cachedRotOffsetEuler;
    private bool _haveCachedOffsets = false;

    // Transform of the boss hand (original ParentConstraint source)
    private Transform _cachedBossHand;

    // homing / return
    private bool _isReturning = false;
    private Vector3 _posVel;
    private Coroutine _returnCo;

    // rotation while in air
    private Quaternion _throwTargetRot;
    private bool _hasThrowTargetRot;

    // sticky state
    private bool _stuckToPlayer = false;

    // we cache this so we can yank them after recall
    private BossKnockback _latchedKnockback;
    private Transform _latchedTarget;

    void Start()
    {
        boss = GetComponentInParent<PirateBossAI>();
        isInAir = false;

        constraint = GetComponent<ParentConstraint>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        EnableCollider(false);

        // cache the original boss hand transform
        if (constraint != null && constraint.sourceCount > sourceIndex)
        {
            ConstraintSource src = constraint.GetSource(sourceIndex);
            _cachedBossHand = src.sourceTransform;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) // debug throw
        {
            Throw();
        }

        // spin toward the fixed throw target
        if (isInAir && _hasThrowTargetRot)
        {
            float maxDegreesThisFrame = rotationDegrees * Time.deltaTime;

            if (!rb.isKinematic)
            {
                Quaternion next = Quaternion.RotateTowards(rb.rotation, _throwTargetRot, maxDegreesThisFrame);
                rb.MoveRotation(next);
            }
            else
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, _throwTargetRot, maxDegreesThisFrame);
            }
        }
    }

    public void EnableCollider(bool boolean)
    {
        col.enabled = boolean;
    }

    public void Throw()
    {
        boss.canRotate = false;

        // stop any recall in progress
        if (_returnCo != null)
        {
            StopCoroutine(_returnCo);
            _returnCo = null;
        }
        _isReturning = false;

        // reset sticky state
        _stuckToPlayer = false;
        _latchedKnockback = null;
        _latchedTarget = null;

        // disable constraint influence so physics takes over
        if (constraint != null)
        {
            constraint.constraintActive = false;
            constraint.weight = 0f;
        }

        // recache anchor's "home" offset from the boss hand
        if (constraint != null && constraint.sourceCount > sourceIndex)
        {
            _cachedTransOffset = constraint.GetTranslationOffset(sourceIndex);
            _cachedRotOffsetEuler = constraint.GetRotationOffset(sourceIndex);
            _haveCachedOffsets = true;

            ConstraintSource src = constraint.GetSource(sourceIndex);
            _cachedBossHand = src.sourceTransform;
        }

        EnableCollider(true);
        rb.isKinematic = false;

        player = GameObject.FindGameObjectWithTag("Player");

        // launch direction toward player
        Vector3 direction = player.transform.position - transform.position;
        Vector3 force = direction.normalized * throwStrength;

        // lock the look target for spin
        Vector3 playerPosAtThrow = player.transform.position;

        // prefer ground-under-player for visual correctness
        if (Physics.Raycast(player.transform.position, Vector3.down, out RaycastHit hit))
        {
            playerPosAtThrow = hit.point;
        }

        Vector3 toPlayerAtThrow = (playerPosAtThrow - transform.position);
        if (toPlayerAtThrow.sqrMagnitude > 1e-6f)
        {
            Vector3 desiredDown = toPlayerAtThrow.normalized;
            Quaternion alignDown = Quaternion.FromToRotation(-transform.up, desiredDown);
            _throwTargetRot = alignDown * transform.rotation;
            _hasThrowTargetRot = true;
        }

        rb.AddForce(force, ForceMode.Impulse);

        isInAir = true;

        // after boss.throwTime, we recall
        StartCoroutine(ResetAnchorAfterThrow());
    }

    private IEnumerator ResetAnchorAfterThrow()
    {
        yield return new WaitForSeconds(boss.throwTime);

        ResetAnchor();

        boss.AnchorThrowLeave();
        boss.canRotate = true;
    }

    public void ResetAnchor()
    {
        // we're not spinning anymore
        isInAir = false;
        _hasThrowTargetRot = false;

        // stop any previous recall
        if (_returnCo != null)
        {
            StopCoroutine(_returnCo);
            _returnCo = null;
        }

        // kill physics so we can move manually
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;        // FIX from linearVelocity
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        EnableCollider(false);

        if (constraint == null || constraint.sourceCount <= sourceIndex)
            return;

        // put ParentConstraint back to boss hand before returning
        if (_cachedBossHand != null)
        {
            constraint.locked = false;

            ConstraintSource bossSrc = constraint.GetSource(sourceIndex);
            bossSrc.sourceTransform = _cachedBossHand;
            bossSrc.weight = 1f;
            constraint.SetSource(sourceIndex, bossSrc);
        }

        // restore original offsets relative to boss hand
        if (_haveCachedOffsets)
        {
            constraint.SetTranslationOffset(sourceIndex, _cachedTransOffset);
            constraint.SetRotationOffset(sourceIndex, _cachedRotOffsetEuler);
        }
        else
        {
            constraint.SetTranslationOffset(sourceIndex, Vector3.zero);
            constraint.SetRotationOffset(sourceIndex, Vector3.zero);
        }

        // keep the constraint OFF while we animate the flyback
        constraint.constraintActive = false;
        constraint.weight = 0f;
        constraint.locked = true;

        _returnCo = StartCoroutine(CoHomeToOriginalRelativePose());
    }

    private IEnumerator CoHomeToOriginalRelativePose()
    {
        // YANK HERE
        if (_stuckToPlayer && boss != null && _latchedKnockback != null && _latchedTarget != null)
        {
            Vector3 pullDir = (boss.transform.position - _latchedTarget.position).normalized;
            Debug.Log("[AnchorWeapon] YANKING player back toward boss dir=" + pullDir);

            _latchedKnockback.ForceKnockbackPlayer(pullDir);
        }
        else
        {
            Debug.LogWarning("[AnchorWeapon] YANK SKIPPED. Details => " +
                             " stuck=" + _stuckToPlayer +
                             " boss? " + (boss != null) +
                             " latchedKnockback? " + (_latchedKnockback != null) +
                             " latchedTarget? " + (_latchedTarget != null));
        }
        _isReturning = true;
        Debug.Log("[AnchorWeapon] RETURN START");

        ConstraintSource src = constraint.GetSource(sourceIndex);
        Transform hand = src.sourceTransform;
        if (hand == null)
        {
            Debug.LogWarning("[AnchorWeapon] RETURN ABORT: hand == null");
            _isReturning = false;
            yield break;
        }

        _posVel = Vector3.zero;

        // We'll track how long we've been trying to return so we don't get stuck forever
        float hardTimeout = 1.5f; // seconds max to chase back before we just snap/yank anyway
        float startTime = Time.time;

        while (true)
        {
            // live target from boss hand
            Vector3 targetPos = hand.TransformPoint(_cachedTransOffset);
            Quaternion targetRot = hand.rotation * Quaternion.Euler(_cachedRotOffsetEuler);

            // move toward boss hand
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPos,
                ref _posVel,
                returnPosSmoothTime
            );

            // rotate toward boss hand
            float step = returnRotDegPerSec * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, step);

            bool posDone = (transform.position - targetPos).sqrMagnitude <= (finishPosEpsilon * finishPosEpsilon);
            bool rotDone = Quaternion.Angle(transform.rotation, targetRot) <= finishRotEpsilonDeg;

            if (posDone && rotDone)
            {
                Debug.Log("[AnchorWeapon] RETURN LOOP BREAK: close enough to hand");
                break;
            }

            // safety timeout in case boss hand is moving/animating and we never converge tight enough
            if (Time.time - startTime >= hardTimeout)
            {
                Debug.LogWarning("[AnchorWeapon] RETURN LOOP TIMEOUT: snapping anyway");
                break;
            }

            // if some other code bailed the recall (new throw), stop but still go to yank path below
            if (_isReturning == false)
            {
                Debug.LogWarning("[AnchorWeapon] RETURN LOOP INTERRUPTED EARLY (_isReturning == false)");
                break;
            }

            yield return null;
        }

        // --- WE ARE NOW EXITING RETURN MOTION ---
        // (no early yield break anymore, we ALWAYS continue through yank logic)

        // snap cleanly into boss hand
        Vector3 finalPos = hand.TransformPoint(_cachedTransOffset);
        Quaternion finalRot = hand.rotation * Quaternion.Euler(_cachedRotOffsetEuler);
        transform.position = finalPos;
        transform.rotation = finalRot;

        // fully restore constraint back on boss hand
        constraint.locked = false;
        constraint.enabled = true;
        constraint.weight = 1f;

        var bossSrcFinal = constraint.GetSource(sourceIndex);
        if (bossSrcFinal.weight <= 0f)
        {
            bossSrcFinal.weight = 1f;
            constraint.SetSource(sourceIndex, bossSrcFinal);
        }

        constraint.constraintActive = true;
        constraint.locked = true;

        Debug.Log("[AnchorWeapon] RETURN COMPLETE. About to YANK? stuck=" + _stuckToPlayer);

        

        // cleanup
        _stuckToPlayer = false;
        _latchedKnockback = null;
        _latchedTarget = null;

        _isReturning = false;
        _returnCo = null;
    }


    // Stick into player and ride along
    private void StickToPlayer(Transform hitTransform, BossKnockback knockRef)
    {
        Debug.Log("[AnchorWeapon] StickToPlayer " + hitTransform.name);

        isInAir = false;
        _hasThrowTargetRot = false;

        _stuckToPlayer = true;

        // cache who to yank later
        _latchedTarget = hitTransform;
        _latchedKnockback = knockRef;

        // stop physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // stop re-trigger spam
        EnableCollider(false);

        // re-wire constraint to follow THIS player transform in place
        if (constraint != null && hitTransform != null && constraint.sourceCount > sourceIndex)
        {
            constraint.locked = false;

            ConstraintSource playerSrc = constraint.GetSource(sourceIndex);
            playerSrc.sourceTransform = hitTransform;
            playerSrc.weight = 1f;
            constraint.SetSource(sourceIndex, playerSrc);

            // preserve the current hit position/orientation as offsets
            Vector3 localPos = hitTransform.InverseTransformPoint(transform.position);
            Quaternion localRot = Quaternion.Inverse(hitTransform.rotation) * transform.rotation;

            constraint.SetTranslationOffset(sourceIndex, localPos);
            constraint.SetRotationOffset(sourceIndex, localRot.eulerAngles);

            // turn constraint ON so we visually attach to them
            constraint.weight = 1f;
            constraint.constraintActive = true;
            constraint.locked = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // if we hit PLAYER
        if (other.CompareTag("Player"))
        {
            // push AWAY from boss now
            Vector3 dirOut = (other.transform.position - boss.transform.position).normalized;

            // IMPORTANT: get knockback from the hierarchy, not just this collider
            BossKnockback knockNow = other.GetComponent<BossKnockback>();
            if (knockNow != null)
            {
                knockNow.KnockbackPlayer(dirOut);
            }

            // apply damage to player
            CombatController combat = other.GetComponentInChildren<CombatController>();
            if (combat != null)
            {
                combat.TakeDamageByBoss(AnchorDamage);
            }

            // latch ONLY if we were mid-air throw
            if (isInAir)
            {
                if (player == null) player = other.gameObject;
                StickToPlayer(other.transform, knockNow);
            }

            return;
        }

        // world hit (ground/wall/etc.)
        _hasThrowTargetRot = false;
        rb.isKinematic = true;
        EnableCollider(false);
        isInAir = false;
    }
}
