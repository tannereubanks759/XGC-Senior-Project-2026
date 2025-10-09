using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("Terrain/Tools/Slope Repaint")]
[ExecuteAlways]
public class SlopeRepaint : MonoBehaviour
{
    [Header("Parameters")]
    public Terrain targetTerrain;
    [Tooltip("Index of the Terrain Layer to paint (must already be on the Terrain).")]
    public int targetLayerIndex = 0;

    [Range(0f, 89.9f)]
    [Tooltip("Paint anywhere the terrain slope (degrees) is >= this value.")]
    public float slopeAngle = 60f;

    [Header("Feedback")]
    public bool verboseLogging = true;

    // Reference to an on-disk ScriptableObject that holds a backup of alphamaps
    public SlopeRepaintAlphamapBackup backupAsset;

    // ---- Public actions (show up in the header menu as well) ----

    [ContextMenu("Capture Backup")]
    public void CaptureBackup()
    {
        if (!ValidateTerrain(out var td)) return;

        int w = td.alphamapWidth;
        int h = td.alphamapHeight;
        int layers = td.alphamapLayers;
        var data = td.GetAlphamaps(0, 0, w, h);

#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(td, "Capture Alphamap Backup");
#endif

        // Create or reuse backup asset
#if UNITY_EDITOR
        if (backupAsset == null)
        {
            backupAsset = ScriptableObject.CreateInstance<SlopeRepaintAlphamapBackup>();
            string path = SaveAssetDialogDefault("SlopeRepaint_AlphamapBackup.asset");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(backupAsset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                // user canceled; still keep it in-memory so Restore works this session
            }
        }

        Undo.RegisterCreatedObjectUndo(backupAsset, "Create Alphamap Backup Asset");
#endif

        backupAsset?.Store(data, w, h, layers);

        if (verboseLogging)
            Debug.Log($"[SlopeRepaint] Captured backup ({w}x{h}, {layers} layers).");
    }

    [ContextMenu("Restore Backup")]
    public void RestoreBackup()
    {
        if (!ValidateTerrain(out var td)) return;

        if (backupAsset == null || !backupAsset.HasData)
        {
            Debug.LogWarning("[SlopeRepaint] No backup to restore. Click 'Capture Backup' first.");
            return;
        }

        if (!backupAsset.Matches(td))
        {
            Debug.LogError("[SlopeRepaint] Backup dimensions/layers do not match current TerrainData.");
            return;
        }

#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(td, "Restore Alphamap Backup");
#endif

        var restored = backupAsset.To3DArray();
        td.SetAlphamaps(0, 0, restored);

        if (verboseLogging)
            Debug.Log("[SlopeRepaint] Alphamaps restored from backup.");
    }

    [ContextMenu("Repaint Slopes Now")]
    public void RepaintNow()
    {
        if (!ValidateTerrain(out var td)) return;

        int layers = td.alphamapLayers;
        if (layers == 0 || targetLayerIndex < 0 || targetLayerIndex >= layers)
        {
            Debug.LogError($"[SlopeRepaint] targetLayerIndex {targetLayerIndex} invalid. Terrain has {layers} layers.");
            return;
        }

        int w = td.alphamapWidth;
        int h = td.alphamapHeight;

        // Auto-capture a backup one time if none exists
        if (backupAsset == null || !backupAsset.HasData)
        {
#if UNITY_EDITOR
            if (verboseLogging) Debug.Log("[SlopeRepaint] No backup found — capturing one automatically.");
#endif
            CaptureBackup();
        }

        float[,,] alphas = td.GetAlphamaps(0, 0, w, h);

#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(td, "Slope Repaint Alphamaps");
#endif

        int painted = 0;
        for (int y = 0; y < h; y++)
        {
            float nz = (h <= 1) ? 0f : (float)y / (h - 1);
            for (int x = 0; x < w; x++)
            {
                float nx = (w <= 1) ? 0f : (float)x / (w - 1);
                float steep = td.GetSteepness(nx, nz);

                if (steep >= slopeAngle)
                {
                    for (int l = 0; l < layers; l++)
                        alphas[y, x, l] = (l == targetLayerIndex) ? 1f : 0f;
                    painted++;
                }
            }
        }

        td.SetAlphamaps(0, 0, alphas);

        if (verboseLogging)
            Debug.Log($"[SlopeRepaint] Painted layer #{targetLayerIndex} on {painted}/{w * h} pixels (≥ {slopeAngle:0.#}°).");
    }

    private bool ValidateTerrain(out TerrainData td)
    {
        td = null;
        if (targetTerrain == null)
        {
            Debug.LogError("[SlopeRepaint] No Terrain assigned.");
            return false;
        }
        td = targetTerrain.terrainData;
        if (td == null)
        {
            Debug.LogError("[SlopeRepaint] TerrainData missing.");
            return false;
        }
        return true;
    }

    private void OnValidate()
    {
        if (targetTerrain == null) return;
        var td = targetTerrain.terrainData;
        if (td == null) return;
        if (targetLayerIndex < 0) targetLayerIndex = 0;
        if (td.alphamapLayers > 0 && targetLayerIndex >= td.alphamapLayers)
            targetLayerIndex = td.alphamapLayers - 1;
    }

#if UNITY_EDITOR
    private static string SaveAssetDialogDefault(string defaultName)
    {
        var folder = "Assets";
        var path = EditorUtility.SaveFilePanelInProject(
            "Save Alphamap Backup Asset",
            defaultName,
            "asset",
            "Choose a location to save the alphamap backup asset.");
        return path;
    }
#endif
}

// ===== ScriptableObject that stores a flattened alphamap backup =====
public class SlopeRepaintAlphamapBackup : ScriptableObject
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int layers;
    [SerializeField] private float[] flattened; // size = w*h*layers

    public bool HasData => flattened != null && flattened.Length == width * height * layers;

    public void Store(float[,,] data, int w, int h, int l)
    {
        width = w; height = h; layers = l;
        flattened = new float[w * h * l];
        int i = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                for (int a = 0; a < l; a++)
                    flattened[i++] = data[y, x, a];
    }

    public bool Matches(TerrainData td)
    {
        return td != null &&
               td.alphamapWidth == width &&
               td.alphamapHeight == height &&
               td.alphamapLayers == layers;
    }

    public float[,,] To3DArray()
    {
        var result = new float[height, width, layers];
        int i = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                for (int a = 0; a < layers; a++)
                    result[y, x, a] = flattened[i++];
        return result;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SlopeRepaint))]
public class SlopeRepaintEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var tool = (SlopeRepaint)target;

        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Capture Backup"))
                tool.CaptureBackup();
            using (new EditorGUI.DisabledScope(tool.backupAsset == null || !tool.backupAsset.HasData))
            {
                if (GUILayout.Button("Restore Backup"))
                    tool.RestoreBackup();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Repaint Slopes Now"))
                tool.RepaintNow();

            EditorGUILayout.HelpBox(
                "Backup is stored as a ScriptableObject asset so you can restore later—even after closing Unity. "
              + "Undo/Redo also works for each action.", MessageType.Info);
        }
    }
}
#endif
