using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
// using UnityEditor;
#endif

[AddComponentMenu("Level/Terrain Object Spawner")]
[ExecuteAlways]
[DefaultExecutionOrder(+200)]
public class TerrainObjectSpawner : MonoBehaviour
{
    [Header("Scene References")]
    public Terrain terrain;

    [Header("Object Arrays (3 passes)")]
    public GameObject[] treePrefabs;   // pass 1 (upright, on Grass layer)
    public GameObject[] grassPrefabs;  // pass 2 (align to normal, on Grass layer)
    public GameObject[] prefabs;       // pass 3 (generic objects)

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
    [Range(0f, 1f)]
    public float normalAlignStrength = 1f;

    [Header("Grass Self-Spacing")]
    [Tooltip("Optional minimal spacing among grass within the same pass only (does NOT consider trees or other passes). Set 0 to allow dense overlap.")]
    public float grassSelfSpacing = 0f;

    [Header("Auto Path Bake From Chests")]
    [Tooltip("If enabled, paints the PATH terrain layer around each chest anchor BEFORE spawning.")]
    public bool autoBakePathsFromChests = true;
    [Tooltip("Chest anchors to paint around (use the same array IslandSetup uses).")]
    public Transform[] chestAnchors;
    [Tooltip("Solid radius (meters) of the path around each chest.")]
    public float chestPathRadius = 6f;
    [Tooltip("Feather distance (meters) beyond the solid radius for a soft falloff.")]
    public float chestPathFeather = 3f;
    [Range(0f, 1f)]
    [Tooltip("Max weight of the PATH layer at the center of each disc.")]
    public float chestPathMaxWeight = 0.95f;

    [Tooltip("When true, we set PATH weight to the max of current and painted weight (non-destructive). When false, we overwrite PATH weight where we paint.")]
    public bool additivePathPaint = true;

    // Internals
    int pathLayerIndex = -1;
    int grassLayerIndex = -1;

    TerrainData tData;
    Vector3 tPos, tSize;
    int alphaW, alphaH;
    float[,,] alphaMaps;

    readonly List<Vector2> terrainTreeXZ = new();   // baked terrain trees
    readonly List<Vector2> spawnedTreeXZ = new();   // trees we spawn in pass 1
    readonly List<Vector2> globalSpacingXZ = new(); // spacing blockers across passes (trees & objects only)

    // Rejection counters
    int rejOcean, rejSlope, rejPath, rejPathPad, rejTree, rejSpacing, rejCollision, rejOutBounds, rejGrassLayer;

    [Tooltip("Any trigger/collider volumes that mark keep-out zones (e.g., a BoxCollider over the fortress).")]
    public List<Collider> noSpawnVolumes = new List<Collider>();
    [Tooltip("Padding radius around the point when checking volumes.")]
    public float noSpawnVolumePadding = 0.25f;


    [Header("Runtime Persistence")]
    [Tooltip("Clone TerrainData in Play Mode so splat/height edits only affect the clone and revert on exit.")]
    public bool cloneTerrainDataAtRuntime = true;

    [Tooltip("Allow painting in Edit Mode (writes directly to the asset). Keep OFF to avoid accidental edits.")]
    public bool allowEditModePainting = false;

    [Header("Path Avoidance (footprint)")]
    [Tooltip("World-space radius around the spawn point to ensure is NOT on path.")]
    public float avoidPathFootprintRadius = 1.25f;   // tweak per prefab size
    [Tooltip("How many concentric rings to sample (>=1). 1 = only edge ring.")]
    [Min(1)] public int avoidPathSampleRings = 2;
    [Tooltip("Samples placed around each ring (like a clock).")]
    [Min(6)] public int avoidPathSamplesPerRing = 16;


    [Header("Auto Path Network Between Chests")]
    [Tooltip("Paint connections between chest anchors before object spawning.")]
    public bool connectChestsWithPaths = true;

    [Tooltip("How to connect chests.")]
    public enum ChestConnectMode { AllPairs, NearestNeighbor, MST }
    public ChestConnectMode chestConnectMode = ChestConnectMode.NearestNeighbor;

    [Tooltip("Brush inner radius (m) for path strokes.")]
    public float pathBrushRadius = 2.0f;
    [Tooltip("Feather distance (m) outside brush radius.")]
    public float pathBrushFeather = 2.0f;
    [Range(0f, 1f)] public float pathBrushMaxWeight = 0.95f;

    [Tooltip("Maximum allowed ground slope (deg) for the path search.")]
    [Range(0f, 89f)] public float pathMaxSlopeDeg = 25f;

    [Tooltip("If no route exists under current slope cap, relax it until a route is found.")]
    public bool pathAutoRelaxSlope = true;
    [Tooltip("Degrees to relax per attempt when no valid route is found.")]
    [Range(1f, 20f)] public float pathRelaxStepDeg = 5f;
    [Tooltip("Upper bound for relaxation.")]
    [Range(0f, 89f)] public float pathRelaxMaxDeg = 60f;

