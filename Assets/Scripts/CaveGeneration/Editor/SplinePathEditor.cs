// =========================
// File: Editors/SplinePathEditor.cs
// Simple handles to move nodes and buttons to add nodes/generate
// Place inside an Editor folder.
// =========================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplinePath))]
public class SplinePathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var sp = (SplinePath)target;
        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Node At End")) { Undo.RecordObject(sp, "Add Node"); sp.AddNodeAtEnd(); EditorUtility.SetDirty(sp); }
            if (GUILayout.Button("Add Node After First")) { Undo.RecordObject(sp, "Add Node"); sp.AddNodeAfter(0); EditorUtility.SetDirty(sp); }
        }
    }

    void OnSceneGUI()
    {
        var sp = (SplinePath)target;
        if (sp.nodes == null) return;
        var tr = sp.transform;
        for (int i = 0; i < sp.nodes.Count; i++)
        {
            var n = sp.nodes[i];
            Vector3 wp = tr.TransformPoint(n.localPosition);
            EditorGUI.BeginChangeCheck();
            float size = HandleUtility.GetHandleSize(wp) * 0.15f;
            Handles.color = Color.yellow;
            wp = Handles.PositionHandle(wp, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sp, "Move Node");
                n.localPosition = tr.InverseTransformPoint(wp);
                EditorUtility.SetDirty(sp);
            }

            // Small radius handle
            Vector3 right = tr.right;
            EditorGUI.BeginChangeCheck();
            Vector3 rHandle = wp + right * n.radius;
            rHandle = Handles.Slider(rHandle, right, size, Handles.CubeHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sp, "Radius");
                float newR = Vector3.Dot(rHandle - wp, right);
                n.radius = Mathf.Max(0.1f, newR);
                EditorUtility.SetDirty(sp);
            }

            Handles.Label(wp + Vector3.up * size * 2f, $"Node {i}\nR={n.radius:F2}");
        }
    }
}

#endif