using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class AmbientZoneBlender : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Underwater Detection")]
    [Tooltip("If assigned, this transform decides underwater status (usually player camera).")]
    public Transform underwaterCheckTransform;

    [Tooltip("Fallback to Camera.main when underwaterCheckTransform is null.")]
    public bool fallbackToMainCamera = true;

    [Tooltip("Sea level / water surface Y. Underwater when checkTransform.y < this value.")]
    public float seaLevelY = 0f;

    [Tooltip("Small buffer around sea level to avoid rapid toggling.")]
    public float underwaterHysteresis = 0.15f;

    [Header("Audio Sources (looping)")]
    public AudioSource beachSource;
    public AudioSource windSource;
    public AudioSource caveSource;
    public AudioSource underwaterSource;

    [Header("Height Blend Settings")]
    [Tooltip("Wind starts blending in at this height.")]
    public float windStartY = 8f;

    [Tooltip("Wind reaches full at/above this height.")]
    public float windFullY = 25f;

    [Tooltip("How far from sea level still counts as near-water for beach presence.")]
    public float beachNearWaterRange = 12f;

    [Header("Transitions")]
    [Range(0.05f, 20f)] public float blendSpeed = 2.5f;
    [Range(0f, 1f)] public float masterVolume = 1f;

    [Header("Safety")]
    public bool autoRestartSources = true;

    [Header("Scene Handling")]
    public bool resetCaveStateOnSceneLoad = true;
    public bool forceBeachOnSceneLoad = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Cave trigger bookkeeping
    private int caveTriggerCount = 0;

    // Smoothed weights [0..1]
    private float currentBeach = 0f;
    private float currentWind = 0f;
    private float currentCave = 0f;
    private float currentUnderwater = 0f;

    private bool isPaused = false;
    private bool wasUnderwater = false;

    private void OnValidate()
    {
        if (windFullY <= windStartY) windFullY = windStartY + 0.01f;
        beachNearWaterRange = Mathf.Max(0.01f, beachNearWaterRange);
        blendSpeed = Mathf.Max(0.01f, blendSpeed);
        underwaterHysteresis = Mathf.Max(0f, underwaterHysteresis);
    }

    private void Awake()
    {
        ValidateSource(beachSource, "Beach");
        ValidateSource(windSource, "Wind");
        ValidateSource(caveSource, "Cave");
        ValidateSource(underwaterSource, "Underwater");

        TryFindPlayerIfMissing();
        TryResolveUnderwaterCheckTransform();

        ForcePlayLoop(beachSource);
        ForcePlayLoop(windSource);
        ForcePlayLoop(caveSource);
        ForcePlayLoop(underwaterSource);

        // Start silent; first Update computes correct targets
        SetRawSourceVolume(beachSource, 0f);
        SetRawSourceVolume(windSource, 0f);
        SetRawSourceVolume(caveSource, 0f);
        SetRawSourceVolume(underwaterSource, 0f);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryFindPlayerIfMissing(force: true);
        TryResolveUnderwaterCheckTransform(force: true);

        if (resetCaveStateOnSceneLoad)
            caveTriggerCount = 0;

        if (forceBeachOnSceneLoad)
        {
            // Immediate nudge to beach on new scene
            currentBeach = 1f;
            currentWind = 0f;
            currentCave = 0f;
            currentUnderwater = 0f;
            ApplyVolumes();
        }

        KeepSourceAlive(beachSource);
        KeepSourceAlive(windSource);
        KeepSourceAlive(caveSource);
        KeepSourceAlive(underwaterSource);

        if (showDebugLogs)
            Debug.Log($"[AmbientZoneBlender] Scene loaded: {scene.name}");
    }

    private void Update()
    {
        if (isPaused) return;

        if (player == null)
        {
            TryFindPlayerIfMissing();
            if (player == null) return;
        }

        TryResolveUnderwaterCheckTransform();

        ComputeTargetWeights(out float targetBeach, out float targetWind, out float targetCave, out float targetUnderwater);

        float t = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
        currentBeach = Mathf.Lerp(currentBeach, targetBeach, t);
        currentWind = Mathf.Lerp(currentWind, targetWind, t);
        currentCave = Mathf.Lerp(currentCave, targetCave, t);
        currentUnderwater = Mathf.Lerp(currentUnderwater, targetUnderwater, t);

        KeepSourceAlive(beachSource);
        KeepSourceAlive(windSource);
        KeepSourceAlive(caveSource);
        KeepSourceAlive(underwaterSource);

        ApplyVolumes();

        if (showDebugLogs)
        {
            float uy = underwaterCheckTransform ? underwaterCheckTransform.position.y : float.NaN;
            Debug.Log(
                $"[Ambience] y={player.position.y:F2} checkY={uy:F2} " +
                $"UW={currentUnderwater:F2} Cave={currentCave:F2} Beach={currentBeach:F2} Wind={currentWind:F2} " +
                $"caveCount={caveTriggerCount}"
            );
        }
    }

    /// <summary>
    /// Priority:
    /// 1) Underwater
    /// 2) Cave
    /// 3) Beach/Wind blend
    /// </summary>
    private void ComputeTargetWeights(out float beach, out float wind, out float cave, out float underwater)
    {
        bool underwaterNow = IsUnderwater();

        if (underwaterNow)
        {
            underwater = 1f;
            cave = 0f;
            beach = 0f;
            wind = 0f;
            return;
        }

        if (IsInCave())
        {
            underwater = 0f;
            cave = 1f;
            beach = 0f;
            wind = 0f;
            return;
        }

        underwater = 0f;
        cave = 0f;

        float y = player.position.y;
        float windByHeight = Mathf.Clamp01(Mathf.InverseLerp(windStartY, windFullY, y));

        float distToWater = Mathf.Abs(y - seaLevelY);
        float beachNearWater = 1f - Mathf.Clamp01(distToWater / beachNearWaterRange);

        beach = beachNearWater * (1f - windByHeight);
        wind = windByHeight;

        float sum = beach + wind;
        if (sum > 1f)
        {
            beach /= sum;
            wind /= sum;
        }
    }

    /// <summary>
    /// Uses hysteresis to reduce flickering right around sea level.
    /// </summary>
    private bool IsUnderwater()
    {
        Transform check = underwaterCheckTransform;
        if (check == null) return false;

        float y = check.position.y;
        bool result;

        if (!wasUnderwater)
        {
            // Must go slightly below sea level to enter underwater
            result = y < (seaLevelY - underwaterHysteresis);
        }
        else
        {
            // Must go slightly above sea level to exit underwater
            result = y < (seaLevelY + underwaterHysteresis);
        }

        wasUnderwater = result;
        return result;
    }

    private void ApplyVolumes()
    {
        SetRawSourceVolume(beachSource, Mathf.Clamp01(currentBeach) * masterVolume);
        SetRawSourceVolume(windSource, Mathf.Clamp01(currentWind) * masterVolume);
        SetRawSourceVolume(caveSource, Mathf.Clamp01(currentCave) * masterVolume);
        SetRawSourceVolume(underwaterSource, Mathf.Clamp01(currentUnderwater) * masterVolume);
    }

    private void SetRawSourceVolume(AudioSource src, float vol)
    {
        if (src == null) return;
        src.volume = Mathf.Clamp01(vol);
    }

    private void ValidateSource(AudioSource src, string label)
    {
        if (src == null)
        {
            Debug.LogWarning($"[AmbientZoneBlender] {label} source is missing.");
            return;
        }

        src.loop = true;
        src.playOnAwake = false;
        // Recommended for global ambience:
        // src.spatialBlend = 0f;
    }

    private void ForcePlayLoop(AudioSource src)
    {
        if (src == null || src.clip == null) return;
        src.loop = true;
        if (!src.isPlaying) src.Play();
    }

    private void KeepSourceAlive(AudioSource src)
    {
        if (!autoRestartSources || src == null) return;
        if (!src.enabled) src.enabled = true;
        if (src.clip == null) return;
        if (!src.isPlaying) src.Play();
    }

    private bool IsInCave() => caveTriggerCount > 0;

    public void EnterCave()
    {
        caveTriggerCount++;
        if (showDebugLogs) Debug.Log($"[AmbientZoneBlender] EnterCave -> {caveTriggerCount}");
    }

    public void ExitCave()
    {
        caveTriggerCount = Mathf.Max(0, caveTriggerCount - 1);
        if (showDebugLogs) Debug.Log($"[AmbientZoneBlender] ExitCave -> {caveTriggerCount}");
    }

    public void ForceExitAllCaves()
    {
        caveTriggerCount = 0;
        if (showDebugLogs) Debug.Log("[AmbientZoneBlender] ForceExitAllCaves()");
    }

    // ---- Pause/Unpause requested ----
    public void PauseAmbience()
    {
        if (isPaused) return;
        isPaused = true;

        if (beachSource != null) beachSource.Pause();
        if (windSource != null) windSource.Pause();
        if (caveSource != null) caveSource.Pause();
        if (underwaterSource != null) underwaterSource.Pause();

        if (showDebugLogs) Debug.Log("[AmbientZoneBlender] Ambience paused.");
    }

    public void UnpauseAmbience()
    {
        if (!isPaused) return;
        isPaused = false;

        UnpauseOrRestart(beachSource);
        UnpauseOrRestart(windSource);
        UnpauseOrRestart(caveSource);
        UnpauseOrRestart(underwaterSource);

        ApplyVolumes();

        if (showDebugLogs) Debug.Log("[AmbientZoneBlender] Ambience unpaused.");
    }

    public void SetPaused(bool paused)
    {
        if (paused) PauseAmbience();
        else UnpauseAmbience();
    }

    private void UnpauseOrRestart(AudioSource src)
    {
        if (src == null || src.clip == null) return;
        src.UnPause();
        if (!src.isPlaying) src.Play();
    }

    private void TryFindPlayerIfMissing(bool force = false)
    {
        if (!force && player != null) return;

        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;
    }

    private void TryResolveUnderwaterCheckTransform(bool force = false)
    {
        if (!force && underwaterCheckTransform != null) return;

        // Prefer player camera child if possible
        if (player != null)
        {
            Camera camInChildren = player.GetComponentInChildren<Camera>(true);
            if (camInChildren != null)
            {
                underwaterCheckTransform = camInChildren.transform;
                return;
            }
        }

        if (fallbackToMainCamera && Camera.main != null)
        {
            underwaterCheckTransform = Camera.main.transform;
        }
    }
}
