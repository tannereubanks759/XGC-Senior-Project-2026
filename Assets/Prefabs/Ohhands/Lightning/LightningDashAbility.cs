using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LightningDashAbility : MonoBehaviour
{
    [Header("Dash")]
    public bool dashUnlocked;
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

    [Header("Vignette")]
    public Image vignetteImage;
    public float vignetteStartAlpha = 0.2f;
    public float vignettePeakAlpha = 1f;
    public float vignetteRecoverySpeed = 3f;

    [Header("Damage")]
    public LayerMask damageMask;
    public float damageRadius = 1.5f;
    public float baseDamage = 20f;
    public chargeBaseScript chargeSource;

    [Header("Cooldown")]
    public float cooldownSeconds = 10f;
    public UnityEvent onDashReady;
    public UnityEvent onDashUsed;
    [Header("Audio")]
    public AudioSource source;
    public AudioClip chargeUp;
    [Range(0f, 1f)] public float chargeUpVol = 0.8f;
    public AudioClip dashSound;
    [Range(0f, 1f)] public float dashVol = 0.8f;

    [Header("Refs")]
    public Rigidbody rb;
    public FirstPersonController controller;
    public Collider playerCollider;
    public string dashingLayerName = "Dashing";
    public string defaultLayerName = "Player";
    public PopUpMessage pum;
    private bool canDash = true;
    private float cooldownTimer = 0f;
    private float baseFov;
    private bool recoveringFov = false;
    private bool recoveringVignette = false;
    private int normalLayer;
    private int dashingLayer;
    public TextMeshProUGUI cooldownTime;
    public GameObject dashUIObject;
    public FirstPersonController fpc;

    private void Awake()
    {
        if(!dashUnlocked)
        {
            dashUIObject.SetActive(false);
            return;
        }
        if (playerCamera == null) playerCamera = Camera.main;
        baseFov = playerCamera.fieldOfView;
        normalLayer = LayerMask.NameToLayer(defaultLayerName);
        dashingLayer = LayerMask.NameToLayer(dashingLayerName);

        // Start fully invisible
        SetVignetteAlpha(0f);
    }
    private void OnDisable()
    {
        if (!dashUnlocked)
        {
            dashUIObject.SetActive(false);
            return;
        }
        StopAllCoroutines();
        SetVignetteAlpha(0f);
       /* if (playerCamera != null)
            playerCamera.fieldOfView = fpc.fov;*/
        recoveringFov = false;
        recoveringVignette = false;
        rb.useGravity = true;
        if (controller != null) controller.playerCanMove = true;
        playerCollider.gameObject.layer = normalLayer;
    }
    private void PlaySound(AudioClip clip, float vol = 1f)
    {
        if (source != null && clip != null)
            source.PlayOneShot(clip, vol);
    }
    private void Update()
    {
        if (dashUnlocked)
        {
            dashUIObject.SetActive(true);
            
        }
        if (!canDash)
        {
            cooldownTime.alpha = 1f;
            cooldownTimer -= Time.deltaTime;
            cooldownTime.text = cooldownTimer.ToString("#");
            if (cooldownTimer <= 0f)
            {
                cooldownTime.alpha = 0f;
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

        if (recoveringVignette)
        {
            float current = GetVignetteAlpha();
            float next = Mathf.Lerp(current, 0f, vignetteRecoverySpeed * Time.deltaTime);
            SetVignetteAlpha(next);

            if (next < 0.01f)
            {
                SetVignetteAlpha(0f);
                recoveringVignette = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.F) && dashUnlocked)
            TryDash();
    }

    public void TryDash()
    {
        if (!canDash)
        {
            pum.ShowMessage("On cooldown (" + cooldownTimer.ToString("#.#") + " seconds)");
            return;
        }
            
        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        cooldownTimer = cooldownSeconds;
        onDashUsed?.Invoke();

        float elapsed = 0f;
        baseFov = fpc.fov;
        float startFov = playerCamera.fieldOfView;
        float boostedFov = startFov + chargeUpFovBoost;
        recoveringFov = false;
        recoveringVignette = false;
        SetVignetteAlpha(vignetteStartAlpha);
        PlaySound(dashSound, dashVol);
        try
        {
            while (elapsed < chargeUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / chargeUpDuration);
                float tEased = t * t;

                playerCamera.fieldOfView = Mathf.Lerp(startFov, boostedFov, tEased);
                SetVignetteAlpha(Mathf.Lerp(vignetteStartAlpha, vignettePeakAlpha, tEased));

                yield return null;
            }

            playerCamera.fieldOfView = boostedFov;
            SetVignetteAlpha(vignettePeakAlpha);

            // Destination
            Vector3 dir = Camera.main.transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) yield break;
            dir.Normalize();

            Vector3 origin = transform.position;

            float travelDistance = blinkDistance;
            if (Physics.SphereCast(origin, playerRadius, dir, out RaycastHit wallHit, blinkDistance, blockingMask, QueryTriggerInteraction.Ignore))
                travelDistance = Mathf.Max(0f, wallHit.distance - playerRadius * 1.1f);

            Vector3 horizontalDestination = origin + dir * travelDistance;
            Vector3 groundCheckOrigin = horizontalDestination + Vector3.up * groundCheckRayHeight;
            Vector3 destination = horizontalDestination;

            if (Physics.Raycast(groundCheckOrigin, Vector3.down, out RaycastHit groundHit,
                groundCheckRayHeight + groundCheckRayDepth, groundMask, QueryTriggerInteraction.Ignore))
                destination = groundHit.point + Vector3.up * groundOffset;

            //Dash travel
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

                rb.MovePosition(Vector3.LerpUnclamped(origin, destination, curved));
                DoDashDamage(transform.position, damage, hitAlready);
            }

            rb.MovePosition(destination);
        }
        // Charge up
        finally
        {
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            if (controller != null) controller.playerCanMove = true;
            playerCollider.gameObject.layer = normalLayer;
            recoveringFov = true;
            recoveringVignette = true;
        }
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (vignetteImage == null) return;
        Color c = vignetteImage.color;
        c.a = alpha;
        vignetteImage.color = c;
    }

    private float GetVignetteAlpha()
    {
        if (vignetteImage == null) return 0f;
        return vignetteImage.color.a;
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