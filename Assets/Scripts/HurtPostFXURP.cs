using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HurtPostFXURP : MonoBehaviour
{
    [Header("Optional: assign an existing Global Volume; if null we'll spawn one at runtime")]
    public Volume volume;

    [Header("Vignette")]
    [Range(0f, 1f)] public float vignetteMax = 0.6f;
    public float riseTime = 0.05f;           // ease in
    public float holdTime = 0.06f;           // keep at peak briefly for visibility
    public float fallTime = 0.25f;           // ease out
    [Range(0f, 1f)] public float minFloor = 0.15f; // visible even for tiny hits

    [Header("Extras")]
    [Range(0f, 100f)] public float desaturateMax = 35f; // maps to -35..0
    [Range(0f, 1f)] public float chromaMax = 0.4f;
    public bool driveDesat = true;
    public bool driveChroma = true;

    [Header("Debug")]
    public bool debugLogs = true;
    public KeyCode testKey = KeyCode.H;

    // URP components
    Vignette vig;
    ColorAdjustments colorAdj;
    ChromaticAberration chroma;

    // state
    Coroutine pulseCo;
    float lastPulseTime = -999f; // debounce
    const float debounce = 0.03f;

    void Awake()
    {
        // Ensure we have our own global volume with its own runtime profile
        if (!volume)
        {
            var go = new GameObject("HurtFX_Volume (Runtime)");
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10000f; // win blends
            volume.weight = 1f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }
        else if (!volume.profile)
        {
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        // Bind or create overrides (overrideState=true)
        EnsureOverride(ref vig, volume.profile);
        EnsureOverride(ref colorAdj, volume.profile);
        EnsureOverride(ref chroma, volume.profile);

        // Start values
        if (vig) { vig.intensity.overrideState = true; vig.intensity.value = 0f; }
        if (colorAdj) { colorAdj.saturation.overrideState = true; colorAdj.saturation.value = 0f; }
        if (chroma) { chroma.intensity.overrideState = true; chroma.intensity.value = 0f; }

        if (debugLogs)
            Debug.Log($"[HurtPostFXURP] Ready. Volume={volume.name} priority={volume.priority} global={volume.isGlobal}");
    }

    void EnsureOverride<T>(ref T field, VolumeProfile profile) where T : VolumeComponent, new()
    {
        if (!profile.TryGet(out field))
            field = profile.Add<T>(true);
    }

    

    /// Call with a FLOAT: Pulse(damage / (float)maxHealth)
    public void Pulse(float severity01)
    {
        // Debounce (keep if you had it)
        // if (Time.unscaledTime - lastPulseTime < 0.03f) return;
        // lastPulseTime = Time.unscaledTime;

        severity01 = Mathf.Clamp01(severity01);

        // >>> New mapping: absolute floor + perceptual curve
        float vignetteMin = 0.20f;         // absolute, always-visible minimum
        float vCurve = Mathf.Pow(severity01, 0.5f);  // sqrt curve ? boosts small hits
        float vigPeak = Mathf.Max(vignetteMin, vignetteMax * vCurve);

        float desatMin = -8f;              // absolute min desaturation (negative = desaturate)
        float satPeak = Mathf.Min(desatMin, -desaturateMax * vCurve);

        float chromaMin = 0.06f;           // absolute min chroma
        float chrPeak = Mathf.Max(chromaMin, chromaMax * vCurve);

        if (debugLogs) Debug.Log($"[HurtPostFXURP] sev={severity01:F2} -> vigPeak={vigPeak:F3} satPeak={satPeak:F1} chrPeak={chrPeak:F2}");

        if (pulseCo != null) StopCoroutine(pulseCo);
        pulseCo = StartCoroutine(PulseRoutine(vigPeak, satPeak, chrPeak));
    }


    // One-button sanity check that bypasses easing logic
    public void ForceFlash(float severity01)
    {
        severity01 = Mathf.Clamp01(severity01);
        float peak = Mathf.Max(vignetteMax * severity01, 0.25f);
        if (vig) vig.intensity.value = peak;
        if (driveDesat && colorAdj) colorAdj.saturation.value = Mathf.Min(-desaturateMax * severity01, -10f * severity01);
        if (driveChroma && chroma) chroma.intensity.value = Mathf.Max(chromaMax * Mathf.Pow(severity01, 0.6f), 0.05f * severity01);
        // Auto clear after 0.2s realtime, regardless of timescale
        if (pulseCo != null) StopCoroutine(pulseCo);
        pulseCo = StartCoroutine(ClearAfterRealtime(0.2f));
    }

    IEnumerator ClearAfterRealtime(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) yield return null;
        if (vig) vig.intensity.value = 0f;
        if (colorAdj) colorAdj.saturation.value = 0f;
        if (chroma) chroma.intensity.value = 0f;
        pulseCo = null;
    }

    IEnumerator PulseRoutine(float vigPeak, float satPeak, float chrPeak)
    {
        float startV = vig ? vig.intensity.value : 0f;
        float startS = colorAdj ? colorAdj.saturation.value : 0f;
        float startC = chroma ? chroma.intensity.value : 0f;

        // rise (unscaled time)
        float t = 0f;
        while (t < riseTime)
        {
            t += Time.unscaledDeltaTime;
            float u = Smooth01(t / Mathf.Max(0.0001f, riseTime));
            if (vig) vig.intensity.value = Mathf.Lerp(startV, vigPeak, u);
            if (driveDesat && colorAdj) colorAdj.saturation.value = Mathf.Lerp(startS, satPeak, u);
            if (driveChroma && chroma) chroma.intensity.value = Mathf.Lerp(startC, chrPeak, u);
            yield return null;
        }

        // hold
        float holdEnd = Time.unscaledTime + holdTime;
        while (Time.unscaledTime < holdEnd)
        {
            if (vig) vig.intensity.value = vigPeak;
            if (driveDesat && colorAdj) colorAdj.saturation.value = satPeak;
            if (driveChroma && chroma) chroma.intensity.value = chrPeak;
            yield return null;
        }

        // fall
        t = 0f;
        while (t < fallTime)
        {
            t += Time.unscaledDeltaTime;
            float u = Smooth01(t / Mathf.Max(0.0001f, fallTime));
            if (vig) vig.intensity.value = Mathf.Lerp(vigPeak, 0f, u);
            if (driveDesat && colorAdj) colorAdj.saturation.value = Mathf.Lerp(satPeak, 0f, u);
            if (driveChroma && chroma) chroma.intensity.value = Mathf.Lerp(chrPeak, 0f, u);
            yield return null;
        }

        if (vig) vig.intensity.value = 0f;
        if (colorAdj) colorAdj.saturation.value = 0f;
        if (chroma) chroma.intensity.value = 0f;
        pulseCo = null;
    }

    static float Smooth01(float x) => x * x * (3f - 2f * x);
}
