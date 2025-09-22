// =========================
// File: SplinePath.cs
// Minimal Catmull–Rom spline with per-node cave params
// =========================
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[ExecuteAlways]
public class SplinePath : MonoBehaviour
{
    [System.Serializable]
    public class Node
    {
        public Vector3 localPosition = Vector3.zero;
        [Min(0.1f)] public float radius = 3f;          // tunnel radius at node (meters)
        public Vector2 ovalScale = Vector2.one;         // x/y scale for oval tunnels
        public float twistDegrees = 0f;                 // roll at node
        [Range(0f, 1f)] public float roughness = 0.2f;  // per-node noise weight
    }

    public List<Node> nodes = new List<Node>()
    {
        new Node{ localPosition = new Vector3(0,0,0), radius=4f },
        new Node{ localPosition = new Vector3(0,-1,8), radius=4f },
        new Node{ localPosition = new Vector3(1,-2,18), radius=5f },
        new Node{ localPosition = new Vector3(2,-2,28), radius=5f }
    };

    [Tooltip("If true, spline wraps around (closed loop). Usually false for tunnels.")]
    public bool closed = false;

    public int Count => nodes?.Count ?? 0;

    public void AddNodeAfter(int index)
    {
        if (nodes.Count == 0) { nodes.Add(new Node()); return; }
        index = Mathf.Clamp(index, 0, nodes.Count - 1);
        var a = nodes[index];
        var b = nodes[Mathf.Min(index + 1, nodes.Count - 1)];
        var mid = Vector3.Lerp(a.localPosition, b.localPosition, 0.5f);
        nodes.Insert(index + 1, new Node
        {
            localPosition = mid,
            radius = Mathf.Lerp(a.radius, b.radius, 0.5f),
            ovalScale = Vector2.Lerp(a.ovalScale, b.ovalScale, 0.5f),
            twistDegrees = Mathf.Lerp(a.twistDegrees, b.twistDegrees, 0.5f),
            roughness = Mathf.Lerp(a.roughness, b.roughness, 0.5f)
        });
    }

    public void AddNodeAtEnd()
    {
        if (nodes.Count < 2)
        {
            nodes.Add(new Node { localPosition = Vector3.zero });
            nodes.Add(new Node { localPosition = Vector3.forward * 5f });
        }
        else AddNodeAfter(nodes.Count - 2);
    }

    // Catmull–Rom segment evaluation between node i and i+1, with p0..p3 as neighbors
    // t in [0,1]
    public Vector3 GetPointWorld(float t)
    {
        GetSegment(t, out int i, out float u);
        var p0 = GetNodePos(i - 1);
        var p1 = GetNodePos(i);
        var p2 = GetNodePos(i + 1);
        var p3 = GetNodePos(i + 2);
        return transform.TransformPoint(CatmullRom(p0, p1, p2, p3, u));
    }

    public Vector3 GetTangentWorld(float t)
    {
        GetSegment(t, out int i, out float u);
        var p0 = GetNodePos(i - 1);
        var p1 = GetNodePos(i);
        var p2 = GetNodePos(i + 1);
        var p3 = GetNodePos(i + 2);
        var d = CatmullRomDerivative(p0, p1, p2, p3, u);
        return transform.TransformDirection(d).normalized;
    }
    int NextIndex(int i)
    {
        int n = Count;
        if (n == 0) return 0;
        return closed ? (i + 1) % n : Mathf.Min(i + 1, n - 1);
    }

    public float GetRadius(float t)
    {
        GetSegment(t, out int i, out float u);
        int j = NextIndex(i);
        return Mathf.Lerp(nodes[i].radius, nodes[j].radius, u);
    }

    public Vector2 GetOval(float t)
    {
        GetSegment(t, out int i, out float u);
        int j = NextIndex(i);
        return Vector2.Lerp(nodes[i].ovalScale, nodes[j].ovalScale, u);
    }

    public float GetTwist(float t)
    {
        GetSegment(t, out int i, out float u);
        int j = NextIndex(i);
        return Mathf.Lerp(nodes[i].twistDegrees, nodes[j].twistDegrees, u);
    }

    public float GetRoughness(float t)
    {
        GetSegment(t, out int i, out float u);
        int j = NextIndex(i);
        return Mathf.Lerp(nodes[i].roughness, nodes[j].roughness, u);
    }

    void GetSegment(float t, out int i, out float u)
    {
        t = Mathf.Clamp01(t);
        if (Count < 2) { i = 0; u = 0f; return; }
        int segments = closed ? Count : Count - 1;
        float ft = t * segments;
        i = Mathf.FloorToInt(ft);
        if (i >= segments) i = segments - 1;
        u = ft - i;
    }

    Vector3 GetNodePos(int idx)
    {
        int n = Count;
        if (closed)
        {
            idx = (idx % n + n) % n;
            return nodes[idx].localPosition;
        }
        // clamp ends by mirroring endpoints for Catmull–Rom continuity
        if (idx < 0) return nodes[0].localPosition + (nodes[0].localPosition - nodes[1].localPosition);
        if (idx >= n) return nodes[n - 1].localPosition + (nodes[n - 1].localPosition - nodes[n - 2].localPosition);
        return nodes[idx].localPosition;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t; float t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
    static Vector3 CatmullRomDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        return 0.5f * ((-p0 + p2) + 2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t + 3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Count < 2) return;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        const int steps = 64;
        Vector3 prev = GetPointWorld(0f);
        for (int s = 1; s <= steps; s++)
        {
            float t = s / (float)steps;
            Vector3 p = GetPointWorld(t);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
        // nodes
        for (int i = 0; i < Count; i++)
        {
            var wp = transform.TransformPoint(nodes[i].localPosition);
            Gizmos.color = Color.yellow; Gizmos.DrawSphere(wp, 0.2f);
        }
    }
#endif
}




