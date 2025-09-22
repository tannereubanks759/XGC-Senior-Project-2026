using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class IslandTerrainGenerator : MonoBehaviour
{
    [Header("Terrain")]
    public Terrain terrain;                     // Assign your Terrain
    [Tooltip("World Y of the terrain root. Set to -50 so sea level (0) gives max 50m depth.")]
    public float terrainBaseY = -50f;           // keep at -50 for your spec
    [Tooltip("Maximum vertical relief (height) above terrain base, in meters.")]
    public float maxIslandHeight = 90f;

    [Header("Sea & Beach")]
    [Tooltip("World Y sea level. 0 for your spec.")]
    public float oceanLevelY = 0f;
    [Tooltip("Beach thickness above sea level in meters to paint as sand.")]
    public float beachHeight = 6f;

    [Header("Island Shape")]
    [Tooltip("Approximate island radius in world meters.")]
    public float islandRadius = 600f;
    [Tooltip("Extra feather distance beyond radius to smoothly fall to sea.")]
    public float edgeFeather = 120f;
    [Tooltip("Center of the island in world space. Leave zero to auto-center on terrain.")]
    public Vector2 islandCenterWorldXZ = Vector2.zero;
    [Tooltip("Controls how sharply the island rises (2–6 typical). Higher = steeper center, softer edge.")]
    [Range(1f, 8f)] public float falloffPower = 3.5f;

    [Header("Noise (Base)")]
    public int seed = 12345;
    [Tooltip("Meters per noise tile; larger = broader features.")]
    public float noiseScale = 380f;
    [Range(1, 8)] public int octaves = 4;
    [Range(0.25f, 0.9f)] public float persistence = 0.55f;
    [Range(1.5f, 3.5f)] public float lacunarity = 2.0f;
    [Tooltip("Vertical influence of the fractal noise (0..1).")]
    [Range(0f, 1f)] public float noiseAmplitude01 = 0.75f;
    [Tooltip("Ridgey terrain (|noise| inverted) vs soft hills.")]
    public bool ridged = false;

    [Header("Shelf / Seafloor")]
    [Tooltip("How much of the non-island area is pulled below sea level (0..1). 1 = deep shelf, 0 = clamp at sea level.")]
    [Range(0f, 1f)] public float seaShelfDepth01 = 0.8f;

    [Header("Textures (Splat)")]
    [Tooltip("Sand TerrainLayer (painted from sea level up to beachHeight).")]
    public TerrainLayer sandLayer;
    [Tooltip("Grass TerrainLayer (painted above beachHeight).")]
    public TerrainLayer grassLayer;

    [Header("Coastline Variety")]
    [Tooltip("Meters-scale wobble carved into the shoreline by noise.")]
    public float coastNoiseStrength = 120f;        // how deep coves/peninsulas push/pull the coast
    [Tooltip("Meters per tile for the coastline noise (lower = larger, slower variations).")]
    public float coastNoiseScale = 420f;
    [Tooltip("Angular waves around the island. 3–9 gives nice lobes.")]
    [Range(0, 12)] public int angularWaves = 6;
    [Tooltip("How strong the angular lobes are, as a fraction of radius (0..0.3 typical).")]
    [Range(0f, 0.4f)] public float angularWaveStrength = 0.12f;
    [Tooltip("Sharpens the carved coves to avoid mushy edges (1=soft, 2–4=sharper).")]
    [Range(1f, 4f)] public float coastSharpness = 2.2f;

    [Header("Gizmos")]
    public bool drawGizmos = true;

    System.Random prng;

    void Reset()
    {
        terrain = FindFirstObjectByType<Terrain>();
    }

    [ContextMenu("Generate Island")]
    public void GenerateIsland()
    {
        if (!ValidateInputs(out TerrainData tData)) return;

        // Lock terrain base Y (your spec)
        var t = terrain.transform;
        if (t.position.y != terrainBaseY)
            t.position = new Vector3(t.position.x, terrainBaseY, t.position.z);

        int hmW = tData.heightmapResolution;
        int hmH = tData.heightmapResolution;

        float[,] heights = new float[hmH, hmW];

        // Helpful locals
        Vector3 tPos = t.position;
        Vector3 tSize = tData.size;

        // Auto-center island on terrain if not set
        Vector2 centerXZ = islandCenterWorldXZ == Vector2.zero
            ? new Vector2(tPos.x + tSize.x * 0.5f, tPos.z + tSize.z * 0.5f)
            : islandCenterWorldXZ;

        // Seed PRNG and noise offsets
        prng = new System.Random(seed);
        Vector2 baseOffset = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));

        // Precompute scales
        float invScale = 1f / Mathf.Max(1e-3f, noiseScale);
        float invCoast = 1f / Mathf.Max(1e-3f, coastNoiseScale);

        // Build heightmap
        for (int z = 0; z < hmH; z++)
        {
            for (int x = 0; x < hmW; x++)
            {
                // World position at this height sample (x,z).
                float u = (float)x / (hmW - 1);
                float v = (float)z / (hmH - 1);
                float wx = Mathf.Lerp(tPos.x, tPos.x + tSize.x, u);
                float wz = Mathf.Lerp(tPos.z, tPos.z + tSize.z, v);

                // ---------- Coastline variety (perturbed shoreline) ----------
                float dx = wx - centerXZ.x;
                float dz = wz - centerXZ.y;
                float d = Mathf.Sqrt(dx * dx + dz * dz);

                // Per-pixel coastal noise (push/pull shoreline)
                float coastN = Mathf.PerlinNoise(
                    (wx + seed * 13.37f) * invCoast,
                    (wz - seed * 7.91f) * invCoast
                ); // 0..1
                coastN = Mathf.Pow(coastN, coastSharpness);                 // sharpen coves
                float coastOffset = (coastN * 2f - 1f) * coastNoiseStrength; // -S..+S meters

                // Angular lobes so the coast isn't circular
                float ang = Mathf.Atan2(dz, dx);
                float lobes = (angularWaves > 0) ? Mathf.Sin(ang * angularWaves + seed * 0.123f) : 0f;
                float lobeOffset = lobes * (angularWaveStrength * islandRadius);

                // Effective target radius at this bearing
                float effectiveRadius = Mathf.Max(8f, islandRadius + lobeOffset + coastOffset);

                // Smooth feather to sea level beyond the (perturbed) radius
                float edge0 = effectiveRadius;
                float edge1 = effectiveRadius + Mathf.Max(4f, edgeFeather);
                float radial = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - edge0) / Mathf.Max(1f, edge1 - edge0)));
                radial = Mathf.Pow(Mathf.Clamp01(radial), falloffPower);
                // -------------------------------------------------------------

                // Fractal noise (0..1)
                float n = FractalNoise(wx * invScale + baseOffset.x, wz * invScale + baseOffset.y, octaves, persistence, lacunarity, ridged);

                // Blend with radial mask
                float island01 = radial * (noiseAmplitude01 * n + (1f - noiseAmplitude01) * radial); // ensures hill in center

                // World Y = base + island_height
                float worldY = terrainBaseY + island01 * maxIslandHeight;

                // Outside island, push down toward seafloor
                if (radial <= 0f)
                {
                    float deep = Mathf.Lerp(oceanLevelY - 50f, terrainBaseY, 1f - seaShelfDepth01); // default ~ -50 at your spec
                    worldY = Mathf.Min(worldY, deep);
                }

                // Clamp so we never exceed terrain vertical bounds
                worldY = Mathf.Clamp(worldY, terrainBaseY, terrainBaseY + tSize.y);

                // Convert worldY -> heightmap value [0..1]
                float h01 = (worldY - terrainBaseY) / tSize.y;
                heights[z, x] = h01;
            }
        }

        tData.SetHeights(0, 0, heights);

        // Paint splats: sand near sea level, grass elsewhere
        PaintTextures(tData, tPos, tSize);

