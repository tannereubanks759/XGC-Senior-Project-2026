using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[AddComponentMenu("Level/Fence Spline Placer")]
public class FenceSplinePlacer : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Fence segment prefab that will be spaced along the spline.")]
    public GameObject fencePrefab;

    [Tooltip("Optional: a post prefab to place at every spline point (and at the end if open).")]
    public GameObject postPrefab;

    [Header("Placement")]
    [Tooltip("Distance between fence segments along the spline.")]
    [Min(0.01f)] public float spacing = 2f;

    [Tooltip("If enabled, the path will connect last point back to first.")]
    public bool closedLoop = false;

    [Tooltip("Smooth the curve using Catmull-Rom. If off, uses straight polyline segments.")]
    public bool smooth = true;

    [Tooltip("How finely to sample for length when smooth is ON (lower = faster, higher = smoother).")]
    [Range(0.01f, 1f)] public float smoothStep = 0.1f;

    [Header("Alignment")]
    [Tooltip("If true, we will raycast downward to snap to ground and use its normal for up alignment.")]
    public bool projectToGround = true;

    [Tooltip("LayerMask for ground projection (Terrain, Ground, etc.).")]
    public LayerMask groundMask = ~0;

    [Tooltip("Extra vertical offset after ground projection (e.g., raise posts slightly).")]
    public float heightOffset = 0f;

    [Tooltip("Random yaw jitter for segments (degrees). 0 = none.")]
    [Range(0f, 10f)] public float randomYaw = 0f;

    [Header("Scene & Output")]
    [Tooltip("Optional parent for spawned fence instances. If null, a _FENCE_INSTANCES child is created.")]
    public Transform instancesParent;

    [Tooltip("Draw gizmo line for the spline.")]
    public bool drawGizmos = true;

    [Tooltip("Live preview (auto-build in editor when you move points/values). Disable if heavy scenes.")]
    public bool livePreview = true;

    [Header("Orientation")]
    [Tooltip("Extra Y-axis rotation (degrees) applied to each fence piece after it faces along the spline.")]
    public float yRotationOffset = 0f;

    [Tooltip("Shift the very first placed piece forward along the path (0..spacing).")]
    [Min(0f)] public float startOffset = 0f;
    [Tooltip("If true, also place a fence piece at the very end of the path (may crowd the last gap).")]
    public bool includeEndPoint = false;

    public enum RotationLockMode { Full3D, LockPitchOnly, YawOnly }

    [Header("Rotation")]
    [Tooltip("How to constrain orientation of each fence piece.")]
    public RotationLockMode rotationLock = RotationLockMode.LockPitchOnly;

    [Tooltip("If true, 'up' is world-up. Turn OFF to allow banking (roll) from ground normals.")]
    public bool useWorldUpForRotation = false;

    [Tooltip("Hard lock: forces Transform X rotation to 0° on all placed pieces (Inspector-visible).")]
    public bool forceXZero = true;

    // keep your existing yRotationOffset and randomYaw fields



    [System.Serializable]
    public struct SplinePoint
    {
        public Vector3 localPos;
    }

    [SerializeField] public List<SplinePoint> points = new List<SplinePoint>();

    const string kContainerName = "_FENCE_INSTANCES";

    Transform EnsureParent()
    {
        if (instancesParent == null)
        {
            var child = transform.Find(kContainerName);
            if (child == null)
            {
                var go = new GameObject(kContainerName);
                go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                go.transform.SetParent(transform, false);
                instancesParent = go.transform;
            }
            else
            {
                instancesParent = child;
            }
        }
        return instancesParent;
    }

    public void Clear()
    {
        var p = EnsureParent();
        // Destroy all children
        // Use immediate in edit mode to avoid scene junk
        for (int i = p.childCount - 1; i >= 0; i--)
        {
            var c = p.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(c.gameObject);
            else Destroy(c.gameObject);
#else
            DestroyImmediate(c.gameObject);
#endif
        }
    }

    public void Build()
    {
        if (fencePrefab == null || points == null || points.Count < 2) return;

        Clear();
        var parent = EnsureParent();

        // 1) Evenly spaced samples along the whole path (no scaling, fixed spacing)
        var samples = SampleEvenly(spacing, startOffset, includeEndPoint);

        // 2) Drop one prefab at EACH sample, oriented by forward tangent at that sample
        var rand = new System.Random(12345);
        for (int i = 0; i < samples.Count; i++)
        {
            var here = samples[i].pos;

            // robust forward using neighbors
            int count = samples.Count;
            int prevIdx = (i == 0 ? (closedLoop ? count - 1 : i) : i - 1);
            int nextIdx = (i == count - 1 ? (closedLoop ? 0 : i) : i + 1);
            Vector3 prevPos = samples[prevIdx].pos;
            Vector3 nextPos = samples[nextIdx].pos;

            Vector3 fwd = (nextPos - prevPos);
            if (fwd.sqrMagnitude < 1e-8f)
            {
                if (i < count - 1) fwd = (samples[i + 1].pos - here);
                else if (i > 0) fwd = (here - samples[i - 1].pos);
            }
            if (fwd.sqrMagnitude < 1e-8f) fwd = Vector3.forward;
            fwd.Normalize();

            Vector3 up = useWorldUpForRotation ? Vector3.up : samples[i].up;
            Quaternion rot;

            switch (rotationLock)
            {
                case RotationLockMode.YawOnly:
                    {
                        // zero pitch & roll (upright); yaw around world up
                        Vector3 flatFwd = Vector3.ProjectOnPlane(fwd, Vector3.up);
                        if (flatFwd.sqrMagnitude < 1e-8f) flatFwd = Vector3.forward;
                        float yaw = Mathf.Atan2(flatFwd.x, flatFwd.z) * Mathf.Rad2Deg;
                        yaw += yRotationOffset;
                        if (randomYaw > 0f) yaw += Mathf.Lerp(-randomYaw, randomYaw, (float)rand.NextDouble());
                        rot = Quaternion.Euler(0f, yaw, 0f);
                        break;
                    }
                case RotationLockMode.LockPitchOnly:
                    {
                        // no pitch, but keep roll (bank with ground)
                        // project forward onto plane perpendicular to 'up'
                        Vector3 flatFwd = Vector3.ProjectOnPlane(fwd, up);
                        if (flatFwd.sqrMagnitude < 1e-8f) flatFwd = Vector3.Cross(Vector3.right, up); // fallback
                        flatFwd.Normalize();

                        rot = Quaternion.LookRotation(flatFwd, up);

                        // yaw offset around 'up' (doesn't add pitch); random yaw too
                        if (Mathf.Abs(yRotationOffset) > 0.0001f) rot = Quaternion.AngleAxis(yRotationOffset, up) * rot;
                        if (randomYaw > 0f) rot = Quaternion.AngleAxis(Mathf.Lerp(-randomYaw, randomYaw, (float)rand.NextDouble()), up) * rot;
                        break;
                    }
                default: // Full3D
                    {
                        // full orientation, orthonormalized for stability
                        Vector3 right = Vector3.Cross(up, fwd);
                        if (right.sqrMagnitude < 1e-8f) { up = Vector3.up; right = Vector3.Cross(up, fwd); }
                        right.Normalize();
                        fwd = Vector3.Cross(right, up).normalized;

                        rot = Quaternion.LookRotation(fwd, up);

                        if (Mathf.Abs(yRotationOffset) > 0.0001f) rot = Quaternion.AngleAxis(yRotationOffset, up) * rot;
                        if (randomYaw > 0f) rot = Quaternion.AngleAxis(Mathf.Lerp(-randomYaw, randomYaw, (float)rand.NextDouble()), up) * rot;
                        break;
                    }
            }
            if (forceXZero)
            {
                // Enforce X=0° in Inspector by rebuilding from yaw (Y) + roll (Z) only.
                Vector3 e = rot.eulerAngles;
                rot = Quaternion.Euler(0f, e.y, e.z);
            }

            InstantiatePrefabSafe(fencePrefab, parent, here, rot);



        }

        // (Optional) Posts exactly at control points (unchanged)
        if (postPrefab != null && points.Count >= 1)
        {
            int limit = closedLoop ? points.Count : points.Count;
            for (int i = 0; i < limit; i++)
            {
                Vector3 w = transform.TransformPoint(points[i].localPos);
                Vector3 n = Vector3.up;
                if (projectToGround && TryGround(ref w, ref n)) w += n * heightOffset;
                else w += Vector3.up * heightOffset;

                InstantiatePrefabSafe(postPrefab, parent, w, Quaternion.identity);
            }
        }
    }


    struct Sample
    {
        public Vector3 pos;
        public Vector3 up;
    }


    List<Sample> SampleEvenly(float targetSpacing, float startOffset, bool includeEnd)
    {
        var result = new List<Sample>();
        if (points == null || points.Count < 2) return result;

        // Build world-space control points
        List<Vector3> wp = new List<Vector3>(points.Count);
        foreach (var sp in points) wp.Add(transform.TransformPoint(sp.localPos));

        int segCount = closedLoop ? wp.Count : wp.Count - 1;

        // Helper to add sample at a world pos with projection
        void AddAt(Vector3 worldPos)
        {
            Vector3 up = Vector3.up;
            if (projectToGround && TryGround(ref worldPos, ref up))
                worldPos += up * heightOffset;
            else
                worldPos += Vector3.up * heightOffset;

            result.Add(new Sample { pos = worldPos, up = up });
        }

        // Start at offset (0..spacing)
        float carry = Mathf.Repeat(startOffset, Mathf.Max(0.0001f, targetSpacing));
        if (carry > 1e-5f) carry = targetSpacing - carry; // we will step 'carry' first

        // Walk every path segment and emit evenly spaced points
        for (int i = 0; i < segCount; i++)
        {
            Vector3 a = wp[i];
            Vector3 b = wp[(i + 1) % wp.Count];

            if (!smooth)
            {
                float segLen = Vector3.Distance(a, b);
                if (segLen < 1e-6f) continue;

                Vector3 dir = (b - a) / segLen;

                // First step uses 'carry' (distance from current segment start)
                float t = carry;
                while (t <= segLen + 1e-6f)
                {
                    AddAt(a + dir * t);
                    t += targetSpacing;
                }
                // Compute leftover for next segment
                carry = t - segLen;
                if (carry < 0f) carry = 0f;
            }
            else
            {
                // Catmull-Rom fine sampling for length-accurate stepping
                Vector3 pm1 = wp[(i - 1 + wp.Count) % wp.Count];
                Vector3 p0 = a;
                Vector3 p1 = b;
                Vector3 p2 = wp[(i + 2) % wp.Count];

                if (!closedLoop)
                {
                    if (i == 0) pm1 = p0;
                    if (i == wp.Count - 2) p2 = p1;
                }

                // Pre-sample the curve into small straight bits
                const float step = 0.05f; // adjust for quality/perf
                Vector3 last = p0;
                float walked = 0f;

                // We'll place first sample at 'carry' into this segment
                float nextMark = carry;

                for (float t = step; t <= 1f + 1e-4f; t += step)
                {
                    Vector3 c = CatmullRom(pm1, p0, p1, p2, Mathf.Clamp01(t));
                    float d = Vector3.Distance(last, c);
                    if (d > 1e-7f)
                    {
                        while (walked + d >= nextMark - 1e-7f)
                        {
                            float over = nextMark - walked;
                            float lerp = Mathf.Clamp01(over / d);
                            Vector3 pos = Vector3.Lerp(last, c, lerp);
                            AddAt(pos);
                            nextMark += targetSpacing;
                        }
                        walked += d;
                    }
                    last = c;
                }

                // Remainder that we need to advance on the next segment
                carry = nextMark - walked;
                if (carry < 0f) carry = 0f;
                // If carry is almost one full spacing, wrap down
                if (carry > targetSpacing - 1e-5f) carry -= targetSpacing;
            }
        }

        // When adding the final end sample in SampleEvenly(...)
        if (!closedLoop && includeEnd && wp.Count >= 2)
        {
            Vector3 end = wp[wp.Count - 1];
            Vector3 up = Vector3.up;
            if (projectToGround && TryGround(ref end, ref up)) end += up * heightOffset;
            else end += Vector3.up * heightOffset;

            // Only add if it's not nearly identical to the last emitted sample
            if (result.Count == 0 || (result[^1].pos - end).sqrMagnitude > (0.05f * 0.05f))
                result.Add(new Sample { pos = end, up = useWorldUpForRotation ? Vector3.up : up });
        }


        return result;
    }


    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // Standard Catmull-Rom (centripetal type would need alpha). This is the "uniform" form.
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    bool TryGround(ref Vector3 pos, ref Vector3 up)
    {
        // Raycast from above to catch uneven ground
        Vector3 origin = pos + Vector3.up * 100f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, 1000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            pos = hit.point;
            up = hit.normal.sqrMagnitude > 1e-6f ? hit.normal.normalized : Vector3.up;
            return true;
        }
        return false;
    }

    GameObject InstantiatePrefabSafe(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(pos, rot);
            Undo.RegisterCreatedObjectUndo(go, "Create Fence Piece");
            return go;
        }
