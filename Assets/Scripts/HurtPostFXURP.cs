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
    [Range(0f, 1f)] public float lowHealthBaseVignetteMax = 0.35f; // max base vignette at 0 health
    public float riseTime = 0.05f;
    public float holdTime = 0.06f;
    public float fallTime = 0.25f;

    [Header("Extras")]
    [Range(0f, 100f)] public float desaturateMax = 35f;
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
    private CombatController controller;

    void Awake()
    {
        controller = GetComponent<CombatController>();

        if (!volume)
        {
            var go = new GameObject("HurtFX_Volume (Runtime)");
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10000f;
            volume.weight = 1f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }
        else if (!volume.profile)
        {
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        EnsureOverride(ref vig, volume.profile);
        EnsureOverride(ref colorAdj, volume.profile);
        EnsureOverride(ref chroma, volume.profile);

        if (vig) { vig.intensity.overrideState = true; vig.intensity.value = 0f; }
        if (colorAdj) { colorAdj.saturation.overrideState = true; colorAdj.saturation.value = 0f; }
        if (chroma) { chroma.intensity.overrideState = true; chroma.intensity.value = 0f; }

        if (debugLogs)
            Debug.Log($"[HurtPostFXURP] Ready. Volume={volume.name} priority={volume.priority} global={volume.isGlobal}");
    }

    void Update()
    {
        // Keep the base vignette updated when not pulsing
        if (pulseCo == null && vig)
        {
            vig.intensity.value = GetBaseVignette();
        }
    }

    void EnsureOverride<T>(ref T field, VolumeProfile profile) where T : VolumeComponent, new()
    {
        if (!profile.TryGet(out field))
            field = profile.Add<T>(true);
    }

    float GetBaseVignette()
    {
        if (controller == null || controller.maxHealth <= 0f)
            return 0f;

        float healthPercent = controller.health / controller.maxHealth;

        // 0 vignette from 50% to 100% health
        if (healthPercent >= 0.5f)
            return 0f;

        // Scale from 0 at 50% health to max at 0% health
        float t = 1f - (healthPercent / 0.5f);
        return lowHealthBaseVignetteMax * t;
    }

    public void Pulse(float severity01)
    {
        severity01 = Mathf.Clamp01(severity01);

        float baseVignette = GetBaseVignette();

        float vignetteMin = 0.20f;
        float vCurve = Mathf.Pow(severity01, 0.5f);
        float vigPeak = Mathf.Max(vignetteMin, vignetteMax * vCurve);

        // Make pulse add on top of base vignette
        vigPeak = Mathf.Clamp(baseVignette + vigPeak, 0f, 1f);

        float desatMin = -8f;
        float satPeak = Mathf.Min(desatMin, -desaturateMax * vCurve);

        float chromaMin = 0.06f;
        float chrPeak = Mathf.Max(chromaMin, chromaMax * vCurve);

        if (debugLogs)
            Debug.Log($"[HurtPostFXURP] sev={severity01:F2} -> vigPeak={vigPeak:F3}");

        if (pulseCo != null) StopCoroutine(pulseCo);
        pulseCo = StartCoroutine(PulseRoutine(vigPeak, satPeak, chrPeak));
    }

    public void ForceFlash(float severity01)
    {
        severity01 = Mathf.Clamp01(severity01);

        float baseVignette = GetBaseVignette();
        float peak = Mathf.Max(vignetteMax * severity01, 0.25f);
        peak = Mathf.Clamp(baseVignette + peak, 0f, 1f);

        if (vig) vig.intensity.value = peak;
        if (driveDesat && colorAdj) colorAdj.saturation.value = Mathf.Min(-desaturateMax * severity01, -10f * severity01);
        if (driveChroma && chroma) chroma.intensity.value = Mathf.Max(chromaMax * Mathf.Pow(severity01, 0.6f), 0.05f * severity01);

        if (pulseCo != null) StopCoroutine(pulseCo);
        pulseCo = StartCoroutine(ClearAfterRealtime(0.2f));
    }

    IEnumerator ClearAfterRealtime(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) yield return null;

        float baseVignette = GetBaseVignette();

        if (vig) vig.intensity.value = baseVignette;
        if (colorAdj) colorAdj.saturation.value = 0f;
        if (chroma) chroma.intensity.value = 0f;

        pulseCo = null;
    }

    IEnumerator PulseRoutine(float vigPeak, float satPeak, float chrPeak)
    {
        float baseVignette = GetBaseVignette();

        float startV = vig ? vig.intensity.value : baseVignette;
        float startS = colorAdj ? colorAdj.saturation.value : 0f;
        float startC = chroma ? chroma.intensity.value : 0f;

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

        float holdEnd = Time.unscaledTime + holdTime;
        while (Time.unscaledTime < holdEnd)
        {
            if (vig) vig.intensity.value = vigPeak;
            if (driveDesat && colorAdj) colorAdj.saturation.value = satPeak;
            if (driveChroma && chroma) chroma.intensity.value = chrPeak;

            yield return null;
        }

        t = 0f;
        while (t < fallTime)
        {
            t += Time.unscaledDeltaTime;
            float u = Smooth01(t / Mathf.Max(0.0001f, fallTime));

            if (vig) vig.intensity.value = Mathf.Lerp(vigPeak, baseVignette, u);
            if (driveDesat && colorAdj) colorAdj.saturation.value = Mathf.Lerp(satPeak, 0f, u);
            if (driveChroma && chroma) chroma.intensity.value = Mathf.Lerp(chrPeak, 0f, u);

            yield return null;
        }

        if (vig) vig.intensity.value = baseVignette;
        if (colorAdj) colorAdj.saturation.value = 0f;
        if (chroma) chroma.intensity.value = 0f;

        pulseCo = null;
    }

    static float Smooth01(float x) => x * x * (3f - 2f * x);
}