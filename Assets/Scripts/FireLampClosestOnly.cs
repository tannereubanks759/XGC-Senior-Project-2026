using UnityEngine;
using UnityEngine.Animations;
using System.Collections.Generic;

public class FireLampClosestOnly : MonoBehaviour
{
    // ----- Static gating across ALL lamps -----
    private static readonly List<FireLampClosestOnly> All = new List<FireLampClosestOnly>(128);
    private static Transform PlayerRoot;
    private static Transform PlayerLook; // camera preferred
    private static FireLampClosestOnly ClosestEligible;

    // Recompute throttling
    private static bool DirtyClosest = true;
    private static int PlayersInsideCount = 0;   // # of lamps that currently contain the player
    private static int NextRecomputeFrame = 0;

    [Header("Closest Check Throttle")]
    [Tooltip("Recompute closest eligible lamp every N frames while player is inside any lamp trigger.")]
    [Range(1, 30)]
    public int recomputeEveryNFrames = 3;

    [Header("Look Gating")]
    [Tooltip("Player must be looking at the lamp within this cone (degrees) to be eligible.")]
    [Range(1f, 180f)]
    public float maxLookAngle = 25f;

    [Tooltip("If true, uses the lamp's prompt/text position (if assigned) for the look target; otherwise uses lamp transform.")]
    public bool useTextAsLookTarget = true;

    [Header("Fire Lamp")]
    private FireSourceScript fire;
    public GameObject text;
    public KeyCode collectKey = KeyCode.E;

    [Header("Runtime")]
    public bool playerInRange = false; // TRUE only for the single active lamp
    public bool hasCollected = false;

    private FireballManager fm;
    private GameObject fireballParent;

    public LookAtConstraint constraint;
    private Transform currentSource;

