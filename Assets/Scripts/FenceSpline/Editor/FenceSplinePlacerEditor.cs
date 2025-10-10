#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FenceSplinePlacer))]
public class FenceSplinePlacerEditor : Editor
{
    FenceSplinePlacer S;

    void OnEnable()
    {
        S = (FenceSplinePlacer)target;
        if (S.points == null)
            S.points = new System.Collections.Generic.List<FenceSplinePlacer.SplinePoint>();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // --- Prefabs ---
        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fencePrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("postPrefab"));

        EditorGUILayout.Space(6);

        // --- Placement ---
        EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spacing"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("includeEndPoint"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("closedLoop"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("smooth"));
        using (new EditorGUI.DisabledScope(!serializedObject.FindProperty("smooth").boolValue))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothStep"));
        }

        EditorGUILayout.Space(6);

        // --- Alignment ---
        EditorGUILayout.LabelField("Alignment", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("projectToGround"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("groundMask"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("heightOffset"));

        EditorGUILayout.Space(6);

        // --- Rotation / Orientation ---
        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationLock"));           // Full3D / LockPitchOnly / YawOnly
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useWorldUpForRotation")); // world-up vs terrain normal
        EditorGUILayout.PropertyField(serializedObject.FindProperty("yRotationOffset"));       // extra yaw around up
        EditorGUILayout.PropertyField(serializedObject.FindProperty("randomYaw"));             // small jitter
        EditorGUILayout.PropertyField(serializedObject.FindProperty("forceXZero"));            // hard lock X=0�

        EditorGUILayout.Space(6);

        // --- Scene & Output ---
        EditorGUILayout.LabelField("Scene & Output", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("instancesParent"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("drawGizmos"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("livePreview"));

        EditorGUILayout.Space(8);

        // --- Spline Points list ---
        EditorGUILayout.LabelField("Spline Points", EditorStyles.boldLabel);
        var pointsProp = serializedObject.FindProperty("points");
        for (int i = 0; i < pointsProp.arraySize; i++)
        {
            var p = pointsProp.GetArrayElementAtIndex(i).FindPropertyRelative("localPos");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(p, new GUIContent("Point " + i));
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                Undo.RecordObject(S, "Delete Spline Point");
                pointsProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
                return;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Point"))
        {
            Undo.RecordObject(S, "Add Spline Point");
            Vector3 addPos = S.transform.position + S.transform.forward * 2f;
            if (S.points.Count > 0)
                addPos = S.transform.TransformPoint(S.points[^1].localPos) + (S.transform.forward * 2f);
            S.points.Add(new FenceSplinePlacer.SplinePoint { localPos = S.transform.InverseTransformPoint(addPos) });
        }
        if (GUILayout.Button("Clear Points"))
        {
            if (EditorUtility.DisplayDialog("Clear Points?", "Remove all spline points?", "Yes", "No"))
            {
                Undo.RecordObject(S, "Clear Spline Points");
                S.points.Clear();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // --- Build/Clear buttons ---
        GUI.enabled = S.fencePrefab != null && S.points.Count >= 2;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Build Fences"))
            S.Build();
        if (GUILayout.Button("Clear Fences"))
            S.Clear();
        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;

        serializedObject.ApplyModifiedProperties();
    }

    // Optional: keep your scene handle workflow (Shift+Click to add points)
    public void OnSceneGUI()
    {
        if (S.points == null) return;

        // Draw point handles
        for (int i = 0; i < S.points.Count; i++)
        {
            Vector3 w = S.transform.TransformPoint(S.points[i].localPos);
            float size = HandleUtility.GetHandleSize(w) * 0.08f;

            EditorGUI.BeginChangeCheck();
            Handles.color = Color.yellow;
            var fmh_134_54_638956406243007019 = Quaternion.identity; Vector3 newW = Handles.FreeMoveHandle(w, size, Vector3.zero, Handles.SphereHandleCap);
            Handles.Label(newW + Vector3.up * (size * 2f), $"P{i}");
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(S, "Move Spline Point");
                S.points[i] = new FenceSplinePlacer.SplinePoint { localPos = S.transform.InverseTransformPoint(newW) };
                if (S.livePreview && S.fencePrefab != null) S.Build();
            }
        }

        // Draw lines
        Handles.color = new Color(0f, 1f, 1f, 0.8f);
        if (S.points.Count >= 2)
        {
            for (int i = 0; i < S.points.Count - 1; i++)
            {
                Vector3 a = S.transform.TransformPoint(S.points[i].localPos);
                Vector3 b = S.transform.TransformPoint(S.points[i + 1].localPos);
                Handles.DrawLine(a, b, 2f);
            }
            if (S.closedLoop && S.points.Count >= 3)
            {
                Vector3 a = S.transform.TransformPoint(S.points[^1].localPos);
                Vector3 b = S.transform.TransformPoint(S.points[0].localPos);
                Handles.DrawLine(a, b, 2f);
            }
        }

        // Shift+Click to add a point at mouse raycast
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Event evt = Event.current;
        if (evt.shift && evt.type == EventType.MouseDown && evt.button == 0)
        {
            Ray r = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            Vector3 pos;
            if (Physics.Raycast(r, out var hit, 5000f, S.groundMask, QueryTriggerInteraction.Ignore))
                pos = hit.point;
            else
            {
                var plane = new Plane(Vector3.up, S.transform.position);
                if (!plane.Raycast(r, out float t)) return;
                pos = r.GetPoint(t);
            }

            Undo.RecordObject(S, "Add Spline Point");
            S.points.Add(new FenceSplinePlacer.SplinePoint { localPos = S.transform.InverseTransformPoint(pos) });
            if (S.livePreview && S.fencePrefab != null) S.Build();
            evt.Use();
        }
    }
}
#endif
