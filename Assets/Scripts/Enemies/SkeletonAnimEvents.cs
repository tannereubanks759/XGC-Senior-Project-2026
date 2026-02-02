using UnityEngine;

public class SkeletonAnimEvents : MonoBehaviour
{
    public Collider swordCol;
    private SkeletonSoundManager SM;
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
