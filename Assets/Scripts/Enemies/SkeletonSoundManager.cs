using JetBrains.Annotations;
using UnityEngine;

public class SkeletonSoundManager : MonoBehaviour
{
    public AudioSource swordAudioSource;
    public AudioSource headAudioSource;

    public AudioClip[] swingClips;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySwingSound()
    {
        int rand = Random.Range(0, swingClips.Length);
        swordAudioSource.PlayOneShot(swingClips[rand]);
    }
}
