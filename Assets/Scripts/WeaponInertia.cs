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

    // --- Parry / Block clash effect ---
    [Header("Parry Clash")]
    [Tooltip("Approx distance pushed toward camera (meters).")]
    public float clashBackDistance = 0.06f;
    [Tooltip("Approx distance pushed back out (meters).")]
    public float clashForwardDistance = 0.04f;
    [Tooltip("Peak tilt (degrees) during clash.")]
    public float clashTiltDegrees = 7f;
    [Tooltip("Time between the 'in' shove and the 'out' rebound.")]
    public float clashDelay = 0.055f;
    [Tooltip("Minimum time between clashes.")]
    public float clashCooldown = 0.05f;

    Coroutine _clashRoutine;
    float _lastClashTime = -999f;

    /// Call this when a blocked hit/clash happens.
    /// Example: weaponInertia.ParryClash();  or weaponInertia.ParryClash(1.25f);
    public void ParryClash(float intensity = 1f)
    {
        if (Time.time - _lastClashTime < clashCooldown) return;
        _lastClashTime = Time.time;

        if (_clashRoutine != null) StopCoroutine(_clashRoutine);
        _clashRoutine = StartCoroutine(CoParryClash(intensity));
    }

    System.Collections.IEnumerator CoParryClash(float intensity)
    {
        // Convert desired distances/angles into velocity impulses for the springs.
        // Use the delay as the "rise time" so it feels snappy but controllable.
        float rise = Mathf.Max(0.015f, clashDelay);
        float backVel = (clashBackDistance * intensity) / rise;          // m/s toward camera (local -Z)
        float outVel = (clashForwardDistance * intensity) / rise;       // m/s away from camera (local +Z)
        float tiltVel = (clashTiltDegrees * intensity) / rise;           // deg/s

        // 1) Shove IN (toward player) + slight tilt up and a tiny randomized roll for life
        AddImpulse(
            new Vector3(0f, 0f, -backVel),
            new Vector3(tiltVel, 0f, Random.Range(-tiltVel * 0.4f, tiltVel * 0.4f))
        );

        yield return new WaitForSeconds(clashDelay);

        // 2) Rebound OUT (away from player) + counter tilt
        AddImpulse(
            new Vector3(0f, 0f, outVel),
            new Vector3(-tiltVel * 0.7f, 0f, 0f)
        );

        // Optional small settle delay to avoid overlap
        yield return new WaitForSeconds(clashDelay * 0.5f);
        _clashRoutine = null;
    }

    // --- Gun Recoil ---
    [Header("Gun Recoil")]
    [Tooltip("How far the weapon kicks back (meters).")]
    public float recoilBackDistance = 0.08f;
    [Tooltip("How much the muzzle rises (degrees).")]
    public float recoilUpDegrees = 5f;
    [Tooltip("Random left/right twist (degrees).")]
    public float recoilYawJitter = 1.2f;
    [Tooltip("Random roll (degrees).")]
    public float recoilRollJitter = 0.8f;
    [Tooltip("Time used to convert distances/angles into a sharp snap impulse.")]
    public float recoilSnapTime = 0.04f;
    [Tooltip("Pause at peak before recovery.")]
    public float recoilReturnDelay = 0.02f;
    [Tooltip("Small forward overshoot during recovery (meters).")]
    public float recoilForwardOvershoot = 0.02f;
    [Tooltip("Small downward overshoot during recovery (degrees).")]
    public float recoilDownOvershoot = 2.0f;
    [Tooltip("Scale recoil when aiming down sights.")]
    public float adsRecoilScale = 0.6f;

    Coroutine _recoilRoutine;

    /// Call when firing. Example:
    /// weaponInertia.FireRecoil();              // hip-fire
    /// weaponInertia.FireRecoil(1.2f, true);    // heavier + ADS
    public void FireRecoil(float intensity = 1f, bool isAiming = false)
    {
        if (_recoilRoutine != null) StopCoroutine(_recoilRoutine);
        _recoilRoutine = StartCoroutine(CoFireRecoil(intensity, isAiming));
    }

    System.Collections.IEnumerator CoFireRecoil(float intensity, bool isAiming)
    {
        float aimScale = isAiming ? adsRecoilScale : 1f;
        float snap = Mathf.Max(0.015f, recoilSnapTime);

        // Convert desired amplitudes into spring-velocity impulses.
        float backVel = (recoilBackDistance * intensity * aimScale) / snap;   // m/s (local -Z)
        float upVel = (recoilUpDegrees * intensity * aimScale) / snap;    // deg/s (pitch up)
        float yawVel = (Random.Range(-recoilYawJitter, recoilYawJitter) * intensity * aimScale) / snap;
        float rollVel = (Random.Range(-recoilRollJitter, recoilRollJitter) * intensity * aimScale) / snap;

        // 1) Immediate kick: back + up, with a touch of random yaw/roll
        AddImpulse(
            new Vector3(0f, 0f, -backVel),
            new Vector3(upVel, yawVel, rollVel)
        );

        // Brief hold at peak
        yield return new WaitForSeconds(recoilReturnDelay);

        // 2) Recovery overshoot: slightly forward + down to settle naturally
        float fwdVel = (recoilForwardOvershoot * intensity * aimScale) / snap;
        float downVel = (recoilDownOvershoot * intensity * aimScale) / snap;

        AddImpulse(
            new Vector3(0f, 0f, fwdVel),
            new Vector3(-downVel, -yawVel * 0.25f, -rollVel * 0.5f)
        );

        // Small settle time to avoid reentry overlaps
        yield return new WaitForSeconds(recoilReturnDelay * 0.5f);
        _recoilRoutine = null;
    }

    // =================== Block / Guard Stagger ===================
    [Header("Block Stagger")]
    [Tooltip("How far the weapon shoves toward camera on guard clash (m).")]
    public float staggerBackDistance = 0.055f;
    [Tooltip("Side skid into the impact (m).")]
    public float staggerSideDistance = 0.03f;
    [Tooltip("How much the weapon tilts from the hit (deg).")]
    public float staggerTiltDegrees = 6.5f;
    [Tooltip("Short pause between shove and rebound (s).")]
    public float staggerDelay = 0.05f;
    [Tooltip("Extra settle time while springs are heavier (s).")]
    public float staggerSettleTime = 0.18f;

    // Temporary spring override (to feel heavier during the clash)
    [Tooltip("Scale pos stiffness during stagger (1 = unchanged). Lower = softer.")]
    public float staggerPosStiffnessScale = 0.85f;
    [Tooltip("Scale pos damping during stagger (1 = unchanged). Higher = heavier.")]
    public float staggerPosDampingScale = 1.25f;
    [Tooltip("Scale rot stiffness during stagger.")]
    public float staggerRotStiffnessScale = 0.85f;
    [Tooltip("Scale rot damping during stagger.")]
    public float staggerRotDampingScale = 1.25f;

    Coroutine _staggerRoutine;
    Coroutine _springOverrideRoutine;

    /// Call this instead of camera shake when your attack is blocked.
    /// `worldHitDirection` should point FROM enemy TO player (the direction the
    /// blow came from). If you don't have it, call the overload without it.
    public void BlockStagger(Vector3 worldHitDirection, float intensity = 1f)
    {
        if (_staggerRoutine != null) StopCoroutine(_staggerRoutine);
        _staggerRoutine = StartCoroutine(CoBlockStagger(worldHitDirection, intensity));
    }

    /// Overload when you don't have the enemy direction.
    /// Uses camera right to pick a sideways skid.
    public void BlockStagger(float intensity = 1f)
    {
        Vector3 approxFromRight = followTarget ? followTarget.right : transform.right;
        BlockStagger(approxFromRight, intensity);
    }

    System.Collections.IEnumerator CoBlockStagger(Vector3 worldHitDir, float intensity)
    {
        // Convert hit direction to weapon local space to know which side got hit
        Vector3 localHit = transform.InverseTransformDirection(worldHitDir.normalized);
        float side = Mathf.Sign(Mathf.Clamp(localHit.x, -1f, 1f)); // -1 = left hit, +1 = right hit

        // Convert distances/angles to spring *velocities* (sharp snap)
        float rise = Mathf.Max(0.015f, staggerDelay);
        float backVel = (staggerBackDistance * intensity) / rise;           // m/s toward camera (local -Z)
        float sideVel = (staggerSideDistance * intensity) / rise * side;    // m/s sideways skid
        float tiltVel = (staggerTiltDegrees * intensity) / rise;            // deg/s

        // Heavier springs while we clash & settle
        if (_springOverrideRoutine != null) StopCoroutine(_springOverrideRoutine);
        _springOverrideRoutine = StartCoroutine(CoSpringOverride(staggerSettleTime));

        // 1) Shove IN + skid into the hit + roll/pitch into the strike
        AddImpulse(
            new Vector3(sideVel * 0.5f, 0f, -backVel),
            new Vector3(tiltVel * 0.65f,  // pitch up a bit
                        -tiltVel * 0.35f * side, // yaw slightly away from hit side
                        -tiltVel * 0.9f * side)  // roll into the hit
        );

        yield return new WaitForSeconds(staggerDelay);

        // 2) Rebound OUT + counter-tilt to settle
        AddImpulse(
            new Vector3(-sideVel * 0.6f, 0f, backVel * 0.75f),
            new Vector3(-tiltVel * 0.5f,
                        tiltVel * 0.25f * side,
                        tiltVel * 0.6f * side)
        );

        // Optional tiny after-kiss for life
        yield return new WaitForSeconds(staggerDelay * 0.5f);
        AddImpulse(
            new Vector3(0f, 0f, backVel * -0.25f),
            new Vector3(tiltVel * 0.2f, 0f, -tiltVel * 0.25f * side)
        );

        _staggerRoutine = null;
    }

    /// Temporarily makes the springs feel heavier/softer, then restores.
    System.Collections.IEnumerator CoSpringOverride(float duration)
    {
        // Cache originals
        float pStiff0 = posStiffness, pDamp0 = posDamping, rStiff0 = rotStiffness, rDamp0 = rotDamping;

        posStiffness = pStiff0 * staggerPosStiffnessScale;
        posDamping = pDamp0 * staggerPosDampingScale;
        rotStiffness = rStiff0 * staggerRotStiffnessScale;
        rotDamping = rDamp0 * staggerRotDampingScale;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Smoothly blend back to originals over a short fade
        float fade = 0.08f;
        float e = 0f;
        while (e < fade)
        {
            e += Time.deltaTime;
            float a = Mathf.Clamp01(e / fade);
            posStiffness = Mathf.Lerp(posStiffness, pStiff0, a);
            posDamping = Mathf.Lerp(posDamping, pDamp0, a);
            rotStiffness = Mathf.Lerp(rotStiffness, rStiff0, a);
            rotDamping = Mathf.Lerp(rotDamping, rDamp0, a);
            yield return null;
        }

        // Ensure exact restore
        posStiffness = pStiff0; posDamping = pDamp0;
        rotStiffness = rStiff0; rotDamping = rDamp0;
        _springOverrideRoutine = null;
    }

}
