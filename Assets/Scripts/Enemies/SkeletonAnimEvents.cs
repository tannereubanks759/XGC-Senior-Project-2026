using UnityEngine;

public class SkeletonAnimEvents : MonoBehaviour
{
    public Collider swordCol;
    private SkeletonSoundManager SM;
    public AudioSource gunSource;
    public AudioClip aimSound;
    public AudioClip shootSound;
    private void Start()
    {
        SM = GetComponentInParent<SkeletonSoundManager>();
    }
    public void EnableSwordCollider()
    {
        if(swordCol != null)
        {
            swordCol.enabled = true;
        }
        
    }
    public void PlayAimSound()
    {
        if (gunSource && aimSound)
        {
            gunSource.PlayOneShot(aimSound);
        }
    }
    public void PlayShootSound()
    {
        if (gunSource && shootSound)
        {
            gunSource.PlayOneShot(shootSound);
        }
    }
    public void PlaySwordSound()
    {
        SM.PlaySwingSound();
    }
    public void DisableSwordCollider()
    {
        if (swordCol != null)
        {
            swordCol.enabled = false;
        }
    }
}
