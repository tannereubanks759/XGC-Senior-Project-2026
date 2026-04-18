using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FootstepAudio : MonoBehaviour
{
    [System.Serializable]
    public enum SurfaceType
    {
        Stone,
        Dirt,
        Sand,
        Grass,
        Wood,
        Gravel,
        Water
    }

    [System.Serializable]
    public class SurfaceProfile
    {
        public SurfaceType type;

        [Header("Overrides (non-terrain / special meshes)")]
        [Tooltip("If the hit collider's tag matches this, this surface is used first.")]
        public string objectTag = "";

        [Header("Terrain Texture Mapping")]
        [Tooltip("Terrain layer indices that map to this surface.")]
        public List<int> terrainTextureIndices = new List<int>();

        [Header("Footstep Clips")]
        public List<AudioClip> clips = new List<AudioClip>();

        [Header("Landing Clips")]
        [Tooltip("If empty, regular footstep clips are used.")]
        public List<AudioClip> landingClips = new List<AudioClip>();

        [Header("Randomization")]
        [Range(0f, 2f)] public float volumeMin = 0.8f;
        [Range(0f, 2f)] public float volumeMax = 1.0f;
        [Range(0.5f, 2f)] public float pitchMin = 0.95f;
        [Range(0.5f, 2f)] public float pitchMax = 1.05f;
    }

    [Header("Profiles")]
    public List<SurfaceProfile> profiles = new List<SurfaceProfile>()
    {
        new SurfaceProfile(){ type = SurfaceType.Stone  },
        new SurfaceProfile(){ type = SurfaceType.Dirt   },
        new SurfaceProfile(){ type = SurfaceType.Sand   },
        new SurfaceProfile(){ type = SurfaceType.Grass  },
        new SurfaceProfile(){ type = SurfaceType.Wood   },
        new SurfaceProfile(){ type = SurfaceType.Gravel },
        new SurfaceProfile(){ type = SurfaceType.Water  }
    };

    [Header("Fallback")]
    public SurfaceProfile defaultProfile = new SurfaceProfile() { type = SurfaceType.Stone };

    [Header("Land Timing")]
    public float walkInterval = 0.45f;
    public float sprintIntervalMult = 0.75f;
    public float crouchIntervalMult = 1.3f;

    [Header("Water Timing")]
    public float waterStepInterval = 0.5f;
    public float waterSprintIntervalMult = 0.9f;
    public float waterCrouchIntervalMult = 1.1f;
    public bool waterIgnoresGrounded = true;
    public bool waterRequiresHorizontalMovement = true;

    [Header("Movement Gates")]
    public float minMoveSpeed = 0.6f;
    public bool requireGrounded = true;

    [Header("Water Override")]
    public bool useWaterLevelOverride = true;
    public float waterFootstepYLevel = 0f;
    public Transform waterLevelCheckPoint;

    [Header("Landing Detection")]
    public float landingMinDownSpeed = 3.0f;
    public float landingMinAirTime = 0.12f;
    public float landingCooldown = 0.08f;
    public float postLandingStepDelay = 0.12f;

    [Header("Audio")]
    public AudioSource audioSource;

    [Tooltip("Master multiplier applied to every footstep and landing sound.")]
    [Range(0f, 2f)] public float masterVolume = 1f;

    [Tooltip("Lowest allowed one-shot volume scale so bad profile values do not mute the source.")]
    [Range(0f, 1f)] public float minimumOneShotVolume = 0.05f;

    [Header("Debug")]
    public bool debugWaterState = false;

    private FirstPersonController fpc;
    private Rigidbody rb;
    private CapsuleCollider capsule;

    private float nextLandStepTime;
    private float nextWaterStepTime;

    private bool wasGroundedLast;
    private bool wasInWaterLast;
    private float airEnterTime;
    private float lastLandingTime;
    private float lastYVelocity;

    private float baseAudioSourceVolume = 1f;
    private float baseAudioSourcePitch = 1f;

    void Awake()
    {
        fpc = GetComponent<FirstPersonController>();
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        baseAudioSourceVolume = Mathf.Max(0f, audioSource.volume);
        baseAudioSourcePitch = Mathf.Max(0.01f, audioSource.pitch);

        wasGroundedLast = fpc.isGrounded;
        wasInWaterLast = IsInWater();
        airEnterTime = -999f;
        lastLandingTime = -999f;

        nextLandStepTime = Time.time;
        nextWaterStepTime = Time.time;
    }

    void OnValidate()
    {
        walkInterval = Mathf.Max(0.05f, walkInterval);
        waterStepInterval = Mathf.Max(0.05f, waterStepInterval);
        minMoveSpeed = Mathf.Max(0f, minMoveSpeed);
        masterVolume = Mathf.Max(0f, masterVolume);
        minimumOneShotVolume = Mathf.Clamp01(minimumOneShotVolume);
    }

    void Update()
    {
        bool inWater = IsInWater();

        HandleWaterStateTransitions(inWater);

        if (inWater)
        {
            HandleWaterFootsteps();
        }
        else
        {
            HandleLandFootsteps();
            HandleLanding();
        }

        lastYVelocity = rb.linearVelocity.y;
        wasGroundedLast = fpc.isGrounded;
        wasInWaterLast = inWater;
    }

    void HandleWaterStateTransitions(bool inWater)
    {
        if (inWater && !wasInWaterLast)
        {
            nextWaterStepTime = Time.time;

            if (debugWaterState)
                Debug.Log("Entered water footsteps zone.");
        }
        else if (!inWater && wasInWaterLast)
        {
            nextLandStepTime = Time.time + 0.05f;

            if (debugWaterState)
                Debug.Log("Exited water footsteps zone.");
        }
    }

    void HandleWaterFootsteps()
    {
        if (!ShouldPlayWaterStep(out float interval))
            return;

        if (Time.time >= nextWaterStepTime)
        {
            PlayWaterFootstep();
            nextWaterStepTime = Time.time + interval;
        }
    }

    void HandleLandFootsteps()
    {
        if (!ShouldPlayLandStep(out float interval))
            return;

        if (Time.time >= nextLandStepTime)
        {
            TriggerLandFootstep();
            nextLandStepTime = Time.time + interval;
        }
    }

    bool ShouldPlayWaterStep(out float interval)
    {
        interval = waterStepInterval;

        Vector3 velocity = rb.linearVelocity;
        float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;

        if (waterRequiresHorizontalMovement)
        {
            if (horizontalSpeed < minMoveSpeed)
                return false;
        }
        else
        {
            if (velocity.magnitude < minMoveSpeed)
                return false;
        }

        if (!waterIgnoresGrounded && requireGrounded && !fpc.isGrounded)
            return false;

        if (IsSprinting())
            interval *= waterSprintIntervalMult;

        if (IsCrouched())
            interval *= waterCrouchIntervalMult;

        interval = Mathf.Max(0.05f, interval);
        return true;
    }

    bool ShouldPlayLandStep(out float interval)
    {
        interval = walkInterval;

        Vector3 velocity = rb.linearVelocity;
        float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;

        if (horizontalSpeed < minMoveSpeed)
            return false;

        if (requireGrounded && !fpc.isGrounded)
            return false;

        if (IsSprinting())
            interval *= sprintIntervalMult;

        if (IsCrouched())
            interval *= crouchIntervalMult;

        interval = Mathf.Max(0.05f, interval);
        return true;
    }

    bool IsSprinting()
    {
        float speed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
        return speed > Mathf.Lerp(fpc.walkSpeed, fpc.sprintSpeed, 0.6f) - 0.05f;
    }

    bool IsCrouched()
    {
        return transform.localScale.y < 0.99f;
    }

    bool IsInWater()
    {
        if (!useWaterLevelOverride)
            return false;

        Transform check = waterLevelCheckPoint != null ? waterLevelCheckPoint : transform;
        return check.position.y < waterFootstepYLevel;
    }

    public void TriggerFootstep()
    {
        if (IsInWater())
            PlayWaterFootstep();
        else
            TriggerLandFootstep();
    }

    void PlayWaterFootstep()
    {
        SurfaceProfile waterProfile = GetProfileByType(SurfaceType.Water);
        PlayClipFromProfile(waterProfile, false);

        if (debugWaterState)
        {
            float y = waterLevelCheckPoint != null ? waterLevelCheckPoint.position.y : transform.position.y;
            Debug.Log($"Water footstep played at y={y:F2}, sourceVolume={audioSource.volume:F2}");
        }
    }

    void TriggerLandFootstep()
    {
        if (requireGrounded && !fpc.isGrounded)
            return;

        if (!TryGetGroundHit(out RaycastHit hit))
            return;

        SurfaceProfile profile = ResolveProfileForHit(hit);
        PlayClipFromProfile(profile, false);
    }

    void HandleLanding()
    {
        bool nowGrounded = fpc.isGrounded;

        if (wasGroundedLast && !nowGrounded)
        {
            airEnterTime = Time.time;
            return;
        }

        if (!wasGroundedLast && nowGrounded)
        {
            float airTime = Time.time - airEnterTime;
            float downwardSpeed = -lastYVelocity;

            if (airTime >= landingMinAirTime &&
                downwardSpeed >= landingMinDownSpeed &&
                Time.time >= lastLandingTime + landingCooldown)
            {
                if (TryGetGroundHit(out RaycastHit hit))
                {
                    PlayLandingAt(hit);
                    lastLandingTime = Time.time;
                    nextLandStepTime = Mathf.Max(nextLandStepTime, Time.time + postLandingStepDelay);
                }
            }
        }
    }

    void PlayLandingAt(RaycastHit hit)
    {
        SurfaceProfile profile = ResolveProfileForHit(hit);
        PlayClipFromProfile(profile, true);
    }

    void PlayClipFromProfile(SurfaceProfile profile, bool landing)
    {
        if (audioSource == null)
            return;

        if (profile == null)
            profile = defaultProfile;

        if (profile == null)
            return;

        List<AudioClip> clipList =
            landing && profile.landingClips != null && profile.landingClips.Count > 0
            ? profile.landingClips
            : profile.clips;

        if (clipList == null || clipList.Count == 0)
            return;

        AudioClip clip = clipList[Random.Range(0, clipList.Count)];
        if (clip == null)
            return;

        float pitchMin = Mathf.Max(0.01f, Mathf.Min(profile.pitchMin, profile.pitchMax));
        float pitchMax = Mathf.Max(pitchMin, profile.pitchMax);
        float volumeMin = Mathf.Max(0f, Mathf.Min(profile.volumeMin, profile.volumeMax));
        float volumeMax = Mathf.Max(volumeMin, profile.volumeMax);

        float randomizedPitch = Random.Range(pitchMin, pitchMax);
        float randomizedVolumeScale = Random.Range(volumeMin, volumeMax) * masterVolume;

        if (randomizedVolumeScale > 0f)
            randomizedVolumeScale = Mathf.Max(minimumOneShotVolume, randomizedVolumeScale);

        audioSource.volume = baseAudioSourceVolume;
        audioSource.pitch = randomizedPitch;
        audioSource.PlayOneShot(clip, randomizedVolumeScale);
        audioSource.volume = baseAudioSourceVolume;
    }

    SurfaceProfile ResolveProfileForHit(RaycastHit hit)
    {
        Collider col = hit.collider;
        if (col == null)
            return defaultProfile;

        foreach (SurfaceProfile profile in profiles)
        {
            if (!string.IsNullOrEmpty(profile.objectTag) && col.CompareTag(profile.objectTag))
                return profile;
        }

        Terrain terrain = GetTerrainFromCollider(col);
        if (terrain != null)
        {
            int dominantIndex = GetDominantTextureIndex(terrain, hit.point);
            if (dominantIndex >= 0)
            {
                foreach (SurfaceProfile profile in profiles)
                {
                    if (profile.terrainTextureIndices != null &&
                        profile.terrainTextureIndices.Contains(dominantIndex))
                    {
                        return profile;
                    }
                }
            }
        }

        return defaultProfile;
    }

    SurfaceProfile GetProfileByType(SurfaceType type)
    {
        foreach (SurfaceProfile profile in profiles)
        {
            if (profile.type == type)
                return profile;
        }

        if (defaultProfile != null && defaultProfile.type == type)
            return defaultProfile;

        return null;
    }

    bool TryGetGroundHit(out RaycastHit hitInfo)
    {
        float radius = Mathf.Max(0.01f, capsule.radius * 0.95f);
        Vector3 origin = transform.position + Vector3.up * (radius + 0.02f);
        float castDistance = (capsule.height * 0.5f) + 0.05f;

        return Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out hitInfo,
            castDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );
    }

    Terrain GetTerrainFromCollider(Collider col)
    {
        if (col == null)
            return null;

        Terrain terrain = col.GetComponent<Terrain>();
        if (terrain != null)
            return terrain;

        terrain = col.GetComponentInParent<Terrain>();
        if (terrain != null)
            return terrain;

        return Terrain.activeTerrain;
    }

    int GetDominantTextureIndex(Terrain terrain, Vector3 worldPos)
    {
        if (terrain == null || terrain.terrainData == null)
            return -1;

        TerrainData td = terrain.terrainData;
        Vector3 localPos = worldPos - terrain.transform.position;

        int mapX = Mathf.Clamp(
            Mathf.RoundToInt((localPos.x / td.size.x) * (td.alphamapWidth - 1)),
            0,
            td.alphamapWidth - 1
        );

        int mapY = Mathf.Clamp(
            Mathf.RoundToInt((localPos.z / td.size.z) * (td.alphamapHeight - 1)),
            0,
            td.alphamapHeight - 1
        );

        float[,,] weights = td.GetAlphamaps(mapX, mapY, 1, 1);
        int layerCount = weights.GetLength(2);

        if (layerCount == 0)
            return -1;

        int dominant = 0;
        float maxWeight = weights[0, 0, 0];

        for (int i = 1; i < layerCount; i++)
        {
            if (weights[0, 0, i] > maxWeight)
            {
                maxWeight = weights[0, 0, i];
                dominant = i;
            }
        }

        return dominant;
    }

    void OnDisable()
    {
        if (audioSource != null)
        {
            audioSource.volume = baseAudioSourceVolume;
            audioSource.pitch = baseAudioSourcePitch;
        }
    }
}