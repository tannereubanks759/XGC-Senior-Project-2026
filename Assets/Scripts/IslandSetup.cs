using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[DefaultExecutionOrder(-200)]
public class IslandSetup : MonoBehaviour
{
    [Header("Terrain & Water")]
    public Terrain terrain;
    public float oceanLevelY = 0f;
    [Tooltip("Chest point must be at least this much above sea level.")]
    public float minHeightAboveSea = 1.0f;
    [Tooltip("Horizontal distance from the shoreline (terrain/sea intersection).")]
    public float minDistanceFromShore = 10f;
    [Tooltip("Maximum allowed ground slope at chest anchor (degrees).")]
    [Range(0f, 89f)] public float maxSlopeDegrees = 28f;

    [Header("Forbidden Zones")]
    [Tooltip("Chests must NOT be placed inside/near these trigger colliders.")]
    public List<Collider> forbiddenTriggers = new List<Collider>();
    [Tooltip("Extra padding distance from the above colliders (meters).")]
    public float triggerPadding = 1.0f;

    [Header("Chest Placement")]
    public bool randomizeChestAnchors = true;
    [Tooltip("How far to jitter the actual chest from its anchor.")]
    public float chestPlacementRadius = 10f;
    [Tooltip("How many random attempts per chest to find a valid anchor.")]
    public int maxAnchorAttempts = 50;

    [Header("Chest Spacing")]
    [Tooltip("Minimum distance between FINAL chest spawns (meters, XZ).")]
    public float minChestSeparation = 30f;
    [Tooltip("Attempts per anchor to find an offset spot that passes all checks + spacing.")]
    public int maxChestPlacementAttempts = 20;

    [Header("Spawn Lists")]
    [Tooltip("Prefabs that can be used for chests.")]
    public GameObject[] usableChests;
    public Transform[] chestLocations;

    [Header("NavMesh")]
    public NavMeshSurface surface;

    [Header("Player")]
    public GameObject playerPref;
    public Transform[] playerSpawnPos;

    [Header("Enemy Spawning")]
    public GameObject[] basicEnemies;
    public float spawnRadius = 10f;
    public int enemiesPerChest = 5;

    // Cache
    TerrainData tData;
    Vector3 tPos;
    Vector3 tSize;

    // Track placed chest positions (XZ) for spacing checks
    readonly List<Vector2> placedChestXZ = new();

    void Awake()
    {
        InitTerrainRefs();
        SpawnPlayer();
    }

    void Start()
    {
        InitTerrainRefs();

        if (randomizeChestAnchors)
            RandomizeChestAnchorPositions();

        SpawnChests();
    }

