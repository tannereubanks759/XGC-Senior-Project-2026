using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)] // manager updates early
public class EnemyFootstepsManager : MonoBehaviour
{
    public static EnemyFootstepsManager Instance { get; private set; }

    [Header("References")]
    public Transform player;

    [Header("Selection")]
    [Min(1)] public int closestCount = 3;
    [Min(0.05f)] public float refreshSeconds = 0.25f;

    // Internal
    private readonly List<EnemyFootsteps> enemies = new List<EnemyFootsteps>(128);
    private readonly List<EnemyFootsteps> topClosest = new List<EnemyFootsteps>(16);
    private float nextRefreshTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate EnemyFootstepsManager found. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!player)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Register(EnemyFootsteps e)
    {
        if (e == null) return;
        if (Instance == null) return; // manager not ready yet
        Instance.RegisterInternal(e);
    }

    public static void Unregister(EnemyFootsteps e)
    {
        if (e == null) return;
        if (Instance == null) return;
        Instance.UnregisterInternal(e);
    }

    private void RegisterInternal(EnemyFootsteps e)
    {
        // Prevent duplicates
        if (!enemies.Contains(e))
            enemies.Add(e);
    }

    private void UnregisterInternal(EnemyFootsteps e)
    {
        enemies.Remove(e);
    }

    private void Update()
    {
        if (!player)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
            if (!player) return;
        }

        if (Time.time < nextRefreshTime) return;
        nextRefreshTime = Time.time + refreshSeconds;

        RefreshClosest();
    }

    private void RefreshClosest()
    {
        // Clean nulls + disable all
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            e.footstepsAllowed = false;
        }

        if (enemies.Count == 0) return;

        topClosest.Clear();
        int targetCount = Mathf.Min(closestCount, enemies.Count);

        // Maintain a small sorted "topClosest" list of size targetCount
        // O(N*K), great when K is tiny (3)
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyFootsteps e = enemies[i];
            if (!e.isActiveAndEnabled) continue;

            float d2 = (e.transform.position - player.position).sqrMagnitude;

            if (topClosest.Count < targetCount)
            {
                InsertSortedByDistance(topClosest, e, d2);
            }
            else
            {
                float farthestD2 = (topClosest[topClosest.Count - 1].transform.position - player.position).sqrMagnitude;
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
            if (itemD2 < d2)
            {
                insertAt = i;
                break;
            }
        }
        list.Insert(insertAt, item);
    }
}
