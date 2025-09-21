using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("Level/Terrain Object Spawner")]
[ExecuteAlways] // so context menu works in Edit mode too
public class TerrainObjectSpawner : MonoBehaviour
{
    [Header("Scene References")]
    public Terrain terrain;
    public GameObject[] prefabs;

    [Header("Spawn Controls")]
    [Min(0)] public int spawnCount = 200;
    [Min(1)] public int maxAttemptsPerObject = 80;

    [Tooltip("Press this key to spawn immediately (both in Play Mode and Edit Mode).")]
    public KeyCode spawnHotkey = KeyCode.F7;
    public bool autoSpawnOnPlay = false;

    [Header("Placement Rules")]
    public float oceanLevelY = 0f;
    public bool enforceSlope = true;
    [Range(0f, 80f)] public float maxSlopeDegrees = 45f;
    public float minDistanceToTrees = 5f;
    public float minSpacingBetweenObjects = 3f;

    [Header("Path Layer Avoidance")]
    public TerrainLayer pathLayer;            // drag the actual TerrainLayer asset from your Terrain
    [Range(0f, 1f)] public float pathWeightThreshold = 0.35f;
    public float pathPadding = 1.5f;
    public int pathPaddingSampleRingPoints = 6;

    [Header("Randomization")]
    public Vector2 uniformScaleRange = new Vector2(0.9f, 1.1f);
    public bool randomYRotation = true;

    [Header("Collision Checks (optional)")]
    public LayerMask collisionMask = 0;
    public float collisionRadius = 0.25f;

    [Header("Parenting")]
    public Transform parentForSpawned;

    [Header("Debug")]
    public bool verboseLogs = true;

    // Internals
    int pathLayerIndex = -1;
    TerrainData tData;
    Vector3 tPos, tSize;
    int alphaW, alphaH;
    float[,,] alphaMaps;
    readonly List<Vector2> treeXZ = new();
    readonly List<Vector2> placedXZ = new();

    // Rejection counters
    int rejOcean, rejSlope, rejPath, rejPathPad, rejTree, rejSpacing, rejCollision, rejOutBounds;

    [Header("Orientation")]
    [Tooltip("Align spawned objects' up-axis to the terrain surface normal.")]
    public bool alignToGroundNormal = true;

    [Range(0f, 1f)]
    [Tooltip("0 = no tilt (upright), 1 = full align to terrain normal.")]
    public float normalAlignStrength = 1f;


    void OnEnable()
    {
        if (verboseLogs) Debug.Log($"[Spawner] OnEnable on '{name}' (activeInHierarchy={gameObject.activeInHierarchy}, isPlaying={Application.isPlaying})");
        EnsureSetup();
    }

    void Awake()
    {
        if (verboseLogs) Debug.Log($"[Spawner] Awake on '{name}'");
        EnsureSetup();
    }

    void Start()
    {
        if (verboseLogs) Debug.Log($"[Spawner] Start on '{name}'");
        if (Application.isPlaying && autoSpawnOnPlay)
        {
            Debug.Log("[Spawner] autoSpawnOnPlay = true → spawning now.");
            SpawnObjects();
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // Allow hotkey in Edit mode too.
            if (spawnHotkey != KeyCode.None && Event.current == null)
            {
                // no event in edit update; use SceneView to catch hotkey instead
            }
        }
#endif
        if (spawnHotkey != KeyCode.None && Input.GetKeyDown(spawnHotkey))
        {
            Debug.Log($"[Spawner] Hotkey {spawnHotkey} pressed → spawning.");
            SpawnObjects();
        }
    }

    void EnsureSetup()
    {
        if (!terrain) terrain = FindFirstObjectByType<Terrain>();
        if (!terrain)
        {
            Debug.LogWarning("[Spawner] No Terrain assigned/found. Assign the Terrain in the Inspector.");
            return;
        }

        tData = terrain.terrainData;
        tPos = terrain.transform.position;
        tSize = tData.size;

        // Cache alphamaps
        alphaW = tData.alphamapWidth;
        alphaH = tData.alphamapHeight;
        alphaMaps = tData.GetAlphamaps(0, 0, alphaW, alphaH);

        // Resolve path layer index
        pathLayerIndex = -1;
        if (pathLayer != null && tData.terrainLayers != null)
        {
            for (int i = 0; i < tData.terrainLayers.Length; i++)
                if (tData.terrainLayers[i] == pathLayer) { pathLayerIndex = i; break; }
        }

        // Cache tree world XZ
        CacheTreePositions();

        if (verboseLogs)
        {
            Debug.Log(
                $"[Spawner] Setup OK\n" +
                $"  Terrain: {terrain.name}  size={tSize}\n" +
                $"  Prefabs: {(prefabs == null ? 0 : prefabs.Length)}\n" +
                $"  PathLayer: {(pathLayer ? pathLayer.name : "null")}  index={pathLayerIndex}\n" +
                $"  Trees: {treeXZ.Count}  AlphaMap: {alphaW}x{alphaH}\n" +
                $"  OceanY={oceanLevelY}  Slope? {enforceSlope}<= {maxSlopeDegrees}°  TreeDist={minDistanceToTrees}  Spacing={minSpacingBetweenObjects}"
            );
        }
    }

