using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class SkullFireball : MonoBehaviour
{
    [Header("Projectile")]
    public float speed = 18f;
    public int damage = 10;
    public float lifeTime = 6f;

    [Header("Collision")]
    [Tooltip("Layers this projectile should interact with (include Player, Ground, enemies, props).")]
    public LayerMask hitLayers;
    [Tooltip("If you have a SphereCollider, we'll use its radius; else this value.")]
    public float fallbackRadius = 0.12f;

    public GameObject hitVFX;

    Rigidbody _rb;
    float _radius;

    public AudioClip[] impactSFX;
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // Derive sweep radius from collider if possible
        _radius = fallbackRadius;
        var sphere = col as SphereCollider;
        if (sphere) _radius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
    }

    void Start()
    {

        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        Vector3 dir = transform.forward;
        float step = speed * Time.fixedDeltaTime;

        // Swept test so we can't miss thin/fast hits (ground, walls, etc.)
        if (Physics.SphereCast(_rb.position, _radius, dir, out RaycastHit hit, step, hitLayers, QueryTriggerInteraction.Ignore))
        {
            _rb.position = hit.point - dir * 0.01f;
            HandleHit(hit.collider);
            return;
        }

        // No hit -> move forward
        _rb.MovePosition(_rb.position + dir * step);
    }

    void OnTriggerEnter(Collider other)
    {
        // If physics matrix is right, this will also fire; keep as a backup path.
        if (((1 << other.gameObject.layer) & hitLayers) == 0) return; // <-- ensure Ground is IN this mask!
        HandleHit(other);
    }

    void HandleHit(Collider other)
    {
        
        // Player
        if (other.CompareTag("Player"))
        {
            var cc = other.GetComponentInChildren<CombatController>();
            if (cc != null && cc.blocking)
            {
                var cam = other.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    transform.rotation = cam.transform.rotation; // deflect, keep flying
                    return;
                }
            }
            if (cc) cc.TakeDamage(damage);
            SpawnVFXAndDestroy();
            return;
        }

        // Friendly fire on skulls
        if (other.CompareTag("Skull"))
        {
            var skull = other.GetComponent<FloatingSkullAI>();
            if (skull) skull.ApplyDamage(damage);
            SpawnVFXAndDestroy();
            return;
        }

        // Ground / walls / anything else in mask
        SpawnVFXAndDestroy();
    }

    void SpawnVFXAndDestroy()
    {
        if (hitVFX)
        {
            GameObject vfx = Instantiate(hitVFX, transform.position, Quaternion.identity);
            AudioSource vfxAudio = vfx.GetComponent<AudioSource>();
            vfxAudio.clip = impactSFX[Random.Range(0, impactSFX.Length)];
            vfxAudio.Play();
            Destroy(vfx, 3f);
        }
        Destroy(gameObject);
    }
}
