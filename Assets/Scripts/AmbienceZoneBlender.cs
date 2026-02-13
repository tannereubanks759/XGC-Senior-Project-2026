using UnityEngine;

/// <summary>
/// Ambience system that blends between Beach, Wind, and Cave ambience.
/// Priority: Cave overrides everything while inside cave trigger.
/// Otherwise blends Beach <-> Wind based on player Y relative to water level.
/// </summary>
[DisallowMultipleComponent]
public class AmbientZoneBlender : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Player transform used to read world position.")]
    public Transform player;

    [Header("Audio Sources (looping)")]
    [Tooltip("Beach ambience AudioSource (set clip + loop=true).")]
    public AudioSource beachSource;

    [Tooltip("Wind ambience AudioSource (set clip + loop=true).")]
    public AudioSource windSource;

    [Tooltip("Cave ambience AudioSource (set clip + loop=true).")]
    public AudioSource caveSource;

    [Header("Water / Height Settings")]
    [Tooltip("World-space water line Y (e.g. ocean surface at y=0).")]
    public float waterLevelY = 0f;

    [Tooltip("Below this height, Beach can dominate. Above this, Wind begins blending in.")]
    public float windStartY = 8f;

    [Tooltip("At/above this height, Wind reaches full intensity (unless in cave).")]
    public float windFullY = 25f;

    [Tooltip("How far above/below water line still counts as 'near water' for beach presence.")]
    public float beachNearWaterRange = 12f;

    [Header("Transitions")]
    [Tooltip("How quickly source volumes move toward targets. Higher = faster.")]
    [Range(0.1f, 10f)]
    public float blendSpeed = 2.5f;

    [Tooltip("Global ambience multiplier for all 3 tracks.")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Header("Debug")]
    public bool showDebug = false;

    // Cave trigger bookkeeping (supports overlapping cave triggers)
    private int caveTriggerCount = 0;

    // Internal current volumes (0..1), multiplied by masterVolume on output
    private float currentBeach;
    private float currentWind;
    private float currentCave;

    private void Reset()
    {
        // Try auto-fill player to this object if useful
        if (player == null) player = transform;
    }

    private void Awake()
    {
        ValidateSources();

        // Start all sources muted and playing for seamless crossfade
        currentBeach = 0f;
        currentWind = 0f;
        currentCave = 0f;

        ForcePlayLoop(beachSource);
        ForcePlayLoop(windSource);
        ForcePlayLoop(caveSource);

        ApplyVolumesImmediate();
    }

    private void Update()
    {
        if (player == null)
        {
            if (showDebug) Debug.LogWarning("[AmbientZoneBlender] Player reference missing.");
            return;
        }

        // 1) Compute target weights
        float targetBeach, targetWind, targetCave;
        ComputeTargetWeights(out targetBeach, out targetWind, out targetCave);

        // 2) Smooth toward targets
        float t = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime); // framerate-independent smoothing
        currentBeach = Mathf.Lerp(currentBeach, targetBeach, t);
        currentWind = Mathf.Lerp(currentWind, targetWind, t);
        currentCave = Mathf.Lerp(currentCave, targetCave, t);

        KeepSourceAlive(beachSource);
        KeepSourceAlive(windSource);
        KeepSourceAlive(caveSource);


        // 3) Apply to AudioSources
        ApplyVolumes();

        if (showDebug)
        {
            Debug.Log(
                $"[AmbientZoneBlender] y={player.position.y:F2} cave={IsInCave()} | " +
                $"Beach={currentBeach:F2} Wind={currentWind:F2} Cave={currentCave:F2}");
        }
    }
    private void KeepSourceAlive(AudioSource src)
    {
        if (src == null) return;
        if (!src.enabled) src.enabled = true;
        if (src.clip == null) return;

        // If something stopped it, restart.
        if (!src.isPlaying)
            src.Play();
    }

    /// <summary>
    /// Cave has highest priority. If in cave, Cave=1 and others=0.
    /// Else blend Beach/Wind by height and near-water shaping.
    /// </summary>
    private void ComputeTargetWeights(out float beach, out float wind, out float cave)
    {
        bool inCave = IsInCave();
        if (inCave)
        {
            beach = 0f;
            wind = 0f;
            cave = 1f;
            return;
        }

        float y = player.position.y;

        // Wind factor by height
        float windByHeight = 0f;
        if (windFullY > windStartY)
            windByHeight = Mathf.InverseLerp(windStartY, windFullY, y);
        windByHeight = Mathf.Clamp01(windByHeight);

        // Beach factor by proximity to water level (1 near water, fades with vertical distance)
        float distToWater = Mathf.Abs(y - waterLevelY);
        float beachNearWater = 1f - Mathf.Clamp01(distToWater / Mathf.Max(0.001f, beachNearWaterRange));

        // Combine so beach is strong near water and weak at high elevations
        // As wind rises, beach gets suppressed.
        beach = beachNearWater * (1f - windByHeight);
        wind = windByHeight;

        // Normalize Beach/Wind pair so sum <= 1, then cave fills none (0 outside cave)
        float sum = beach + wind;
        if (sum > 1f)
        {
            beach /= sum;
            wind /= sum;
        }

        cave = 0f;
    }

    private void ApplyVolumes()
    {
        if (beachSource != null) beachSource.volume = Mathf.Clamp01(currentBeach) * masterVolume;
        if (windSource != null) windSource.volume = Mathf.Clamp01(currentWind) * masterVolume;
        if (caveSource != null) caveSource.volume = Mathf.Clamp01(currentCave) * masterVolume;
    }

    private void ApplyVolumesImmediate()
    {
        if (beachSource != null) beachSource.volume = 0f;
        if (windSource != null) windSource.volume = 0f;
        if (caveSource != null) caveSource.volume = 0f;
    }

    private void ValidateSources()
    {
        // Optional warnings to help setup
        if (beachSource == null) Debug.LogWarning("[AmbientZoneBlender] Beach source missing.");
        if (windSource == null) Debug.LogWarning("[AmbientZoneBlender] Wind source missing.");
        if (caveSource == null) Debug.LogWarning("[AmbientZoneBlender] Cave source missing.");
    }

    private void ForcePlayLoop(AudioSource src)
    {
        if (src == null) return;
        src.loop = true;
        if (!src.isPlaying) src.Play();
    }

    private bool IsInCave() => caveTriggerCount > 0;

    /// <summary>
    /// Called by cave trigger script when player enters.
    /// </summary>
    public void EnterCave()
    {
        caveTriggerCount++;
        if (showDebug) Debug.Log($"[AmbientZoneBlender] EnterCave -> count={caveTriggerCount}");
    }

    /// <summary>
    /// Called by cave trigger script when player exits.
    /// </summary>
    public void ExitCave()
    {
        caveTriggerCount = Mathf.Max(0, caveTriggerCount - 1);
        if (showDebug) Debug.Log($"[AmbientZoneBlender] ExitCave -> count={caveTriggerCount}");
    }
}
