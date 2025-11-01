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
    public void PlayShootSound()
    {
        PlaySound(shootSound);
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

    public void PlayClickSound()
    {
        PlaySound(clickSound);
    }

    public void PlayEquipSound()
    {
        PlaySound(equipSound);
    }
}
