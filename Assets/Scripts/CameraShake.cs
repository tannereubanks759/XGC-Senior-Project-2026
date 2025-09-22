using UnityEngine;

/// Attach to your Player Camera (or, even better, to a child "ShakePivot" under the camera)
public class CameraShake : MonoBehaviour
{
    [Header("Shake Tuning")]
    public float kickDegrees = 6f;              // degrees at intensity = 1
    public float maxDegrees = 10f;              // safety clamp
    [Range(0.03f, 0.6f)] public float returnSmoothTime = 0.18f;
    public bool useUnscaledTime = true;

    [Header("Axis Weights (0 disables axis)")]
    public float pitchWeight = 1f;              // X (look up/down nod)
    public float yawWeight = 0.6f;            // Y (look left/right)
    public float rollWeight = 0.8f;            // Z (screen tilt)

    [Header("Randomization")]
    [Range(0f, 1f)] public float variance = 0.25f;

    // --- Internal ---
    Vector3 _offsetEuler;   // current shake offset (degrees)
    Vector3 _offsetVel;     // SmoothDamp velocity store

    void OnEnable()
    {
        _offsetEuler = Vector3.zero;
        _offsetVel = Vector3.zero;
    }

    void LateUpdate()
    {
        // IMPORTANT: read the camera's CURRENT rotation (as set by your look code in Update)
        Quaternion baseLocalRot = transform.localRotation;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // Ease offsets back to zero
        _offsetEuler.x = Mathf.SmoothDampAngle(_offsetEuler.x, 0f, ref _offsetVel.x, returnSmoothTime, Mathf.Infinity, dt);
        _offsetEuler.y = Mathf.SmoothDampAngle(_offsetEuler.y, 0f, ref _offsetVel.y, returnSmoothTime, Mathf.Infinity, dt);
        _offsetEuler.z = Mathf.SmoothDampAngle(_offsetEuler.z, 0f, ref _offsetVel.z, returnSmoothTime, Mathf.Infinity, dt);

        // Apply ADDITIVELY: base * shake
        transform.localRotation = baseLocalRot * Quaternion.Euler(_offsetEuler);
    }

    /// Random shake (no hit direction)
    public void Shake(float intensity = 1f)
    {
        Vector3 dir = RandomUnitBiased();
        ApplyKick(dir, intensity);
    }

    /// Directional shake: pass direction from camera toward the damage source (world space)
    public void ShakeFromHit(Vector3 hitWorldDir, float intensity = 1f)
    {
        if (hitWorldDir.sqrMagnitude < 1e-6f) { Shake(intensity); return; }

        Vector3 local = transform.InverseTransformDirection(hitWorldDir.normalized);

        // Map hit direction to rotational response
        Vector3 dir = new Vector3(
            -Mathf.Clamp(local.z, -1f, 1f),    // front hit => look up a bit
             Mathf.Clamp(local.x, -1f, 1f),    // right hit => yaw right
            -Mathf.Clamp(local.x, -1f, 1f)     // right hit => roll right
        ).normalized;

        ApplyKick(dir, intensity);
    }

    // --- Helpers ---
    void ApplyKick(Vector3 unitDir, float intensity)
    {
        Vector3 axisWeighted = new Vector3(
            unitDir.x * pitchWeight,
            unitDir.y * yawWeight,
            unitDir.z * rollWeight
        );

        float rand = 1f + (variance > 0f ? Random.Range(-variance, variance) : 0f);
        Vector3 degrees = axisWeighted * (kickDegrees * Mathf.Max(0f, intensity) * rand);

        _offsetEuler += degrees;
        _offsetEuler.x = Mathf.Clamp(_offsetEuler.x, -maxDegrees, maxDegrees);
        _offsetEuler.y = Mathf.Clamp(_offsetEuler.y, -maxDegrees, maxDegrees);
        _offsetEuler.z = Mathf.Clamp(_offsetEuler.z, -maxDegrees, maxDegrees);
    }

    static Vector3 RandomUnitBiased()
    {
        Vector3 v = new Vector3(
            Random.Range(-0.2f, 1f),  // bias toward positive pitch
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        );
        if (v.sqrMagnitude < 1e-4f) v = new Vector3(0.4f, 0.2f, -0.3f);
        return v.normalized;
    }
}
