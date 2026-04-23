using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    public enum MusicState
    {
        Exploration,
        Combat,
        Boss
    }

    [Header("Tracks")]
    public AudioClip[] explorationTracks;
    public AudioClip[] combatTracks;
    public AudioClip[] bossTracks;

    [Header("Settings")]
    public float fadeDuration = 2f;
    public float musicVolume = 1f;
    public AudioMixerGroup audioMixer;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;
    private AudioSource inactiveSource;

    private MusicState currentState = MusicState.Exploration;
    private MusicState requestedState = MusicState.Exploration;

    private AudioClip lastClip;
    private bool bossOverrideActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();

        SetupSource(sourceA);
        SetupSource(sourceB);

        activeSource = sourceA;
        inactiveSource = sourceB;
    }

    private void SetupSource(AudioSource src)
    {
        src.outputAudioMixerGroup = audioMixer;
        src.loop = true;
        src.playOnAwake = false;
        src.volume = 0f;
    }

    private void Start()
    {
        ApplyState(MusicState.Exploration, true);
    }

    public void SetState(MusicState newState, bool instant = false)
    {
        requestedState = newState;

        if (bossOverrideActive && newState != MusicState.Boss)
            return;

        ApplyState(newState, instant);
    }

    public void EnterBossMusic(bool instant = false)
    {
        bossOverrideActive = true;
        ApplyState(MusicState.Boss, instant);
    }

    public void ExitBossMusic(bool instant = false)
    {
        bossOverrideActive = false;
        ApplyState(requestedState, instant);
    }

    private void ApplyState(MusicState newState, bool instant = false)
    {
        if (newState == currentState && activeSource.isPlaying)
            return;

        AudioClip nextClip = GetRandomClipForState(newState);
        if (nextClip == null)
            return;

        currentState = newState;

        if (instant)
        {
            StopAllCoroutines();

            activeSource.Stop();
            inactiveSource.Stop();

            activeSource.clip = nextClip;
            activeSource.volume = musicVolume;
            activeSource.Play();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(CrossfadeTo(nextClip));
    }

    private AudioClip GetRandomClipForState(MusicState state)
    {
        AudioClip[] pool = null;

        switch (state)
        {
            case MusicState.Exploration:
                pool = explorationTracks;
                break;
            case MusicState.Combat:
                pool = combatTracks;
                break;
            case MusicState.Boss:
                pool = bossTracks;
                break;
        }

        if (pool == null || pool.Length == 0)
            return null;

        if (pool.Length == 1)
        {
            lastClip = pool[0];
            return pool[0];
        }

        AudioClip chosen = null;
        int safety = 20;

        while (safety-- > 0)
        {
            chosen = pool[Random.Range(0, pool.Length)];
            if (chosen != lastClip)
                break;
        }

        lastClip = chosen;
        return chosen;
    }

    private IEnumerator CrossfadeTo(AudioClip nextClip)
    {
        inactiveSource.clip = nextClip;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        float time = 0f;
        float startVolume = activeSource.isPlaying ? activeSource.volume : 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = fadeDuration > 0f ? time / fadeDuration : 1f;

            activeSource.volume = Mathf.Lerp(startVolume, 0f, t);
            inactiveSource.volume = Mathf.Lerp(0f, musicVolume, t);

            yield return null;
        }

        activeSource.Stop();
        activeSource.volume = 0f;
        inactiveSource.volume = musicVolume;

        AudioSource temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;
    }

    public bool IsBossMusicActive()
    {
        return bossOverrideActive;
    }
}