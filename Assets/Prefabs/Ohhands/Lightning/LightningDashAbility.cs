using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LightningDashAbility : MonoBehaviour
{
    [Header("Dash")]
    public float blinkDistance = 10f;
    public float dashTravelDuration = 0.12f;
    public AnimationCurve travelCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Header("Ground Detection")]
    public float groundCheckRayHeight = 3f;
    public float groundCheckRayDepth = 10f;
    public float groundOffset = .5f;
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
    public float damageRadius = 1.5f;
    public float baseDamage = 20f;
    public chargeBaseScript chargeSource;
    [Header("Cooldown")]
    public float cooldownSeconds = 10f;
    public UnityEvent onDashReady;
    public UnityEvent onDashUsed;
    [Header("Refs")]
    public Rigidbody rb;
    public FirstPersonController controller;
    public Collider playerCollider;
    public string dashingLayerName = "Dashing";
    public string defaultLayerName = "Player";
    private bool canDash = true;
    private float cooldownTimer = 0f;
    private float baseFov;
    private bool recoveringFov = false;
    private int normalLayer;
    private int dashingLayer;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        baseFov = playerCamera.fieldOfView;
        normalLayer = LayerMask.NameToLayer(defaultLayerName);
        dashingLayer = LayerMask.NameToLayer(dashingLayerName);
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
            TryDash();
    }

    public void TryDash()
    {
        if (!canDash) return;
        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
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
        Vector3 dir = Camera.main.transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) yield break;
        dir.Normalize();
        Vector3 origin = transform.position;

        // Wall check
        float travelDistance = blinkDistance;
        if (Physics.SphereCast(origin, playerRadius, dir, out RaycastHit wallHit, blinkDistance, blockingMask, QueryTriggerInteraction.Ignore))
            travelDistance = Mathf.Max(0f, wallHit.distance - playerRadius * 1.1f);

        Vector3 horizontalDestination = origin + dir * travelDistance;
        Vector3 groundCheckOrigin = horizontalDestination + Vector3.up * groundCheckRayHeight;
        Vector3 destination = horizontalDestination;
        if (Physics.Raycast(groundCheckOrigin, Vector3.down, out RaycastHit groundHit,
            groundCheckRayHeight + groundCheckRayDepth, groundMask, QueryTriggerInteraction.Ignore))
            destination = groundHit.point + Vector3.up * groundOffset;
        playerCollider.gameObject.layer = dashingLayer;
        if (controller != null) controller.playerCanMove = false;

        float charge = GetCharge01();
        float damage = baseDamage + (charge * .2f);
        HashSet<DamageRef> hitAlready = new HashSet<DamageRef>();

        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;

        float dashElapsed = 0f;
        float duration = Mathf.Max(0.0001f, dashTravelDuration);

        while (dashElapsed < duration)
        {
            yield return new WaitForFixedUpdate();
            dashElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(dashElapsed / duration);
            float curved = travelCurve.Evaluate(t);

            Vector3 nextPos = Vector3.LerpUnclamped(origin, destination, curved);
            rb.MovePosition(nextPos);

            DoDashDamage(transform.position, damage, hitAlready);
        }

        rb.MovePosition(destination);
        rb.useGravity = true;

        if (controller != null) controller.playerCanMove = true;
        playerCollider.gameObject.layer = normalLayer;
        recoveringFov = true;
    }

    private void DoDashDamage(Vector3 center, float damage, HashSet<DamageRef> hitAlready)
    {
        Collider[] hits = Physics.OverlapSphere(center, damageRadius, damageMask, QueryTriggerInteraction.Collide);
        foreach (Collider hit in hits)
        {
            if (hit == null) continue;
            DamageRef dr = hit.GetComponentInParent<DamageRef>();
            if (dr == null || hitAlready.Contains(dr)) continue;
            hitAlready.Add(dr);
            dr.TakeDamage(damage);
        }
    }

    private float GetCharge01() => chargeSource.currentCharge;
}