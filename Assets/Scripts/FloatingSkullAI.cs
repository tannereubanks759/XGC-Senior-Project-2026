using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FloatingSkullAI : MonoBehaviour
{
    public enum SkullState { Patrol, Chase, Attack, Divebomb, Dead }

    [Header("Refs")]
    public Transform player;
    public Transform firePoint;
    public GameObject fireballPrefab;

    [Header("Ranges")]
    public float patrolRadius = 12f;
    public float detectRange = 18f;
    public float attackRange = 10f;
    public float loseAggroRange = 24f;

    [Header("Speeds")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 6.5f;
    public float divebombSpeed = 13f;
    public float turnSpeed = 10f;

    [Header("Altitude")]
    public float maxHeightAboveGround = 6f;
    public float desiredHoverHeight = 3.5f;
    public LayerMask groundMask = ~0;

    [Header("Line of Sight")]
    public LayerMask obstacleMask;
    public float eyeOffset = 0.5f;

    [Header("Attack")]
    public float fireRate = 0.8f;
    public float fireballSpeed = 18f;
    public float fireSpreadDegrees = 1.5f;

    [Header("Health / Divebomb")]
    public int maxHealth = 60;
    public int currentHealth = 60;
    [Tooltip("When health <= this value, the skull commits to a divebomb on the player's current position.")]
    public int divebombHealthThreshold = 15;
    public float impactExplodeRadius = 4.25f;
    public int impactDamage = 35;
    public LayerMask damageTargets;
    public LayerMask fireballDamageTargets;

    [Header("FX (optional)")]
    public GameObject explodeVFX;

    [Tooltip("One-shots & non-looping SFX (fires, hurt, death, explode). If null, one is added.")]
    public AudioSource sfxSource;
    [Tooltip("Looping propulsion/idle source. If null, one is added and set to 3D loop.")]
    public AudioSource loopSource;

    [Header("SFX Clips")]
    public AudioClip fireSFX;       // each fireball
    public AudioClip explodeSFX;    // divebomb impact only
    public AudioClip deathSFX;      // normal death (non-divebomb)
    public AudioClip hurtSFX;       // non-lethal damage
    public AudioClip idleLoopSFX;   // propulsion loop (3D, looping)

    [Header("Idle Loop Tuning")]
    [Range(0.1f, 3f)] public float idleBasePitch = 1.0f;
    [Range(0f, 2f)] public float chasePitchBoost = 0.12f;
    [Range(0f, 2f)] public float attackPitchBoost = 0.18f;
    [Range(0f, 2f)] public float divePitchBoost = 0.35f;
    [Range(0.01f, 2f)] public float idlePitchLerp = 6f; // higher = snappier

    [Header("Attack Positioning")]
    public float preferredAttackDistance = 8f;
    public float attackDistanceTolerance = 1.0f;
    public float keepOutRadius = 3f;
    public bool enableAttackStrafe = true;
    public float attackStrafeSpeed = 2.5f;
    public Vector2 strafeDirFlipInterval = new Vector2(1.8f, 3.2f);

    [Header("Facing / Banking")]
    public Transform modelTransform;
    public bool facePitchInChase = true;
    public bool facePitchInAttack = true;
    public bool bankRollInChase = true;
    public bool bankRollInAttack = true;
    [Range(0f, 45f)] public float bankAmount = 12f;
    public float bankSmoothing = 12f;

    [Header("Divebomb")]
    public float divebombFuse = 2.5f;
    float _divebombTimer;

    [Range(0.03f, 0.35f)]
    public float hoverSmoothTime = 0.12f;
    [Range(0f, 1f)] public float bankVelLerp = 0.2f;

    [Header("Debug")]
    public SkullState state = SkullState.Patrol;
    public bool drawGizmos = true;

    // --- Private ---
    Vector3 _spawnPos;
    Vector3 _patrolTarget;
    float _fireCooldown;
    Vector3 _diveLockedTarget;
    bool _hasDiveTarget;
    float _strafeTimer;
    int _strafeDir = 1;
    Vector3 _prevPos;
    Vector3 _vel;
    float _yVel;
    Vector3 _velSmooth;
    Collider _col;
    float _targetIdlePitch;
    // --- Private ---
    Vector3 _diveDir;   // locked straight-line direction during dive

    void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void Start()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;

        // Ensure audio sources exist & are configured
        if (!sfxSource)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 1f;
            sfxSource.rolloffMode = AudioRolloffMode.Linear;
            sfxSource.minDistance = 2f;
            sfxSource.maxDistance = 20f;
        }
        if (!loopSource)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;
            loopSource.spatialBlend = 1f;
            loopSource.rolloffMode = AudioRolloffMode.Linear;
            loopSource.minDistance = 2f;
            loopSource.maxDistance = 25f;
        }
        if (idleLoopSFX)
        {
            loopSource.clip = idleLoopSFX;
            loopSource.pitch = idleBasePitch;
            loopSource.volume = 1f;
            loopSource.Play();
        }

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

        switch (state)
        {
            case SkullState.Patrol: PatrolTick(); LookForPlayer(); break;
            case SkullState.Chase: ChaseTick(); break;
            case SkullState.Attack: AttackTick(); break;
            case SkullState.Divebomb: DivebombTick(); break;
        }

        if (state != SkullState.Divebomb)
            ClampAltitudeSmooth();

        _vel = (transform.position - _prevPos) / Mathf.Max(Time.deltaTime, 1e-4f);
        _velSmooth = Vector3.Lerp(_velSmooth, _vel, bankVelLerp);
        _prevPos = transform.position;

        // --- Idle loop pitch target by state ---
        _targetIdlePitch = idleBasePitch;
        if (state == SkullState.Chase) _targetIdlePitch += chasePitchBoost;
        if (state == SkullState.Attack) _targetIdlePitch += attackPitchBoost;
        if (state == SkullState.Divebomb) _targetIdlePitch += divePitchBoost;

        if (loopSource && loopSource.isPlaying)
            loopSource.pitch = Mathf.Lerp(loopSource.pitch, _targetIdlePitch, Time.deltaTime * idlePitchLerp);
    }

    // ---------- STATE TICKS ----------
    void PatrolTick()
    {
        MoveTowards(_patrolTarget, patrolSpeed);
        if (Vector3.Distance(transform.position, _patrolTarget) < 1.0f)
            PickNewPatrolPoint();
    }

    void ChaseTick()
    {
        if (!player) { state = SkullState.Patrol; return; }

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > loseAggroRange) { state = SkullState.Patrol; PickNewPatrolPoint(); return; }

        bool los = HasLineOfSight();
        if (los && dist <= attackRange) { state = SkullState.Attack; _fireCooldown = 0f; return; }

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

        // --- movement ---
        if (distXZ < minDist)
        {
            // back away on XZ
            MoveAwayFromHorizontal(player.position, Mathf.Max(chaseSpeed * 0.5f, patrolSpeed), false, false);
        }
        else if (distXZ > maxDist)
        {
            // move in on XZ
            MoveTowards(player.position, Mathf.Min(chaseSpeed * 0.6f, 4.5f), false, false, false);
        }
        else
        {
            // hold band and strafe
            Vector3 toPlayerFlat = Flat(player.position - transform.position);
            AttackStrafeTick(toPlayerFlat);
        }

        // --- ALWAYS face the player in Attack ---
        FaceTowards(player.position, facePitchInAttack, bankRollInAttack);

        // --- firing ---
        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown <= 0f)
        {
            ShootFireball();
            _fireCooldown = 1f / Mathf.Max(0.01f, fireRate);
        }

        // --- divebomb check ---
        if (currentHealth <= divebombHealthThreshold) StartDivebomb();
    }


    void DivebombTick()
    {
        if (!_hasDiveTarget)
        {
            _diveLockedTarget = transform.position + transform.forward * 2f;
            _diveDir = transform.forward;
            _hasDiveTarget = true;
        }

        // Move strictly along the locked direction, ignoring proximity to the player.
        transform.position += _diveDir * (divebombSpeed * Time.deltaTime);

        // Optional: keep the model facing along the dive direction (no bank)
        FaceDirection(_diveDir, true, false);

        // Fuse check — only time-out can explode mid-air
        _divebombTimer -= Time.deltaTime;
        if (_divebombTimer <= 0f)
        {
            ExplodeImpact();
            return;
        }

        // NOTE: Explosion on ground contact is handled by OnTriggerEnter/OnCollisionEnter
        // where we check against groundMask. We do NOT explode due to proximity anymore.
    }



    // ---------- ACTIONS ----------
    void PickNewPatrolPoint()
    {
        Vector2 rnd = Random.insideUnitCircle * patrolRadius;
        Vector3 candidate = _spawnPos + new Vector3(rnd.x, 0f, rnd.y);
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
            return false;

        return true;
    }

    void LookForPlayer()
    {
        if (!player) return;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= detectRange && HasLineOfSight())
        {
            state = SkullState.Chase;
            return;
        }
        if (currentHealth <= divebombHealthThreshold)
            StartDivebomb();
    }

    void MoveTowards(Vector3 target, float speed, bool faceExactly = false, bool allowPitch = true, bool allowBank = false)
    {
        Vector3 pos = transform.position;
        Vector3 t = target;
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
        if (d > 2.5f)
        {
            Vector3 step = Vector3.ClampMagnitude(delta, maxNudgeSpeed * Time.deltaTime);
            transform.position += step;
        }
    }

    Transform FaceTarget => modelTransform ? modelTransform : transform;

    void FaceDirection(Vector3 dirWorld, bool allowPitch, bool allowBank)
    {
        if (dirWorld.sqrMagnitude < 1e-6f) return;

        Vector3 up = Vector3.up;
        Quaternion look = Quaternion.LookRotation(dirWorld.normalized, up);

        if (!allowPitch)
        {
            Vector3 f = dirWorld; f.y = 0f;
            if (f.sqrMagnitude > 1e-6f) look = Quaternion.LookRotation(f.normalized, up);
        }

        if (allowBank)
        {
            Vector3 lat = _velSmooth; lat.y = 0f;
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
        float groundY = SampleGroundHeight(transform.position + Vector3.up * 50f);
        float targetAbove = Mathf.Min(desiredHoverHeight, maxHeightAboveGround);
        float targetY = groundY + Mathf.Clamp(targetAbove, 0.25f, maxHeightAboveGround);
        Vector3 p = transform.position;
        p.y = Mathf.SmoothDamp(p.y, targetY, ref _yVel, hoverSmoothTime, Mathf.Infinity, Time.deltaTime);
        transform.position = p;
    }

    float SampleGroundHeight(Vector3 from)
    {
        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return transform.position.y - desiredHoverHeight;
    }

    void ShootFireball()
    {
        if (!fireballPrefab || !firePoint) return;

        Quaternion spread = Quaternion.Euler(
            Random.Range(-fireSpreadDegrees, fireSpreadDegrees),
            Random.Range(-fireSpreadDegrees, fireSpreadDegrees),
            0f
        );
        Quaternion rot = firePoint.rotation * spread;
        GameObject go = Instantiate(fireballPrefab, firePoint.position, rot);

        var proj = go.GetComponent<SkullFireball>();
        if (proj)
        {
            proj.speed = fireballSpeed;
            proj.damage = 10;
            proj.lifeTime = 6f;
            proj.hitLayers = fireballDamageTargets;
        }

        PlayOneShotSafe(fireSFX, 0.9f, 1.1f);
    }

    void StartDivebomb()
    {
        if (state == SkullState.Divebomb || state == SkullState.Dead) return;

        Vector3 lockPos = player ? player.position : transform.position + transform.forward * 3f;
        if (player)
        {
            var pc = player.GetComponent<Collider>();
            if (pc) lockPos = pc.bounds.center;
        }

        _diveLockedTarget = lockPos;
        _hasDiveTarget = true;
        _divebombTimer = divebombFuse;

        // Lock a straight direction from current position toward the locked target (once).
        _diveDir = (_diveLockedTarget - transform.position);
        if (_diveDir.sqrMagnitude < 1e-6f) _diveDir = transform.forward; // fallback
        _diveDir.Normalize();

        state = SkullState.Divebomb;
    }


    // --- AUDIO-AWARE death/explode paths ---
    void ExplodeImpact()
    {
        // Divebomb AoE death (+ explode SFX)
        if (explodeVFX) Destroy(Instantiate(explodeVFX, transform.position, Quaternion.identity), 3f);
        StopIdleLoop();
        PlayOneShotSafe(explodeSFX);

        Collider[] hits = Physics.OverlapSphere(transform.position, impactExplodeRadius, damageTargets, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            var cc = h.GetComponentInChildren<CombatController>();
            if (cc) cc.TakeDamage(impactDamage);
        }

        state = SkullState.Dead;
        Destroy(gameObject, 0.02f);
    }

    void Die()
    {
        // Normal death (no AoE), plays deathSFX
        if (explodeVFX) Instantiate(explodeVFX, transform.position, Quaternion.identity); // optional: keep VFX
        StopIdleLoop();
        PlayOneShotSafe(deathSFX);

        state = SkullState.Dead;
        Destroy(gameObject, deathSFX ? Mathf.Max(0.02f, deathSFX.length * 0.2f) : 0.02f);
    }

    // Optional: take damage entry point
    public void ApplyDamage(int amount)
    {
        if (state == SkullState.Dead) return;

        int prev = currentHealth;
        currentHealth -= Mathf.Max(0, amount);

        if (currentHealth <= 0)
        {
            // If we were divebombing, still explode; otherwise normal death
            if (state == SkullState.Divebomb) ExplodeImpact();
            else Die();
            return;
        }

        // Non-lethal: play hurt SFX (rate limited by clip length naturally)
        if (amount > 0) PlayOneShotSafe(hurtSFX, 0.95f, 1.05f);

        if (currentHealth <= divebombHealthThreshold && state != SkullState.Divebomb)
            StartDivebomb();
    }

    void OnTriggerEnter(Collider other)
    {
        if (state != SkullState.Divebomb) return;

        // Only explode if we hit something on the groundMask
        if (((1 << other.gameObject.layer) & groundMask) != 0)
        {
            ExplodeImpact();
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
        if (from.sqrMagnitude < 1e-6f) from = transform.right;

        Vector3 step = from.normalized * speed * Time.deltaTime;
        transform.position += step;
        FaceTowards(target, allowPitch, allowBank);
    }

    void AttackStrafeTick(Vector3 toPlayerFlat)
    {
        if (!enableAttackStrafe) return;

        _strafeTimer -= Time.deltaTime;
        if (_strafeTimer <= 0f)
        {
            _strafeDir = Random.value < 0.5f ? -_strafeDir : _strafeDir;
            _strafeTimer = Random.Range(strafeDirFlipInterval.x, strafeDirFlipInterval.y);
        }

        Vector3 strafe = Vector3.Cross(Vector3.up, toPlayerFlat.normalized) * _strafeDir;
        transform.position += strafe * attackStrafeSpeed * Time.deltaTime;
    }

    // --- Audio helpers ---
    void PlayOneShotSafe(AudioClip clip, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (!clip || !sfxSource) return;
        float oldPitch = sfxSource.pitch;
        sfxSource.pitch = (pitchMin == pitchMax) ? pitchMin : Random.Range(pitchMin, pitchMax);
        sfxSource.PlayOneShot(clip);
        sfxSource.pitch = oldPitch;
    }

    void StopIdleLoop()
    {
        if (loopSource && loopSource.isPlaying) loopSource.Stop();
    }
}