    void CacheTreePositions()
    {
        treeXZ.Clear();
        if (tData == null) return;
        var trees = tData.treeInstances;
        for (int i = 0; i < trees.Length; i++)
        {
            Vector3 world = new Vector3(
                tPos.x + trees[i].position.x * tSize.x,
                0f,
                tPos.z + trees[i].position.z * tSize.z
            );
            treeXZ.Add(new Vector2(world.x, world.z));
        }
    }

    [ContextMenu("Spawn Objects Now")]
    public void SpawnObjects()
    {
        if (prefabs == null || prefabs.Length == 0) { Debug.LogError("[Spawner] No prefabs assigned."); return; }
        if (!terrain || tData == null || alphaMaps == null) { Debug.LogError("[Spawner] Terrain/alphamaps not ready."); return; }

        placedXZ.Clear();
        rejOcean = rejSlope = rejPath = rejPathPad = rejTree = rejSpacing = rejCollision = rejOutBounds = 0;

        int placed = 0;
        int attempts = 0;
        int maxAttempts = Mathf.Max(1, spawnCount * maxAttemptsPerObject);

        while (placed < spawnCount && attempts < maxAttempts)
        {
            attempts++;

            float x = Random.Range(tPos.x, tPos.x + tSize.x);
            float z = Random.Range(tPos.z, tPos.z + tSize.z);

            if (!TryPickValidLocation(x, z, out float y)) continue;

            // Choose prefab & instantiate
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            Vector3 pos = new Vector3(x, y, z);

            // Base yaw (random spin) — about "up" at first
            Quaternion yaw;
            if (randomYRotation)
                yaw = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);
            else
                yaw = Quaternion.identity;

            // If aligning to ground, tilt "up" toward the terrain normal, with optional strength
            Quaternion rot;
            if (alignToGroundNormal)
            {
                Vector3 n = GetTerrainNormal(x, z);

                // Blend between straight-up and true normal
                Vector3 blendedUp = Vector3.Slerp(Vector3.up, n, Mathf.Clamp01(normalAlignStrength)).normalized;

                // Make a rotation whose up-axis matches blendedUp, and keep forward reasonable:
                // Start with any forward (e.g., world forward), then orthonormalize to get a stable basis.
                Vector3 fwd = Vector3.ProjectOnPlane(Vector3.forward, blendedUp);
                if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.ProjectOnPlane(Vector3.right, blendedUp);
                fwd.Normalize();

                Quaternion tilt = Quaternion.LookRotation(fwd, blendedUp);

                // Apply yaw *around the surface normal* so it spins naturally on slopes
                Quaternion yawAroundNormal = Quaternion.AngleAxis(Random.Range(0f, 360f), blendedUp);
                rot = (randomYRotation ? yawAroundNormal : Quaternion.identity) * tilt;
            }
            else
            {
                // Old behavior: upright + optional random yaw
                rot = yaw;
            }


            if (collisionMask.value != 0 && Physics.CheckSphere(pos, collisionRadius, collisionMask, QueryTriggerInteraction.Ignore))
            { rejCollision++; continue; }

            Transform parent = parentForSpawned ? parentForSpawned : transform;
            var go = Instantiate(prefab, pos, rot, parent);

            // Random uniform scale
            if (uniformScaleRange.x != 1f || uniformScaleRange.y != 1f)
            {
                float s = Random.Range(uniformScaleRange.x, uniformScaleRange.y);
                go.transform.localScale = new Vector3(s, s, s);
            }

            placedXZ.Add(new Vector2(x, z));
            placed++;
        }