    [Tooltip("Grid resolution in meters used by the A* path search.")]
    [Min(0.5f)] public float pathGridCellSize = 2f;

    [Tooltip("How far (m) to expand the search bounds beyond chest AABB.")]
    public float pathSearchPadding = 12f;

    [Header("Extra Path Targets")]
    [Tooltip("Extra points to connect into the path net (nearest chest).")]
    public Transform[] extraPathPoints;
    [Tooltip("You can also drop GameObjects here; their transforms are used.")]
    public GameObject[] extraPathPointObjects;


    // Backing refs
    TerrainData originalTerrainData;   // asset reference
    bool terrainDataIsRuntimeClone = false;

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
            SpawnAllPasses();
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // Edit mode hotkey handled by SceneView; nothing to do here.
        }
#endif
        if (spawnHotkey != KeyCode.None && Input.GetKeyDown(spawnHotkey))
        {
            Debug.Log($"[Spawner] Hotkey {spawnHotkey} pressed → spawning.");
            SpawnAllPasses();
        }
    }
    void MaybeCloneTerrainDataForRuntime()
    {
        if (!Application.isPlaying || !cloneTerrainDataAtRuntime) return;
        if (terrainDataIsRuntimeClone) return; // already cloned

        // Keep a reference to the original asset
        originalTerrainData = terrain.terrainData;

        // Instantiate creates an in-memory duplicate (not saved as an asset)
        var runtimeClone = Instantiate(originalTerrainData);
        runtimeClone.name = originalTerrainData.name + " (RuntimeClone)";

        // Assign to Terrain (and TerrainCollider so collisions stay correct)
        terrain.terrainData = runtimeClone;
        var tcol = terrain.GetComponent<TerrainCollider>();
        if (tcol) tcol.terrainData = runtimeClone;

        terrainDataIsRuntimeClone = true;

        if (verboseLogs) Debug.Log($"[Spawner] Cloned TerrainData for runtime: {runtimeClone.name}");
    }

    void EnsureSetup()
    {
        if (!terrain) terrain = FindFirstObjectByType<Terrain>();
        if (!terrain)
        {
            Debug.LogWarning("[Spawner] No Terrain assigned/found. Assign the Terrain in the Inspector.");
            return;
        }
        MaybeCloneTerrainDataForRuntime();

        tData = terrain.terrainData;
        tPos = terrain.transform.position;
        tSize = tData.size;

        // Cache alphamaps AFTER any possible swap
        alphaW = tData.alphamapWidth;
        alphaH = tData.alphamapHeight;
        alphaMaps = tData.GetAlphamaps(0, 0, alphaW, alphaH);

        // Resolve layer indices
        pathLayerIndex = ResolveLayerIndex(pathLayer, tData.terrainLayers);
        grassLayerIndex = ResolveLayerIndex(grassLayer, tData.terrainLayers);

        // Cache baked terrain tree XZ
        CacheTerrainTreePositions();

        if (verboseLogs)
        {
            Debug.Log(
                $"[Spawner] Setup OK\n" +
                $"  Terrain: {terrain.name}  size={tSize}\n" +
                $"  Arrays: trees={Len(treePrefabs)} grass={Len(grassPrefabs)} objects={Len(prefabs)}\n" +
                $"  PathLayer: {(pathLayer ? pathLayer.name : "null")} idx={pathLayerIndex} thr={pathWeightThreshold}\n" +
                $"  GrassLayer: {(grassLayer ? grassLayer.name : "null")} idx={grassLayerIndex} thr={grassWeightThreshold}\n" +
                $"  Baked Trees: {terrainTreeXZ.Count}  AlphaMap: {alphaW}x{alphaH}\n" +
                $"  OceanY={oceanLevelY}  Slope? {enforceSlope}<= {maxSlopeDegrees}°  TreeDist={minDistanceToTrees}  GlobalSpacing={minSpacingBetweenObjects}"
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

    float LayerWeightAtBilinear(float worldX, float worldZ, int layerIndex)
    {
        if (layerIndex < 0) return 0f;
        float tx = Mathf.InverseLerp(tPos.x, tPos.x + tSize.x, worldX) * (alphaW - 1);
        float tz = Mathf.InverseLerp(tPos.z, tPos.z + tSize.z, worldZ) * (alphaH - 1);

        int x0 = Mathf.Clamp(Mathf.FloorToInt(tx), 0, alphaW - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(tz), 0, alphaH - 1);
        int x1 = Mathf.Clamp(x0 + 1, 0, alphaW - 1);
        int z1 = Mathf.Clamp(z0 + 1, 0, alphaH - 1);

        float fx = tx - x0;
        float fz = tz - z0;

        float a = alphaMaps[z0, x0, layerIndex];
        float b = alphaMaps[z0, x1, layerIndex];
        float c = alphaMaps[z1, x0, layerIndex];
        float d = alphaMaps[z1, x1, layerIndex];

        float ab = Mathf.Lerp(a, b, fx);
        float cd = Mathf.Lerp(c, d, fx);
        return Mathf.Lerp(ab, cd, fz);
    }

    // area-based path test using rings + bilinear sampling
    bool IsOnPathArea(float worldX, float worldZ)
    {
        if (pathLayerIndex < 0) return false;

        // center check
        if (LayerWeightAtBilinear(worldX, worldZ, pathLayerIndex) > pathWeightThreshold)
            return true;

        // padding ring (matches your TryPickValidLocation logic)
        if (pathPadding > 0.01f && pathPaddingSampleRingPoints > 0)
        {
            float step = 360f / pathPaddingSampleRingPoints;
            for (int i = 0; i < pathPaddingSampleRingPoints; i++)
            {
                float ang = step * i * Mathf.Deg2Rad;
                float sx = worldX + Mathf.Cos(ang) * pathPadding;
                float sz = worldZ + Mathf.Sin(ang) * pathPadding;
                if (!InsideTerrainBounds(sx, sz)) continue;
                if (LayerWeightAtBilinear(sx, sz, pathLayerIndex) > pathWeightThreshold)
                    return true;
            }
        }

        // footprint rings (keeps the mesh bounds off paths)
        float R = Mathf.Max(0f, avoidPathFootprintRadius);
        if (R <= 0f) return false;

        int rings = Mathf.Max(1, avoidPathSampleRings);
        int per = Mathf.Max(6, avoidPathSamplesPerRing);

        for (int r = 1; r <= rings; r++)
        {
            float rad = (R * r) / rings;
            for (int i = 0; i < per; i++)
            {
                float ang = (i / (float)per) * Mathf.PI * 2f;
                float sx = worldX + Mathf.Cos(ang) * rad;
                float sz = worldZ + Mathf.Sin(ang) * rad;
                if (!InsideTerrainBounds(sx, sz)) continue;
                if (LayerWeightAtBilinear(sx, sz, pathLayerIndex) > pathWeightThreshold)
                    return true;
            }
        }

        return false;
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
        if (!terrain || tData == null || alphaMaps == null) { Debug.LogError("[Spawner] Terrain/alphamaps not ready."); return; }

        // 0) *** FIRST: bake path layer from chest anchors (so spawns avoid it) ***
        if (autoBakePathsFromChests)
        {
            BakePathsFromChests();
        }

        // Fresh counters
        rejOcean = rejSlope = rejPath = rejPathPad = rejTree = rejSpacing = rejCollision = rejOutBounds = rejGrassLayer = 0;

        // PASS 1: TREES
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

        // PASS 2: GRASS
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

        // PASS 3: OBJECTS
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
    }

    // ---------------- PATH PAINTING ----------------

    public void BakePathsFromChests()
    {
        if (pathLayerIndex < 0)
        {
            if (verboseLogs) Debug.LogWarning("[Spawner] BakePathsFromChests: pathLayer is not assigned or not present on the Terrain.");
            return;
        }
        if (chestAnchors == null || chestAnchors.Length == 0)
        {
            if (verboseLogs) Debug.LogWarning("[Spawner] BakePathsFromChests: no chestAnchors assigned.");
            return;
        }
        if (tData == null || alphaMaps == null)
        {
            if (verboseLogs) Debug.LogWarning("[Spawner] BakePathsFromChests: alphamaps not ready.");
            return;
        }

        // meters -> alphamap pixels
        float pxPerMeterX = (alphaW - 1) / tSize.x;
        float pxPerMeterZ = (alphaH - 1) / tSize.z;

        int layers = tData.terrainLayers.Length;

        int discsPainted = 0;
        foreach (var anchor in chestAnchors)
        {
            if (!anchor) continue;

            Vector3 c = anchor.position;
            // Convert world to alpha indices
            float tx = Mathf.InverseLerp(tPos.x, tPos.x + tSize.x, c.x);
            float tz = Mathf.InverseLerp(tPos.z, tPos.z + tSize.z, c.z);
            int cx = Mathf.Clamp(Mathf.RoundToInt(tx * (alphaW - 1)), 0, alphaW - 1);
            int cz = Mathf.Clamp(Mathf.RoundToInt(tz * (alphaH - 1)), 0, alphaH - 1);

            // Pixel radii
            int rSolidX = Mathf.CeilToInt(chestPathRadius * pxPerMeterX);
            int rSolidZ = Mathf.CeilToInt(chestPathRadius * pxPerMeterZ);
            int rFeatherX = Mathf.CeilToInt((chestPathRadius + chestPathFeather) * pxPerMeterX);
            int rFeatherZ = Mathf.CeilToInt((chestPathRadius + chestPathFeather) * pxPerMeterZ);

            int minX = Mathf.Clamp(cx - rFeatherX, 0, alphaW - 1);
            int maxX = Mathf.Clamp(cx + rFeatherX, 0, alphaW - 1);
            int minZ = Mathf.Clamp(cz - rFeatherZ, 0, alphaH - 1);
            int maxZ = Mathf.Clamp(cz + rFeatherZ, 0, alphaH - 1);

            // Loop pixels in bounding box
            for (int az = minZ; az <= maxZ; az++)
            {
                // Back to world Z of this alpha row
                float wz = Mathf.Lerp(tPos.z, tPos.z + tSize.z, az / (float)(alphaH - 1));
                float dzMetersZ = Mathf.Abs(wz - c.z);

                for (int ax = minX; ax <= maxX; ax++)
                {
                    float wx = Mathf.Lerp(tPos.x, tPos.x + tSize.x, ax / (float)(alphaW - 1));
                    float dxMetersX = Mathf.Abs(wx - c.x);

                    // Elliptical distance using terrain scale in X/Z
                    // Normalize so that chestPathRadius is the 1.0 threshold,
                    // then feather to chestPathRadius + chestPathFeather.
                    float dist = Mathf.Sqrt(
                        (dxMetersX * dxMetersX) / (chestPathRadius * chestPathRadius + 1e-6f) +
                        (dzMetersZ * dzMetersZ) / (chestPathRadius * chestPathRadius + 1e-6f)
                    );

                    float weightHere = 0f;
                    if (dist <= 1f)
                    {
                        // Inside solid radius: full weight
                        weightHere = chestPathMaxWeight;
                    }
                    else if (dist <= (chestPathRadius + chestPathFeather) / Mathf.Max(1e-6f, chestPathRadius))
                    {
                        // Feather: linear falloff to 0
                        float dMeters = Mathf.Sqrt(dxMetersX * dxMetersX + dzMetersZ * dzMetersZ);
                        float t = Mathf.InverseLerp(chestPathRadius + chestPathFeather, chestPathRadius, dMeters);
                        weightHere = chestPathMaxWeight * Mathf.Clamp01(t);
                    }
                    else
                    {
                        continue; // outside influence
                    }

                    // Apply to path layer, then renormalize
                    float oldPath = alphaMaps[az, ax, pathLayerIndex];
                    float newPath = additivePathPaint ? Mathf.Max(oldPath, weightHere) : weightHere;

                    // If no change, skip work
                    if (Mathf.Approximately(newPath, oldPath)) continue;

                    float totalOld = 0f;
                    for (int li = 0; li < layers; li++) totalOld += alphaMaps[az, ax, li];
                    if (totalOld <= 1e-5f) totalOld = 1f; // safety

                    // Set path weight
                    alphaMaps[az, ax, pathLayerIndex] = newPath;

                    // Renormalize other layers to fill remaining 1 - newPath
                    float sumOthersOld = totalOld - oldPath;
                    float remain = Mathf.Clamp01(1f - newPath);

                    if (sumOthersOld <= 1e-6f)
                    {
                        // Spread evenly among non-path layers
                        float each = (layers > 1) ? remain / (layers - 1) : 0f;
                        for (int li = 0; li < layers; li++)
                            if (li != pathLayerIndex) alphaMaps[az, ax, li] = each;
                    }
                    else
                    {
                        // Scale proportionally
                        for (int li = 0; li < layers; li++)
                        {
                            if (li == pathLayerIndex) continue;
                            float old = alphaMaps[az, ax, li];
                            alphaMaps[az, ax, li] = (old / sumOthersOld) * remain;
                        }
                    }
                }
            }

            discsPainted++;
        }

        // ... inside BakePathsFromChests(), after all loops and discsPainted++ ...

        // after discsPainted++, before SetAlphamaps
        BakePathsBetweenChests();


        // Push back to terrain
        tData.SetAlphamaps(0, 0, alphaMaps);

        // 🔄 Refresh cache so all path checks see the latest paint immediately
        alphaMaps = tData.GetAlphamaps(0, 0, alphaW, alphaH);

        if (verboseLogs)
            Debug.Log($"[Spawner] BakePathsFromChests: Painted {discsPainted} chest path discs onto '{terrain.name}'.");


    }

    // ---------------- SPAWN PASSES (unchanged from your version, with minor wiring) ----------------
    bool IsOnPath(float worldX, float worldZ)
    {
        if (pathLayerIndex < 0) return false;
        float w = LayerWeightAt(worldX, worldZ, pathLayerIndex);
        if (w > pathWeightThreshold) return true;

        // Respect pathPadding ring exactly like TryPickValidLocation does
        if (pathPadding > 0.01f && pathPaddingSampleRingPoints > 0)
        {
            float step = 360f / pathPaddingSampleRingPoints;
            for (int i = 0; i < pathPaddingSampleRingPoints; i++)
            {
                float ang = step * i * Mathf.Deg2Rad;
                float sx = worldX + Mathf.Cos(ang) * pathPadding;
                float sz = worldZ + Mathf.Sin(ang) * pathPadding;
                if (!InsideTerrainBounds(sx, sz)) continue;
                if (LayerWeightAt(sx, sz, pathLayerIndex) > pathWeightThreshold) return true;
            }
        }
        return false;
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

            // Never spawn on/near paths (center, padding, and footprint rings)
            if (IsOnPathArea(pos.x, pos.z)) { rejPath++; continue; }

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

            // Extra safety: never spawn on/near paths, even if something changed mid-frame
            if (IsOnPath(pos.x, pos.z)) { rejPath++; continue; }


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

        // REQUIRE: Grass layer (for tree/grass passes)
        if (requireLayerIndex >= 0)
        {
            float gw = LayerWeightAt(x, z, requireLayerIndex);
            if (gw < requireLayerThreshold) { rejGrassLayer++; return false; }
        }

        // AVOID: Path layer
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
        float u = (worldX - tPos.x) / tSize.x; // 0..1
        float v = (worldZ - tPos.z) / tSize.z; // 0..1
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

    struct GridBounds
    {
        public Vector3 min;   // world-space min (x,z used)
        public Vector3 max;   // world-space max (x,z used)
        public int nx, nz;    // grid dims
    }
    GridBounds MakeGridBoundsFromNetwork()
    {
        var bmin = new Vector3(float.PositiveInfinity, 0, float.PositiveInfinity);
        var bmax = new Vector3(float.NegativeInfinity, 0, float.NegativeInfinity);

        void Acc(Vector3 p)
        {
            bmin.x = Mathf.Min(bmin.x, p.x);
            bmin.z = Mathf.Min(bmin.z, p.z);
            bmax.x = Mathf.Max(bmax.x, p.x);
            bmax.z = Mathf.Max(bmax.z, p.z);
        }

        // chests
        if (chestAnchors != null)
            foreach (var t in chestAnchors) if (t) Acc(t.position);

        // extras (both arrays)
        if (extraPathPoints != null)
            foreach (var t in extraPathPoints) if (t) Acc(t.position);
        if (extraPathPointObjects != null)
            foreach (var go in extraPathPointObjects) if (go) Acc(go.transform.position);

        // pad & clamp
        bmin.x = Mathf.Max(tPos.x, bmin.x - pathSearchPadding);
        bmin.z = Mathf.Max(tPos.z, bmin.z - pathSearchPadding);
        bmax.x = Mathf.Min(tPos.x + tSize.x, bmax.x + pathSearchPadding);
        bmax.z = Mathf.Min(tPos.z + tSize.z, bmax.z + pathSearchPadding);

        int nx = Mathf.Max(2, Mathf.CeilToInt((bmax.x - bmin.x) / Mathf.Max(0.001f, pathGridCellSize)) + 1);
        int nz = Mathf.Max(2, Mathf.CeilToInt((bmax.z - bmin.z) / Mathf.Max(0.001f, pathGridCellSize)) + 1);

        return new GridBounds { min = bmin, max = bmax, nx = nx, nz = nz };
    }


    Vector3 GridToWorld(GridBounds gb, int gx, int gz)
    {
        float x = gb.min.x + gx * pathGridCellSize;
        float z = gb.min.z + gz * pathGridCellSize;
        float y = terrain.SampleHeight(new Vector3(x, 0, z)) + tPos.y;
        return new Vector3(x, y, z);
    }

    void WorldToGrid(GridBounds gb, Vector3 w, out int gx, out int gz)
    {
        gx = Mathf.Clamp(Mathf.RoundToInt((w.x - gb.min.x) / pathGridCellSize), 0, gb.nx - 1);
        gz = Mathf.Clamp(Mathf.RoundToInt((w.z - gb.min.z) / pathGridCellSize), 0, gb.nz - 1);
    }

    float SlopeAt(float worldX, float worldZ)
    {
        Vector3 n = tData.GetInterpolatedNormal(
            Mathf.InverseLerp(tPos.x, tPos.x + tSize.x, worldX),
            Mathf.InverseLerp(tPos.z, tPos.z + tSize.z, worldZ)
        );
        return Vector3.Angle(n, Vector3.up);
    }

    // paint a soft disc into path layer at world position
    void PaintPathDisc(Vector3 c, float innerRadius, float feather, float maxWeight, bool additive)
    {
        if (pathLayerIndex < 0) return;

        float pxPerMeterX = (alphaW - 1) / tSize.x;
        float pxPerMeterZ = (alphaH - 1) / tSize.z;

        int cx = Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(tPos.x, tPos.x + tSize.x, c.x) * (alphaW - 1)), 0, alphaW - 1);
        int cz = Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(tPos.z, tPos.z + tSize.z, c.z) * (alphaH - 1)), 0, alphaH - 1);

        int rFeatherX = Mathf.CeilToInt((innerRadius + feather) * pxPerMeterX);
        int rFeatherZ = Mathf.CeilToInt((innerRadius + feather) * pxPerMeterZ);

        int minX = Mathf.Clamp(cx - rFeatherX, 0, alphaW - 1);
        int maxX = Mathf.Clamp(cx + rFeatherX, 0, alphaW - 1);
        int minZ = Mathf.Clamp(cz - rFeatherZ, 0, alphaH - 1);
        int maxZ = Mathf.Clamp(cz + rFeatherZ, 0, alphaH - 1);

        int layers = tData.terrainLayers.Length;

        for (int az = minZ; az <= maxZ; az++)
        {
            float wz = Mathf.Lerp(tPos.z, tPos.z + tSize.z, az / (float)(alphaH - 1));
            float dz = Mathf.Abs(wz - c.z);
            for (int ax = minX; ax <= maxX; ax++)
            {
                float wx = Mathf.Lerp(tPos.x, tPos.x + tSize.x, ax / (float)(alphaW - 1));
                float dx = Mathf.Abs(wx - c.x);

                float d = Mathf.Sqrt(dx * dx + dz * dz);

                float w = 0f;
                if (d <= innerRadius) w = maxWeight;
                else if (d <= innerRadius + feather)
                {
                    float t = Mathf.InverseLerp(innerRadius + feather, innerRadius, d);
                    w = maxWeight * t;
                }
                else continue;

                float oldPath = alphaMaps[az, ax, pathLayerIndex];
                float newPath = additive ? Mathf.Max(oldPath, w) : w;
                if (Mathf.Approximately(newPath, oldPath)) continue;

                // renormalize
                float totalOld = 0f;
                for (int li = 0; li < layers; li++) totalOld += alphaMaps[az, ax, li];
                if (totalOld <= 1e-6f) totalOld = 1f;

                alphaMaps[az, ax, pathLayerIndex] = newPath;

                float sumOthersOld = totalOld - oldPath;
                float remain = Mathf.Clamp01(1f - newPath);

                if (sumOthersOld <= 1e-6f)
                {
                    float each = (layers > 1) ? remain / (layers - 1) : 0f;
                    for (int li = 0; li < layers; li++)
                        if (li != pathLayerIndex) alphaMaps[az, ax, li] = each;
                }
                else
                {
                    for (int li = 0; li < layers; li++)
                    {
                        if (li == pathLayerIndex) continue;
                        float old = alphaMaps[az, ax, li];
                        alphaMaps[az, ax, li] = (old / sumOthersOld) * remain;
                    }
                }
            }
        }
    }

    // stroke a polyline with the path brush
    void PaintPolyline(List<Vector3> pts)
    {
        if (pts == null || pts.Count == 0) return;
        float step = Mathf.Max(0.5f, pathGridCellSize * 0.5f);
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 a = pts[i], b = pts[i + 1];
            float dist = Vector3.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / step));
            for (int s = 0; s <= steps; s++)
            {
                Vector3 p = Vector3.Lerp(a, b, s / (float)steps);
                PaintPathDisc(p, pathBrushRadius, pathBrushFeather, pathBrushMaxWeight, additivePathPaint);
            }
        }
    }
    void BakePathsBetweenChests()
    {
        if (!connectChestsWithPaths) return;
        if (chestAnchors == null || chestAnchors.Length < 1) return;

        // 1) Build node lists
        List<Transform> chestNodes = new List<Transform>();
        foreach (var t in chestAnchors) if (t) chestNodes.Add(t);
        if (chestNodes.Count == 0) return;

        List<Vector3> extraNodes = GetExtraPoints(); // can be empty

        // 2) Initial edges among CHESTS according to mode (guarantees at least a spanning structure if MST)
        List<(int a, int b, bool isChestChest)> chestEdges = new();

        if (chestNodes.Count >= 2)
        {
            if (chestConnectMode == ChestConnectMode.AllPairs)
            {
                for (int i = 0; i < chestNodes.Count; i++)
                    for (int j = i + 1; j < chestNodes.Count; j++)
                        chestEdges.Add((i, j, true));
            }
            else if (chestConnectMode == ChestConnectMode.NearestNeighbor)
            {
                for (int i = 0; i < chestNodes.Count; i++)
                {
                    int bestJ = -1; float best = float.PositiveInfinity;
                    for (int j = 0; j < chestNodes.Count; j++)
                    {
                        if (i == j) continue;
                        float d = (chestNodes[i].position - chestNodes[j].position).sqrMagnitude;
                        if (d < best) { best = d; bestJ = j; }
                    }
                    if (bestJ >= 0)
                    {
                        // avoid duplicates: only keep i<bestJ
                        if (i < bestJ) chestEdges.Add((i, bestJ, true));
                    }
                }
            }
            else // MST (Prim) on chests
            {
                int n = chestNodes.Count;
                bool[] inTree = new bool[n];
                inTree[0] = true;
                for (int k = 1; k < n; k++)
                {
                    float best = float.PositiveInfinity; int bi = -1, bj = -1;
                    for (int i = 0; i < n; i++) if (inTree[i])
                            for (int j = 0; j < n; j++) if (!inTree[j])
                                {
                                    float d = (chestNodes[i].position - chestNodes[j].position).sqrMagnitude;
                                    if (d < best) { best = d; bi = i; bj = j; }
                                }
                    if (bi >= 0) { chestEdges.Add((bi, bj, true)); inTree[bj] = true; }
                }
            }
        }

        // 3) Edges from each EXTRA to its NEAREST CHEST (one edge each)
        List<(int chestIndex, int extraIndex)> extraEdges = new();
        for (int ei = 0; ei < extraNodes.Count; ei++)
        {
            int ci = NearestChestIndex(extraNodes[ei], chestNodes);
            if (ci >= 0) extraEdges.Add((ci, ei));
        }

        // 4) Attempt to route and paint all planned edges; track which succeed
        // Build a unified node index space: [0..C-1]=chests, [C..C+E-1]=extras
        int C = chestNodes.Count, E = extraNodes.Count, N = C + E;
        DSU dsu = new DSU(N);

        int painted = 0, failed = 0;

        // Try chest-chest
        foreach (var e in chestEdges)
        {
            Vector3 a = chestNodes[e.a].position;
            Vector3 b = chestNodes[e.b].position;
            if (TryFindPathAutoRelax(a, b, out var route, out var used))
            {
                PaintPolyline(route);
                painted++;
                dsu.Union(e.a, e.b);
                if (verboseLogs) Debug.Log($"[Spawner] Chest link {e.a}-{e.b} (≤{used:0.#}°) ok");
            }
            else { failed++; }
        }

        // Try extra->nearest chest
        for (int k = 0; k < extraEdges.Count; k++)
        {
            int ci = extraEdges[k].chestIndex;
            int ei = extraEdges[k].extraIndex;
            Vector3 a = chestNodes[ci].position;
            Vector3 b = extraNodes[ei];
            if (TryFindPathAutoRelax(a, b, out var route, out var used))
            {
                PaintPolyline(route);
                painted++;
                dsu.Union(ci, C + ei);
                if (verboseLogs) Debug.Log($"[Spawner] Extra {ei}→Chest {ci} (≤{used:0.#}°) ok");
            }
            else { failed++; }
        }

        // 5) If still not fully connected, auto-patch components by shortest pairwise bridges.
        // Prefer bridging via CHESTS (more sensible roads); fall back to any nodes if needed.
        if (dsu.CountRoots() > 1)
        {
            bool progress = true;
            while (progress && dsu.CountRoots() > 1)
            {
                progress = false;
                float best = float.PositiveInfinity;
                int ai = -1, bi = -1; bool aIsExtra = false, bIsExtra = false;

                // Try chest-chest shortest cross-component pair
                for (int i = 0; i < C; i++)
                    for (int j = i + 1; j < C; j++)
                        if (dsu.Root(i) != dsu.Root(j))
                        {
                            float d = (chestNodes[i].position - chestNodes[j].position).sqrMagnitude;
                            if (d < best) { best = d; ai = i; bi = j; aIsExtra = false; bIsExtra = false; }
                        }

                // If none found (degenerate cases), allow chest/extra or extra/extra bridging
                if (ai < 0)
                {
                    // all nodes list
                    System.Func<int, Vector3> NodePos = idx =>
                        idx < C ? chestNodes[idx].position : extraNodes[idx - C];

                    for (int i = 0; i < N; i++)
                        for (int j = i + 1; j < N; j++)
                            if (dsu.Root(i) != dsu.Root(j))
                            {
                                float d = (NodePos(i) - NodePos(j)).sqrMagnitude;
                                if (d < best) { best = d; ai = i; bi = j; aIsExtra = i >= C; bIsExtra = j >= C; }
                            }
                }

                if (ai >= 0)
                {
                    Vector3 a = (ai < C) ? chestNodes[ai].position : extraNodes[ai - C];
                    Vector3 b = (bi < C) ? chestNodes[bi].position : extraNodes[bi - C];

                    if (TryFindPathAutoRelax(a, b, out var route, out var used))
                    {
                        PaintPolyline(route);
                        painted++;
                        dsu.Union(ai, bi);
                        progress = true;
                        if (verboseLogs) Debug.Log($"[Spawner] Auto-bridged components ({ai}{(aIsExtra ? "*" : "")}↔{bi}{(bIsExtra ? "*" : "")}) (≤{used:0.#}°)");
                    }
                    else
                    {
                        // Couldn't bridge the closest pair; try next closest in next loop
                        // (loop continues unless no progress made this pass)
                    }
                }
            }
        }

        if (verboseLogs)
            Debug.Log($"[Spawner] Chest network total: painted {painted}, failed {failed}, components now {dsu.CountRoots()}.");
    }

    // 8-connected neighbors
    static readonly (int dx, int dz, float cost)[] NBRS = new (int, int, float)[]
    {
    (-1,  0, 1f), (1,  0, 1f), (0, -1, 1f), (0,  1, 1f),
    (-1, -1, 1.4142f), (1, -1, 1.4142f), (-1, 1, 1.4142f), (1, 1, 1.4142f)
    };

    bool FindPathAStar(GridBounds gb, Vector3 startW, Vector3 goalW, float slopeLimitDeg, out List<Vector3> path)
    {
        path = null;
        WorldToGrid(gb, startW, out int sx, out int sz);
        WorldToGrid(gb, goalW, out int gx, out int gz);

        int N = gb.nx * gb.nz;
        float[] g = new float[N];
        int[] came = new int[N];
        bool[] closed = new bool[N];
        for (int i = 0; i < N; i++) { g[i] = float.PositiveInfinity; came[i] = -1; closed[i] = false; }

        int Idx(int x, int z) => z * gb.nx + x;

        // heuristic: euclidean in grid cells
        float Heu(int x, int z) => Vector2.Distance(new Vector2(x, z), new Vector2(gx, gz));

        // walkable check by slope
        bool Walkable(int x, int z)
        {
            Vector3 w = GridToWorld(gb, x, z);
            if (w.y < oceanLevelY) return false;
            float s = SlopeAt(w.x, w.z);
            return s <= slopeLimitDeg;
        }

        // min-heap replacement: simple linear open list (ok for small grids)
        List<int> open = new List<int>();
        int sIdx = Idx(sx, sz);
        g[sIdx] = 0f;
        open.Add(sIdx);

        while (open.Count > 0)
        {
            // pick node with lowest f = g + h
            int bestI = 0;
            float bestF = float.PositiveInfinity;
            for (int i = 0; i < open.Count; i++)
            {
                int id = open[i];
                int x = id % gb.nx, z = id / gb.nx;
                float f = g[id] + Heu(x, z);
                if (f < bestF) { bestF = f; bestI = i; }
            }

            int cur = open[bestI];
            open.RemoveAt(bestI);
            if (closed[cur]) continue;
            closed[cur] = true;

            int cx = cur % gb.nx, cz = cur / gb.nx;
            if (cx == gx && cz == gz)
            {
                // reconstruct
                List<Vector3> pts = new List<Vector3>();
                int k = cur;
                while (k != -1)
                {
                    int x = k % gb.nx, z = k / gb.nx;
                    pts.Add(GridToWorld(gb, x, z));
                    k = came[k];
                }
                pts.Reverse();
                path = pts;
                return true;
            }

            for (int n = 0; n < NBRS.Length; n++)
            {
                int nx = cx + NBRS[n].dx;
                int nz = cz + NBRS[n].dz;
                if (nx < 0 || nz < 0 || nx >= gb.nx || nz >= gb.nz) continue;

                int nid = Idx(nx, nz);
                if (closed[nid]) continue;
                if (!Walkable(nx, nz)) continue;

                float tentative = g[cur] + NBRS[n].cost;
                if (tentative < g[nid])
                {
                    g[nid] = tentative;
                    came[nid] = cur;
                    if (!open.Contains(nid)) open.Add(nid);
                }
            }
        }

        return false; // no route
    }

    bool TryFindPathAutoRelax(Vector3 a, Vector3 b, out List<Vector3> pts, out float usedSlopeLimit)
    {
        float limit = pathMaxSlopeDeg;
        pts = null;
        usedSlopeLimit = limit;

        GridBounds gb = MakeGridBoundsFromNetwork(); // bounds large enough for all pairs

        while (true)
        {
            if (FindPathAStar(gb, a, b, limit, out pts))
            {
                usedSlopeLimit = limit;
                return true;
            }

            if (!pathAutoRelaxSlope) return false;
            if (limit >= pathRelaxMaxDeg) return false;

            limit = Mathf.Min(limit + pathRelaxStepDeg, pathRelaxMaxDeg);
            // loop and try again with a looser slope
        }
    }

    // Union-Find (Disjoint Set) for connectivity checks
    class DSU
    {
        int[] p; int[] r;
        public DSU(int n) { p = new int[n]; r = new int[n]; for (int i = 0; i < n; i++) p[i] = i; }

        // keep the internal path-compressing find private
        int Find(int x) => p[x] == x ? x : (p[x] = Find(p[x]));

        // public accessor for comparisons outside this class
        public int Root(int x) => Find(x);

        public void Union(int a, int b)
        {
            a = Find(a); b = Find(b); if (a == b) return;
            if (r[a] < r[b]) p[a] = b; else if (r[a] > r[b]) p[b] = a; else { p[b] = a; r[a]++; }
        }

        public int CountRoots()
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < p.Length; i++) seen.Add(Find(i));
            return seen.Count;
        }
    }


    // Gather extra points as world positions
    List<Vector3> GetExtraPoints()
    {
        var list = new List<Vector3>();
        if (extraPathPoints != null)
            foreach (var t in extraPathPoints) if (t) list.Add(t.position);
        if (extraPathPointObjects != null)
            foreach (var go in extraPathPointObjects) if (go) list.Add(go.transform.position);
        return list;
    }

    // Returns index of nearest chest to point p (world)
    int NearestChestIndex(Vector3 p, List<Transform> chests)
    {
        int best = -1; float bestD = float.PositiveInfinity;
        for (int i = 0; i < chests.Count; i++)
        {
            float d = (chests[i].position - p).sqrMagnitude;
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }


}
