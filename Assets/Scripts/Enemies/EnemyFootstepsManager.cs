using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class EnemyFootstepsManager : MonoBehaviour
{
    public static EnemyFootstepsManager Instance { get; private set; }
    public static bool InstanceExists => Instance != null;

    [Header("References")]
    public Transform player;

    [Header("Selection")]
    [Min(1)] public int closestCount = 3;
    [Min(0.05f)] public float refreshSeconds = 0.25f;

    // Use HashSet to prevent duplicates without O(n) Contains checks
    private readonly HashSet<EnemyFootsteps> enemies = new HashSet<EnemyFootsteps>();
    private readonly List<EnemyFootsteps> scratch = new List<EnemyFootsteps>(256);
    private readonly List<EnemyFootsteps> topClosest = new List<EnemyFootsteps>(16);

    private float nextRefreshTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // IMPORTANT: don't destroy the whole GO (can be part of other systems)
            // Just disable this component.
            enabled = false;
            return;
        }

        Instance = this;

        // If you have PlayerLocator, use it (zero search cost)
        if (!player && PlayerLocator.PlayerRoot != null)
            player = PlayerLocator.PlayerRoot;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Register(EnemyFootsteps e)
    {
        if (e == null) return;
        if (Instance == null || !Instance.enabled) return;
        Instance.enemies.Add(e);
    }

    public static void Unregister(EnemyFootsteps e)
    {
        if (e == null) return;
        if (Instance == null || !Instance.enabled) return;
        Instance.enemies.Remove(e);
    }

    // Call this from your player once (recommended)
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    private void Update()
    {
        // Lazy assign from PlayerLocator (no searching)
        if (!player && PlayerLocator.PlayerRoot != null)
            player = PlayerLocator.PlayerRoot;

        if (!player) return;

        if (Time.time < nextRefreshTime) return;
        nextRefreshTime = Time.time + refreshSeconds;

        RefreshClosest();
    }

    private void RefreshClosest()
    {
        // Disable all + clean nulls without modifying the HashSet while iterating
        scratch.Clear();
        foreach (var e in enemies)
        {
            if (e == null) continue;
            e.footstepsAllowed = false;
            if (e.isActiveAndEnabled) scratch.Add(e);
        }

        // Remove nulls after
        // (We only remove nulls occasionally; doing it every tick is fine too)
        enemies.RemoveWhere(e => e == null);

        if (scratch.Count == 0) return;

        topClosest.Clear();
        int targetCount = Mathf.Min(closestCount, scratch.Count);

        // O(N*K) with tiny K
        for (int i = 0; i < scratch.Count; i++)
        {
            var e = scratch[i];
            float d2 = (e.transform.position - player.position).sqrMagnitude;

            if (topClosest.Count < targetCount)
            {
                InsertSortedByDistance(topClosest, e, d2);
            }
            else
            {
                float farthestD2 =
                    (topClosest[topClosest.Count - 1].transform.position - player.position).sqrMagnitude;

                if (d2 < farthestD2)
                {
                    topClosest.RemoveAt(topClosest.Count - 1);
                    InsertSortedByDistance(topClosest, e, d2);
                }
            }
        }

        for (int i = 0; i < topClosest.Count; i++)
            topClosest[i].footstepsAllowed = true;
    }

    private void InsertSortedByDistance(List<EnemyFootsteps> list, EnemyFootsteps item, float itemD2)
    {
        int insertAt = list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            float d2 = (list[i].transform.position - player.position).sqrMagnitude;
            if (itemD2 < d2) { insertAt = i; break; }
        }
        list.Insert(insertAt, item);
    }
}