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
    private Collider col;
    Rigidbody rb;
    ParentConstraint constraint;
    bool isInAir = false;
    private GameObject player;
    // ParentConstraint source index (0 if you have one source)
    [SerializeField] private int sourceIndex = 0;

    // Cached offsets so we can restore the original relative pose to the hand
    private Vector3 _cachedTransOffset;
    private Vector3 _cachedRotOffsetEuler; // rotation offset is Euler for constraints
    private bool _haveCachedOffsets = false;
    [Header("Return Settings")]
    public float returnPosSmoothTime = 0.12f;    // lower = snappier
    public float returnRotDegPerSec = 540f;      // max degrees/sec toward target
    public float finishPosEpsilon = 0.01f;       // meters
    public float finishRotEpsilonDeg = 0.5f;     // degrees

    private PirateBossAI boss;
    // Add this field near your other privates
    private bool _isReturning = false;

    private Vector3 _posVel;                     // SmoothDamp velocity
    private Coroutine _returnCo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        boss = GetComponentInParent<PirateBossAI>();
        isInAir = false;
        constraint = GetComponent<ParentConstraint>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        EnableCollider(false);

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) //test anchor throw
        {
            Throw();
        }
        if (isInAir == true)
        {
            // Ensure we have a player reference
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p) player = p; else return; // no player found
            }

            Vector3 toPlayer = player.transform.position - transform.position;
            if (toPlayer.sqrMagnitude > 1e-6f)
            {
                // We want transform.down to face the player
                Vector3 desiredDown = toPlayer.normalized;

                // Minimal rotation aligning current down to desiredDown
                Quaternion alignDown = Quaternion.FromToRotation(-transform.up, desiredDown);
                Quaternion targetRotation = alignDown * transform.rotation;

                // Rotate gradually (rotationDegrees = degrees per second)
                float maxDegreesThisFrame = rotationDegrees * Time.deltaTime;

                if (rb != null && !rb.isKinematic)
                {
                    Quaternion next = Quaternion.RotateTowards(rb.rotation, targetRotation, maxDegreesThisFrame);
                    rb.MoveRotation(next);
                }
                else
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxDegreesThisFrame);
                }
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
        // --- NEW: cancel any in-progress homing ---
        if (_returnCo != null)
        {
            StopCoroutine(_returnCo);
            _returnCo = null;
        }
        _isReturning = false;

        // If the constraint was left active by a prior reset, disable its influence now
        if (constraint != null)
        {
            constraint.constraintActive = false;
            constraint.weight = 0f;   // explicit: no blend influence
                                      // leave constraint.locked as-is (it only affects offset editing)
        }

        // (Re)cache offsets from current constrained pose if available
        if (constraint != null && constraint.sourceCount > sourceIndex)
        {
            _cachedTransOffset = constraint.GetTranslationOffset(sourceIndex);
            _cachedRotOffsetEuler = constraint.GetRotationOffset(sourceIndex);
            _haveCachedOffsets = true;
        }

        EnableCollider(true);
        rb.isKinematic = false;

        player = GameObject.FindGameObjectWithTag("Player");
        Vector3 direction = player.transform.position - transform.position;
        Vector3 force = direction.normalized * throwStrength;

        rb.AddForce(force, ForceMode.Impulse);
        isInAir = true;
        StartCoroutine(ResetAnchorAfterThrow());
    }

    public void ResetAnchor()
    {
        // Stop airborne behavior, physics, and hits
        isInAir = false;

        // Cancel any previous homing just in case
        if (_returnCo != null)
        {
            StopCoroutine(_returnCo);
            _returnCo = null;
        }

        if (rb != null && rb.isKinematic == false)
        {
            rb.linearVelocity = Vector3.zero;          // use velocity in most Unity versions
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        EnableCollider(false);

        if (constraint == null || constraint.sourceCount <= sourceIndex) return;

        // Ensure original relative pose offsets are applied
        constraint.locked = false;
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

        // Keep constraint disabled during manual homing to avoid tug-of-war
        constraint.constraintActive = false;
        constraint.weight = 0f;
        constraint.locked = true;

        _returnCo = StartCoroutine(CoHomeToOriginalRelativePose());
    }

    private System.Collections.IEnumerator CoHomeToOriginalRelativePose()
    {
        _isReturning = true;

        ConstraintSource src = constraint.GetSource(sourceIndex);
        Transform hand = src.sourceTransform;
        if (hand == null) { _isReturning = false; yield break; }

        _posVel = Vector3.zero;

        while (_isReturning)
        {
            Vector3 targetPos = hand.TransformPoint(_cachedTransOffset);
            Quaternion targetRot = hand.rotation * Quaternion.Euler(_cachedRotOffsetEuler);

            // Position homing
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPos,
                ref _posVel,
                returnPosSmoothTime
            );

            // Rotation homing
            float step = returnRotDegPerSec * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, step);

            bool posDone = (transform.position - targetPos).sqrMagnitude <= (finishPosEpsilon * finishPosEpsilon);
            bool rotDone = Quaternion.Angle(transform.rotation, targetRot) <= finishRotEpsilonDeg;

            if (posDone && rotDone) break;

            yield return null;
        }

        if (!_isReturning) yield break; // aborted by Throw()

        // Snap to final
        transform.position = hand.TransformPoint(_cachedTransOffset);
        transform.rotation = hand.rotation * Quaternion.Euler(_cachedRotOffsetEuler);

        // Hand control back to the constraint (defensive full re-enable)
        constraint.locked = false;

        // Make sure the component is enabled and has influence
        constraint.enabled = true;          // component on
        constraint.weight = 1f;             // overall blend weight

        // Ensure the source itself contributes (per-source weight)
        var src1 = constraint.GetSource(sourceIndex);
        if (src1.weight <= 0f)
        {
            src1.weight = 1f;
            constraint.SetSource(sourceIndex, src1);
        }

        // Reactivate constraint using the cached offsets we already applied
        constraint.constraintActive = true;
        constraint.locked = true;

        _isReturning = false;
        _returnCo = null;


        _isReturning = false;
        _returnCo = null;
    }
    public IEnumerator ResetAnchorAfterThrow()
    {
        
        yield return new WaitForSeconds(boss.throwTime);
        ResetAnchor();
        boss.AnchorThrowLeave();
        boss.canRotate = true;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 direction = (other.transform.position - this.GetComponentInParent<PirateBossAI>().transform.position).normalized;
            other.GetComponent<BossKnockback>().KnockbackPlayer(direction);
            other.GetComponentInChildren<CombatController>().TakeDamage(20);
            if (isInAir == false)
            {
                EnableCollider(false);
            }
            
        } 
        else
        {
            rb.isKinematic = true;
            EnableCollider(false);
            isInAir = false;
        }
    }
}
