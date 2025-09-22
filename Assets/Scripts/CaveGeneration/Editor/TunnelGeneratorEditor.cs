/*#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(TunnelGenerator))]
public class TunnelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var gen = (TunnelGenerator)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Rebuild Tunnel"))
        {
            gen.Rebuild();
        }
        EditorGUILayout.HelpBox("To edit: Select SplinePath and move nodes in Scene view. Set per-node radius/oval/twist/roughness. Then click 'Rebuild Tunnel'. Use a triplanar rock material for best look.", MessageType.Info);
    }
}
#endif*/