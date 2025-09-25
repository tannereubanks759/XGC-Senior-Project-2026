using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

[AddComponentMenu("Level/Terrain Object Spawner")]
public class TerrainObjectSpawner : MonoBehaviour
{
    [Header("Scene References")]
    public Terrain terrain;

    [Header("Level Loading UI")]
    [Tooltip("Root GO that contains your Panel (with Image) + any child Texts. Will be enabled at Start and faded out when NavMesh bake completes.")]
    public GameObject levelLoadingUI;
    [Tooltip("How long the fade-out takes (seconds).")]
    public float loadingFadeDuration = 0.6f;
    [Tooltip("Keep the cursor visible while loading UI is up.")]
    public bool showCursorWhileLoading = false;

    // ===== Ocean / NavMesh Settings =====
    [Header("NavMesh Rebuild")]
    [Tooltip("Disable these objects before baking so they don't block the NavMesh (e.g., your ocean/water).")]
    public List<GameObject> waterObjects = new List<GameObject>();

    [Tooltip("NavMesh surfaces to rebuild. If empty, the script will auto-find all NavMeshSurface components in the scene.")]
    public List<NavMeshSurface> navSurfaces = new List<NavMeshSurface>();

    [Tooltip("After SpawnAllPasses(), automatically rebuild NavMesh with ocean disabled.")]
    public bool rebuildNavMeshAfterSpawn = true;

    [Tooltip("Frames to wait in Play Mode after toggling water before rebuilding (lets Unity register active-state changes).")]
    [Min(0)] public int playmodeDelayFrames = 1;
    // ====================================

    [Header("Object Arrays (3 passes)")]
    public GameObject[] treePrefabs;
    public GameObject[] grassPrefabs;
    public GameObject[] prefabs;

    [Header("Spawn Controls")]
    [Min(0)] public int treeCount = 100;
    [Min(0)] public int grassCount = 300;
    [Min(0)] public int objectCount = 200;
    [Min(1)] public int maxAttemptsPerObject = 80;

    [Tooltip("Press this key to spawn immediately (both in Play Mode and Edit Mode).")]
    public KeyCode spawnHotkey = KeyCode.F7;
    public bool autoSpawnOnPlay = false;

    [Header("Placement Rules (global)")]
    public float oceanLevelY = 0f;
    public bool enforceSlope = true;
    [Range(0f, 80f)] public float maxSlopeDegrees = 45f;

    [Tooltip("Minimum distance away from any tree (terrain trees + spawned tree prefabs). Applies to trees and generic objects; IGNORED by grass.")]
    public float minDistanceToTrees = 5f;

    [Tooltip("Minimum spacing between spawned objects across passes (trees & generic objects contribute). Grass ignores this and does not contribute.")]
    public float minSpacingBetweenObjects = 3f;

    [Header("Terrain Layer Filters")]
    [Tooltip("TerrainLayer that represents PATHS to AVOID anywhere.")]
    public TerrainLayer pathLayer;
    [Range(0f, 1f)] public float pathWeightThreshold = 0.35f;
    public float pathPadding = 1.5f;
    public int pathPaddingSampleRingPoints = 6;

    [Space(6)]
    [Tooltip("TerrainLayer that represents GRASS. Trees & grass will ONLY spawn where this layer's weight >= threshold.")]
    public TerrainLayer grassLayer;
    [Range(0f, 1f)] public float grassWeightThreshold = 0.5f;

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

    [Header("Orientation (for non-tree things)")]
    [Tooltip("Align spawned objects' up-axis to the terrain surface normal (used for grass & generic objects).")]
    public bool alignToGroundNormal = true;
    [Range(0f, 1f)] public float normalAlignStrength = 1f;

    [Header("Grass Self-Spacing")]
    [Tooltip("Optional minimal spacing among grass within the same pass only (does NOT consider trees or other passes). Set 0 to allow dense overlap.")]
    public float grassSelfSpacing = 0f;

    // Internals
    int pathLayerIndex = -1;
    int grassLayerIndex = -1;

    TerrainData tData;
    Vector3 tPos, tSize;
    int alphaW, alphaH;
    float[,,] alphaMaps;

    readonly List<Vector2> terrainTreeXZ = new();
    readonly List<Vector2> spawnedTreeXZ = new();
    readonly List<Vector2> globalSpacingXZ = new();

    // Rejection counters
    int rejOcean, rejSlope, rejPath, rejPathPad, rejTree, rejSpacing, rejCollision, rejOutBounds, rejGrassLayer;