    // Local trigger state
    private bool playerInsideTrigger = false;
    private bool baseEligible = false; // "can collect" ignoring closest gating

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        DirtyClosest = true;
    }

    void OnDisable()
    {
        if (playerInsideTrigger)
        {
            playerInsideTrigger = false;
            PlayersInsideCount = Mathf.Max(0, PlayersInsideCount - 1);
        }

        All.Remove(this);

        if (ClosestEligible == this) ClosestEligible = null;
        DirtyClosest = true;
    }

    void Awake()
    {
        fire = GetComponentInChildren<FireSourceScript>(true);
        if (text != null) text.SetActive(false);

        constraint = GetComponentInChildren<LookAtConstraint>(true);
        if (constraint == null)
        {
            Debug.LogError($"FireLampClosestOnly on '{name}' couldn't find a LookAtConstraint in children.", this);
        }
        else
        {
            constraint.constraintActive = false;
            ClearConstraintSources();
        }
    }

    void Update()
    {
        if (PlayerRoot == null || PlayerLook == null)
        {
            SetActiveLamp(false);
            return;
        }

        // Only compute closest while player is inside at least one lamp trigger
        if (PlayersInsideCount > 0)
        {
            int f = Time.frameCount;
            bool cadenceHit = f >= NextRecomputeFrame;

            if (DirtyClosest || cadenceHit)
            {
                RecomputeClosestEligible();

                int n = Mathf.Max(1, recomputeEveryNFrames);
                NextRecomputeFrame = f + n;
            }
        }
        else
        {
            // nobody is inside any lamp triggers
            ClosestEligible = null;
        }

        // IMPORTANT: Only the chosen lamp is "active" (text shown + collect allowed)
        bool isActiveLamp = (ClosestEligible == this);

        // If not active, force everything off no matter what our trigger says
        if (!isActiveLamp)
        {
            SetActiveLamp(false);
            return;
        }

        // Active lamp must still be eligible (baseEligible) to show prompt + allow collect
        bool canCollect = baseEligible && !hasCollected && fire != null && !fire.isCollected;

        SetActiveLamp(canCollect);

        // Only the one WITH text shown can be collected:
        // We require text.activeSelf to be true as the final gate.
        if (text != null && text.activeSelf && Input.GetKeyDown(collectKey))
        {
            fire.isCollected = true;
            hasCollected = true;

            SetBaseEligible(false);
            SetActiveLamp(false);

            ClearConstraintSources();
            if (constraint != null) constraint.constraintActive = false;

            DirtyClosest = true;
        }
    }

    private void SetActiveLamp(bool on)
    {
        playerInRange = on;
        if (text != null && text.activeSelf != on) text.SetActive(on);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CachePlayerTransforms(other);

        if (!playerInsideTrigger)
        {
            playerInsideTrigger = true;
            PlayersInsideCount++;
        }

        if (constraint == null)
            constraint = GetComponentInChildren<LookAtConstraint>(true);

        if (constraint != null)
            EnsureLookAtSource(other.transform);

        DirtyClosest = true;
        NextRecomputeFrame = 0;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CachePlayerTransforms(other);

        if (!playerInsideTrigger)
        {
            playerInsideTrigger = true;
            PlayersInsideCount++;
            DirtyClosest = true;
            NextRecomputeFrame = 0;
        }

        if (fire == null || fire.isCollected || hasCollected)
        {
            SetBaseEligible(false);
            return;
        }

        if (fireballParent == null)
        {
            var offhand = other.GetComponentInChildren<offhandHandler>();
            if (offhand != null) fireballParent = offhand.fireBall;
        }

        if (fm == null)
            fm = other.GetComponentInChildren<FireballManager>();

        bool canCollectIgnoringClosest =
            fireballParent != null &&
            fireballParent.activeSelf &&
            fm != null &&
            !fm.bothReadyOrRecharging;

        // Note: do NOT show/hide text here. Only Update() controls the prompt.
        SetBaseEligible(canCollectIgnoringClosest);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (playerInsideTrigger)
        {
            playerInsideTrigger = false;
            PlayersInsideCount = Mathf.Max(0, PlayersInsideCount - 1);
        }

        SetBaseEligible(false);

        // If we were active, hide prompt immediately
        SetActiveLamp(false);

        if (other.transform == currentSource)
        {
            ClearConstraintSources();
            if (constraint != null) constraint.constraintActive = false;
        }

        DirtyClosest = true;
        NextRecomputeFrame = 0;
    }

    private void CachePlayerTransforms(Collider playerCollider)
    {
        PlayerRoot = playerCollider.transform;

        // Prefer camera for "looking at" checks
        var cam = playerCollider.GetComponentInChildren<Camera>(true);
        PlayerLook = (cam != null) ? cam.transform : PlayerRoot;
    }

    private void SetBaseEligible(bool value)
    {
        if (baseEligible != value)
        {
            baseEligible = value;
            DirtyClosest = true;
        }
    }

    private static void RecomputeClosestEligible()
    {
        DirtyClosest = false;

        if (PlayerRoot == null || PlayerLook == null || All.Count == 0 || PlayersInsideCount <= 0)
        {
            ClosestEligible = null;
            return;
        }

        Vector3 lookPos = PlayerLook.position;
        Vector3 lookFwd = PlayerLook.forward;

        float bestSqr = float.PositiveInfinity;
        FireLampClosestOnly best = null;

        Vector3 playerPos = PlayerRoot.position;

        for (int i = 0; i < All.Count; i++)
        {
            var lamp = All[i];
            if (lamp == null) continue;

            if (!lamp.playerInsideTrigger) continue;
            if (!lamp.baseEligible) continue;
            if (lamp.hasCollected) continue;
            if (lamp.fire != null && lamp.fire.isCollected) continue;

            // Look cone check
            Vector3 targetPos = lamp.GetLookTargetPosition();
            Vector3 toLamp = targetPos - lookPos;

            float sqrMag = toLamp.sqrMagnitude;
            if (sqrMag > 0.0001f)
            {
                float invMag = 1.0f / Mathf.Sqrt(sqrMag);
                Vector3 dirNorm = toLamp * invMag;

                float dot = Vector3.Dot(lookFwd, dirNorm);
                float cosLimit = Mathf.Cos(lamp.maxLookAngle * Mathf.Deg2Rad);
                if (dot < cosLimit) continue;
            }

            // Closest among eligible (distance from player root)
            Vector3 d = lamp.transform.position - playerPos;
            float distSqr = d.sqrMagnitude;

            if (distSqr < bestSqr)
            {
                bestSqr = distSqr;
                best = lamp;
            }
        }

        ClosestEligible = best;
    }

    private Vector3 GetLookTargetPosition()
    {
        if (useTextAsLookTarget && text != null)
            return text.transform.position;

        return transform.position;
    }

    private void EnsureLookAtSource(Transform target)
    {
        if (constraint == null) return;

        if (currentSource == target && constraint.constraintActive) return;

        ClearConstraintSources();

        var source = new ConstraintSource { sourceTransform = target, weight = 1f };
        constraint.AddSource(source);
        currentSource = target;

        constraint.constraintActive = true;
    }

    private void ClearConstraintSources()
    {
        if (constraint == null) return;
        constraint.SetSources(new List<ConstraintSource>());
        currentSource = null;
    }
}