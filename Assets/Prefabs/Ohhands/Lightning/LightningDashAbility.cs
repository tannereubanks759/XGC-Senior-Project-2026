using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LightningDashAbility : MonoBehaviour
{
    [Header("Dash")]
    public float dashSpeedChange = 25f;
    public float dashAccelDuration = 0.1f;
    public float maxHorizontalSpeed = 25f;
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Damage")]
    public LayerMask damageMask;
    public float damageRadius = 1.5f;
    public float baseDamage = 20f;
    public float bonusDamageAtFullCharge = 40f;
    public chargeBaseScript chargeSource;

    [Header("Cooldown")]
    public float cooldownSeconds = 10f;
    public UnityEvent onDashReady;
    public UnityEvent onDashUsed;

    [Header("Refs")]
    public Rigidbody rb;
    public FirstPersonController controller;
    public string dashingLayerName = "Dashing";
    public Collider playerCollider;
    private bool canDash = true;
    private float cooldownTimer = 0f;
    private int normalLayer;
    private int dashingLayer;

    private void Awake()
    {
        normalLayer = gameObject.layer;
        dashingLayer = LayerMask.NameToLayer(dashingLayerName);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
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

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryDash();
        }
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

        float charge = GetCharge01();
        float damage = baseDamage + (bonusDamageAtFullCharge * charge);

        // Direction - camera forward so it always goes where you're looking
        Vector3 dir = Camera.main.transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) yield break;
        dir.Normalize();

        // Zero horizontal velocity for consistent feel
        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, v.y, 0f);

        float targetAlong = dashSpeedChange;
        float startAlong = 0f;
        float prevK = speedCurve.Evaluate(0f);
        float t = 0f;
        float duration = Mathf.Max(0.0001f, dashAccelDuration);

        playerCollider.gameObject.layer = dashingLayer;
        HashSet<DamageRef> hitAlready = new HashSet<DamageRef>();

        if (controller != null) controller.playerCanMove = false;

        while (t < duration)
        {
            yield return new WaitForFixedUpdate();
            float dt = Time.fixedDeltaTime;
            t = Mathf.Min(t + dt, duration);

            float k = speedCurve.Evaluate(t / duration);
            float desiredDelta = (targetAlong - startAlong) * (k - prevK);
            prevK = k;

            Vector3 vCur = rb.linearVelocity;
            float curAlong = Vector3.Dot(new Vector3(vCur.x, 0f, vCur.z), dir);
            float remain = targetAlong - curAlong;

            float stepDeltaV = 0f;
            if (Mathf.Sign(desiredDelta) == Mathf.Sign(remain))
                stepDeltaV = Mathf.Clamp(desiredDelta, -Mathf.Abs(remain), Mathf.Abs(remain));

            if (dt > 0f && stepDeltaV != 0f)
            {
                Vector3 a = (dir * stepDeltaV) / dt;
                a.y = 0f;
                rb.AddForce(a, ForceMode.Acceleration);
            }

            // Horizontal speed cap
            Vector3 hVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (hVel.magnitude > maxHorizontalSpeed)
                rb.linearVelocity = new Vector3(
                    hVel.normalized.x * maxHorizontalSpeed,
                    rb.linearVelocity.y,
                    hVel.normalized.z * maxHorizontalSpeed
                );

            DoDashDamage(transform.position, damage, hitAlready);
        }

        if (controller != null) controller.playerCanMove = true;
        playerCollider.gameObject.layer = normalLayer;
        //Vector3 h = rb.linearVelocity;
        //if (h.y > 0f) rb.linearVelocity = new Vector3(h.x, 0f, h.z);
    }

    private void DoDashDamage(Vector3 center, float damage, HashSet<DamageRef> hitAlready)
    {
        Collider[] hits = Physics.OverlapSphere(center, damageRadius, damageMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            DamageRef dr = hits[i].GetComponentInParent<DamageRef>();
            if (dr == null) continue;
            if (hitAlready.Contains(dr)) continue;
            hitAlready.Add(dr);
            dr.TakeDamage(damage);
        }
    }

    private float GetCharge01()
    {
        return chargeSource.currentCharge;
    }
}