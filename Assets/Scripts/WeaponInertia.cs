using UnityEngine;

/// Attach to your "Weapons" object (the parent of Sword/Gun, etc.)
public class WeaponInertia : MonoBehaviour
{
    [Header("Targets")]
    public Transform followTarget;           // Usually PlayerCamera or Joint

    [Header("Rest Pose (local)")]
    public Vector3 restLocalPosition = Vector3.zero;
    public Vector3 restLocalEuler = Vector3.zero;

    [Header("Spring (position)")]
    [Tooltip("How strongly the weapon is pulled toward the target pose.")]
    public float posStiffness = 120f;
    [Tooltip("How much velocity is damped; raise to reduce oscillation.")]
    public float posDamping = 18f;
    [Tooltip("Clamp for maximum positional lag in local space.")]
    public float maxOffset = 0.15f;

    [Header("Spring (rotation)")]
    public float rotStiffness = 140f;
    public float rotDamping = 20f;
    [Tooltip("Clamp for maximum angular lag, in degrees.")]
    public float maxAngle = 8f;

    [Header("Extra Effects")]
    [Tooltip("Scales how much camera movement causes sway.")]
    public float movementSway = 0.4f;
    [Tooltip("Scales how much look input causes sway.")]
    public float lookSway = 0.02f;
    [Tooltip("Impulse applied opposite vertical acceleration (jump/fall).")]
    public float jumpKick = 0.05f;

    // Internals
    Vector3 _posVel;                 // positional spring velocity (local)
    Vector3 _rotVel;                 // rotational spring velocity (degrees/sec)
    Vector3 _targetLocalPos;
    Vector3 _targetLocalEuler;
    Vector3 _prevFollowPos;
    Vector3 _prevFollowEuler;

    void Awake()
    {
        if (!followTarget) followTarget = transform.parent; // fallback
        _targetLocalPos = restLocalPosition;
        _targetLocalEuler = restLocalEuler;
        if (followTarget)
        {
            _prevFollowPos = followTarget.position;
            _prevFollowEuler = followTarget.eulerAngles;
        }
    }

    void LateUpdate()
    {
        if (!followTarget) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // --- Compute follow target deltas (world) ---
        Vector3 followDelta = followTarget.position - _prevFollowPos;
        Vector3 followVel = followDelta / dt;

        // Approx look delta (degrees/frame)
        Vector3 followEuler = followTarget.eulerAngles;
        Vector3 lookDelta = DeltaEuler(_prevFollowEuler, followEuler);

        _prevFollowPos = followTarget.position;
        _prevFollowEuler = followEuler;

        // --- Build desired local pose with sway ---
        // Base at rest pose
        _targetLocalPos = restLocalPosition;

        // Movement sway (convert world motion into local space of the target)
        Vector3 localVel = followTarget.InverseTransformDirection(followVel);
        _targetLocalPos += new Vector3(-localVel.x, -localVel.y, -localVel.z) * movementSway * 0.01f;

        // Look sway (a tiny tilt/shift opposite look motion)
        _targetLocalPos += new Vector3(-lookDelta.y, lookDelta.x, 0f) * lookSway * 0.01f;

        // Jump kick (opposes vertical acceleration = change in vertical velocity)
        // crude but effective: use change of local Y velocity frame-to-frame
        float verticalKick = -localVel.y * jumpKick * 0.02f;
        _targetLocalPos += new Vector3(0f, verticalKick, 0f);

        // Clamp offset
        Vector3 offsetFromRest = _targetLocalPos - restLocalPosition;
        _targetLocalPos = restLocalPosition + Vector3.ClampMagnitude(offsetFromRest, maxOffset);

        // Rotation target (small counter-lean to motion & look)
        _targetLocalEuler = restLocalEuler
                          + new Vector3(lookDelta.x, 0f, -lookDelta.y) * (lookSway * 0.6f)
                          + new Vector3(localVel.z, -localVel.x, localVel.x) * (movementSway * 0.2f);

        _targetLocalEuler = ClampEulerDelta(restLocalEuler, _targetLocalEuler, maxAngle);

        // --- Critically-damped-ish spring for position (local) ---
        Vector3 currentLocal = transform.localPosition;
        Vector3 toTarget = _targetLocalPos - currentLocal;
        _posVel += (posStiffness * toTarget - posDamping * _posVel) * dt;
        currentLocal += _posVel * dt;
        transform.localPosition = currentLocal;

        // --- Spring for rotation using Euler (small angles) ---
        Vector3 currentEuler = transform.localEulerAngles;
        Vector3 currentEulerDelta = DeltaEuler(restLocalEuler, currentEuler);
        Vector3 targetEulerDelta = DeltaEuler(restLocalEuler, _targetLocalEuler);

        Vector3 rotError = targetEulerDelta - currentEulerDelta;
        _rotVel += (rotStiffness * rotError - rotDamping * _rotVel) * dt;
        currentEulerDelta += _rotVel * dt;

        Vector3 newEuler = restLocalEuler + currentEulerDelta;
        transform.localRotation = Quaternion.Euler(newEuler);
    }

    // Public API for other scripts (e.g., firing recoil)
    public void AddImpulse(Vector3 localPositionImpulse, Vector3 localEulerImpulse)
    {
        _posVel += localPositionImpulse;
        _rotVel += localEulerImpulse;
    }

    // --- Helpers ---
    static Vector3 DeltaEuler(Vector3 from, Vector3 to)
    {
        // shortest-path delta (degrees)
        return new Vector3(
            Mathf.DeltaAngle(from.x, to.x),
            Mathf.DeltaAngle(from.y, to.y),
            Mathf.DeltaAngle(from.z, to.z)
        );
    }

    static Vector3 ClampEulerDelta(Vector3 center, Vector3 value, float maxDeg)
    {
        Vector3 d = new Vector3(
            Mathf.DeltaAngle(center.x, value.x),
            Mathf.DeltaAngle(center.y, value.y),
            Mathf.DeltaAngle(center.z, value.z)
        );
        d.x = Mathf.Clamp(d.x, -maxDeg, maxDeg);
        d.y = Mathf.Clamp(d.y, -maxDeg, maxDeg);
        d.z = Mathf.Clamp(d.z, -maxDeg, maxDeg);
        return center + d;
    }
}
