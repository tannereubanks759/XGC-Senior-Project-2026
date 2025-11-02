using UnityEditor;
using UnityEngine.Splines;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace BKPureNature
{
    [CustomEditor(typeof(BK_SplinePath))]
    public class BK_SplinePathEditor : Editor
    {
        private const string MaterialAssetName = "Lava_stream";

        BK_SplinePathEditor editor;
        BK_SplinePath editorTarget;

        void OnEnable()
        {
            editor = this;
            editorTarget = (BK_SplinePath)target;

            Setup();

            Spline.Changed += OnSplineChanged;

            InitializeMesh();
            Rebuild();
        }


        void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
        }

        void Setup()
        {
            if (editorTarget.m_Container == null || editorTarget.m_Container.Spline == null)
            {
                editorTarget.m_Container = editorTarget.GetComponent<SplineContainer>();

                if(editorTarget.m_Container == null)
                {
                    editorTarget.m_Container = editorTarget.GetComponentInParent<SplineContainer>();
                }
            }

            editorTarget.m_Mesh = new Mesh();
            editorTarget.m_Mesh.name = "BK_SplinePathMesh_" + Guid.NewGuid();
            editorTarget.GetComponent<MeshFilter>().sharedMesh = editorTarget.m_Mesh;

            if( editorTarget.m_Material == null)
            {
                editorTarget.m_Material = FindMaterialAsset(MaterialAssetName);
            }
        }

        void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
        {
            if (editorTarget.m_Container == null || editorTarget.m_Container.Spline == null || editorTarget.m_Container.Spline != spline || spline.Count < 2)
                return;

            Rebuild();
        }


        public override void OnInspectorGUI()
        {

            BK_SplinePath splinePath = (BK_SplinePath)target;

            EditorGUI.BeginChangeCheck();

            splinePath.m_Container = (SplineContainer)EditorGUILayout.ObjectField("Spline Container", splinePath.m_Container, typeof(SplineContainer), true);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.LabelField("Texture", EditorStyles.boldLabel);
            Vector2 uvTiling = EditorGUILayout.Vector2Field("Tiling", splinePath.UVTiling);
            uvTiling.x = Mathf.Max(uvTiling.x, 0.01f);
            uvTiling.y = Mathf.Max(uvTiling.y, 0.01f);
            splinePath.UVTiling = uvTiling;

            splinePath.m_Material = (Material)EditorGUILayout.ObjectField("Material", splinePath.m_Material, typeof(Material), true);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);

            float width = EditorGUILayout.Slider("Width", splinePath.m_Width, 0.1f, 100f);
            float heightOffset = EditorGUILayout.Slider("Height Offset", splinePath.m_HeightOffset, -10f, 10f);
            float borderHeight = EditorGUILayout.Slider("Border height", splinePath.m_BorderHeight, 0f, 40f);
            float curve = EditorGUILayout.Slider("Curve strength", splinePath.curve, 0f, 1f);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.LabelField("Segments", EditorStyles.boldLabel);
            int lengthSegments = EditorGUILayout.IntSlider("Length", splinePath.m_LengthSegments, 1, 7);
            int widthSegments = EditorGUILayout.IntSlider("Width", splinePath.m_WidthSegments, 1, 7);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(splinePath, "Spline Path Changed");

                splinePath.m_Width = width;
                splinePath.m_HeightOffset = heightOffset;
                splinePath.m_BorderHeight = borderHeight;
                splinePath.curve = curve;
                splinePath.m_LengthSegments = lengthSegments;
                splinePath.m_WidthSegments = widthSegments;

                EditorUtility.SetDirty(splinePath);
                Rebuild();
            }
        }

        public void Rebuild()
        {
            if (editorTarget.m_Container == null || editorTarget.m_Container.Spline == null || editorTarget.m_Mesh == null)
                return;

            var spline = editorTarget.m_Container.Spline;
            float length = spline.GetLength();
            int steps = Mathf.CeilToInt((length / 8f) * editorTarget.m_LengthSegments);

            Undo.RegisterCompleteObjectUndo(editorTarget.m_Mesh, "Update Lava Mesh");

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            List<Color> colors = new List<Color>();

            float USize = editorTarget.UVTiling.x;
            float VSize = editorTarget.UVTiling.y * length;

            // convert from spline container matrix to bk spline path matrix
            Matrix4x4 matrix = editorTarget.transform.worldToLocalMatrix * editorTarget.m_Container.transform.localToWorldMatrix;

            for (int j = 0; j < editorTarget.m_WidthSegments; j++)
            {
                float u = (float)j / editorTarget.m_WidthSegments;
                float widthStart = Mathf.Lerp(0, editorTarget.m_Width, u);
                float widthEnd = Mathf.Lerp(0, editorTarget.m_Width, u + 1f / editorTarget.m_WidthSegments);
                float UVStart = widthStart / editorTarget.m_Width;
                float UVEnd = widthEnd / editorTarget.m_Width;

                for (int i = 0; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    Vector3 pos = (Vector3)spline.EvaluatePosition(t);
                    Vector3 dir = ((Vector3)spline.EvaluateTangent(t)).normalized;
                    Vector3 normal = Vector3.Cross(dir, Vector3.down).normalized;

                    Vector3 offsetStart = Vector3.up * (-editorTarget.m_BorderHeight) * Mathf.Lerp(1 - editorTarget.curve * 2, 1, Mathf.Abs(UVStart - 0.5f) * 2);
                    Vector3 offsetEnd = Vector3.up * (-editorTarget.m_BorderHeight) * Mathf.Lerp(1 - editorTarget.curve * 2, 1, Mathf.Abs(UVEnd - 0.5f) * 2);

                    Vector3 left = pos + offsetStart * Mathf.Abs(UVStart - 0.5f) * 2 + normal * widthStart - normal * editorTarget.m_Width / 2;
                    Vector3 right = pos + offsetEnd * Mathf.Abs(UVEnd - 0.5f) * 2 + normal * widthEnd - normal * editorTarget.m_Width / 2;

                    left += new Vector3(0f, 1f, 0f) * editorTarget.m_HeightOffset;
                    right += new Vector3(0f, 1f, 0f) * editorTarget.m_HeightOffset;

                    left = matrix.MultiplyPoint(left);
                    right = matrix.MultiplyPoint(right);

                    verts.Add(left);
                    verts.Add(right);

                    colors.Add(Color.black);
                    colors.Add(Color.black);

                    float v = t * VSize;
                    uvs.Add(new Vector2(UVStart * USize * editorTarget.m_Width, v));
                    uvs.Add(new Vector2(UVEnd * USize * editorTarget.m_Width, v));
                }
            }

            for (int i = 0; i < verts.Count / 2 - 1; i++)
            {
                int idx = i * 2;
                if (i % ((verts.Count / 2) / editorTarget.m_WidthSegments) == ((verts.Count / 2) / editorTarget.m_WidthSegments) - 1)
                    continue;

                tris.Add(idx);
                tris.Add(idx + 2);
                tris.Add(idx + 1);

                tris.Add(idx + 1);
                tris.Add(idx + 2);
                tris.Add(idx + 3);
            }

            editorTarget.m_Mesh.Clear();
            editorTarget.m_Mesh.vertices = verts.ToArray();
            editorTarget.m_Mesh.triangles = tris.ToArray();
            editorTarget.m_Mesh.colors = colors.ToArray();
            editorTarget.m_Mesh.uv = uvs.ToArray();
            editorTarget.m_Mesh.RecalculateNormals();
            editorTarget.m_Mesh.RecalculateBounds();

            if (editorTarget.m_Material != null)
                editorTarget.GetComponent<MeshRenderer>().sharedMaterial = editorTarget.m_Material;

            MeshCollider meshCollider = editorTarget.GetComponent<MeshCollider>();
            if( meshCollider != null)
            {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = editorTarget.m_Mesh;
            }
        }

        void InitializeMesh()
        {
            if (editorTarget.m_Container == null)
                editorTarget.m_Container = editorTarget.GetComponent<SplineContainer>();

            var filter = editorTarget.GetComponent<MeshFilter>();
            if (filter == null)
                return;

            if (filter.sharedMesh == null || !filter.sharedMesh.isReadable)
            {
                editorTarget.m_Mesh = new Mesh();
                editorTarget.m_Mesh.name = "BK_SplinePathMesh_" + GetInstanceID();
                filter.sharedMesh = editorTarget.m_Mesh;
            }
            else
            {
                editorTarget.m_Mesh = filter.sharedMesh;
            }
        }

        public static Material FindMaterialAsset(string materialName)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Material {materialName}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.name == materialName)
                {
                    return mat;
                }
            }
            return null;
        }
    }
}
