using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RaycastKnockback : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("Camera used to raycast from screen center.")]
    public Camera cam;
    [Tooltip("Max distance for the knockback ray.")]
    public float maxDistance = 12f;
    [Tooltip("Which layers can be hit by the raycast.")]
    public LayerMask hittableLayers = ~0;

    [Header("Knockback")]
    [Tooltip("Initial push speed (meters/second).")]
    public float knockbackForce = 12f;
    [Tooltip("How long the knockback push lasts (seconds).")]
    public float knockbackDuration = 0.25f;
    [Tooltip("Degrees to tilt the push direction upward.")]
    [Range(0f, 45f)]
    public float upAngleDeg = 12f;
    [Tooltip("Optional extra downward pull during the push (m/s^2). 0 to ignore.")]
    public float extraGravity = 0f;

    [Header("Rigidbody (optional)")]
    [Tooltip("If the enemy has a non-kinematic Rigidbody, use AddForce instead of transform moves.")]
    public bool preferRigidbodyWhenAvailable = true;

    // Add these tunables near your other fields:
    [Header("Landing")]
    [Tooltip("Layers considered ground while settling back down.")]
    public LayerMask groundLayers = ~0;
    [Tooltip("How strong the downward pull is after the push (m/s^2).")]
    public float fallGravity = 18f;
    [Tooltip("Max downward speed while falling (m/s).")]
    public float terminalFallSpeed = 14f;
    [Tooltip("How quickly we ease onto ground once close (m/s).")]
    public float settleSpeed = 6f;
    [Tooltip("How far to raycast below the enemy to find ground (meters).")]
    public float groundProbeDistance = 6f;
    [Tooltip("Distance at which we transition to 'settle' mode (meters).")]
    public float landThreshold = 0.35f;
    [Tooltip("Y offset to keep the feet slightly above the ground when landing.")]
    public float landingOffset = 0.03f;
    [Tooltip("Safety timeout so we don't get stuck in the air (seconds).")]
    public float maxAirTime = 1.25f;
    [Header("Safety")]
    [Tooltip("If, for any reason, the agent wasn't re-enabled by the end of the knockback, force-enable it after this many seconds.")]
    public float agentFallbackReenableTime = 1.5f;
    [Header("Artifact KnockBack Settings")]
    public bool upgradedKnockback = false; 
    public float upgradedForceMultiplier = 1.75f;
    public int knockBackDamage = 3;
    public GameObject fxObject;
    public GameObject fxSpawnPoint;
    public Vector3 offset = new Vector3(90f, 0f, 0f);
    /// <summary>
    /// Casts a ray from the camera center and, if an enemy with a NavMeshAgent is hit,
    /// pushes it away from the camera, slightly upward.
    /// </summary>
    public void Knockback()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) { Debug.LogWarning("RaycastKnockback: No Camera assigned."); return; }
        }
        
            
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            NavMeshAgent agent = hit.collider.GetComponentInParent<NavMeshAgent>();

            
            if (upgradedKnockback)
            {
                var health = hit.collider.GetComponentInParent<BaseEnemyAI>();
                Quaternion baseRot = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
                Quaternion fxRot = baseRot * Quaternion.Euler(offset);
                GameObject objToDelete = Instantiate(fxObject, fxSpawnPoint.transform.position, fxRot, fxSpawnPoint.transform);
                Destroy(objToDelete, .15f);
                health.TakeDamage(knockBackDamage);
            }

            if (agent == null || agent.enabled == false)
                return;

            Vector3 flatForward = cam.transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = cam.transform.forward;
            flatForward.Normalize();

            float rad = Mathf.Deg2Rad * Mathf.Clamp(upAngleDeg, 0f, 89f);
            Vector3 pushDir = (flatForward * Mathf.Cos(rad) + Vector3.up * Mathf.Sin(rad)).normalized;

            float finalForce = knockbackForce;
            if (upgradedKnockback)
                finalForce *= upgradedForceMultiplier;

            Rigidbody rb = agent.GetComponent<Rigidbody>();
            if (preferRigidbodyWhenAvailable && rb != null && !rb.isKinematic)
                StartCoroutine(ApplyRigidbodyKnockback(agent, rb, pushDir, finalForce));
            else
                StartCoroutine(ApplyNavmeshKnockback(agent, pushDir, finalForce));

#if UNITY_EDITOR
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.cyan, 0.5f);
            Debug.DrawRay(hit.point, pushDir * 1.5f, Color.yellow, 0.5f);
