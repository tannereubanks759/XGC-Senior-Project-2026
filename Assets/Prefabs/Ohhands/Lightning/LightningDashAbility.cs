using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LightningDashAbility : MonoBehaviour
{
    [Header("TP")]
    public float blinkDistance = 10f;
    public float groundCheckRayHeight = 3f;
    public float groundCheckRayDepth = 10f;
    public float groundOffset = 1f;
    public LayerMask blockingMask;
    public LayerMask groundMask;
    public float playerRadius = 0.4f;

    [Header("Charge Up")]
    public float chargeUpDuration = 0.3f;
    public float chargeUpFovBoost = 15f;
    public float fovRecoverySpeed = 10f;
    public Camera playerCamera;
    [Header("Damage")]
    public LayerMask damageMask;
    public float damageRadius = 2f;
    public float baseDamage = 15f;
    public chargeBaseScript chargeSource;
    [Header("Cooldown")]
    public float cooldownSeconds = 10f;
    public UnityEvent onDashReady;
    public UnityEvent onDashUsed;
    [Header("Refs")]
    public Rigidbody rb;
    public FirstPersonController controller;
    public Collider playerCollider;

    private bool canDash = true;
    private float cooldownTimer = 0f;
    private float baseFov;
    private bool recoveringFov = false;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        baseFov = playerCamera.fieldOfView;
    }

    private void Update()
    {
        if (!canDash)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                canDash = true;
                onDashReady?.Invoke();
            }
        }

        if (recoveringFov)
        {
            playerCamera.fieldOfView = Mathf.Lerp(
                playerCamera.fieldOfView, baseFov, fovRecoverySpeed * Time.deltaTime);

            if (Mathf.Abs(playerCamera.fieldOfView - baseFov) < 0.05f)
            {
                playerCamera.fieldOfView = baseFov;
                recoveringFov = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryDash();
        }
    }

    public void TryDash()
    {
        if (!canDash) return;
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        canDash = false;
        cooldownTimer = cooldownSeconds;
        onDashUsed?.Invoke();
        float elapsed = 0f;
        float startFov = playerCamera.fieldOfView;
        float boostedFov = baseFov + chargeUpFovBoost;
        recoveringFov = false;

        while (elapsed < chargeUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / chargeUpDuration);
            playerCamera.fieldOfView = Mathf.Lerp(startFov, boostedFov, t * t);
            yield return null;
        }
        playerCamera.fieldOfView = boostedFov;
        float charge = GetCharge01();
        float damage = baseDamage + (charge * .2f);
        Vector3 dir = Camera.main.transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) yield break;
        dir.Normalize();
        Vector3 origin = transform.position;
        float travelDistance = blinkDistance;

        if (Physics.SphereCast(origin, playerRadius, dir, out RaycastHit wallHit, blinkDistance, blockingMask, QueryTriggerInteraction.Ignore))
        {
            travelDistance = Mathf.Max(0f, wallHit.distance - playerRadius * 1.1f);
        }
        Vector3 horizontalDestination = origin + dir * travelDistance;
        Vector3 groundCheckOrigin = horizontalDestination + Vector3.up * groundCheckRayHeight;
        Vector3 destination = horizontalDestination;
        if (Physics.Raycast(groundCheckOrigin, Vector3.down, out RaycastHit groundHit, groundCheckRayHeight + groundCheckRayDepth, groundMask, QueryTriggerInteraction.Ignore))
        {
            destination = groundHit.point + Vector3.up * groundOffset;
        }
        DoDashDamage(origin, destination, damage);
        rb.linearVelocity = Vector3.zero;
        rb.MovePosition(destination);
        recoveringFov = true;
        yield return null;
    }

    private void DoDashDamage(Vector3 from, Vector3 to, float damage)
    {
        HashSet<DamageRef> hitAlready = new HashSet<DamageRef>();

        int steps = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(from, to) / (damageRadius * 0.75f)));
        for (int i = 0; i <= steps; i++)
        {
            Vector3 samplePoint = Vector3.Lerp(from, to, (float)i / steps);
            Collider[] hits = Physics.OverlapSphere(samplePoint, damageRadius, damageMask, QueryTriggerInteraction.Collide);
            foreach (Collider hit in hits)
            {
                if (hit == null) continue;
                DamageRef dr = hit.GetComponentInParent<DamageRef>();
                if (dr == null || hitAlready.Contains(dr)) continue;
                hitAlready.Add(dr);
                dr.TakeDamage(damage);
            }
        }
    }

    private float GetCharge01()
    {
        return chargeSource.currentCharge;
    }
}