    void InitTerrainRefs()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            tData = terrain.terrainData;
            tPos = terrain.transform.position;
            tSize = tData.size;
        }
    }

    // ---------------- Chest Anchor Randomization ----------------

    void RandomizeChestAnchorPositions()
    {
        if (terrain == null || tData == null) return;
        if (chestLocations == null) return;

        for (int i = 0; i < chestLocations.Length; i++)
        {
            Vector3 found;
            if (TryFindValidAnchor(out found))
            {
                chestLocations[i].position = found;
            }
            else
            {
                Debug.LogWarning($"[Chest #{i}] Failed to find a valid anchor after {maxAnchorAttempts} attempts; leaving as-is.");
            }
        }
    }

    bool TryFindValidAnchor(out Vector3 result)
    {
        float minX = tPos.x;
        float maxX = tPos.x + tSize.x;
        float minZ = tPos.z;
        float maxZ = tPos.z + tSize.z;

        for (int attempt = 0; attempt < maxAnchorAttempts; attempt++)
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + tPos.y;

            if (y < oceanLevelY + minHeightAboveSea) continue;

            Vector3 pos = new Vector3(x, y, z);

            float slopeDeg = GetSlopeDegrees(x, z);
            if (slopeDeg > maxSlopeDegrees) continue;

            float d = EstimateDistanceToShore(pos, stepMeters: 2f, maxMeters: 100f, heightTol: 0.25f);
            if (d < minDistanceFromShore) continue;

            if (IsInsideForbidden(pos, triggerPadding)) continue;

            result = pos;
            return true;
        }

        result = default;
        return false;
    }

    float GetSlopeDegrees(float worldX, float worldZ)
    {
        float nx = (worldX - tPos.x) / tSize.x;
        float nz = (worldZ - tPos.z) / tSize.z;
        Vector3 n = tData.GetInterpolatedNormal(nx, nz);
        return Vector3.Angle(n, Vector3.up);
    }

    bool IsInsideForbidden(Vector3 point, float padding)
    {
        if (forbiddenTriggers == null || forbiddenTriggers.Count == 0) return false;

        foreach (var col in forbiddenTriggers)
        {
            if (col == null) continue;
            Vector3 closest = col.ClosestPoint(point);
            float dist = Vector3.Distance(point, closest);
            if (dist <= Mathf.Max(0.01f, padding)) return true;
            if (dist < 0.001f && col.isTrigger) return true;
        }
        return false;
    }

    float EstimateDistanceToShore(Vector3 start, float stepMeters, float maxMeters, float heightTol)
    {
        if (terrain == null) return Mathf.Infinity;

        const int DIRS = 16;
        float best = Mathf.Infinity;

        for (int k = 0; k < DIRS; k++)
        {
            float ang = (Mathf.PI * 2f) * (k / (float)DIRS);
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));

            for (float d = stepMeters; d <= maxMeters; d += stepMeters)
            {
                Vector3 p = start + dir * d;
                if (!InsideTerrainBoundsXZ(p)) break;

                float h = terrain.SampleHeight(p) + tPos.y;
                if (h <= oceanLevelY + heightTol)
                {
                    best = Mathf.Min(best, d);
                    break;
                }
            }
        }

        return best;
    }

    bool InsideTerrainBoundsXZ(Vector3 p)
    {
        return (p.x >= tPos.x && p.x <= tPos.x + tSize.x &&
                p.z >= tPos.z && p.z <= tPos.z + tSize.z);
    }

    // ---------------- Spawn Chests & Enemies ----------------

    void SpawnChests()
    {
        if (usableChests == null || usableChests.Length == 0)
        {
            Debug.LogError("usableChests is empty. Assign chest prefabs in the Inspector.");
            return;
        }

        placedChestXZ.Clear(); // reset for this run

        for (int i = 0; i < chestLocations.Length; i++)
        {
            Transform anchor = chestLocations[i];
            if (!anchor) continue;

            // Snap anchor to ground for a reliable base
            if (!Physics.Raycast(anchor.position + Vector3.up * 50f, Vector3.down, out var groundHit, 200f, ~0, QueryTriggerInteraction.Ignore))
            {
                Debug.LogWarning($"Chest Unable to raycast at chest anchor {i}");
                continue;
            }

            bool placed = false;

            // Try random offsets around the anchor first
            for (int attempt = 0; attempt < Mathf.Max(1, maxChestPlacementAttempts); attempt++)
            {
                Vector2 jitter = Random.insideUnitCircle * chestPlacementRadius;
                Vector3 chestTry = groundHit.point + new Vector3(jitter.x, 0f, jitter.y);

                if (!Physics.Raycast(chestTry + Vector3.up * 50f, Vector3.down, out var chestHit, 200f, ~0, QueryTriggerInteraction.Ignore))
                    continue;

                Vector3 finalPos = chestHit.point;

                if (IsValidFinalChestSpot(finalPos) && IsFarFromOtherChests(finalPos))
                {
                    PlaceChestAndEnemies(finalPos, chestHit.normal);
                    placedChestXZ.Add(new Vector2(finalPos.x, finalPos.z));
                    placed = true;
                    break;
                }
            }

            // Fallback: anchor center (still must pass rules + spacing)
            if (!placed)
            {
                Vector3 finalPos = groundHit.point;
                if (IsValidFinalChestSpot(finalPos) && IsFarFromOtherChests(finalPos))
                {
                    PlaceChestAndEnemies(finalPos, groundHit.normal);
                    placedChestXZ.Add(new Vector2(finalPos.x, finalPos.z));
                    placed = true;
                }
            }

            if (!placed)
            {
                Debug.LogWarning($"Chest #{i} could not find a valid position with required spacing. Skipping.");
            }
        }
    }

    bool IsFarFromOtherChests(Vector3 pos)
    {
        if (minChestSeparation <= 0f || placedChestXZ.Count == 0) return true;
        float minSqr = minChestSeparation * minChestSeparation;
        Vector2 p = new Vector2(pos.x, pos.z);
        for (int k = 0; k < placedChestXZ.Count; k++)
        {
            if ((placedChestXZ[k] - p).sqrMagnitude < minSqr) return false;
        }
        return true;
    }

    void PlaceChestAndEnemies(Vector3 position, Vector3 groundNormal)
    {
        int random = Random.Range(0, usableChests.Length);
        var chest = Instantiate(
            usableChests[random],
            position,
            Quaternion.FromToRotation(Vector3.up, groundNormal)
        );
        chest.AddComponent<PatrolArea>();

        // Enemies around chest
        var chestHit = new RaycastHit { point = position };
        for (int j = 0; j < enemiesPerChest; j++)
            GetRandSpawnPoint(chestHit);
    }

    bool IsValidFinalChestSpot(Vector3 pos)
    {
        if (pos.y < oceanLevelY + minHeightAboveSea) return false;

        float slope = GetSlopeDegrees(pos.x, pos.z);
        if (slope > maxSlopeDegrees) return false;

        float d = EstimateDistanceToShore(pos, 2f, 100f, 0.25f);
        if (d < minDistanceFromShore) return false;

        if (IsInsideForbidden(pos, triggerPadding)) return false;

        return true;
    }

    void SpawnEnemy(NavMeshHit hit, int rand)
    {
        Instantiate(basicEnemies[rand], hit.position, Quaternion.identity);
    }

    void GetRandSpawnPoint(RaycastHit chestGroundHit)
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 randomPoint = chestGroundHit.point + new Vector3(randomCircle.x, 0f, randomCircle.y);

        if (NavMesh.SamplePosition(randomPoint, out var hit, 5f, NavMesh.AllAreas))
        {
            SpawnEnemy(hit, Random.Range(0, basicEnemies.Length));
        }
    }

    // ---------------- Player ----------------

    void SpawnPlayer()
    {
        if (playerSpawnPos == null || playerSpawnPos.Length == 0) return;

        int random = Random.Range(0, playerSpawnPos.Length);
        Vector3 rayStart = playerSpawnPos[random].position + Vector3.up * 50f;

        if (Physics.Raycast(rayStart, Vector3.down, out var hit, Mathf.Infinity))
        {
            if (GameObject.FindGameObjectWithTag("Player"))
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                player.transform.position = hit.point;
                Debug.Log("Existing player spawned successfully");
            }
            else
            {
                Instantiate(playerPref, hit.point, Quaternion.identity);
            }
        }
    }
}
