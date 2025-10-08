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

    [Header("Safety")]
    public bool verboseLogging = true;

    [ContextMenu("Repaint Slopes Now")]
    public void RepaintNow()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("[SlopeRepaint] No Terrain assigned.");
            return;
        }

        var td = targetTerrain.terrainData;
        if (td == null)
        {
            Debug.LogError("[SlopeRepaint] TerrainData missing.");
            return;
        }

        int layers = td.alphamapLayers;
        if (layers == 0 || targetLayerIndex < 0 || targetLayerIndex >= layers)
        {
            Debug.LogError($"[SlopeRepaint] targetLayerIndex {targetLayerIndex} invalid. Terrain has {layers} layers.");
            return;
        }

        int w = td.alphamapWidth;
        int h = td.alphamapHeight;
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

    private void OnValidate()
    {
        if (targetTerrain == null) return;
        var td = targetTerrain.terrainData;
        if (td == null) return;
        if (targetLayerIndex < 0) targetLayerIndex = 0;
        if (td.alphamapLayers > 0 && targetLayerIndex >= td.alphamapLayers)
            targetLayerIndex = td.alphamapLayers - 1;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SlopeRepaint))]
public class SlopeRepaintEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("Repaint Slopes Now"))
            {
                var tool = (SlopeRepaint)target;
                tool.RepaintNow();
            }

            EditorGUILayout.HelpBox(
                "Button not working? Ensure the target Terrain has the layer at Target Layer Index.",
                MessageType.Info);
        }
    }
}
#endif