        Debug.Log(
            $"[Spawner] Placed {placed}/{spawnCount} with {attempts} attempts.\n" +
            $"Rejections — Ocean:{rejOcean}, Slope:{rejSlope}, Path:{rejPath}, PathPad:{rejPathPad}, Trees:{rejTree}, Spacing:{rejSpacing}, Overlap:{rejCollision}, OutBounds:{rejOutBounds}"
        );
    }

    bool TryPickValidLocation(float x, float z, out float yOut)
    {
        yOut = 0f;
        if (!InsideTerrainBounds(x, z)) { rejOutBounds++; return false; }

        float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + tPos.y;
        if (y < oceanLevelY) { rejOcean++; return false; }

        if (enforceSlope && maxSlopeDegrees < 89f)
        {
            Vector3 n = tData.GetInterpolatedNormal(
                (x - tPos.x) / tSize.x,
                (z - tPos.z) / tSize.z
            );
            float slopeDeg = Vector3.Angle(n, Vector3.up);
            if (slopeDeg > maxSlopeDegrees) { rejSlope++; return false; }
        }

        if (pathLayerIndex >= 0)
        {
            float w = PathWeightAt(x, z);
            if (w > pathWeightThreshold) { rejPath++; return false; }

            if (pathPadding > 0.01f && pathPaddingSampleRingPoints > 0)
            {
                float step = 360f / pathPaddingSampleRingPoints;
                for (int i = 0; i < pathPaddingSampleRingPoints; i++)
                {
                    float ang = step * i * Mathf.Deg2Rad;
                    float sx = x + Mathf.Cos(ang) * pathPadding;
                    float sz = z + Mathf.Sin(ang) * pathPadding;
                    if (!InsideTerrainBounds(sx, sz)) continue;
                    if (PathWeightAt(sx, sz) > pathWeightThreshold) { rejPathPad++; return false; }
                }
            }
        }

        if (minDistanceToTrees > 0.01f && treeXZ.Count > 0)
        {
            Vector2 p = new Vector2(x, z);
            float minSqr = minDistanceToTrees * minDistanceToTrees;
            for (int i = 0; i < treeXZ.Count; i++)
                if ((treeXZ[i] - p).sqrMagnitude < minSqr) { rejTree++; return false; }
        }

        if (minSpacingBetweenObjects > 0.01f && placedXZ.Count > 0)
        {
            Vector2 p = new Vector2(x, z);
            float minSqr = minSpacingBetweenObjects * minSpacingBetweenObjects;
            for (int i = 0; i < placedXZ.Count; i++)
                if ((placedXZ[i] - p).sqrMagnitude < minSqr) { rejSpacing++; return false; }
        }

        yOut = y;
        return true;
    }
    Vector3 GetTerrainNormal(float worldX, float worldZ)
    {
        float u = (worldX - tPos.x) / tSize.x;   // 0..1
        float v = (worldZ - tPos.z) / tSize.z;   // 0..1
        Vector3 n = tData.GetInterpolatedNormal(u, v);
        return n.sqrMagnitude > 0f ? n.normalized : Vector3.up;
    }

    bool InsideTerrainBounds(float x, float z)
    {
        return x >= tPos.x && x <= tPos.x + tSize.x &&
               z >= tPos.z && z <= tPos.z + tSize.z;
    }

    float PathWeightAt(float worldX, float worldZ)
    {
        float tx = Mathf.InverseLerp(tPos.x, tPos.x + tSize.x, worldX);
        float tz = Mathf.InverseLerp(tPos.z, tPos.z + tSize.z, worldZ);
        int ax = Mathf.Clamp(Mathf.RoundToInt(tx * (alphaW - 1)), 0, alphaW - 1);
        int az = Mathf.Clamp(Mathf.RoundToInt(tz * (alphaH - 1)), 0, alphaH - 1);
        return alphaMaps[az, ax, pathLayerIndex];
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Terrain Spawner/Spawn From Selected", priority = 0)]
    static void SpawnFromSelected()
    {
        foreach (var obj in Selection.gameObjects)
        {
            var sp = obj.GetComponent<TerrainObjectSpawner>();
            if (sp != null)
            {
                Debug.Log($"[Spawner] Menu → spawning from '{sp.name}'");
                sp.SpawnObjects();
            }
        }
    }
#endif

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!terrain) return;
        var data = terrain.terrainData;
        var pos = terrain.transform.position;
        Gizmos.color = new Color(0, 1, 0, 0.15f);
        Gizmos.DrawCube(pos + data.size * 0.5f, data.size);

        Gizmos.color = new Color(0, 0.5f, 1f, 0.2f);
        Vector3 c = new Vector3(pos.x + data.size.x * 0.5f, oceanLevelY, pos.z + data.size.z * 0.5f);
        Vector3 s = new Vector3(data.size.x, 0.02f, data.size.z);
        Gizmos.DrawCube(c, s);
    }
#endif
}
