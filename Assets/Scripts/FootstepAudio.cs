using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(FirstPersonController))]
public class FootstepAudio : MonoBehaviour
{
    [System.Serializable]
    public enum SurfaceType { Stone, Dirt, Sand, Grass, Wood, Gravel }

    [System.Serializable]
    public class SurfaceProfile
    {
        public SurfaceType type;

        [Header("Overrides (non-terrain / special meshes)")]
        [Tooltip("If the hit collider's tag matches this (non-empty), this surface is used. Tag override takes priority over terrain textures.")]
        public string objectTag = "";

        [Header("Terrain Texture Mapping")]
        [Tooltip("Terrain texture indices (from TerrainData.terrainLayers order) that map to this surface.")]
        public List<int> terrainTextureIndices = new List<int>();

        [Header("Clips: Footstep")]
        public List<AudioClip> clips = new List<AudioClip>();

        [Header("Clips: Landing (optional)")]
        [Tooltip("If empty, will fall back to Footstep clips for landing.")]
        public List<AudioClip> landingClips = new List<AudioClip>();

        [Header("Randomization")]
        [Range(0f, 1f)] public float volumeMin = 0.8f;
        [Range(0f, 1f)] public float volumeMax = 1.0f;
        [Range(0.5f, 2f)] public float pitchMin = 0.95f;
        [Range(0.5f, 2f)] public float pitchMax = 1.05f;
    }

    [Header("Profiles (fill all six)")]
    public List<SurfaceProfile> profiles = new List<SurfaceProfile>()
    {
        new SurfaceProfile(){ type = SurfaceType.Stone },
        new SurfaceProfile(){ type = SurfaceType.Dirt  },
        new SurfaceProfile(){ type = SurfaceType.Sand  },
        new SurfaceProfile(){ type = SurfaceType.Grass },
        new SurfaceProfile(){ type = SurfaceType.Wood  },
        new SurfaceProfile(){ type = SurfaceType.Gravel}
    };

    [Header("Fallback")]
    [Tooltip("Used if nothing matches. Leave clips empty to mute.")]
    public SurfaceProfile defaultProfile = new SurfaceProfile() { type = SurfaceType.Stone };

    [Header("Timing")]
    [Tooltip("Seconds between steps while walking.")]
    public float walkInterval = 0.45f;
    [Tooltip("Multiplier applied when sprinting.")]
    public float sprintIntervalMult = 0.75f;
    [Tooltip("Multiplier applied when crouched.")]
    public float crouchIntervalMult = 1.3f;

    [Header("Movement Gates")]
    [Tooltip("Minimum horizontal speed (m/s) to count as moving.")]
    public float minMoveSpeed = 0.6f;
    [Tooltip("Don’t play footsteps if airborne.")]
    public bool requireGrounded = true;

    [Header("Landing Detection")]
    [Tooltip("Minimum downward speed (m/s) at impact to trigger a landing sound.")]
    public float landingMinDownSpeed = 3.0f;
    [Tooltip("Minimum time in air to consider it a landing (filters micro hops).")]
    public float landingMinAirTime = 0.12f;
    [Tooltip("Cooldown to avoid rapid re-triggers (sec).")]
    public float landingCooldown = 0.08f;
    [Tooltip("Delay added after a landing before auto footsteps resume (sec).")]
    public float postLandingStepDelay = 0.12f;

    [Header("Audio")]
    public AudioSource audioSource; // if null, one will be created