    [Tooltip("Any trigger/collider volumes that mark keep-out zones (e.g., a BoxCollider over the fortress).")]
    public List<Collider> noSpawnVolumes = new List<Collider>();
    [Tooltip("Padding radius around the point when checking volumes.")]
    public float noSpawnVolumePadding = 0.25f;

    // Runtime flags
    static bool _isBakingNavMesh;
    bool _levelUIWasShown;

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
        if (!Application.isPlaying) return;
        if (gameObject.scene != SceneManager.GetActiveScene()) return;

        // Show level loading UI immediately
        ShowLevelLoadingUI();

        // Defer heavy work a couple frames
        StartCoroutine(DeferredSpawnAndBake());
    }

    IEnumerator DeferredSpawnAndBake()
    {
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        // If you spawn content at load, do it first
        bool willBakeViaSpawn = autoSpawnOnPlay && rebuildNavMeshAfterSpawn;
        if (autoSpawnOnPlay)
        {
            if (verboseLogs) Debug.Log("[Spawner] autoSpawnOnPlay → spawning now (deferred).");
            SpawnAllPasses(); // this will StartCoroutine(RebuildNavMeshRoutine()) if rebuildNavMeshAfterSpawn = true
        }

        // If no bake was kicked by SpawnAllPasses, bake explicitly
        if (!willBakeViaSpawn)
            StartCoroutine(RebuildNavMeshRoutine());
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) { }
#endif
        if (spawnHotkey != KeyCode.None && Input.GetKeyDown(spawnHotkey))
        {
            Debug.Log($"[Spawner] Hotkey {spawnHotkey} pressed → spawning.");
            SpawnAllPasses();
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

        alphaW = tData.alphamapWidth;
        alphaH = tData.alphamapHeight;
        alphaMaps = tData.GetAlphamaps(0, 0, alphaW, alphaH);

        pathLayerIndex = ResolveLayerIndex(pathLayer, tData.terrainLayers);
        grassLayerIndex = ResolveLayerIndex(grassLayer, tData.terrainLayers);

        CacheTerrainTreePositions();

        if (navSurfaces == null || navSurfaces.Count == 0)
        {
            var found = FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (found != null && found.Length > 0) navSurfaces.AddRange(found);
        }

        if (verboseLogs)
        {
            Debug.Log(
                $"[Spawner] Setup OK\n" +
                $"  Terrain: {terrain.name}  size={tSize}\n" +
                $"  Arrays: trees={Len(treePrefabs)} grass={Len(grassPrefabs)} objects={Len(prefabs)}\n" +
                $"  PathLayer: {(pathLayer ? pathLayer.name : "null")} idx={pathLayerIndex} thr={pathWeightThreshold}\n" +
                $"  GrassLayer: {(grassLayer ? grassLayer.name : "null")} idx={grassLayerIndex} thr={grassWeightThreshold}\n" +
                $"  Baked Trees: {terrainTreeXZ.Count}  AlphaMap: {alphaW}x{alphaH}\n" +
                $"  OceanY={oceanLevelY}  Slope? {enforceSlope}<= {maxSlopeDegrees}°  TreeDist={minDistanceToTrees}  GlobalSpacing={minSpacingBetweenObjects}\n" +
                $"  NavSurfaces: {navSurfaces.Count}  WaterObjs: {waterObjects.Count}"
            );
        }
    }

    int Len(GameObject[] a) => a == null ? 0 : a.Length;

    int ResolveLayerIndex(TerrainLayer layer, TerrainLayer[] layers)
    {
        if (layer == null || layers == null) return -1;
        for (int i = 0; i < layers.Length; i++)
            if (layers[i] == layer) return i;
        return -1;
    }

    void CacheTerrainTreePositions()
    {
        terrainTreeXZ.Clear();
        spawnedTreeXZ.Clear();
        globalSpacingXZ.Clear();

        if (tData == null) return;
        var trees = tData.treeInstances;
        for (int i = 0; i < trees.Length; i++)
        {
            Vector3 world = new Vector3(
                tPos.x + trees[i].position.x * tSize.x,
                0f,
                tPos.z + trees[i].position.z * tSize.z
            );
            var p = new Vector2(world.x, world.z);
            terrainTreeXZ.Add(p);
            if (minSpacingBetweenObjects > 0f) globalSpacingXZ.Add(p);
        }
    }

    [ContextMenu("Spawn All (Trees → Grass → Objects)")]
    public void SpawnAllPasses()
    {
        if (!terrain) terrain = FindFirstObjectByType<Terrain>();
        if (!terrain) { Debug.LogError("[Spawner] No Terrain found at spawn."); return; }

        tData = terrain.terrainData;
        tPos = terrain.transform.position;
        tSize = tData.size;

        alphaW = tData.alphamapWidth;
        alphaH = tData.alphamapHeight;
        alphaMaps = tData.GetAlphamaps(0, 0, alphaW, alphaH);

        CacheTerrainTreePositions();

        rejOcean = rejSlope = rejPath = rejPathPad = rejTree = rejSpacing = rejCollision = rejOutBounds = rejGrassLayer = 0;

        if (Len(treePrefabs) > 0 && treeCount > 0)
        {
            SpawnPass(
                count: treeCount,
                array: treePrefabs,
                requireLayerIndex: grassLayerIndex,
                requireLayerThreshold: grassWeightThreshold,
                alignToNormal: false,
                respectTreeClearance: true,
                useGlobalSpacingAgainstExisting: true,
                contributeToGlobalSpacing: true,
                selfSpacingOverride: Mathf.Max(0f, minSpacingBetweenObjects),
                onPlaced: (pos) => { spawnedTreeXZ.Add(new Vector2(pos.x, pos.z)); }
            );
        }

        if (Len(grassPrefabs) > 0 && grassCount > 0)
        {
            SpawnPass(
                count: grassCount,
                array: grassPrefabs,
                requireLayerIndex: grassLayerIndex,
                requireLayerThreshold: grassWeightThreshold,
                alignToNormal: true,
                respectTreeClearance: false,
                useGlobalSpacingAgainstExisting: false,
                contributeToGlobalSpacing: false,
                selfSpacingOverride: Mathf.Max(0f, grassSelfSpacing),
                onPlaced: null
            );
        }

        if (Len(prefabs) > 0 && objectCount > 0)
        {
            SpawnPass(
                count: objectCount,
                array: prefabs,
                requireLayerIndex: -1,
                requireLayerThreshold: 0f,
                alignToNormal: alignToGroundNormal,
                respectTreeClearance: true,
                useGlobalSpacingAgainstExisting: true,
                contributeToGlobalSpacing: true,
                selfSpacingOverride: Mathf.Max(0f, minSpacingBetweenObjects),
                onPlaced: null
            );
        }

        Debug.Log(
            $"[Spawner] Done.\n" +
            $"Rejections — Ocean:{rejOcean}, Slope:{rejSlope}, Path:{rejPath}, PathPad:{rejPathPad}, Trees:{rejTree}, Spacing:{rejSpacing}, Overlap:{rejCollision}, OutBounds:{rejOutBounds}, NotGrass:{rejGrassLayer}"
        );

        // Kick bake after spawn if desired
        if (rebuildNavMeshAfterSpawn)
            StartCoroutine(RebuildNavMeshRoutine());
    }

    static bool IsNullOrDestroyed(Object o) => o == null;

    IEnumerator RebuildNavMeshRoutine()
    {
        if (_isBakingNavMesh)
        {
            if (verboseLogs) Debug.LogWarning("[Spawner] Bake already in progress—skipping.");
            yield break;
        }

        _isBakingNavMesh = true;
        try
        {
            if (verboseLogs) Debug.Log("[Spawner] Starting runtime NavMesh bake…");

            ToggleWater(false);
            for (int i = 0; i < Mathf.Max(0, playmodeDelayFrames); i++) yield return null;

            SetAgentsEnabled(false);
            BuildAllSurfaces();   // synchronous (will block a little)
            yield return null;

            SetAgentsEnabled(true);
            ToggleWater(true);

            if (verboseLogs) Debug.Log("[Spawner] NavMesh rebuilt (Play Mode) with water disabled then re-enabled.");
        }
        finally
        {
            _isBakingNavMesh = false;
        }

        // Fade out level loading UI once baking is done
        yield return FadeOutAndHideLevelUI();
    }

    void SetAgentsEnabled(bool enabled)
    {
        var agents = FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < agents.Length; i++) if (!IsNullOrDestroyed(agents[i])) agents[i].enabled = enabled;

        var obstacles = FindObjectsByType<UnityEngine.AI.NavMeshObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < obstacles.Length; i++) if (!IsNullOrDestroyed(obstacles[i])) obstacles[i].enabled = enabled;
    }

    void ToggleWater(bool enabled)
    {
        if (waterObjects == null) return;
        for (int i = 0; i < waterObjects.Count; i++)
        {
            var go = waterObjects[i];
            if (!go) continue;
            if (go.activeSelf != enabled) go.SetActive(enabled);
        }
    }

    void BuildAllSurfaces()
    {
        if (navSurfaces == null) navSurfaces = new List<NavMeshSurface>();
        if (navSurfaces.Count == 0)
        {
            var found = FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (found != null && found.Length > 0) navSurfaces.AddRange(found);
        }

        if (navSurfaces.Count == 0)
        {
            Debug.LogError("[Spawner] No NavMeshSurface found in scene.");
            return;
        }

        int built = 0;
        for (int i = 0; i < navSurfaces.Count; i++)
        {
            var s = navSurfaces[i];
            if (!s) continue;
            s.RemoveData();
            s.BuildNavMesh(); // main-thread build
            built++;
        }

        if (verboseLogs) Debug.Log($"[Spawner] Rebuilt {built} NavMeshSurface(s).");
    }

    void SpawnPass(
        int count,
        GameObject[] array,
        int requireLayerIndex,
        float requireLayerThreshold,
        bool alignToNormal,
        bool respectTreeClearance,
        bool useGlobalSpacingAgainstExisting,
        bool contributeToGlobalSpacing,
        float selfSpacingOverride,
        System.Action<Vector3> onPlaced
    )
    {
        int placed = 0;
        int attempts = 0;
        int maxAttempts = Mathf.Max(1, count * maxAttemptsPerObject);

        List<Vector2> passPlacedXZ = new();

        while (placed < count && attempts < maxAttempts)
        {
            attempts++;
            float x = Random.Range(tPos.x, tPos.x + tSize.x);
            float z = Random.Range(tPos.z, tPos.z + tSize.z);

            if (!TryPickValidLocation(
                    x, z,
                    requireLayerIndex, requireLayerThreshold,
                    respectTreeClearance,
                    useGlobalSpacingAgainstExisting ? minSpacingBetweenObjects : 0f,
                    selfSpacingOverride,
                    passPlacedXZ,
                    out float y))
                continue;

            GameObject prefab = array[Random.Range(0, array.Length)];
            Vector3 pos = new Vector3(x, y, z);

            if (collisionMask.value != 0 && Physics.CheckSphere(pos, collisionRadius, collisionMask, QueryTriggerInteraction.Ignore))
            { rejCollision++; continue; }

            Quaternion rot;
            if (alignToNormal)
            {
                Vector3 n = GetTerrainNormal(x, z);
                Vector3 blendedUp = Vector3.Slerp(Vector3.up, n, Mathf.Clamp01(normalAlignStrength)).normalized;
                Vector3 fwd = Vector3.ProjectOnPlane(Vector3.forward, blendedUp);
                if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.ProjectOnPlane(Vector3.right, blendedUp);
                fwd.Normalize();
                Quaternion tilt = Quaternion.LookRotation(fwd, blendedUp);
                Quaternion yawAroundUp = randomYRotation ? Quaternion.AngleAxis(Random.Range(0f, 360f), blendedUp) : Quaternion.identity;
                rot = yawAroundUp * tilt;
            }
            else
            {
                rot = randomYRotation ? Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) : Quaternion.identity;
            }

            Transform parent = parentForSpawned ? parentForSpawned : transform;
            var go = Instantiate(prefab, pos, rot, parent);

            if (uniformScaleRange.x != 1f || uniformScaleRange.y != 1f)
            {
                float s = Random.Range(uniformScaleRange.x, uniformScaleRange.y);
                go.transform.localScale = new Vector3(s, s, s);
            }

            passPlacedXZ.Add(new Vector2(x, z));
            if (contributeToGlobalSpacing && minSpacingBetweenObjects > 0f)
                globalSpacingXZ.Add(new Vector2(x, z));

            onPlaced?.Invoke(pos);
            placed++;
        }

        if (verboseLogs)
            Debug.Log($"[Spawner] Pass done: placed {placed}/{count} for array '{(array.Length > 0 ? array[0].name : "empty")}' with attempts={attempts}.");
    }

    bool InsideNoSpawnVolume(Vector3 pos)
    {
        if (noSpawnVolumes == null || noSpawnVolumes.Count == 0) return false;
        float r = Mathf.Max(0f, noSpawnVolumePadding);
        for (int i = 0; i < noSpawnVolumes.Count; i++)
        {
            var col = noSpawnVolumes[i];
            if (!col) continue;
            Vector3 cp = col.ClosestPoint(pos);
            if ((cp - pos).sqrMagnitude <= r * r) return true;
        }
        return false;
    }

    bool TryPickValidLocation(
        float x, float z,
        int requireLayerIndex,
        float requireLayerThreshold,
        bool respectTreeClearance,
        float globalSpacingDist,
        float selfSpacingDist,
        List<Vector2> passPlacedXZ,
        out float yOut
    )
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

        if (requireLayerIndex >= 0)
        {
            float gw = LayerWeightAt(x, z, requireLayerIndex);
            if (gw < requireLayerThreshold) { rejGrassLayer++; return false; }
        }

        if (pathLayerIndex >= 0)
        {
            float w = LayerWeightAt(x, z, pathLayerIndex);
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
                    if (LayerWeightAt(sx, sz, pathLayerIndex) > pathWeightThreshold) { rejPathPad++; return false; }
                }
            }
        }

        if (InsideNoSpawnVolume(new Vector3(x, y, z))) { rejPath++; return false; }

        Vector2 p = new Vector2(x, z);

        if (respectTreeClearance && minDistanceToTrees > 0.01f && (terrainTreeXZ.Count > 0 || spawnedTreeXZ.Count > 0))
        {
            float minTreeSqr = minDistanceToTrees * minDistanceToTrees;
            for (int i = 0; i < terrainTreeXZ.Count; i++)
                if ((terrainTreeXZ[i] - p).sqrMagnitude < minTreeSqr) { rejTree++; return false; }
            for (int i = 0; i < spawnedTreeXZ.Count; i++)
                if ((spawnedTreeXZ[i] - p).sqrMagnitude < minTreeSqr) { rejTree++; return false; }
        }

        if (globalSpacingDist > 0.01f && globalSpacingXZ.Count > 0)
        {
            float minSqr = globalSpacingDist * globalSpacingDist;
            for (int i = 0; i < globalSpacingXZ.Count; i++)
                if ((globalSpacingXZ[i] - p).sqrMagnitude < minSqr) { rejSpacing++; return false; }
        }

        if (selfSpacingDist > 0.01f && passPlacedXZ.Count > 0)
        {
            float minSqr = selfSpacingDist * selfSpacingDist;
            for (int i = 0; i < passPlacedXZ.Count; i++)
                if ((passPlacedXZ[i] - p).sqrMagnitude < minSqr) { rejSpacing++; return false; }
        }

        yOut = y;
        return true;
    }

    Vector3 GetTerrainNormal(float worldX, float worldZ)
    {
        float u = (worldX - tPos.x) / tSize.x;
        float v = (worldZ - tPos.z) / tSize.z;
        Vector3 n = tData.GetInterpolatedNormal(u, v);
        return n.sqrMagnitude > 0f ? n.normalized : Vector3.up;
    }

    bool InsideTerrainBounds(float x, float z)
    {
        return x >= tPos.x && x <= tPos.x + tSize.x &&
               z >= tPos.z && z <= tPos.z + tSize.z;
    }

    float LayerWeightAt(float worldX, float worldZ, int layerIndex)
    {
        if (layerIndex < 0) return 0f;
        float tx = Mathf.InverseLerp(tPos.x, tPos.x + tSize.x, worldX);
        float tz = Mathf.InverseLerp(tPos.z, tPos.z + tSize.z, worldZ);
        int ax = Mathf.Clamp(Mathf.RoundToInt(tx * (alphaW - 1)), 0, alphaW - 1);
        int az = Mathf.Clamp(Mathf.RoundToInt(tz * (alphaH - 1)), 0, alphaH - 1);
        return alphaMaps[az, ax, layerIndex];
    }

    // ===== Level Loading UI helpers =====
    void ShowLevelLoadingUI()
    {
        if (!levelLoadingUI) return;
        var cg = levelLoadingUI.GetComponent<CanvasGroup>();
        if (!cg) cg = levelLoadingUI.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        levelLoadingUI.SetActive(true);
        _levelUIWasShown = true;

        if (showCursorWhileLoading)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    IEnumerator FadeOutAndHideLevelUI()
    {
        if (!_levelUIWasShown || !levelLoadingUI) yield break;

        var cg = levelLoadingUI.GetComponent<CanvasGroup>();
        if (!cg) cg = levelLoadingUI.AddComponent<CanvasGroup>();

        float t = 0f;
        float dur = Mathf.Max(0.01f, loadingFadeDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / dur);
            yield return null;
        }
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        levelLoadingUI.SetActive(false);
    }
}