#endif
        }
    }


    private IEnumerator ApplyNavmeshKnockback(NavMeshAgent agent, Vector3 pushDir, float appliedForce)
    {
        bool wasEnabled = agent.enabled;
        bool prevStopped = agent.isStopped;

        float dur = Mathf.Max(0.05f, knockbackDuration);
        float v0 = Mathf.Max(0f, appliedForce);


        // Disable so we can move the transform freely
        if (wasEnabled)
        {
            agent.enabled = false;
            // Start the safety re-enable (will no-op if we re-enable earlier)
            StartCoroutine(ReenableAgentAfterDelay(agent, prevStopped, agentFallbackReenableTime));
        }

        // Horizontal + vertical velocity (explicit upward tilt already in pushDir)
        Vector3 velocity = pushDir * v0;

        // --- Phase 1: push with ease-out ---
        float t = 0f;
        while (t < dur && agent != null)
        {
            float d = 1f - (t / dur);           // linear ease-out (1 -> 0)
            Vector3 frameVel = velocity * d;

            agent.transform.position += frameVel * Time.deltaTime;
            t += Time.deltaTime;
            yield return null;
        }

        // --- Phase 2: smooth fall with soft landing ---
        float airTimer = 0f;
        float vy = Mathf.Min(0f, velocity.y);   // start falling from current vertical vel if downward
        Transform tr = agent.transform;

        while (agent != null && airTimer < maxAirTime)
        {
            airTimer += Time.deltaTime;

            // Gravity
            vy -= fallGravity * Time.deltaTime;
            vy = Mathf.Max(vy, -Mathf.Abs(terminalFallSpeed));

            // Move horizontally very slightly (optional drift)
            // (You can damp horizontal drift to near-zero if you want complete stop)
            Vector3 pos = tr.position;
            pos += new Vector3(0f, vy * Time.deltaTime, 0f);

            // Ground probe
            if (Physics.Raycast(pos + Vector3.up * 0.1f, Vector3.down, out RaycastHit gHit, groundProbeDistance, groundLayers, QueryTriggerInteraction.Ignore))
            {
                float dist = (pos.y - gHit.point.y);
                if (dist <= landThreshold)
                {
                    // Ease to ground instead of snapping
                    float targetY = gHit.point.y + landingOffset;
                    pos.y = Mathf.MoveTowards(pos.y, targetY, settleSpeed * Time.deltaTime);

                    // If we've basically landed, break
                    if (Mathf.Abs(pos.y - targetY) < 0.005f)
                    {
                        tr.position = pos;
                        break;
                    }
                    tr.position = pos;
                    yield return null;
                    continue;
                }
            }

            tr.position = pos;
            yield return null;
        }

        // --- Hand control back to the agent cleanly ---
        if (agent != null && wasEnabled)
        {
            // Re-enable now (satisfies both normal path and the timeout race)
            if (!agent.enabled) agent.enabled = true;

            // Warp AFTER enabling so the agent locks to the nearest NavMesh location at current position
            agent.Warp(agent.transform.position);
            agent.isStopped = prevStopped;
        }
    }




    private IEnumerator ApplyRigidbodyKnockback(NavMeshAgent agent, Rigidbody rb, Vector3 pushDir, float appliedForce)
    {
        // Briefly stop the agent so it doesn't fight physics
        bool prevStopped = agent.isStopped;
        agent.isStopped = true;

        // Convert our "force" (speed) into a velocity change impulse
        Vector3 initialVel = pushDir * Mathf.Max(0f, appliedForce);
        rb.AddForce(initialVel, ForceMode.VelocityChange);

        float t = 0f;
        float dur = Mathf.Max(0.01f, knockbackDuration);

        // Apply extra gravity over time if requested
        while (t < dur && rb != null)
        {
            if (extraGravity > 0f)
                rb.AddForce(Vector3.down * extraGravity * Time.deltaTime, ForceMode.VelocityChange);
            t += Time.deltaTime;
            yield return null;
        }

        // Hand control back to NavMesh
        if (agent != null)
        {
            agent.Warp(agent.transform.position);
            agent.isStopped = prevStopped;
        }
    }
    private IEnumerator ReenableAgentAfterDelay(NavMeshAgent agent, bool restoreStopped, float delay)
    {
        // Wait the timeout; if another path already re-enabled the agent, this will no-op.
        float t = 0f;
        while (agent != null && t < delay)
        {
            // Early out if something else already re-enabled the agent.
            if (agent.enabled) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        if (agent == null) yield break;

        // Force-enable and gently put it back on the NavMesh at its current transform
        agent.enabled = true;
        agent.Warp(agent.transform.position);
        agent.isStopped = restoreStopped;
    }

}