    [Header("Debug")]
    public bool showSphereCastGizmo = false;
    public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.35f);

    // refs
    private FirstPersonController fpc;
    private Rigidbody rb;
    private CapsuleCollider capsule;

    // cadence
    private float nextStepTime;

    // landing state
    private bool wasGroundedLast;
    private float airEnterTime;
    private float lastLandingTime;
    private float lastYVelocity;

    void Awake()
    {
        fpc = GetComponent<FirstPersonController>();
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D
            audioSource.playOnAwake = false;
        }

        wasGroundedLast = fpc.isGrounded;
        airEnterTime = -999f;
        lastLandingTime = -999f;
    }

    void Update()
    {
        // --- Auto cadence for walking/sprinting/crouching ---
        if (ShouldPlayAutoStep(out float interval))
        {
            if (Time.time >= nextStepTime)
            {
                TriggerFootstep();
                nextStepTime = Time.time + interval;
            }
        }
        else
        {
            nextStepTime = Time.time + GetBaseInterval();
        }

        // --- Landing detection (air -> ground transition) ---
        HandleLanding();

        // remember last frame state
        lastYVelocity = rb != null ? rb.linearVelocity.y : 0f;
        wasGroundedLast = fpc.isGrounded;
    }

    void HandleLanding()
    {
        bool nowGrounded = fpc.isGrounded;

        // went airborne this frame
        if (wasGroundedLast && !nowGrounded)
        {
            airEnterTime = Time.time;
            return;
        }

        // landed this frame
        if (!wasGroundedLast && nowGrounded)
        {
            float airTime = Time.time - airEnterTime;
            float downSpeed = -lastYVelocity; // positive when moving down

            if (airTime >= landingMinAirTime &&
                downSpeed >= landingMinDownSpeed &&
                Time.time >= lastLandingTime + landingCooldown)
            {
                if (TryGetGroundHit(out RaycastHit hit))
                {
                    PlayLandingAt(hit);
                    lastLandingTime = Time.time;

                    // small delay so we don't double with a footstep immediately after landing
                    nextStepTime = Mathf.Max(nextStepTime, Time.time + postLandingStepDelay);
                }
            }
        }
    }

    bool ShouldPlayAutoStep(out float interval)
    {
        interval = GetBaseInterval();

        // movement
        Vector3 v = rb != null ? rb.linearVelocity : Vector3.zero;
        float horizSpeed = new Vector2(v.x, v.z).magnitude;

        if (horizSpeed < minMoveSpeed) return false;
        if (requireGrounded && !fpc.isGrounded) return false;

        if (IsSprinting()) interval *= sprintIntervalMult;
        if (IsCrouched()) interval *= crouchIntervalMult;

        return true;
    }

    float GetBaseInterval() => Mathf.Max(0.05f, walkInterval);

    bool IsSprinting()
    {
        if (rb == null) return false;
        float speed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
        return speed > Mathf.Lerp(fpc.walkSpeed, fpc.sprintSpeed, 0.6f) - 0.05f;
    }

    bool IsCrouched()
    {
        // Heuristic based on your controller’s scale toggle.
        return transform.localScale.y < 0.99f;
    }

    /// <summary>
    /// Public so you can call from animation events.
    /// </summary>
    public void TriggerFootstep()
    {
        if (audioSource == null) return;
        if (requireGrounded && !fpc.isGrounded) return;

        if (!TryGetGroundHit(out RaycastHit hit))
            return;

        var profile = ResolveProfileForHit(hit);
        PlayClipFromProfile(profile, landing: false);
    }

    void PlayLandingAt(RaycastHit hit)
    {
        var profile = ResolveProfileForHit(hit);
        PlayClipFromProfile(profile, landing: true);
    }

    void PlayClipFromProfile(SurfaceProfile profile, bool landing)
    {
        if (profile == null) profile = defaultProfile;
        if (profile == null) return;

        List<AudioClip> list = landing && profile.landingClips != null && profile.landingClips.Count > 0
            ? profile.landingClips
            : profile.clips;

        if (list == null || list.Count == 0 || audioSource == null) return;

        var clip = list[Random.Range(0, list.Count)];
        audioSource.pitch = Random.Range(profile.pitchMin, profile.pitchMax);
        audioSource.volume = Random.Range(profile.volumeMin, profile.volumeMax);
        audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 1) Tag override for non-terrain / special meshes.
    /// 2) If Terrain hit, sample splatmap for dominant texture and map to profile.
    /// 3) Fallback profile.
    /// </summary>
    SurfaceProfile ResolveProfileForHit(RaycastHit hit)
    {
        Collider col = hit.collider;
        if (col == null) return defaultProfile;

        // 1) Tag override (priority)
        foreach (var p in profiles)
        {
            if (!string.IsNullOrEmpty(p.objectTag) && col.CompareTag(p.objectTag))
                return p;
        }

        // 2) Terrain texture detection
        Terrain terrain = GetTerrainFromCollider(col);
        if (terrain != null)
        {
            int dominant = GetDominantTextureIndex(terrain, hit.point);
            if (dominant >= 0)
            {
                foreach (var p in profiles)
                {
                    if (p.terrainTextureIndices != null && p.terrainTextureIndices.Contains(dominant))
                        return p;
                }
            }
        }

        // 3) Fallback
        return defaultProfile;
    }

    /// <summary>
    /// Mirror your ground check. Uses CapsuleCollider metrics like your controller.
    /// </summary>
    bool TryGetGroundHit(out RaycastHit hitInfo)
    {
        hitInfo = default;

        if (capsule == null)
            capsule = GetComponent<CapsuleCollider>();

        float radius = capsule ? Mathf.Max(0.01f, capsule.radius * 0.95f) : 0.3f;
        Vector3 origin = transform.position + Vector3.up * (radius + 0.02f);
        float castDist = (capsule ? (capsule.height * 0.5f) : 0.9f) + 0.05f;

        return Physics.SphereCast(origin, radius, Vector3.down, out hitInfo, castDist, ~0, QueryTriggerInteraction.Ignore);
    }

    Terrain GetTerrainFromCollider(Collider col)
    {
        if (col == null) return null;

        var terrain = col.GetComponent<Terrain>();
        if (terrain != null) return terrain;

        terrain = col.GetComponentInParent<Terrain>();
        if (terrain != null) return terrain;

        if (Terrain.activeTerrain != null)
            return Terrain.activeTerrain;

        return null;
    }

    int GetDominantTextureIndex(Terrain terrain, Vector3 worldPos)
    {
        if (terrain == null || terrain.terrainData == null) return -1;

        TerrainData td = terrain.terrainData;

        // Convert world position to alphamap coordinates
        Vector3 localPos = worldPos - terrain.transform.position;

        int aw = td.alphamapWidth;
        int ah = td.alphamapHeight;

        int x = Mathf.Clamp(Mathf.RoundToInt((localPos.x / td.size.x) * (aw - 1)), 0, aw - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((localPos.z / td.size.z) * (ah - 1)), 0, ah - 1);

        float[,,] weights = td.GetAlphamaps(x, y, 1, 1);
        int numLayers = weights.GetLength(2);
        if (numLayers == 0) return -1;

        int dominantIndex = 0;
        float max = weights[0, 0, 0];
        for (int i = 1; i < numLayers; i++)
        {
            if (weights[0, 0, i] > max)
            {
                max = weights[0, 0, i];
                dominantIndex = i;
            }
        }
        return dominantIndex;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showSphereCastGizmo) return;

        if (capsule == null)
            capsule = GetComponent<CapsuleCollider>();

        float radius = capsule ? Mathf.Max(0.01f, capsule.radius * 0.95f) : 0.3f;
        Vector3 origin = transform.position + Vector3.up * (radius + 0.02f);
        float castDist = (capsule ? (capsule.height * 0.5f) : 0.9f) + 0.05f;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(origin, radius);
        Gizmos.DrawLine(origin, origin + Vector3.down * castDist);
        Gizmos.DrawWireSphere(origin + Vector3.down * castDist, radius);
    }
#endif
}
