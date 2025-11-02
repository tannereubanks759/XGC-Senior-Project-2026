using UnityEngine;
using UnityEngine.Splines;

namespace BKPureNature
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class BK_SplinePath : MonoBehaviour
    {
        [SerializeField] public SplineContainer m_Container;
        [HideInInspector] public float m_Width = 1f;
        [HideInInspector] public float m_HeightOffset = 0f;
        [HideInInspector] public float m_BorderHeight = 0.2f;
        [HideInInspector] public int m_LengthSegments = 4;
        [HideInInspector] public int m_WidthSegments = 4;
        [HideInInspector] public Vector2 UVTiling = new Vector2(1, 1);
        [HideInInspector] public float curve = 1f;
        [HideInInspector] public Material m_Material;
        [HideInInspector] public Mesh m_Mesh;

    }
}
