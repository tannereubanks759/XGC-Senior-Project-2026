#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CompassStripGenerator : MonoBehaviour
{
    [Header("Construction")]
    public RectTransform tickParent;   // usually self
    public float stripWidthPixels = 2048f;
    public float stripHeight = 64f;

    [Header("Optional Prefabs (leave null to auto-generate)")]
    public GameObject majorTickPrefab; // thin Image
    public GameObject minorTickPrefab; // thin Image
    public TMP_Text labelPrefab;       // TMP label for N/E/S/W
    public Color labelColor = Color.white;

    [Header("Tick Spacing")]
    public int minorEveryDegrees = 5;
    public int majorEveryDegrees = 15;

    void OnValidate()
    {
        if (!tickParent) tickParent = GetComponent<RectTransform>();
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Strip")]
    void Rebuild()
    {
        if (!tickParent) return;

        // Clear children
        var toDelete = new System.Collections.Generic.List<GameObject>();
        foreach (Transform c in tickParent) toDelete.Add(c.gameObject);
        foreach (var g in toDelete) DestroyImmediate(g);

        float pxPerDeg = stripWidthPixels / 360f;

        // Ensure strip size
        var self = tickParent.GetComponent<RectTransform>();
        self.sizeDelta = new Vector2(stripWidthPixels, stripHeight);
        self.anchorMin = new Vector2(0, 0.5f);
        self.anchorMax = new Vector2(0, 0.5f);
        self.pivot = new Vector2(0f, 0.5f);

        for (int deg = 0; deg < 360; deg += minorEveryDegrees)
        {
            bool isMajor = (deg % majorEveryDegrees) == 0;

            // --- Tick ---
            var tickGO = (isMajor ? majorTickPrefab : minorTickPrefab)
                         ? (GameObject)PrefabUtility.InstantiatePrefab(isMajor ? majorTickPrefab : minorTickPrefab, tickParent)
                         : CreateTickGO(tickParent, isMajor ? 0.6f : 0.3f, isMajor ? 2f : 1f, 0.9f);

            var tickRT = tickGO.GetComponent<RectTransform>();
            tickRT.anchorMin = tickRT.anchorMax = new Vector2(0f, 0.5f);
            tickRT.pivot = new Vector2(0.5f, 0.5f);
            tickRT.anchoredPosition = new Vector2(deg * pxPerDeg, 0f);

            // --- Labels at 0/90/180/270 ---
            if (deg % 90 == 0)
            {
                TMP_Text lbl = labelPrefab
                    ? Instantiate(labelPrefab, tickParent)
                    : CreateLabelTMP(tickParent, labelColor, stripHeight * 0.42f);

                var rt = lbl.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(deg * pxPerDeg, stripHeight * 0.18f);

                lbl.text = (deg == 0) ? "N" : (deg == 90) ? "E" : (deg == 180) ? "S" : "W";
            }
        }
    }

    GameObject CreateTickGO(Transform parent, float heightFrac, float widthPx, float alpha)
    {
        var go = new GameObject("Tick", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(widthPx, stripHeight * heightFrac);
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, alpha);
        return go;
    }

    TMP_Text CreateLabelTMP(Transform parent, Color color, float fontSize)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.color = color;
        txt.fontSize = fontSize;
        txt.enableAutoSizing = false;
        txt.alignment = TextAlignmentOptions.Center;
        // Use project default TMP font if available
#if UNITY_EDITOR
        if (TMP_Settings.instance && TMP_Settings.defaultFontAsset)
            txt.font = TMP_Settings.defaultFontAsset;
#endif
        return txt;
    }
#endif
}


// Assets/Editor/CompassStripGeneratorEditor.cs
#if UNITY_EDITOR
//using UnityEditor;
//using UnityEngine;

[CustomEditor(typeof(CompassStripGenerator))]
public class CompassStripGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the normal inspector first
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Rebuild Strip", GUILayout.Height(30)))
        {
            // Call the generator's Rebuild() even if it's non-public
            var t = (CompassStripGenerator)target;
            var m = typeof(CompassStripGenerator).GetMethod("Rebuild",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (m != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Rebuild Compass Strip");
                m.Invoke(t, null);
                // Mark scene dirty so changes persist
                EditorUtility.SetDirty(t);
                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
            }
            else
            {
                Debug.LogError("Rebuild() method not found on CompassStripGenerator.");
            }
        }
    }
}
#endif
