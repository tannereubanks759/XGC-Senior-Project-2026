using UnityEngine;
using System.Collections;
using UnityEngine.Serialization; 

[AddComponentMenu("Audio/Ambience Crossfader")]
[RequireComponent(typeof(AudioSource))]
public class Ambience : MonoBehaviour
{
    [Header("Clips")]
    [FormerlySerializedAs("AmbienceClips")] 
    public AudioClip[] ambienceClips;

    [Header("Crossfade")]
    [Min(0f)] public float crossfadeDuration = 3f;
    [Min(0f)] public float leadInBeforeEnd = 3f;
    public bool equalPowerFade = true;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    [Header("Behavior")]
    [Tooltip("If true, will disable itself only after logging when no clips are assigned.")]
    public bool disableIfNoClips = false;

    // --- Add near the top of the class ---
    [Header("Pause")]
    [Tooltip("Toggle this from your Pause script. True = pause ambience.")]
    public bool paused = false;

    private bool wasPaused = false;


    private AudioSource a, b;
    private AudioSource active, idle;
    private int currentClipIndex = -1;
    private bool crossfading;

    void Awake()
    {
        a = GetComponent<AudioSource>();
        a.playOnAwake = false; a.loop = false;
        b = gameObject.AddComponent<AudioSource>();
        b.playOnAwake = false; b.loop = false;

        a.volume = 0f; b.volume = 0f;
        active = a; idle = b;
    }

    void OnEnable()
    {
        TryStartIfReady();
    }

    void Start()
    {
        TryStartIfReady();
    }

    void TryStartIfReady()
    {
        if (paused) return;

        if (ambienceClips == null || ambienceClips.Length == 0)
        {
            Debug.LogWarning("[Ambience] No clips assigned. Waiting for clips...");
            if (disableIfNoClips) enabled = false; // optional
            return;
        }

        if (active.isPlaying) return;

        currentClipIndex = PickNextIndex(-1);
        active.clip = ambienceClips[currentClipIndex];
        active.outputAudioMixerGroup = a.outputAudioMixerGroup; // keep routing consistent
        active.pitch = a.pitch;
        active.volume = masterVolume;
        active.Play();
    }

    void Update()
    {
        if ((ambienceClips == null || ambienceClips.Length == 0))
            return;

        // Handle pause state changes
        if (paused && !wasPaused)
        {
            if (active != null && active.isPlaying) active.Pause();
            if (idle != null && idle.isPlaying) idle.Pause();
            wasPaused = true;
            return; // don't advance timers while paused
        }
        else if (!paused && wasPaused)
        {
            if (active != null) active.UnPause();
            if (idle != null) idle.UnPause();
            wasPaused = false;
            // continue with normal update below
        }

        if (active.clip == null || !active.isPlaying)
        {
            if (!crossfading)
                StartCoroutine(CrossfadeToNext());
            return;
        }

        float remaining = active.clip.length - active.time;
        float trigger = Mathf.Max(0.01f, leadInBeforeEnd);

        if (!crossfading && remaining <= trigger)
            StartCoroutine(CrossfadeToNext());

        active.volume = Mathf.Min(active.volume, masterVolume);
        idle.volume = Mathf.Min(idle.volume, masterVolume);
    }

    private IEnumerator CrossfadeToNext()
    {
        crossfading = true;

        int nextIndex = PickNextIndex(currentClipIndex);
        idle.clip = ambienceClips[nextIndex];
        idle.time = 0f;
        idle.volume = 0f;
        idle.pitch = active.pitch;
        idle.outputAudioMixerGroup = active.outputAudioMixerGroup;
        idle.Play();

        float dur = Mathf.Max(0f, crossfadeDuration);
        float t = 0f;

        if (dur <= Mathf.Epsilon)
        {
            active.Stop();
            idle.volume = masterVolume;
        }
        else
        {
            while (t < dur)
            {
                if (paused)
                {
                    yield return null;
                    continue;
                }

                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);

                if (equalPowerFade)
                {
                    float inGain = Mathf.Sin(u * Mathf.PI * 0.5f);
                    float outGain = Mathf.Cos(u * Mathf.PI * 0.5f);
                    idle.volume = inGain * masterVolume;
                    active.volume = outGain * masterVolume;
                }
                else
                {
                    idle.volume = u * masterVolume;
                    active.volume = (1f - u) * masterVolume;
                }

                yield return null;
            }

            idle.volume = masterVolume;
            active.volume = 0f;
            active.Stop();
        }

        var tmp = active; active = idle; idle = tmp;
        currentClipIndex = nextIndex;
        crossfading = false;
    }

    private int PickNextIndex(int last)
    {
        if (ambienceClips == null || ambienceClips.Length == 0) return -1;
        if (ambienceClips.Length == 1) return 0;

        int idx;
        do { idx = Random.Range(0, ambienceClips.Length); }
        while (idx == last);
        return idx;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (Mathf.Abs(leadInBeforeEnd - crossfadeDuration) < 0.0001f)
                leadInBeforeEnd = crossfadeDuration;
        }
        masterVolume = Mathf.Clamp01(masterVolume);
    }
#endif
}