#endif
        return Instantiate(prefab, pos, rot, parent);
    }

    void FitToDistanceIfPossible(GameObject instance, float distance)
    {
        // Optional quality-of-life: if the fence piece has a MeshRenderer,
        // attempt to scale its local Z to match the gap (use at your discretion).
        // Comment out if your prefab already matches "spacing".
        var mr = instance.GetComponentInChildren<MeshRenderer>();
        if (mr == null) return;

        // Assume fence forward = +Z (typical). If not, rotate prefab or adjust here.
        Vector3 s = instance.transform.localScale;
        if (distance > 0.01f)
        {
            // You can keep this 1:1 or provide a multiplier if your model is 1 unit long etc.
            s.z = distance;
            instance.transform.localScale = s;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos || points.Count < 2) return;

        Gizmos.color = Color.cyan;
        var worldPts = new List<Vector3>(points.Count);
        foreach (var sp in points)
            worldPts.Add(transform.TransformPoint(sp.localPos));

        if (!smooth)
        {
            for (int i = 0; i < worldPts.Count - 1; i++)
                Gizmos.DrawLine(worldPts[i], worldPts[i + 1]);
            if (closedLoop) Gizmos.DrawLine(worldPts[^1], worldPts[0]);
        }
        else
        {
            int segCount = closedLoop ? worldPts.Count : worldPts.Count - 1;
            for (int i = 0; i < segCount; i++)
            {
                Vector3 pm1 = worldPts[(i - 1 + worldPts.Count) % worldPts.Count];
                Vector3 p0 = worldPts[i];
                Vector3 p1 = worldPts[(i + 1) % worldPts.Count];
                Vector3 p2 = worldPts[(i + 2) % worldPts.Count];

                if (!closedLoop)
                {
                    if (i == 0) pm1 = p0;
                    if (i == worldPts.Count - 2) p2 = p1;
                }

                Vector3 last = p0;
                for (float t = 0.05f; t <= 1f; t += 0.05f)
                {
                    Vector3 c = CatmullRom(pm1, p0, p1, p2, t);
                    Gizmos.DrawLine(last, c);
                    last = c;
                }
            }
        }
    }

    // In editor—auto rebuild on changes for quick iteration
    void Update()
    {
        if (!Application.isPlaying && livePreview && fencePrefab != null)
        {
            // Keep it light: only rebuild if scene is focused & this component is selected?
            // For simplicity, rebuild when values change. A brute-force but effective approach.
            // You can optimize later by hashing inputs.
            Build();
        }
    }
#endif
}
