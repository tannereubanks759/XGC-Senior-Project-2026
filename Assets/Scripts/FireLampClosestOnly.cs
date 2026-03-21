using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class FireLampClosestOnly : MonoBehaviour
{
    // ----- Static gating across ALL lamps -----
    private static readonly List<FireLampClosestOnly> All = new List<FireLampClosestOnly>(128);
    private static Transform PlayerRoot;
    private static Transform PlayerLook;
    private static FireLampClosestOnly ClosestEligible;

    // Hard guarantee: only 1 collect can happen per frame
    private static int LastCollectFrame = -999999;

    // Recompute throttling
    private static bool DirtyClosest = true;
    private static int PlayersInsideCount = 0;
    private static int NextRecomputeFrame = 0;

    [Header("Closest Check Throttle")]
    [Range(1, 30)] public int recomputeEveryNFrames = 3;

    [Header("Look Gating")]
    [Range(1f, 180f)] public float maxLookAngle = 25f;
    public bool useTextAsLookTarget = true;

    [Header("Fire Lamp")]
    private FireSourceScript fire;
    public GameObject text;
    public KeyCode collectKey = KeyCode.E;

    [Header("Respawn")]
    public bool respawnFire = true;
    public GameObject firePrefab;
    public float respawnDelay = 10f;

    [Tooltip("Optional. If left empty, the fire respawns at this object's transform.")]
    public Transform fireSpawnPoint;

    [Header("Scene Restriction")]
    public bool onlyRespawnInSpecificScene = true;
    public string allowedSceneName = "YourSceneNameHere";

    [Header("Runtime")]
    public bool playerInRange = false;
    public bool hasCollected = false;

    private FireballManager fm;
    private GameObject fireballParent;

    public LookAtConstraint constraint;
    private Transform currentSource;
    private Transform currentPlayerTransform;

    private bool playerInsideTrigger = false;
    private bool baseEligible = false;
    private Coroutine respawnRoutine;

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

        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }
    }

    void Awake()
    {
        RefreshFireReference();

        if (text != null)
            text.SetActive(false);

        if (fireSpawnPoint == null)
            fireSpawnPoint = transform;

        RefreshConstraintReference();

        if (constraint != null)
        {
            constraint.constraintActive = false;
            ClearConstraintSources();
        }
    }

    void Update()
    {
        RefreshRuntimeReferences();

        if (PlayerRoot == null || PlayerLook == null)
        {
            SetActiveLamp(false);
            DisableConstraintIfNeeded();
            return;
        }

        if (PlayersInsideCount > 0)
        {
            int f = Time.frameCount;
            if (DirtyClosest || f >= NextRecomputeFrame)
            {
                RecomputeClosestEligible();
                NextRecomputeFrame = f + Mathf.Max(1, recomputeEveryNFrames);
            }
        }
        else
        {
            ClosestEligible = null;
        }

        bool isActiveLamp = (ClosestEligible == this);

        if (!isActiveLamp)
        {
            SetActiveLamp(false);
            MaintainLookConstraint();
            return;
        }

        bool canCollect =
            baseEligible &&
            !hasCollected &&
            fire != null &&
            !fire.isCollected;

        SetActiveLamp(canCollect);
        MaintainLookConstraint();

        if (playerInRange &&
            Input.GetKeyDown(collectKey) &&
            LastCollectFrame != Time.frameCount)
        {
            LastCollectFrame = Time.frameCount;

            if (fire != null)
                fire.isCollected = true;

            hasCollected = true;

            SetBaseEligible(false);
            SetActiveLamp(false);

            ClearConstraintSources();
            DisableConstraintIfNeeded();

            DirtyClosest = true;

            if (respawnFire && CanRespawnInCurrentScene())
            {
                if (respawnRoutine != null)
                    StopCoroutine(respawnRoutine);

                respawnRoutine = StartCoroutine(RespawnFireAfterDelay());
            }
        }
    }

    private bool CanRespawnInCurrentScene()
    {
        if (!onlyRespawnInSpecificScene)
            return true;

        return SceneManager.GetActiveScene().name == allowedSceneName;
    }

    private void SetActiveLamp(bool on)
    {
        playerInRange = on;

        if (text != null && text.activeSelf != on)
            text.SetActive(on);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        currentPlayerTransform = other.transform;
        CachePlayerTransforms(other);

        if (!playerInsideTrigger)
        {
            playerInsideTrigger = true;
            PlayersInsideCount++;
        }

        RefreshConstraintReference();
        EnsureLookAtSource(other.transform);

        DirtyClosest = true;
        NextRecomputeFrame = 0;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        currentPlayerTransform = other.transform;
        CachePlayerTransforms(other);
        RefreshRuntimeReferences();
        EnsureLookAtSource(other.transform);

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
            var offhand = other.GetComponentInChildren<offhandHandler>(true);
            if (offhand != null)
                fireballParent = offhand.fireBall;
        }

        if (fm == null)
            fm = other.GetComponentInChildren<FireballManager>(true);

        bool canCollectIgnoringClosest =
            fireballParent != null &&
            fireballParent.activeSelf &&
            fm != null &&
            !fm.bothReadyOrRecharging;

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

        if (currentPlayerTransform == other.transform)
            currentPlayerTransform = null;

        SetBaseEligible(false);
        SetActiveLamp(false);

        if (other.transform == currentSource)
        {
            ClearConstraintSources();
            DisableConstraintIfNeeded();
        }

        DirtyClosest = true;
        NextRecomputeFrame = 0;
    }

    private void CachePlayerTransforms(Collider playerCollider)
    {
        PlayerRoot = playerCollider.transform;

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
        Vector3 playerPos = PlayerRoot.position;

        float bestSqr = float.PositiveInfinity;
        FireLampClosestOnly best = null;

        for (int i = 0; i < All.Count; i++)
        {
            var lamp = All[i];
            if (lamp == null) continue;

            lamp.RefreshRuntimeReferences();

            if (!lamp.playerInsideTrigger) continue;
            if (!lamp.baseEligible) continue;
            if (lamp.hasCollected) continue;
            if (lamp.fire != null && lamp.fire.isCollected) continue;

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

    private void RefreshRuntimeReferences()
    {
        RefreshFireReference();
        RefreshConstraintReference();
    }

    private void RefreshFireReference()
    {
        if (fire != null)
            return;

        fire = GetComponentInChildren<FireSourceScript>(true);
    }

    private void RefreshConstraintReference()
    {
        if (constraint != null)
            return;

        constraint = GetComponentInChildren<LookAtConstraint>(true);
    }

    private void MaintainLookConstraint()
    {
        RefreshConstraintReference();

        if (!playerInsideTrigger || currentPlayerTransform == null)
        {
            ClearConstraintSources();
            DisableConstraintIfNeeded();
            return;
        }

        EnsureLookAtSource(currentPlayerTransform);
    }

    private void EnsureLookAtSource(Transform target)
    {
        if (target == null)
        {
            ClearConstraintSources();
            DisableConstraintIfNeeded();
            return;
        }

        RefreshConstraintReference();
        if (constraint == null) return;

        bool needsRebind = false;

        if (currentSource != target)
        {
            needsRebind = true;
        }
        else if (constraint.sourceCount == 0)
        {
            needsRebind = true;
        }
        else
        {
            ConstraintSource existing = constraint.GetSource(0);
            if (existing.sourceTransform != target || existing.weight <= 0f)
                needsRebind = true;
        }

        if (needsRebind)
        {
            ClearConstraintSources();

            var source = new ConstraintSource
            {
                sourceTransform = target,
                weight = 1f
            };

            constraint.AddSource(source);
            currentSource = target;
        }

        if (!constraint.locked)
            constraint.locked = true;

        if (!constraint.constraintActive)
            constraint.constraintActive = true;

        constraint.weight = 1f;
    }

    private void ClearConstraintSources()
    {
        if (constraint == null) return;
        constraint.SetSources(new List<ConstraintSource>());
        currentSource = null;
    }

    private void DisableConstraintIfNeeded()
    {
        if (constraint != null)
            constraint.constraintActive = false;
    }

    private IEnumerator RespawnFireAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (!CanRespawnInCurrentScene())
        {
            respawnRoutine = null;
            yield break;
        }

        if (fire != null)
        {
            Destroy(fire.gameObject);
            fire = null;
        }

        if (constraint != null && fire != null)
        {
            constraint = null;
        }
        else
        {
            constraint = null;
        }

        if (firePrefab != null)
        {
            GameObject newFireObj = Instantiate(
                firePrefab,
                fireSpawnPoint.position,
                fireSpawnPoint.rotation,
                fireSpawnPoint
            );

            fire = newFireObj.GetComponent<FireSourceScript>();

            if (fire == null)
                fire = newFireObj.GetComponentInChildren<FireSourceScript>(true);
        }

        RefreshRuntimeReferences();

        hasCollected = false;
        SetBaseEligible(false);
        SetActiveLamp(false);
        DirtyClosest = true;

        if (playerInsideTrigger && currentPlayerTransform != null)
            EnsureLookAtSource(currentPlayerTransform);

        respawnRoutine = null;
    }
}
