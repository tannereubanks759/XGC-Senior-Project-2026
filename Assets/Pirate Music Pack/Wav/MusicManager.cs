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

    private MusicState currentState;
    private AudioClip lastClip;

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
        SetState(MusicState.Exploration, true);
    }

    public void SetState(MusicState newState, bool instant = false)
    {
        if (newState == currentState && activeSource.isPlaying)
            return;

        currentState = newState;

        AudioClip nextClip = GetRandomClipForState(newState);
        if (nextClip == null)
            return;

        if (instant)
        {
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
        float startVolume = activeSource.volume;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

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
}