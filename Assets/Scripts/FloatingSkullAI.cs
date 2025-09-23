using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FloatingSkullAI : MonoBehaviour
{
    public enum SkullState { Patrol, Chase, Attack, Divebomb, Dead }

    [Header("Refs")]
    public Transform player;                       // If null, will try to FindWithTag("Player") on Start
    public Transform firePoint;                    // Where fireballs spawn (front of the skull)
    public GameObject fireballPrefab;              // Your fireball prefab

    [Header("Ranges")]
    public float patrolRadius = 12f;               // Max wander distance from spawn
    public float detectRange = 18f;                // Begin chasing if within this range AND in LOS
    public float attackRange = 10f;                // Stop and shoot when within this range AND in LOS
    public float loseAggroRange = 24f;             // If player gets beyond this, return to patrol (unless divebombing)

    [Header("Speeds")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 6.5f;
    public float divebombSpeed = 13f;
    public float turnSpeed = 10f;                  // How fast we rotate to face movement / player

    [Header("Altitude")]
    public float maxHeightAboveGround = 6f;        // Clamp skull to stay <= this above ground
    public float desiredHoverHeight = 3.5f;        // Typical cruising altitude above ground
    public LayerMask groundMask = ~0;              // Which layers count as "ground" for altitude raycast

    [Header("Line of Sight")]
    public LayerMask obstacleMask;                 // Obstacles that block vision (e.g., Default + Environment)
    public float eyeOffset = 0.5f;                 // Raise ray origin slightly from center

    [Header("Attack")]
    public float fireRate = 0.8f;                  // Shots per second while in Attack
    public float fireballSpeed = 18f;              // Passed into projectile script
    public float fireSpreadDegrees = 1.5f;         // Tiny inaccuracy for flavour

    [Header("Health / Divebomb")]
    public int maxHealth = 60;
    public int currentHealth = 60;
    [Tooltip("When health <= this value, the skull commits to a divebomb on the player's current position.")]
    public int divebombHealthThreshold = 15;
    public float impactExplodeRadius = 4.25f;
    public int impactDamage = 35;                  // Damage dealt on divebomb explosion
    public LayerMask damageTargets;                // Who should be damaged by the explosion (e.g., Player layer)
    public LayerMask fireballDamageTargets;

    [Header("FX (optional)")]
    public GameObject explodeVFX;                  // Spawned on explode
    public AudioSource sfxSource;
    public AudioClip fireSFX;
    public AudioClip explodeSFX;

    [Header("Attack Positioning")]
    [Tooltip("Where the skull tries to hover while firing.")]
    public float preferredAttackDistance = 8f;

    [Tooltip("How much wiggle room around the preferred distance.")]
    public float attackDistanceTolerance = 1.0f;     // hover band: [pref - tol, pref + tol]

    [Tooltip("Never get closer than this while attacking.")]
    public float keepOutRadius = 3f;

    [Tooltip("Optional strafing while holding distance.")]
    public bool enableAttackStrafe = true;
    public float attackStrafeSpeed = 2.5f;
    public Vector2 strafeDirFlipInterval = new Vector2(1.8f, 3.2f); // randomize cadence a bit

    [Header("Facing / Banking")]
    public Transform modelTransform;          // Optional: rotate only the visible model
    public bool facePitchInChase = true;
    public bool facePitchInAttack = true;
    public bool bankRollInChase = true;
    public bool bankRollInAttack = true;
    [Range(0f, 45f)] public float bankAmount = 12f;     // degrees of roll when strafing/moving
    public float bankSmoothing = 12f;                   // higher = snappier

    [Header("Divebomb")]
    public float divebombFuse = 2.5f;   // seconds until auto-explode after dive starts
    float _divebombTimer;
                      // for SmoothDamp on Y
    [Range(0.03f, 0.35f)]
    public float hoverSmoothTime = 0.12f;

    
    [Range(0f, 1f)] public float bankVelLerp = 0.2f;   // 0.2–0.35 feels good


    [Header("Debug")]
    public SkullState state = SkullState.Patrol;
    public bool drawGizmos = true;

    // --- Private ---
    Vector3 _spawnPos;
    Vector3 _patrolTarget;
    float _fireCooldown;
    Vector3 _diveLockedTarget;     // The position we lock onto at dive start
    bool _hasDiveTarget;
    // --- Private (add with other privates) ---
    float _strafeTimer;
    int _strafeDir = 1;  // +1 or -1 (left/right)
    Vector3 _prevPos;
    Vector3 _vel;
    float _yVel;
    Vector3 _velSmooth;
    Collider _col;

    void Reset()
    {
        // Make the skull a trigger so it can "ghost" through while flying and still detect divebomb impact.
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void Start()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;

        _spawnPos = transform.position;
        PickNewPatrolPoint();

        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        if (state == SkullState.Dead) return;

        // 1) DO NOT clamp at the start; let the state move first
        switch (state)
        {
            case SkullState.Patrol: PatrolTick(); LookForPlayer(); break;
            case SkullState.Chase: ChaseTick(); break;
            case SkullState.Attack: AttackTick(); break;
            case SkullState.Divebomb: DivebombTick(); break;
        }

        // 2) Now clamp altitude (skip during dive)
        if (state != SkullState.Divebomb)
            ClampAltitudeSmooth();   // <-- new method (below)

        // 3) compute smoothed velocity for banking
        _vel = (transform.position - _prevPos) / Mathf.Max(Time.deltaTime, 1e-4f);
        _velSmooth = Vector3.Lerp(_velSmooth, _vel, bankVelLerp);

        _prevPos = transform.position;
    }



    // ---------- STATE TICKS ----------

    void PatrolTick()
    {
        MoveTowards(_patrolTarget, patrolSpeed);

        float dist = Vector3.Distance(transform.position, _patrolTarget);
        if (dist < 1.0f)
            PickNewPatrolPoint();
    }

    void ChaseTick()
    {
        if (!player) { state = SkullState.Patrol; return; }

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > loseAggroRange) { state = SkullState.Patrol; PickNewPatrolPoint(); return; }

        bool los = HasLineOfSight();
        if (los && dist <= attackRange) { state = SkullState.Attack; _fireCooldown = 0f; return; }

        // This call will also do the facing; no extra FaceTowards() needed
        MoveTowards(player.position, los ? chaseSpeed : patrolSpeed, false, facePitchInChase, bankRollInChase);

        if (currentHealth <= divebombHealthThreshold) StartDivebomb();
    }


    void AttackTick()
    {
        if (!player) { state = SkullState.Patrol; return; }
        float dist3D = Vector3.Distance(transform.position, player.position);
        float distXZ = HorizontalDistanceTo(player);
        bool los = HasLineOfSight();
        if (!los || dist3D > attackRange + 1f) { state = SkullState.Chase; return; }

        float minDist = Mathf.Max(keepOutRadius, preferredAttackDistance - attackDistanceTolerance);
        float maxDist = preferredAttackDistance + attackDistanceTolerance;

        if (distXZ < minDist)
        {
            MoveAwayFromHorizontal(player.position,
                                   Mathf.Max(chaseSpeed * 0.5f, patrolSpeed),
                                   facePitchInAttack,
                                   bankRollInAttack);
        }
        else if (distXZ > maxDist)
        {
            // closes in AND faces via MoveTowards
            MoveTowards(player.position,
                        Mathf.Min(chaseSpeed * 0.6f, 4.5f),
                        false,
                        facePitchInAttack,
                        bankRollInAttack);
        }
        else
        {
            // inside band: strafe AND explicitly face the player
            Vector3 toPlayerFlat = Flat(player.position - transform.position);
            AttackStrafeTick(toPlayerFlat);
            FaceTowards(player.position, facePitchInAttack, bankRollInAttack);
        }

        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown <= 0f) { ShootFireball(); _fireCooldown = 1f / Mathf.Max(0.01f, fireRate); }

        if (currentHealth <= divebombHealthThreshold) StartDivebomb();
    }


    void DivebombTick()
    {
        if (!_hasDiveTarget) { _diveLockedTarget = transform.position; _hasDiveTarget = true; }

        // move straight to the locked point (3D)
        MoveTowards(_diveLockedTarget, divebombSpeed, true, true, false);

        // explode if close enough
        if ((transform.position - _diveLockedTarget).sqrMagnitude < 0.35f * 0.35f)
        {
            Explode();
            return;
        }

        // or explode when the fuse runs out
        _divebombTimer -= Time.deltaTime;
        if (_divebombTimer <= 0f)
        {
            Explode();
            return;
        }
    }



    // ---------- ACTIONS ----------

    void PickNewPatrolPoint()
    {
        // Pick a random point on a disc around spawn, then set altitude to ground + desiredHoverHeight
        Vector2 rnd = Random.insideUnitCircle * patrolRadius;
        Vector3 candidate = _spawnPos + new Vector3(rnd.x, 0f, rnd.y);

        // Set target height to desired hover at that horizontal location
        float groundY = SampleGroundHeight(candidate + Vector3.up * 50f);
        candidate.y = groundY + Mathf.Min(desiredHoverHeight, maxHeightAboveGround);

        _patrolTarget = candidate;
    }

    bool HasLineOfSight()
    {
        if (!player) return false;

        Vector3 origin = transform.position + Vector3.up * eyeOffset;
        Vector3 target = player.position + Vector3.up * 0.5f;
        Vector3 dir = target - origin;
        float dist = dir.magnitude;
        if (dist < 0.001f) return true;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // Blocked by something
            return false;
        }
        return true;
    }

    void LookForPlayer()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // If the player is within detection range AND we have line of sight, start chasing
        if (dist <= detectRange && HasLineOfSight())
        {
            state = SkullState.Chase;
            return;
        }

        // Optional: if we're hurt while patrolling, commit to divebomb
        if (currentHealth <= divebombHealthThreshold)
            StartDivebomb();
    }

    void MoveTowards(Vector3 target, float speed, bool faceExactly = false,
                 bool allowPitch = true, bool allowBank = false)
    {
        Vector3 pos = transform.position;
        Vector3 t = target;

        // If not in divebomb (faceExactly==false), move only horizontally.
        if (!faceExactly) t.y = pos.y;

        Vector3 to = t - pos;
        Vector3 step = Vector3.ClampMagnitude(to, speed * Time.deltaTime);
        transform.position += step;

        Vector3 faceDir = faceExactly ? (target - transform.position)
                                      : (step.sqrMagnitude > 1e-6f ? step : to);

        FaceDirection(faceDir, allowPitch, allowBank);
    }



    void HoldPositionNear(Vector3 target, float maxNudgeSpeed)
    {
        Vector3 delta = target - transform.position;
        float d = delta.magnitude;
        if (d > 2.5f) // gently close in if we drift out while attacking
        {
            Vector3 step = Vector3.ClampMagnitude(delta, maxNudgeSpeed * Time.deltaTime);
            transform.position += step;
        }
    }

    Transform FaceTarget => modelTransform ? modelTransform : transform;

    void FaceDirection(Vector3 dirWorld, bool allowPitch, bool allowBank)
    {
        if (dirWorld.sqrMagnitude < 1e-6f) return;

        // Desired look rotation (with pitch if allowed)
        Vector3 up = Vector3.up;
        Quaternion look = Quaternion.LookRotation(dirWorld.normalized, up);

        if (!allowPitch)
        {
            // Extract yaw only: project forward onto XZ to remove pitch
            Vector3 f = dirWorld; f.y = 0f;
            if (f.sqrMagnitude > 1e-6f) look = Quaternion.LookRotation(f.normalized, up);
        }

        // Optional roll/bank based on lateral velocity
        if (allowBank)
        {
            Vector3 lat = _velSmooth; lat.y = 0f;   // use smoothed vel
            float side = 0f;
            if (lat.sqrMagnitude > 1e-6f)
            {
                Vector3 right = FaceTarget.right; right.y = 0f; right.Normalize();
                side = Vector3.Dot(lat.normalized, right);
            }
            float targetRoll = -side * bankAmount;
            look = look * Quaternion.Euler(0f, 0f, targetRoll);
        }


        FaceTarget.rotation = Quaternion.Slerp(FaceTarget.rotation, look, turnSpeed * Time.deltaTime);
    }

    void FaceTowards(Vector3 worldPos, bool allowPitch, bool allowBank)
    {
        Vector3 to = worldPos - FaceTarget.position;
        FaceDirection(to, allowPitch, allowBank);
    }


    void ClampAltitudeSmooth()
    {
        // Cast from well above to avoid noise hitting small props
        float groundY = SampleGroundHeight(transform.position + Vector3.up * 50f);

        // Desired hover above ground, clamped to max
        float targetAbove = Mathf.Min(desiredHoverHeight, maxHeightAboveGround);
        float targetY = groundY + Mathf.Clamp(targetAbove, 0.25f, maxHeightAboveGround);

        // SmoothDamp vertical only (prevents “sawing” jitter)
        Vector3 p = transform.position;
        p.y = Mathf.SmoothDamp(p.y, targetY, ref _yVel, hoverSmoothTime, Mathf.Infinity, Time.deltaTime);
        transform.position = p;
    }


    float SampleGroundHeight(Vector3 from)
    {
        // Cast down to find ground; default to current y if nothing is found
        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;

        return transform.position.y - desiredHoverHeight; // fallback
    }

    void ShootFireball()
    {
        if (!fireballPrefab || !firePoint) return;

        // Tiny random spread
        Quaternion spread = Quaternion.Euler(
            Random.Range(-fireSpreadDegrees, fireSpreadDegrees),
            Random.Range(-fireSpreadDegrees, fireSpreadDegrees),
            0f
        );

        Quaternion rot = firePoint.rotation * spread;
        GameObject go = Instantiate(fireballPrefab, firePoint.position, rot);

        // Try to pass basic parameters to the projectile
        var proj = go.GetComponent<SkullFireball>();
        if (proj)
        {
            proj.speed = fireballSpeed;
            proj.damage = 10;
            proj.lifeTime = 6f;
            proj.hitLayers = fireballDamageTargets;
        }

        if (sfxSource && fireSFX) sfxSource.PlayOneShot(fireSFX);
    }

    void StartDivebomb()
    {
        if (state == SkullState.Divebomb || state == SkullState.Dead) return;

        Vector3 lockPos = player ? player.position : transform.position + transform.forward * 2f;
        if (player)
        {
            var pc = player.GetComponent<Collider>();
            if (pc) lockPos = pc.bounds.center; // better vertical aim
        }

        _diveLockedTarget = lockPos;
        _hasDiveTarget = true;
        _divebombTimer = divebombFuse;   // start fuse (see next step)
        state = SkullState.Divebomb;
    }


    void Explode()
    {
        if (explodeVFX) Instantiate(explodeVFX, transform.position, Quaternion.identity);
        if (sfxSource && explodeSFX) sfxSource.PlayOneShot(explodeSFX);

        // Damage everything in radius that matches damageTargets
        Collider[] hits = Physics.OverlapSphere(transform.position, impactExplodeRadius, damageTargets, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            h.GetComponentInChildren<CombatController>().TakeDamage(impactDamage);
        }

        state = SkullState.Dead;
        Debug.Log("Skull is Dead");
        Destroy(gameObject, 0.02f);
    }

    // Optional: take damage entry point
    public void ApplyDamage(int amount)
    {
        if (state == SkullState.Dead) return;

        currentHealth -= Mathf.Max(0, amount);
        if (currentHealth <= 0)
        {
            // Immediate explode if killed (you could separate death VFX if desired)
            Explode();
            return;
        }

        // If low, commit to divebomb next tick
        if (currentHealth <= divebombHealthThreshold && state != SkullState.Divebomb)
            StartDivebomb();
    }

    // Trigger explode on impact when divebombing (e.g., hit player or ground)
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Skull Touched Something");
        if (state != SkullState.Divebomb) return;

        // Ignore minor triggers; explode on any solid things or designated target layers
        if (((1 << other.gameObject.layer) & (damageTargets)) != 0 || other.attachedRigidbody || !other.isTrigger)
        {
            Debug.Log("Skull Touched Damagable Target");
            Explode();
        }
    }

    Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    float HorizontalDistanceTo(Transform t)
    {
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = t.position; b.y = 0f;
        return Vector3.Distance(a, b);
    }



    void MoveAwayFromHorizontal(Vector3 target, float speed, bool allowPitch, bool allowBank)
    {
        Vector3 pos = transform.position;
        Vector3 t = target; t.y = pos.y;
        Vector3 from = pos - t;
        if (from.sqrMagnitude < 1e-6f) from = transform.right; // fallback

        Vector3 step = from.normalized * speed * Time.deltaTime;
        transform.position += step;

        // Keep facing the target (player), not the retreat direction
        FaceTowards(target, allowPitch, allowBank);
    }


    void AttackStrafeTick(Vector3 toPlayerFlat)
    {
        if (!enableAttackStrafe) return;

        _strafeTimer -= Time.deltaTime;
        if (_strafeTimer <= 0f)
        {
            _strafeDir = Random.value < 0.5f ? -_strafeDir : _strafeDir;   // sometimes flip, sometimes keep
            _strafeTimer = Random.Range(strafeDirFlipInterval.x, strafeDirFlipInterval.y);
        }

        // Perpendicular to player direction on the horizontal plane
        Vector3 strafe = Vector3.Cross(Vector3.up, toPlayerFlat.normalized) * _strafeDir;
        transform.position += strafe * attackStrafeSpeed * Time.deltaTime;
    }


}
