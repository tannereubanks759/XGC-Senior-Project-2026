using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine;
using System.Collections.Generic;

public class IslandSetup : MonoBehaviour
{
    [Header("Chest Prefabs By Rarity")]
    public GameObject[] ChestObjects;
    private GameObject[] usableChests;

    [Header("Chest Spawn Points")]
    public Transform[] chestLocations;

    [Header("NavMesh")]
    public NavMeshSurface surface;

    [Header("Player Spawn")]
    public GameObject playerPref; // used only if player doesn't already exist in scene
    public Transform[] playerSpawnPos;

    // === NEW: define quotas as *per chest* ===
    [System.Serializable]
    public class EnemyQuotaPerChest
    {
        public GameObject prefab;
        [Min(0)] public int MaxPerChest = 3; // how many of this enemy may spawn AROUND EACH CHEST
    }

    [Header("Enemy Spawning (Per-Chest Quotas)")]
    [Tooltip("Define each enemy prefab and how many of that enemy may spawn around EACH chest.")]
    public List<EnemyQuotaPerChest> enemyQuotas = new List<EnemyQuotaPerChest>();

    [Tooltip("Spawn radius (meters) around each chest where enemies are placed.")]
    public float spawnRadius = 10f;

    [Tooltip("Try to spawn this many enemies around each chest (capped by the sum of per-chest quotas).")]
    [Min(0)] public int enemiesPerChest = 5;

    [Tooltip("Max NavMesh sampling attempts for EACH enemy near a chest before giving up on that enemy.")]
    [Min(1)] public int maxSampleTriesPerEnemy = 8;

    [Tooltip("How far off the random point we allow NavMesh.SamplePosition to snap.")]
    [Min(0.1f)] public float navMeshSnapMaxDistance = 5f;


    void Start()
    {
        //SpawnChests();
    }

    private void Awake()
    {
        SpawnPlayer();
    }

    void SpawnChests()
    {
        usableChests = ChestObjects;

        if (usableChests == null || usableChests.Length == 0)
        {
            Debug.LogWarning("No usable chests configured for the selected rarity.");
            return;
        }

        for (int i = 0; i < chestLocations.Length; i++)
        {
            var chestAnchor = chestLocations[i];
            if (!Physics.Raycast(chestAnchor.position, Vector3.down, out var groundHit))
            {
                Debug.LogWarning($"Chest raycast failed at chest location index {i}.");
                continue;
            }

            int rand = Random.Range(0, usableChests.Length);
            var chest = Instantiate(
                usableChests[rand],
                groundHit.point,
                Quaternion.FromToRotation(Vector3.up, groundHit.normal)
            );

            chest.AddComponent<PatrolArea>();

            chest.GetComponent<PatrolArea>().patrolRadius = 25f;

            // === NEW: spawn enemies with a FRESH per-chest quota set ===
            SpawnEnemiesAroundChest(groundHit);
        }
    }

    // --- Per-chest spawning using local quotas ---
    void SpawnEnemiesAroundChest(RaycastHit chestGroundHit)
    {
        if (enemyQuotas == null || enemyQuotas.Count == 0) return;

        // Build local "remaining" counts for this chest
        int countTypes = enemyQuotas.Count;
        int[] remaining = new int[countTypes];
        int totalAvailable = 0;
        for (int i = 0; i < countTypes; i++)
        {
            int cap = Mathf.Max(0, enemyQuotas[i].MaxPerChest);
            remaining[i] = cap;
            totalAvailable += cap;
        }

        if (totalAvailable <= 0) return;

        int targetForThisChest = Mathf.Min(enemiesPerChest, totalAvailable);

        int spawned = 0;
        int safetyAttempts = 0;
        int maxOverallAttempts = targetForThisChest * maxSampleTriesPerEnemy * 2; // generous safety

        while (spawned < targetForThisChest && totalAvailable > 0 && safetyAttempts < maxOverallAttempts)
        {
            safetyAttempts++;

            int idx = ChooseIndexWeightedByRemaining(remaining, totalAvailable);
            if (idx < 0) break; // nothing remains

            var prefab = enemyQuotas[idx].prefab;
            if (prefab == null)
            {
                // Skip invalid entry, mark as consumed to avoid infinite loop
                remaining[idx] = 0;
                totalAvailable = Sum(remaining);
                continue;
            }

            // Try several random offsets to find a valid NavMesh point for THIS enemy
            bool placed = false;
            for (int attempt = 0; attempt < maxSampleTriesPerEnemy; attempt++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 randomWorld = chestGroundHit.point + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(randomWorld, out var navHit, navMeshSnapMaxDistance, NavMesh.AllAreas))
                {
                    Instantiate(prefab, navHit.position, Quaternion.identity);
                    placed = true;
                    break;
                }
            }

            // Consume one from this type whether placed or not? We only consume on success.
            if (placed)
            {
                remaining[idx]--;
                totalAvailable--;
                spawned++;
            }
            else
            {
                // Couldn't place this type this time—try another type next loop
                // To avoid repeatedly picking an unplaceable type forever, temporarily "reserve" one attempt:
                // If this type keeps failing, it will be re-picked less often due to weighting.
                // (Optional: you could track per-type failures and zero them out after N failures.)
            }
        }
        // Debug.Log($"Spawned {spawned}/{targetForThisChest} around this chest.");
    }

    int ChooseIndexWeightedByRemaining(int[] remaining, int total)
    {
        if (total <= 0) return -1;
        int pick = Random.Range(0, total);
        for (int i = 0; i < remaining.Length; i++)
        {
            int r = remaining[i];
            if (r <= 0) continue;
            if (pick < r) return i;
            pick -= r;
        }
        return -1;
    }

    int Sum(int[] arr)
    {
        int s = 0;
        for (int i = 0; i < arr.Length; i++) s += arr[i];
        return s;
    }

    // --- Player ---
    void SpawnPlayer()
    {
        if (playerSpawnPos == null || playerSpawnPos.Length == 0)
        {
            Debug.LogWarning("No player spawn transforms assigned.");
            return;
        }

        int random = Random.Range(0, playerSpawnPos.Length);
        if (!Physics.Raycast(playerSpawnPos[random].position, Vector3.down, out var hit))
        {
            Debug.LogWarning("Player spawn raycast failed; placing at raw transform position.");
            hit.point = playerSpawnPos[random].position;
        }

        var existing = GameObject.FindGameObjectWithTag("Player");
        if (existing)
        {
            existing.transform.position = hit.point;
            Debug.Log("Existing player moved to spawn.");
            GameObject.FindAnyObjectByType<CompassUI>().headingSource = existing.GetComponentInChildren<Camera>().transform;
        }
        else
        {
            GameObject player = Instantiate(playerPref, hit.point, Quaternion.identity);
            GameObject.FindAnyObjectByType<CompassUI>().headingSource = player.GetComponentInChildren<Camera>().transform;
        }

        
    }
}
