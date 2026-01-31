using System.Collections.Generic;
using UnityEngine;

public class EnemyBlunderbuss : MonoBehaviour
{
    [Header("Shot")]
    [Tooltip("Where the raycasts originate + forward direction. Usually the gun barrel/aim transform.")]
    public Transform raycastOrigin;

    [Tooltip("Where the tracer lines start. Usually the muzzle tip.")]
    public Transform barrelTip;

    [Tooltip("Max ray distance.")]
    public float range = 60f;

    [Tooltip("Layers the pellets can hit.")]
    public LayerMask hitLayers = ~0;

    [Tooltip("Damage per pellet.")]
    public int damagePerPellet = 25;

    [Tooltip("How many pellets (raycasts) per shot. Keep at 4 for your design.")]
    public int pelletsPerShot = 4;

    [Header("Spread (Degrees)")]
    [Tooltip("X = Yaw (left/right), Y = Pitch (up/down). Must have length = pelletsPerShot (or it will auto-fill).")]
    public Vector2[] pelletAngles =
    {
        new Vector2(-2f, -1f),
        new Vector2( 2f, -1f),
        new Vector2(-2f,  1f),
        new Vector2( 2f,  1f),
    };

    [Tooltip("Optional extra random spread added on top of pelletAngles (degrees).")]
    public float randomAngleJitter = 0.25f;

    [Header("Tracer")]
    [Tooltip("Optional LineRenderer prefab for tracers. If null, a temporary LineRenderer is created.")]
    public LineRenderer tracerPrefab;

    [Tooltip("How long tracers stay alive.")]
    public float tracerLife = 0.05f;

    [Tooltip("Tracer width.")]
    public float tracerWidth = 0.02f;

    [Header("Ignore Self")]
    [Tooltip("If true, will ignore hits on this weapon's root (prevents self-hits).")]
    public bool ignoreSelfRoot = true;


    [Header("Bullet FX")]
    public GameObject MuzzleFlash;

    private Transform _selfRoot;

    private void Awake()
    {
        _selfRoot = transform.root;

        // Safety: if you forget to assign, use this transform
        if (!raycastOrigin) raycastOrigin = transform;
        if (!barrelTip) barrelTip = raycastOrigin;
    }

    /// <summary>
    /// Call this from the enemy gun animation event (or AI code).
    /// </summary>
    public void Fire()
    {
        if (!raycastOrigin) return;

        EnsureAnglesLength();

        // Batch hits to avoid multi-collider double application
        var damageRefHits = new Dictionary<DamageRef, int>();
        var combatControllerHits = new Dictionary<CombatController, int>();

        Vector3 origin = raycastOrigin.position;
        Vector3 tracerStart = barrelTip ? barrelTip.position : origin;

        for (int i = 0; i < pelletsPerShot; i++)
        {
            Vector2 ang = pelletAngles[i];

            if (randomAngleJitter > 0f)
            {
                ang.x += Random.Range(-randomAngleJitter, randomAngleJitter);
                ang.y += Random.Range(-randomAngleJitter, randomAngleJitter);
            }

            Quaternion rot = Quaternion.Euler(ang.y, ang.x, 0f);
            Vector3 dir = rot * raycastOrigin.forward;

            // Use RaycastAll so we can skip self-colliders reliably
            Ray ray = new Ray(origin, dir);
            RaycastHit hit;
            bool didHit = TryRaycastFirstValid(ray, out hit);

            Vector3 endPoint = didHit ? hit.point : (origin + dir * range);

            // Tracer from muzzle to hit point
            SpawnTracer(tracerStart, endPoint);

            if (!didHit) continue;

            // Choose a good "target transform"
            Transform t = hit.collider.attachedRigidbody ? hit.collider.attachedRigidbody.transform : hit.transform;

            // DamageRef: look in parents (boss/enemy style)
            var dref = t.GetComponentInParent<DamageRef>();
            if (dref)
            {
                damageRefHits.TryGetValue(dref, out int c);
                damageRefHits[dref] = c + 1;
                continue;
            }

            // CombatController: look in children (player style, per your request)
            // We search from the root of what we hit so it finds nested components.
            Transform root = t.root;
            var cc = root.GetComponentInChildren<CombatController>(true);
            if (cc)
            {
                combatControllerHits.TryGetValue(cc, out int c2);
                combatControllerHits[cc] = c2 + 1;
            }
        }

        // Apply batched damage
        foreach (var kvp in damageRefHits)
        {
            int totalDamage = kvp.Value * damagePerPellet;
            kvp.Key.TakeDamage(totalDamage);
        }

        foreach (var kvp in combatControllerHits)
        {
            int totalDamage = kvp.Value * damagePerPellet;
            kvp.Key.TakeDamage(totalDamage);
        }

        if (MuzzleFlash)
        {
            Destroy(Instantiate(MuzzleFlash, barrelTip.transform.position, barrelTip.transform.rotation), 3f);
        }
        
    }

    private void EnsureAnglesLength()
    {
        if (pelletAngles == null || pelletAngles.Length != pelletsPerShot)
        {
            // Auto-fill a simple spread if mismatch
            pelletAngles = new Vector2[pelletsPerShot];

            // For 4 pellets, use a box-ish pattern
            if (pelletsPerShot == 4)
            {
                pelletAngles[0] = new Vector2(-2f, -1f);
                pelletAngles[1] = new Vector2(2f, -1f);
                pelletAngles[2] = new Vector2(-2f, 1f);
                pelletAngles[3] = new Vector2(2f, 1f);
            }
            else
            {
                // For other counts, do a small circle-ish distribution
                for (int i = 0; i < pelletsPerShot; i++)
                {
                    float a = (i / Mathf.Max(1f, pelletsPerShot)) * Mathf.PI * 2f;
                    pelletAngles[i] = new Vector2(Mathf.Cos(a) * 2f, Mathf.Sin(a) * 2f);
                }
            }
        }
    }

    private bool TryRaycastFirstValid(Ray ray, out RaycastHit validHit)
    {
        validHit = default;

        RaycastHit[] hits = Physics.RaycastAll(ray, range, hitLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return false;

        // Sort by distance so we pick the first valid
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h.collider) continue;

            if (ignoreSelfRoot && _selfRoot && h.collider.transform.IsChildOf(_selfRoot))
                continue;

            validHit = h;
            return true;
        }

        return false;
    }

    private void SpawnTracer(Vector3 start, Vector3 end)
    {
        LineRenderer lr;

        if (tracerPrefab)
        {
            lr = Instantiate(tracerPrefab);
        }
        else
        {
            GameObject go = new GameObject("EnemyShotTracer");
            lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startWidth = tracerWidth;
            lr.endWidth = tracerWidth;
            lr.positionCount = 2;
        }

        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        Destroy(lr.gameObject, tracerLife);
    }
}
