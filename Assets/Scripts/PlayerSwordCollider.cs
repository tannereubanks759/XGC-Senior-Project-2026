using UnityEngine;

public class PlayerSwordCollider : MonoBehaviour
{

    public Collider col;
    private Animator anim;
    public AudioSource source;
    public AudioClip lightningSwingClip;
    public swordDamageDeterminer swordDamage;

    private void Start()
    {
        anim = this.GetComponent<Animator>();
    }
    public void DisableStaggered()
    {
        anim.SetBool("Staggered", false);
    }

    public void EnableSwordCollider()
    {
        col.enabled = true;
        if (swordDamage != null && swordDamage.isLighting && lightningSwingClip != null)
        {
            source.pitch = -1.6f;
            source.PlayOneShot(lightningSwingClip, 0.5f);
            source.pitch = 1f;
        }

    }
    public void DisableSwordCollider()
    {
        col.enabled = false;
    }
}