#if UNITY_EDITOR
        EditorUtility.SetDirty(tData);
        EditorUtility.SetDirty(terrain);
#endif
    }

    float FractalNoise(float x, float y, int oct, float pers, float lac, bool ridge)
    {
        float amp = 1f;
        float freq = 1f;
        float sum = 0f;
        float norm = 0f;

        for (int i = 0; i < oct; i++)
        {
            float nx = x * freq;
            float ny = y * freq;
            float v = Mathf.PerlinNoise(nx, ny); // 0..1

            if (ridge)
            {
                // Ridged: invert |2v-1|
                v = 1f - Mathf.Abs(2f * v - 1f);
            }

            sum += v * amp;
            norm += amp;
            amp *= pers;
            freq *= lac;
        }

        if (norm < 1e-5f) return 0f;

        // Normalize to 0..1
        float n01 = Mathf.Clamp01(sum / norm);
        return n01;
    }

    void PaintTextures(TerrainData tData, Vector3 tPos, Vector3 tSize)
    {
        // Ensure layers
        if (sandLayer == null || grassLayer == null)
        {
            Debug.LogWarning("Assign TerrainLayers for Sand & Grass to paint textures.");
            return;
        }

        // Apply layers to terrain if not already present
        var layers = tData.terrainLayers;
        bool needSand = true, needGrass = true;
        foreach (var l in layers)
        {
            if (l == sandLayer) needSand = false;
            if (l == grassLayer) needGrass = false;
        }
        if (needSand || needGrass)
        {
            var list = new System.Collections.Generic.List<TerrainLayer>(layers);
            if (needSand) list.Add(sandLayer);
            if (needGrass) list.Add(grassLayer);
            tData.terrainLayers = list.ToArray();
        }

        // Find indices
        int sandIdx = System.Array.IndexOf(tData.terrainLayers, sandLayer);
        int grassIdx = System.Array.IndexOf(tData.terrainLayers, grassLayer);
        if (sandIdx < 0 || grassIdx < 0)
        {
            Debug.LogWarning("Could not find both sand & grass layers on TerrainData.");
            return;
        }

        int aw = tData.alphamapWidth;
        int ah = tData.alphamapHeight;
        int layersCount = tData.alphamapLayers;

        float[,,] maps = new float[ah, aw, layersCount];

        // Shoreline jitter scale (reuse coastNoiseScale feel)
        float shoreInv = 1f / Mathf.Max(1e-3f, coastNoiseScale * 0.75f);

        for (int z = 0; z < ah; z++)
        {
            for (int x = 0; x < aw; x++)
            {
                // World position for this splat texel
                float u = (float)x / (aw - 1);
                float v = (float)z / (ah - 1);
                float wx = Mathf.Lerp(tPos.x, tPos.x + tSize.x, u);
                float wz = Mathf.Lerp(tPos.z, tPos.z + tSize.z, v);

                // Sample height
                float h01 = tData.GetInterpolatedHeight(u, v) / tSize.y;
                float worldY = terrainBaseY + h01 * tSize.y;

                // Small jitter so beach line meanders
                float shoreJitter = (Mathf.PerlinNoise((wx + seed) * shoreInv, (wz - seed) * shoreInv) * 2f - 1f) * 1.5f; // ~±1.5m

                // Sand if within beachHeight above sea level (with jitter)
                float sandWeight = Mathf.Clamp01(1f - Mathf.InverseLerp(oceanLevelY, oceanLevelY + beachHeight + shoreJitter, worldY));
                sandWeight = Mathf.SmoothStep(0f, 1f, sandWeight);

                float grassWeight = 1f - sandWeight;

                // Initialize all to 0
                for (int l = 0; l < layersCount; l++) maps[z, x, l] = 0f;

                maps[z, x, sandIdx] = sandWeight;
                maps[z, x, grassIdx] = grassWeight;
            }
        }

        tData.SetAlphamaps(0, 0, maps);
    }

    bool ValidateInputs(out TerrainData tData)
    {
        tData = null;

        if (!terrain)
        {
            Debug.LogError("Assign a Terrain.");
            return false;
        }

        tData = terrain.terrainData;
        if (!tData)
        {
            Debug.LogError("Terrain has no TerrainData.");
            return false;
        }

        if (tData.heightmapResolution <= 0)
        {
            Debug.LogError("Terrain heightmap resolution invalid.");
            return false;
        }

        if (islandRadius <= 4f)
        {
            Debug.LogWarning("Island radius is tiny—consider increasing.");
        }

        return true;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(IslandTerrainGenerator))]
    public class IslandTerrainGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var gen = (IslandTerrainGenerator)target;

            GUILayout.Space(8);
            if (GUILayout.Button("Generate Island", GUILayout.Height(36)))
            {
                gen.GenerateIsland();
            }

            EditorGUILayout.HelpBox(
                "Steps:\n" +
                "1) Assign Terrain, Sand & Grass TerrainLayers.\n" +
                "2) Click 'Generate Island'.\n" +
                "Notes:\n" +
                "- Sea level is world Y=0.\n" +
                "- Terrain is positioned at Y=-50 so max ocean depth is 50.\n" +
                "- Use Island Radius to control general island size; Edge Feather softens the shoreline.\n" +
                "- Coastline Variety adds coves/lobes without creating extra islands.",
                MessageType.Info);
        }
    }
#endif

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || terrain == null) return;

        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;

        Vector3 center = new Vector3(
            islandCenterWorldXZ == Vector2.zero ? tPos.x + tSize.x * 0.5f : islandCenterWorldXZ.x,
            oceanLevelY,
            islandCenterWorldXZ == Vector2.zero ? tPos.z + tSize.z * 0.5f : islandCenterWorldXZ.y
        );

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.35f);
        DrawWireDisc(center, Vector3.up, islandRadius);
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.15f);
        DrawWireDisc(center, Vector3.up, islandRadius + edgeFeather);
    }

    // Simple disc drawer that works in Edit mode
    void DrawWireDisc(Vector3 center, Vector3 up, float radius, int segments = 96)
    {
        Vector3 right = Vector3.Cross(up, Vector3.forward);
        if (right.sqrMagnitude < 1e-4f) right = Vector3.Cross(up, Vector3.right);
        right.Normalize();
        Vector3 forward = Vector3.Cross(right, up).normalized;

        Vector3 prev = center + right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 p = center + (right * Mathf.Cos(a) + forward * Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}
