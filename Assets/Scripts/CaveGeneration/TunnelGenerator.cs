// =========================
// File: TunnelGenerator.cs
// Extrudes a (possibly oval) cross-section along the spline to build a tunnel mesh
// =========================
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TunnelGenerator : MonoBehaviour
{
    public SplinePath spline;

    [Header("Resolution")]
    [Tooltip("Approx points along the spline per meter.")]
    [Range(0.5f, 8f)] public float samplesPerMeter = 2.0f;
    [Tooltip("Number of vertices around the ring (>=3).")]
    [Range(3, 64)] public int ringSegments = 16;

    [Header("Noise / Roughness")]
    [Tooltip("World-space Perlin scale for wall noise.")] public float noiseScale = 0.4f;
    [Tooltip("Max wall displacement (m) multiplied by node roughness.")] public float noiseAmplitude = 0.6f;
    [Tooltip("Jitter ring rotation to break patterns.")] public float randomTwistPerMeter = 5f;

    [Header("Floor")]
    public bool flatFloor = true;
    [Tooltip("Meters below the tunnel center to the walkable floor plane.")]
    [Min(0f)] public float floorHeight = 1.0f;
    [Tooltip("Rounding at the wall-floor junction (meters). Set 0 for perfectly flat.")]
    [Min(0f)] public float floorFillet = 0.30f;
    [Tooltip("Widen the lower walls near the floor (1 = none).")]
    public float floorWidthScale = 1.15f;
    [Tooltip("Reduce wall noise at the floor (0 = full noise, 1 = none on the floor).")]
    [Range(0f, 1f)] public float floorNoiseDampen = 0.8f;

    [Header("UVs")]
    [Tooltip("Meters per repeat along the tunnel (V).")] public float uvV_MetersPerTile = 2f;
    [Tooltip("Meters per repeat around circumference (U).")] public float uvU_MetersPerTile = 2f;

    [Header("Outputs")]
    public bool generateCollider = true;
    public bool recalcNormals = false; // usually keep our own

    MeshFilter mf; MeshCollider mc;

    void Reset()
    {
        mf = GetComponent<MeshFilter>();
        mc = GetComponent<MeshCollider>();
        spline = GetComponentInParent<SplinePath>();
    }

    void OnEnable()
    {
        if (!mf) mf = GetComponent<MeshFilter>();
        if (!mc) mc = GetComponent<MeshCollider>();
    }

    [ContextMenu("Rebuild Now")]
    public void Rebuild()
    {
        if (!spline || spline.Count < 2) return;

        // smooth min (k ~ radius of the soft edge); k=0 => hard min
        float SmoothMin(float a, float b, float k)
        {
            if (k <= 0f) return Mathf.Min(a, b);
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        // 1) Sample spline
        var points = new List<Sample>();
        float approxLen = ApproximateLength(64);
        int samples = Mathf.Max(4, Mathf.RoundToInt(approxLen * samplesPerMeter));
        int sampleCount = spline.closed ? samples : samples + 1;  // closed: no duplicate end
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            points.Add(new Sample
            {
                t = t,
                pos = spline.GetPointWorld(t),
                tan = spline.GetTangentWorld(t),
                radius = spline.GetRadius(t),
                oval = spline.GetOval(t),
                twist = spline.GetTwist(t),
                rough = spline.GetRoughness(t)
            });
        }

        // 2) Build stable frames (tangent-forward)
        Vector3 up = Vector3.up;
        for (int i = 0; i < points.Count; i++)
        {
            var fwd = points[i].tan;

            if (i == 0)
            {
                if (Mathf.Abs(Vector3.Dot(fwd, up)) > 0.95f) up = Vector3.right;
                var right0 = Vector3.Normalize(Vector3.Cross(fwd, up));
                up = Vector3.Normalize(Vector3.Cross(right0, fwd));
                points[i].right = right0;
                points[i].up = up;
                continue;
            }

            var rot = Quaternion.FromToRotation(points[i - 1].tan, fwd);
            up = Vector3.Normalize(rot * up);
            var rightN = Vector3.Normalize(Vector3.Cross(fwd, up));
            up = Vector3.Normalize(Vector3.Cross(rightN, fwd));

            points[i].right = rightN;
            points[i].up = up;
        }

        // 3) Create rings and triangles
        int vertsPerRing = ringSegments;
        int ringCount = points.Count;
        var verts = new Vector3[ringCount * vertsPerRing];
        var norms = new Vector3[ringCount * vertsPerRing];
        var uvs = new Vector2[ringCount * vertsPerRing];
        int quadRings = spline.closed ? ringCount : ringCount - 1;
        var tris = new int[quadRings * ringSegments * 6];


        float ringAngleStep = 360f / ringSegments;
        float randTwistAccum = 0f;
        float accMeters = 0f; // distance along the path

        for (int i = 0; i < ringCount; i++)
        {
            var s = points[i];

            if (i > 0)
            {
                float stepDist = Vector3.Distance(points[i].pos, points[i - 1].pos);
                randTwistAccum += randomTwistPerMeter * stepDist;
                accMeters += stepDist; // advance V tiling
            }

            Quaternion ringRot =
                Quaternion.AngleAxis(s.twist + randTwistAccum, s.tan) *
                Quaternion.LookRotation(s.tan, s.up); // forward = tangent, up = up

            for (int r = 0; r < ringSegments; r++)
            {
                float ang = r * ringAngleStep;

                // Unit ring on XY, then apply oval scaling
                Vector2 circle = (Vector2)(Quaternion.AngleAxis(ang, Vector3.forward) * Vector3.right);
                circle = new Vector2(circle.x * s.oval.x, circle.y * s.oval.y);

                // ---- FLAT FLOOR SHAPING (normalized space) ----
                if (flatFloor)
                {
                    float floorN = -Mathf.Clamp(floorHeight / s.radius, 0f, 0.99f);
                    float filletN = Mathf.Clamp(floorFillet / s.radius, 0f, 0.49f);

                    float y;
                    if (filletN <= 0f) // hard clamp for perfectly flat
                        y = Mathf.Max(circle.y, floorN);
                    else
                        y = -SmoothMin(-circle.y, -floorN, filletN);

                    float tFlat = Mathf.InverseLerp(floorN, floorN + Mathf.Max(filletN, 0.0001f), y);
                    float widen = Mathf.Lerp(floorWidthScale, 1f, tFlat);
                    circle = new Vector2(circle.x * widen, y);
                }

                // Scale to meters
                Vector3 local = new Vector3(circle.x, circle.y, 0f) * s.radius;

                // ---- NOISE (damped near floor) ----
                if (s.rough > 0f && noiseAmplitude > 0f)
                {
                    float nx = Mathf.PerlinNoise((s.pos.x + local.x) * noiseScale, (s.pos.y + local.y) * noiseScale);
                    float ny = Mathf.PerlinNoise((s.pos.z + local.x) * noiseScale, (s.pos.y + local.y + 13.7f) * noiseScale);
                    float n = (nx * 0.6f + ny * 0.4f) * 2f - 1f;

                    Vector3 dir = local.sqrMagnitude > 1e-6f ? local.normalized : Vector3.up;

                    if (flatFloor && floorNoiseDampen > 0f)
                    {
                        float floorY = -floorHeight;
                        float band = Mathf.Max(0.0001f, floorFillet);
                        float tBand = Mathf.InverseLerp(floorY, floorY + band, local.y);
                        float killY = 1f - floorNoiseDampen * (1f - tBand); // 0 at floor when dampen=1
                        dir.y *= killY;
                        dir.Normalize();
                    }

                    local += dir * (n * noiseAmplitude * s.rough);
                }

                // Final guarantee: never dip below the floor plane
                if (flatFloor && local.y < -floorHeight)
                    local.y = -floorHeight;

                // Build vertex
                Vector3 world = s.pos + ringRot * local;
                int vi = i * vertsPerRing + r;
                verts[vi] = transform.InverseTransformPoint(world);

                // Approx normal from radial direction
                Vector3 ringNormal = (ringRot * local).normalized;
                norms[vi] = transform.InverseTransformDirection(ringNormal);

                // UVs in meters
                float angRad = (r / (float)ringSegments) * (2f * Mathf.PI);
                float ringArcMeters = angRad * s.radius * Mathf.Max(0.0001f, s.oval.x);
                float u = ringArcMeters / Mathf.Max(0.0001f, uvU_MetersPerTile);
                float v = accMeters / Mathf.Max(0.0001f, uvV_MetersPerTile);
                uvs[vi] = new Vector2(u, v);
            }
        }

        int ti = 0;
        int lastRing = spline.closed ? ringCount : ringCount - 1;
        for (int i = 0; i < lastRing; i++)
        {
            int next = (i + 1) % ringCount; // wraps to 0 when closed; unused final iter when open
            for (int r = 0; r < ringSegments; r++)
            {
                int a = i * vertsPerRing + r;
                int b = i * vertsPerRing + (r + 1) % vertsPerRing;
                int c = next * vertsPerRing + r;
                int d = next * vertsPerRing + (r + 1) % vertsPerRing;

                tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                tris[ti++] = b; tris[ti++] = c; tris[ti++] = d;
            }
        }


        // Mesh
        var mesh = mf.sharedMesh ? mf.sharedMesh : new Mesh();
        mesh.name = "TunnelMesh";
        mesh.indexFormat = (verts.Length > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.normals = norms;   // keep our normals for nice cylindrical shading
        mesh.uv = uvs;

        // Tangents for normal-mapped/triplanar materials
        mesh.tangents = null;
        mesh.RecalculateTangents(UnityEngine.Rendering.MeshUpdateFlags.Default);

        if (recalcNormals) mesh.RecalculateNormals(UnityEngine.Rendering.MeshUpdateFlags.Default);

        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;

        if (generateCollider)
        {
            if (!mc) mc = GetComponent<MeshCollider>();
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
        }
    }

    float ApproximateLength(int steps)
    {
        if (spline == null || spline.Count < 2) return 0f;
        Vector3 prev = spline.GetPointWorld(0f);
        float len = 0f;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 p = spline.GetPointWorld(t);
            len += Vector3.Distance(prev, p);
            prev = p;
        }
        return len;
    }

    class Sample
    {
        public float t; public Vector3 pos; public Vector3 tan; public Vector3 up; public Vector3 right;
        public float radius; public Vector2 oval; public float twist; public float rough;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!spline) return;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.4f);
        Gizmos.DrawLine(spline.GetPointWorld(0f), spline.GetPointWorld(1f));
    }
#endif
}
