using UnityEngine;

public class GunSounds : MonoBehaviour
{
    [Header("Sounds")]
    public AudioSource gunSource;
    public AudioClip shootSound;
    public AudioClip packGunpowederSound;
    public AudioClip AimSound;
    public AudioClip clickSound;
    public AudioClip equipSound;
    public AudioClip collectAmmoSound;

    public float GunShotVolume = 1f;
    public void PlayShootSound()
    {
        PlaySound(shootSound, GunShotVolume);
    }

    public void PlayPackSound()
    {
        PlaySound(packGunpowederSound);
    }

    public void PlayAimSound()
    {
        PlaySound(AimSound);
    }

    void PlaySound(AudioClip clip)
    {
        gunSource.PlayOneShot(clip);
    }
    void PlaySound(AudioClip clip, float v)
    {
        gunSource.PlayOneShot(clip, v);
    }

    public void PlayClickSound()
    {
        PlaySound(clickSound);
    }

    public void PlayEquipSound()
    {
        PlaySound(equipSound);
    }

    public void PlayCollectAmmoSound()
    {
        PlaySound(collectAmmoSound);
    }
}
