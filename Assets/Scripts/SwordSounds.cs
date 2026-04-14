using UnityEngine;

public class SwordSounds : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    [Header("Sounds")]
    public AudioSource swordSource;
    public AudioClip[] swordSwings;
    public AudioClip[] swordClash;
    public AudioClip equipSound;
    public AudioClip pushSound;
    public AudioClip blockSound;

    private float nextTime = 0;
    public void PlaySwing()
    {
        if(swordSwings.Length > 0)
        {
            int random = Random.Range(0, swordSwings.Length);
            swordSource.PlayOneShot(swordSwings[random]);
        }
        
    }
    private void Start()
    {
        nextTime = Time.time + 2;
    }
    public void OnEnable()
    {
        if(Time.time > nextTime)
        {
            PlayEquip();
        }
    }
    public void PlayEquip()
    {
        swordSource.PlayOneShot(equipSound);
    }
    public void PlayPushSound()
    {
        swordSource.PlayOneShot(pushSound);
    }
    public void PlayBlockSound()
    {
        swordSource.PlayOneShot(blockSound);
    }
    public void PlayClashSound()
    {
        if (swordClash.Length > 0)
        {
            int random = Random.Range(0, swordClash.Length);
            swordSource.PlayOneShot(swordClash[random]);
        }
    }
}
